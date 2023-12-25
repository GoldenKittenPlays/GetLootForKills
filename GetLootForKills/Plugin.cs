using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using GetLootForKills.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

            logger.LogInfo("GetLootForKills initiated!");
            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
            MethodInfo AI_KillEnemy_Method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient), null, null);
            MethodInfo KillEnemy_Patch_Method = AccessTools.Method(typeof(KillPatch), nameof(KillPatch.patchKillEnemyOnOwnerClient), null, null);
            harmony.Patch(AI_KillEnemy_Method, new HarmonyMethod(KillEnemy_Patch_Method), null, null, null, null);
        }

        public static string RemoveWhitespaces(string source)
        {
            return string.Join("", source.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        public static List<string> GetMobItems(string mobName)
        {
            foreach (ConfigDefinition entry in Instance.Config.Keys)
            {
                if (RemoveWhitespaces(entry.Key.ToUpper()).Equals(mobName))
                {
                    return Instance.Config[entry].BoxedValue.ToString().ToUpper().Split(',').ToList();
                }
            }
            logger.LogInfo("No mob found!");
            return new List<string>();
        }
    }
}