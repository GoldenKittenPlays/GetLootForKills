using HarmonyLib;

namespace GetLootForKills.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch("SyncScrapValuesClientRpc")]
        [HarmonyPostfix]
        private static void fixScrapTotalPatch(int[] allScrapValue, ref float ___totalScrapValueInLevel)
        {
            int num = 0;
            if (allScrapValue != null)
            {
                for (int i = 0; i < allScrapValue.Length; i++)
                {
                    num = ((allScrapValue[i] < 9000) ? (num + allScrapValue[i]) : (num + (allScrapValue[i] - 9999)));
                }
            }
            ___totalScrapValueInLevel = num;
        }
    }
}
