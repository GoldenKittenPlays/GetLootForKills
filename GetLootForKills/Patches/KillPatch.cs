using GetLootForKills.Component;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace GetLootForKills.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class KillPatch
    {

        [HarmonyPatch("KillEnemyOnOwnerClient")]
        [HarmonyPostfix]
        static void patchKillEnemyOnOwnerClient(ref EnemyAI __instance)
        {
            if (__instance.isEnemyDead && __instance.gameObject.GetComponent<DroppedItemEnemy>() == null)
            {
                RoundManager instance = RoundManager.Instance;
                int itemID = UnityEngine.Random.Range(0, instance.currentLevel.spawnableScrap.Count);
                Item item = instance.currentLevel.spawnableScrap[itemID].spawnableItem;
                Vector3 position = __instance.transform.position + Vector3.up * 0.6f;
                position += new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
                GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                GrabbableObject component = obj.GetComponent<GrabbableObject>();
                component.fallTime = 0f;
                component.SetScrapValue(itemID);
                int scrapValue = (int)(UnityEngine.Random.Range(instance.currentLevel.minTotalScrapValue / 3, instance.currentLevel.maxTotalScrapValue / 3) * instance.scrapValueMultiplier);
                if (scrapValue > 30)
                {
                    component.SetScrapValue(30);
                }
                else
                {
                    component.SetScrapValue(scrapValue);
                }
                __instance.gameObject.AddComponent<DroppedItemEnemy>();
                obj.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
