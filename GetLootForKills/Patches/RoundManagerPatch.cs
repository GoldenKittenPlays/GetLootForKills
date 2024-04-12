using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GetLootForKills.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void PatchStart()
        {
            //This happens at the end of waiting for entrance teleport spawn
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList().Distinct().ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList().Distinct().ToList();
            List<string> itemsNames = Plugin.items.ConvertAll(i => Plugin.RemoveInvalidCharacters(i.itemName));
            itemsNames.Sort();
            Plugin.possibleItems = Plugin.Instance.Config.Bind("General",
                                        "ItemNames",
                                        "Notice The List Please",
                                        "" + string.Join("|", itemsNames));
            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName)))
                {
                    ConfigEntry<string> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName, // The key of the configuration option in the configuration file
                                             "Old phone, 1, 3, 50, 80, 1000, Flask, 1, 1, 200, 400, 100", // The default value
                                             "The First setting is the item name." +
                                             "The Second setting is the minimum item drop amount." +
                                             "The Third setting is the max item drop amount." +
                                             "The Fourth setting is the minimum scrap value amount." +
                                             "The Fifth setting is the maximum scrap value amount." +
                                             "The Last setting is the chance of it dropping from 1-1000." +
                                             "You can add more items like all config entries do," +
                                             "up to however many you want(each has 2 item drops default)." +
                                             "You can also put 'None' so that the mob drops nothing.");
                }
            }
        }
    }
}
