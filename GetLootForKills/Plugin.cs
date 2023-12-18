using BepInEx;
using BepInEx.Logging;
using GetLootForKills.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetLootForKills
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "GoldenKitten.GetLootForKills", modName = "GetLootForKills", modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static Plugin Instance;

        public static ManualLogSource logger;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            logger.LogInfo("GetLootForKills initiated!");

            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(KillPatch));
        }
    }
}
