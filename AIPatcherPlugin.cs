using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using EFT;
using Comfort.Common;

namespace Fenix_NoAI_ESP
{

    [BepInPlugin("com.fenix.NoAI_ESP", "Fenix.NoAI_ESP", "1.0.0")]
    class AIPatcherPlugin : BaseUnityPlugin
    {

        internal static ConfigEntry<bool> applyToBots;
        internal static ConfigEntry<bool> applyToPlayer;
        private void Awake()
        {

            

            applyToBots = Config.Bind(
                "ESP Options (Changes Require RESTART)",
                "Disable for Bots?",
                false,
                new ConfigDescription("Will disable ESP for checking if a Bot is looking at another Bot. This applies on game start so you must re-start for changes to take effect.")
                );
            applyToPlayer = Config.Bind(
                "ESP Options (Changes Require RESTART)",
                "Disable for Player?",
                true,
                new ConfigDescription("Will disable ESP for checking if a Player is looking at a Bot. This applies on game start so you must re-start for changes to take effect.")
                );

            if (AIPatcherPlugin.applyToPlayer.Value == true && AIPatcherPlugin.applyToBots.Value == false)
            {
                new AIESPPatcherAimPlayer().Enable();
            }
            if (AIPatcherPlugin.applyToPlayer.Value == false && AIPatcherPlugin.applyToBots.Value == true)
            {
                new AIESPPatcherAimBot().Enable();
            }
            if (AIPatcherPlugin.applyToPlayer.Value == true && AIPatcherPlugin.applyToBots.Value == true)
            {
                new AIESPPatcherAimBoth().Enable();
            }
            if (AIPatcherPlugin.applyToPlayer.Value == false && AIPatcherPlugin.applyToBots.Value == false)
            {
                //Disabled
            }

        }

        public static bool isGameReady()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (gameWorld == null || gameWorld.AllPlayers == null || gameWorld.AllPlayers.Count <= 0 || sessionResultPanel != null)
            {
                return false;
            }
            return true;
        }
    }
}
