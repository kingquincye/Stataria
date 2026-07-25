using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Stataria.Helpers;
using Stataria.Players;
using Stataria.Core;

namespace Stataria
{
    public class WrathOfTheGodsSupportHelper : ModSystem
    {
        public static bool WotGLoaded { get; private set; }
        private static Type playerDeletionSystemType;
        private static PropertyInfo playerWasDeletedProp;
        private static PropertyInfo deletionTimerProp;
        private static PropertyInfo deletedByNamelessProp;
        private static PropertyInfo deletedByLaRugaProp;

        public override void Load()
        {
            On_Player.Spawn += Hook_Player_Spawn;

            WotGLoaded = ModLoader.HasMod("NoxusBoss");
            if (WotGLoaded)
            {
                try
                {
                    Mod wotgMod = ModLoader.GetMod("NoxusBoss");
                    playerDeletionSystemType = wotgMod?.Code?.GetType("NoxusBoss.Core.Graphics.SpecificEffectManagers.EmptinessSprayPlayerDeletionSystem");
                    if (playerDeletionSystemType != null)
                    {
                        playerWasDeletedProp = playerDeletionSystemType.GetProperty("PlayerWasDeleted", BindingFlags.Public | BindingFlags.Static);
                        deletionTimerProp = playerDeletionSystemType.GetProperty("DeletionTimer", BindingFlags.Public | BindingFlags.Static);
                        deletedByNamelessProp = playerDeletionSystemType.GetProperty("PlayerWasDeletedByNamelessDeity", BindingFlags.Public | BindingFlags.Static);
                        deletedByLaRugaProp = playerDeletionSystemType.GetProperty("PlayerWasDeletedByLaRuga", BindingFlags.Public | BindingFlags.Static);
                    }
                }
                catch (Exception ex)
                {
                    StatariaLogger.Error("Failed to initialize WrathOfTheGodsSupportHelper: " + ex.Message);
                }
            }
        }

        public override void Unload()
        {
            On_Player.Spawn -= Hook_Player_Spawn;

            WotGLoaded = false;
            playerDeletionSystemType = null;
            playerWasDeletedProp = null;
            deletionTimerProp = null;
            deletedByNamelessProp = null;
            deletedByLaRugaProp = null;
        }

        private static void Hook_Player_Spawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
        {
            if (self != null && self.active)
            {
                var adaptor = self.GetModPlayer<AdaptationPlayer>();
                if (adaptor != null && adaptor.IsAdaptorActive)
                {
                    int maxLevel = AdaptationData.GetMaxLevel();
                    bool deathAdapted = adaptor.IsDeathAdapted(maxLevel);
                    bool erasureAdapted = adaptor.IsErasureAdapted(maxLevel);

                    if (deathAdapted || erasureAdapted)
                    {
                        // Prevent spawn teleport! Keep player exactly where they stood before death/erasure
                        if (adaptor.LastValidPosition != Vector2.Zero)
                        {
                            self.position = adaptor.LastValidPosition;
                            self.velocity = Vector2.Zero;
                        }

                        self.dead = false;
                        self.respawnTimer = 0;
                        self.immuneAlpha = 0;
                        self.statLife = self.statLifeMax2;
                        self.statMana = self.statManaMax2;
                        Main.gameMenu = false;
                        return; // Cancel vanilla Spawn teleport routine!
                    }
                }
            }

            orig(self, context);
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
                return;

            Player localPlayer = Main.LocalPlayer;
            if (localPlayer == null || !localPlayer.active)
                return;

            var adaptor = localPlayer.GetModPlayer<AdaptationPlayer>();
            if (adaptor == null || !adaptor.IsAdaptorActive)
                return;

            int maxLevel = AdaptationData.GetMaxLevel();
            adaptor.CheckAndHandleErasureAdaptation(maxLevel);
        }

        public static bool IsPlayerWasDeletedActive()
        {
            if (!WotGLoaded || playerWasDeletedProp == null)
                return false;

            try
            {
                var val = playerWasDeletedProp.GetValue(null);
                return val is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        public static void ClearErasureState()
        {
            if (!WotGLoaded || playerDeletionSystemType == null)
                return;

            try
            {
                playerWasDeletedProp?.SetValue(null, false);
                deletionTimerProp?.SetValue(null, 0);
                deletedByNamelessProp?.SetValue(null, false);
                deletedByLaRugaProp?.SetValue(null, false);
            }
            catch (Exception ex)
            {
                StatariaLogger.Error("Error clearing WotG erasure state: " + ex.Message);
            }
        }
    }
}
