using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
