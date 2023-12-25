using GetLootForKills.Component;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace GetLootForKills.Patches
{
    internal class KillPatch
    {
        public static void patchKillEnemyOnOwnerClient(ref EnemyAI __instance, bool overrideDestroy = false)
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
                RoundManager instance = RoundManager.Instance;
                Plugin.logger.LogInfo("Killed Mob Name: " + __instance.enemyType.enemyName.ToUpper());
                Vector3 position = __instance.transform.position + Vector3.up * 0.6f;
                List<ItemToDrop> items = GetItemsForMob(Plugin.RemoveWhitespaces(__instance.enemyType.enemyName.ToUpper()));
                if (!items.Any())
                {
                    int num = UnityEngine.Random.Range(0, instance.currentLevel.spawnableScrap.Count);
                    items.Add(new ItemToDrop(instance.currentLevel.spawnableScrap[num].spawnableItem, UnityEngine.Random.Range(30, 50)));
                }
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].scrapItem.spawnPrefab != null)
                    {
                        DropItem(position, items[i].scrapItem.spawnPrefab, items[i].scrapValue, instance);
                    }
                    else
                    {
                        Plugin.logger.LogWarning("Item drop does not exist running default drop.");
                        DropItem(position, instance.currentLevel.spawnableScrap[UnityEngine.Random.Range(0, instance.currentLevel.spawnableScrap.Count)].spawnableItem.spawnPrefab, UnityEngine.Random.Range(50, 150), instance);
                    }
                }
                __instance.gameObject.AddComponent<DroppedItemEnemy>();
            }
        }

        static void DropItem(Vector3 position, GameObject itemPrefab, int scrapValue, RoundManager instance)
        {
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
            NetworkObject net = obj.GetComponent<NetworkObject>();
            net.Spawn();
            instance.SyncScrapValuesClientRpc(new List<NetworkObjectReference>() { net }.ToArray(), new List<int>() { valueOfScrap }.ToArray());
            Plugin.logger.LogWarning("Item drop synced.");
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
                    string name = Plugin.RemoveWhitespaces(items[count]);
                    int minItemDropAmount = int.Parse(Plugin.RemoveWhitespaces(items[count + 1]));
                    int maxItemDropAmount = int.Parse(Plugin.RemoveWhitespaces(items[count + 2]));
                    int minScrapValue = int.Parse(Plugin.RemoveWhitespaces(items[count + 3]));
                    int maxScrapValue = int.Parse(Plugin.RemoveWhitespaces(items[count + 4]));
                    int dropChance = int.Parse(Plugin.RemoveWhitespaces(items[count + 5]));
                    int rand = UnityEngine.Random.Range(1, 1000);
                    if (rand < dropChance)
                    {
                        foreach (Item itemToGive in Plugin.items)
                        {
                            if (Plugin.RemoveWhitespaces(itemToGive.itemName.ToUpper()).Equals(Plugin.RemoveWhitespaces(name.ToUpper())))
                            {
                                rand = 1;
                                if (minItemDropAmount < maxItemDropAmount && minItemDropAmount > 0)
                                {
                                    rand = UnityEngine.Random.Range(minItemDropAmount, maxItemDropAmount);
                                }
                                for (int id = 0; id < rand; id++)
                                {
                                    itemsToGive.Add(new ItemToDrop(itemToGive, UnityEngine.Random.Range(minScrapValue, maxScrapValue)));
                                    Plugin.logger.LogInfo("Item Added!");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Plugin.logger.LogInfo("No Config Items Found For Mob!");
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