using UnityEngine;

namespace GetLootForKills.Component
{
    internal class DroppedItemEnemy : MonoBehaviour
    {
        void Awake()
        {
            Plugin.logger.LogInfo("Enemy Object Drop Loaded.");
        }
    }
}
