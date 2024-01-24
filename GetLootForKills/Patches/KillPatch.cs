using BepInEx;
using GameNetcodeStuff;
using GetLootForKills.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace GetLootForKills.Patches
{
    internal class KillPatch
    {
        public static void PatchKillEnemyOnOwnerClient(ref EnemyAI __instance, bool overrideDestroy = false)
        {
            if (__instance.gameObject.GetComponent<DroppedItemEnemy>() == null && __instance.IsOwner)
            {
                if (__instance is HoarderBugAI)
                {
                    HoarderBugAI bug = ((HoarderBugAI)__instance);
                    if (bug.heldItem != null)
                    {
                        GrabbableObject held = bug.heldItem.itemGrabbableObject;
                        bug.DropItemServerRpc(held.GetComponent<NetworkObject>(), __instance.transform.position, false);
                    }
                }
                string mobName = Plugin.RemoveInvalidCharacters(__instance.enemyType.enemyName.ToUpper());
                Vector3 position = __instance.transform.position + Vector3.up * 0.6f;
                List<ItemToDrop> items = GetItemsForMob(mobName);
                if (items.Any())
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i].scrapItem.spawnPrefab != null)
                        {
                            DropItem(__instance, position, items[i].scrapItem.spawnPrefab, items[i].scrapValue);
                        }
                    }
                }
                __instance.gameObject.AddComponent<DroppedItemEnemy>();
            }
        }
        static void DropItem(EnemyAI enemy, Vector3 position, GameObject itemPrefab, int scrapValue)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.IsHost)
            {
                DropItemHost(player, position, itemPrefab, scrapValue);
            }
            else if (player.IsClient && !player.IsHost)
            {
                DropItemServerRpc(player, position, itemPrefab, scrapValue);
            }
        }

        [ServerRpc]
        static void DropItemServerRpc(PlayerControllerB player, Vector3 position, GameObject itemPrefab, int scrapValue)
        {
            DropItemHost(player, position, itemPrefab, scrapValue);
        }

        static void DropItemHost(PlayerControllerB player, Vector3 position, GameObject itemPrefab, int scrapValue)
        {
            RoundManager instance = RoundManager.Instance;
            Transform scrapContainer = instance.spawnedScrapContainer;
            position += new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
            GameObject obj = UnityEngine.Object.Instantiate(itemPrefab, position, Quaternion.identity, scrapContainer);
            GrabbableObject component = obj.GetComponent<GrabbableObject>();
            component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
            component.fallTime = 0f;
            int valueOfScrap = scrapValue;
            if (instance.scrapValueMultiplier > 1)
            {
                valueOfScrap = (int)(valueOfScrap * instance.scrapValueMultiplier);
            }
            component.itemProperties.isScrap = true;
            component.SetScrapValue(valueOfScrap);
            component.itemProperties.creditsWorth = valueOfScrap;
            instance.totalScrapValueInLevel += valueOfScrap;
            NetworkObject net = obj.GetComponent<NetworkObject>();
            net.Spawn();
            SyncScrapValuesClientRpc(player, new NetworkObjectReference[] { net }, new int[] { valueOfScrap });
        }

        [ClientRpc]
        static void SyncScrapValuesClientRpc(PlayerControllerB player, NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
        {
            Plugin.logger.LogInfo($"clientRPC scrap values length: {allScrapValue.Length}");
            int num = 0;
            for (int i = 0; i < spawnedScrap.Length; i++)
            {
                if (spawnedScrap[i].TryGet(out var networkObject))
                {
                    GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
                    if (component != null)
                    {
                        if (i >= allScrapValue.Length)
                        {
                            Plugin.logger.LogError($"spawnedScrap amount exceeded allScrapValue!: {spawnedScrap.Length}");
                            break;
                        }

                        component.SetScrapValue(allScrapValue[i]);
                        num += allScrapValue[i];
                        if (component.itemProperties.meshVariants.Length != 0)
                        {
                            component.gameObject.GetComponent<MeshFilter>().mesh = component.itemProperties.meshVariants[UnityEngine.Random.Range(0, component.itemProperties.meshVariants.Length)];
                        }

                        try
                        {
                            if (component.itemProperties.materialVariants.Length != 0)
                            {
                                component.gameObject.GetComponent<MeshRenderer>().sharedMaterial = component.itemProperties.materialVariants[UnityEngine.Random.Range(0, component.itemProperties.materialVariants.Length)];
                            }
                        }
                        catch (Exception arg)
                        {
                            Plugin.logger.LogInfo($"Item name: {component.gameObject.name}; {arg}");
                        }
                    }
                    else
                    {
                        Plugin.logger.LogError("Scrap networkobject object did not contain grabbable object!: " + networkObject.gameObject.name);
                    }
                }
                else
                {
                    Plugin.logger.LogError($"Failed to get networkobject reference for scrap. id: {spawnedScrap[i].NetworkObjectId}");
                }
            }

            RoundManager.Instance.totalScrapValueInLevel += num;
            Plugin.logger.LogInfo($"Round Manager Total Scrap Value: {RoundManager.Instance.totalScrapValueInLevel}");
            //scrapCollectedInLevel = 0;
            //valueOfFoundScrapItems = 0;
        }

        static List<ItemToDrop> GetItemsForMob(string mobName)
        {
            List<ItemToDrop> itemsToGive = new List<ItemToDrop>();
            List<string> items = Plugin.GetMobItems(mobName);
            if (items.Any() && items != null)
            {
                for (int i = 0; i < (items.Count / 6); i++)
                {
                    int count = (6 * i);
                    if (!items[count].IsNullOrWhiteSpace())
                    {
                        string name = Plugin.RemoveInvalidCharacters(items[count]);
                        if (name.ToUpper().Equals("NONE"))
                        {
                            Item empty = (Item)ScriptableObject.CreateInstance(typeof(Item));
                            empty.name = "NONE";
                            empty.spawnPrefab = new GameObject("NONE");
                            empty.minValue = 0;
                            empty.maxValue = 0;
                            empty.creditsWorth = 0;
                            itemsToGive.Add(new ItemToDrop(empty, 1));
                            Plugin.logger.LogInfo("Worthless Item Dropped!");
                        }
                        else
                        {
                            if (int.TryParse(Plugin.RemoveInvalidCharacters(items[count + 1]), out int minItemDropAmount))
                            {
                                if (int.TryParse(Plugin.RemoveInvalidCharacters(items[count + 2]), out int maxItemDropAmount))
                                {
                                    if (int.TryParse(Plugin.RemoveInvalidCharacters(items[count + 3]), out int minScrapValue))
                                    {
                                        if (int.TryParse(Plugin.RemoveInvalidCharacters(items[count + 4]), out int maxScrapValue))
                                        {
                                            if (int.TryParse(Plugin.RemoveInvalidCharacters(items[count + 5]), out int dropChance))
                                            {
                                                int rand = UnityEngine.Random.Range(1, 1000);
                                                if (rand < dropChance)
                                                {
                                                    foreach (Item itemToGive in Plugin.items)
                                                    {
                                                        if (name.ToUpper().Equals("RANDOM"))
                                                        {
                                                            int num = UnityEngine.Random.Range(0, RoundManager.Instance.currentLevel.spawnableScrap.Count);
                                                            rand = 0;
                                                            if (minItemDropAmount < maxItemDropAmount)
                                                            {
                                                                rand = UnityEngine.Random.Range(minItemDropAmount, maxItemDropAmount + 1);
                                                            }
                                                            else
                                                            {
                                                                rand = minItemDropAmount;
                                                            }
                                                            if (rand > 0)
                                                            {
                                                                for (int id = 0; id < rand; id++)
                                                                {
                                                                    itemsToGive.Add(new ItemToDrop(RoundManager.Instance.currentLevel.spawnableScrap[num].spawnableItem, UnityEngine.Random.Range(minScrapValue, maxScrapValue)));
                                                                    Plugin.logger.LogInfo("Spawning random item!");
                                                                }
                                                            }
                                                            break;
                                                        }
                                                        else if (Plugin.RemoveInvalidCharacters(itemToGive.itemName.ToUpper()).Equals(Plugin.RemoveInvalidCharacters(name.ToUpper())))
                                                        {
                                                            rand = 0;
                                                            if (minItemDropAmount < maxItemDropAmount)
                                                            {
                                                                rand = UnityEngine.Random.Range(minItemDropAmount, maxItemDropAmount);
                                                            }
                                                            else
                                                            {
                                                                rand = minItemDropAmount;
                                                            }
                                                            if (rand > 0)
                                                            {
                                                                for (int id = 0; id < rand; id++)
                                                                {
                                                                    itemsToGive.Add(new ItemToDrop(itemToGive, UnityEngine.Random.Range(minScrapValue, maxScrapValue)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Plugin.logger.LogInfo("Drop Chance is not a number!");
                                            }
                                        }
                                        else
                                        {
                                            Plugin.logger.LogInfo("Max Scrap Value is not a number!");
                                        }
                                    }
                                    else
                                    {
                                        Plugin.logger.LogInfo("Min Scrap Value is not a number!");
                                    }
                                }
                                else
                                {
                                    Plugin.logger.LogInfo("Max Item Drop is not a number!");
                                }
                            }
                            else
                            {
                                Plugin.logger.LogInfo("Min Item Drop is not a number!");
                            }
                        }
                    }
                    else
                    {
                        Plugin.logger.LogInfo("The Item Name does not match any existing item names!");
                    }
                }
            }
            else
            {
                Plugin.logger.LogInfo("Item name either does not exist or is empty. Backup Item Loaded!");
                int num = UnityEngine.Random.Range(0, RoundManager.Instance.currentLevel.spawnableScrap.Count);
                itemsToGive.Add(new ItemToDrop(RoundManager.Instance.currentLevel.spawnableScrap[num].spawnableItem, UnityEngine.Random.Range(30, 100)));
            }
            return itemsToGive;
        }

        public class ItemToDrop
        {
            public int scrapValue;
            public Item scrapItem;

            public ItemToDrop(Item item, int value)
            {
                scrapValue = value;
                scrapItem = item;
            }
        }
    }
}