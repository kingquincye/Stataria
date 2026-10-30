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
        public static int EmptinessSprayerGasProjType { get; private set; } = -1;
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

                    if (wotgMod != null)
                    {
                        if (wotgMod.TryFind<ModProjectile>("EmptinessSprayerGas", out ModProjectile gasProj))
                        {
                            EmptinessSprayerGasProjType = gasProj.Type;
                        }
                        else if (wotgMod.TryFind<ModProjectile>("NoxusSprayerGas", out ModProjectile legacyGasProj))
                        {
                            EmptinessSprayerGasProjType = legacyGasProj.Type;
                        }
                    }
                }
                catch (Exception ex)
                {
                    StatariaLogger.Error("Failed to initialize WrathOfTheGodsSupportHelper: " + ex.Message);
                }
            }
        }

        public override void PostSetupContent()
        {
            if (WotGLoaded)
            {
                RegisterHugoReflection();
            }
        }

        public static void RegisterHugoReflection(int hugoType = -1)
        {
            if (!WotGLoaded)
                return;

            try
            {
                Mod wotgMod = ModLoader.GetMod("NoxusBoss");
                if (wotgMod == null)
                    return;

                if (EmptinessSprayerGasProjType <= 0)
                {
                    if (wotgMod.TryFind<ModProjectile>("EmptinessSprayerGas", out ModProjectile gasProj))
                        EmptinessSprayerGasProjType = gasProj.Type;
                    else if (wotgMod.TryFind<ModProjectile>("NoxusSprayerGas", out ModProjectile legacyGasProj))
                        EmptinessSprayerGasProjType = legacyGasProj.Type;
                }

                if (hugoType <= 0)
                {
                    hugoType = ModContent.NPCType<NPCs.Hugo>();
                }

                if (hugoType <= 0)
                    return;

                Type emptinessSprayerType = wotgMod.Code?.GetType("NoxusBoss.Content.Items.MiscOPTools.EmptinessSprayer")
                    ?? wotgMod.Code?.GetType("NoxusBoss.Content.Items.MiscOPTools.NoxusSprayer");

                if (emptinessSprayerType != null)
                {
                    PropertyInfo reflectProp = emptinessSprayerType.GetProperty("NPCsThatReflectSpray", BindingFlags.Public | BindingFlags.Static);
                    if (reflectProp != null)
                    {
                        bool[] reflectArray = reflectProp.GetValue(null) as bool[];
                        if (reflectArray != null)
                        {
                            if (hugoType >= reflectArray.Length)
                            {
                                Array.Resize(ref reflectArray, hugoType + 1);
                                FieldInfo backingField = emptinessSprayerType.GetField("<NPCsThatReflectSpray>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
                                backingField?.SetValue(null, reflectArray);
                            }
                            reflectArray[hugoType] = true;
                            StatariaLogger.Info($"[Stataria] Registered Hugo (ID: {hugoType}) to reflect Wrath of the Gods Emptiness Sprayer.");
                        }
                    }

                    // Ensure Hugo is NOT in NPCsToNotDelete, because NPCsToNotDelete causes EmptinessSprayerGas to early-continue before checking reflection
                    PropertyInfo noDeleteProp = emptinessSprayerType.GetProperty("NPCsToNotDelete", BindingFlags.Public | BindingFlags.Static);
                    if (noDeleteProp != null)
                    {
                        bool[] noDeleteArray = noDeleteProp.GetValue(null) as bool[];
                        if (noDeleteArray != null && hugoType < noDeleteArray.Length)
                        {
                            noDeleteArray[hugoType] = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatariaLogger.Error("Failed to register Hugo for WotG spray reflection: " + ex.Message);
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
            EmptinessSprayerGasProjType = -1;
        }

        private static void Hook_Player_Spawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
        {
            // Do not interfere with Magic Mirror, Recall Potions, or initial world join!
            if (context != PlayerSpawnContext.ReviveFromDeath)
            {
                orig(self, context);
                return;
            }

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
                        // Run vanilla Spawn first so the player sprite and limbs are restored properly
                        orig(self, context);

                        // Now keep the player right where they died instead of at the base bed/spawn
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
                        return;
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
            if (adaptor != null && adaptor.IsAdaptorActive)
            {
                int maxLevel = AdaptationData.GetMaxLevel();
                adaptor.CheckAndHandleErasureAdaptation(maxLevel);
            }

            // In multiplayer, WotG disables the singleplayer menu deletion kick.
            // If the player hit themselves with a reflected emptiness spray, erase/kill them if not adapted.
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient && !localPlayer.dead && EmptinessSprayerGasProjType > 0)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == EmptinessSprayerGasProjType && p.owner == localPlayer.whoAmI && p.ai[0] == 1f && p.ai[1] >= 20f)
                    {
                        if (p.Hitbox.Intersects(localPlayer.Hitbox))
                        {
                            int maxLevel = AdaptationData.GetMaxLevel();
                            bool erasureAdapted = adaptor != null && adaptor.IsAdaptorActive && adaptor.IsErasureAdapted(maxLevel);
                            if (!erasureAdapted)
                            {
                                localPlayer.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromLiteral(localPlayer.name + " was erased by reflected emptiness.")), 999999, 0);
                            }
                            break;
                        }
                    }
                }
            }
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
