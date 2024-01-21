using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GetLootForKills.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GetLootForKills
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.GetLootForKills", modName = "GetLootForKills", modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static Plugin Instance;

        public static ConfigEntry<string> possibleItems;

        public static ManualLogSource logger;

        public static List<EnemyType> enemies;

        public static List<Item> items;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
            MethodInfo AI_KillEnemy_Method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient), null, null);
            MethodInfo KillEnemy_Patch_Method = AccessTools.Method(typeof(KillPatch), nameof(KillPatch.PatchKillEnemyOnOwnerClient), null, null);
            harmony.Patch(AI_KillEnemy_Method, new HarmonyMethod(KillEnemy_Patch_Method), null, null, null, null);
            logger.LogInfo("GetLootForKills initiated!");
        }

        public static string RemoveWhitespaces(string source)
        {
            return string.Join("", source.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        public static string RemoveSpecialCharacters(string source)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in source)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string RemoveInvalidCharacters(string source)
        {
            return RemoveWhitespaces(RemoveSpecialCharacters(source));
        }

        public static Item GetItemByName(string name)
        {
            Item itemBy = null;
            items.ForEach(item =>
            {
                if (RemoveInvalidCharacters(item.itemName).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    itemBy = item;
                }
            });
            return itemBy;
        }

        public static List<string> GetMobItems(string mobName)
        {
            foreach (ConfigDefinition entry in Instance.Config.Keys)
            {
                if (RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(mobName))
                {
                    if (Instance.Config[entry].BoxedValue.ToString().ToUpper().IsNullOrWhiteSpace())
                    {
                        logger.LogInfo("Invalid Entries For: " + mobName);
                        return new List<string>();
                    }
                    else
                    {
                        logger.LogInfo("Found Items: " + mobName);
                        return Instance.Config[entry].BoxedValue.ToString().ToUpper().Replace(" ", "")
                                .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                }
            }
            logger.LogInfo("No Items: " + mobName);
            return new List<string>();
        }
    }
}