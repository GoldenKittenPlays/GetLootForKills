using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GetLootForKills.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void patchStart()
        {
            //This happens at the end of waiting for entrance teleport spawn
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();
            List<string> itemsNames = Plugin.items.ConvertAll(i => i.itemName);
            itemsNames.Sort();
            Plugin.possibleItems = Plugin.Instance.Config.Bind("General",
                                        "ItemNames",
                                        "Notice The List Please",
                                        "" + string.Join("|", itemsNames));
            foreach (Item item in Plugin.items)
            {
                Plugin.logger.LogInfo("ItemName: " + item.itemName);
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                Plugin.logger.LogInfo("MobName: " + enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", enemy.enemyName)))
                {
                    ConfigEntry<string> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             enemy.enemyName, // The key of the configuration option in the configuration file
                                             "Old phone, 1, 3, 50, 80, 1000, Flask, 1, 1, 200, 400, 100", // The default value
                                             "The First setting is the item name." +
                                             "The Second setting is the minimum item drop amount." +
                                             "The Third setting is the max item drop amount." +
                                             "The Fourth setting is the minimum scrap value amount." +
                                             "The Fifth setting is the maximum scrap value amount." +
                                             "The Last setting is the chance of it dropping from 1-1000." +
                                             "You can add more items like all config entries do," +
                                             "up to however many you want(each has 2 item drops default).");
                }
            }
        }
    }
}
