using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Stataria.Buffs;
using System;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.GameInput;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.Localization;


namespace Stataria
{
    public class ClericPlayer : ModPlayer
    {
        private int regenTimer = 0;
        private HashSet<int> playersInAura = new HashSet<int>();
        
        public bool IsClericActive => GetClericRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;
        public bool IsAngelActive => IsClericActive && Player.GetModPlayer<RPGPlayer>().AscendedRoles.Contains("Cleric");
        public bool IsDivineInterventionActive => Player.HasBuff(ModContent.BuffType<DivineInterventionBuff>());

        // Spirit Form / Soul Anchor Mechanics
        public bool IsInSpiritForm = false;
        public int SpiritFormTimer = 0;
        public int SpiritAngelWhoAmI = -1;
        public float ReceivedTeammateHealthBonus = 0f;
        public PlayerDeathReason SpiritDeathReason;
        public bool IsBypassingSoulAnchor = false;

        // Divine Resurrection active ability
        public int DivineResurrectionCooldownTimer = 0;
        public bool IsResurrectionChanneling = false;
        public int ChannelingTargetWhoAmI = -1;
        public int ChannelingTimer = 0;
        public int ChannelingMaxTime = 0;
        public float ChannelingProgress => ChannelingMaxTime > 0 ? (float)(ChannelingMaxTime - ChannelingTimer) / ChannelingMaxTime : 0f;
        public int ResurrectionInvincibilityTimer = 0;

        private Role GetClericRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Cleric", out Role role) ? role : null;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["IsInSpiritForm"] = IsInSpiritForm;
            tag["SpiritFormTimer"] = SpiritFormTimer;
            tag["SpiritAngelWhoAmI"] = SpiritAngelWhoAmI;
            tag["DivineResurrectionCooldownTimer"] = DivineResurrectionCooldownTimer;
        }

        public override void LoadData(TagCompound tag)
        {
            IsInSpiritForm = tag.ContainsKey("IsInSpiritForm") && tag.GetBool("IsInSpiritForm");
            SpiritFormTimer = tag.ContainsKey("SpiritFormTimer") ? tag.GetInt("SpiritFormTimer") : 0;
            SpiritAngelWhoAmI = tag.ContainsKey("SpiritAngelWhoAmI") ? tag.GetInt("SpiritAngelWhoAmI") : -1;
            DivineResurrectionCooldownTimer = tag.ContainsKey("DivineResurrectionCooldownTimer") ? tag.GetInt("DivineResurrectionCooldownTimer") : 0;
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncAngelState(toWho, fromWho);
        }

        public void SyncAngelState(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncAngelState);
            packet.Write(Player.whoAmI);
            packet.Write(IsInSpiritForm);
            packet.Write(SpiritFormTimer);
            packet.Write(SpiritAngelWhoAmI);
            packet.Write(DivineResurrectionCooldownTimer);
            packet.Write(IsResurrectionChanneling);
            packet.Write(ChannelingTimer);
            packet.Write(ChannelingMaxTime);
            packet.Send(toWho, fromWho);
        }

        public override void ResetEffects()
        {
            float savedHealthBonus = ReceivedTeammateHealthBonus;
            ReceivedTeammateHealthBonus = 0f;

            if (ResurrectionInvincibilityTimer > 0)
            {
                ResurrectionInvincibilityTimer--;
                Player.noFallDmg = true;
            }

            if (!IsClericActive)
            {
                playersInAura.Clear();
                if (savedHealthBonus > 0f)
                {
                    Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + savedHealthBonus / 100f));
                }
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = Player.GetModPlayer<RPGPlayer>();
            bool isAngel = rpg.AscendedRoles.Contains("Cleric");

            if (isAngel)
            {
                float healthBonus = config.roleSettings.AngelHealthBonus / 100f;
                Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + healthBonus));
                
                float defensePenalty = config.roleSettings.AngelDefensePenalty / 100f;
                Player.statDefense = Player.statDefense * (1f - defensePenalty);

                // Seraphim Wings: fall damage immunity and flight
                Player.noFallDmg = true;
                if (Player.wings == 0)
                {
                    Player.wings = 2; // Angel Wings index in vanilla (1 is Demon/Bone-like)
                    Player.wingsLogic = 2;
                }
                int desiredWingTime = (int)(config.roleSettings.AngelWingFlightTime * 60f);
                if (Player.wingTimeMax < desiredWingTime)
                {
                    Player.wingTimeMax = desiredWingTime;
                }

                if (Player.velocity.Y == 0f || Player.sliding || Player.controlHook || Player.mount.Active)
                {
                    Player.wingTime = Player.wingTimeMax;
                }

                if (Player.velocity.Y != 0)
                {
                    Player.moveSpeed += config.roleSettings.AngelInAirMoveSpeedBonus / 100f;
                }

                // Soul Anchor: Angel gains X% damage reduction while an ally is in Spirit Form
                bool allyInSpirit = false;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player other = Main.player[i];
                    bool isTeammate = false;
                    if (other != null && other.active && other.whoAmI != Player.whoAmI)
                    {
                        if (Player.team != 0 && Player.team == other.team)
                        {
                            isTeammate = true;
                        }
                        else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                        {
                            isTeammate = true;
                        }
                    }
                    if (isTeammate)
                    {
                        var otherCleric = other.GetModPlayer<ClericPlayer>();
                        if (otherCleric.IsInSpiritForm && otherCleric.SpiritAngelWhoAmI == Player.whoAmI)
                        {
                            allyInSpirit = true;
                            break;
                        }
                    }
                }
                if (allyInSpirit)
                {
                    Player.endurance += config.roleSettings.AngelSoulAnchorDamageReduction / 100f;
                }
            }
            else
            {
                float healthBonus = config.roleSettings.ClericHealthBonus / 100f;
                Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + healthBonus));
                
                float defensePenalty = config.roleSettings.ClericDefensePenalty / 100f;
                Player.statDefense = Player.statDefense * (1f - defensePenalty);
            }

            Player.AddBuff(ModContent.BuffType<ClericAuraBuff>(), 2);
        }

        public override void PostUpdate()
        {
            // Update Divine Intervention Buff state for the local player
            UpdateDivineInterventionAuraCheck();

            // Cooldown ticks down locally
            if (DivineResurrectionCooldownTimer > 0)
            {
                DivineResurrectionCooldownTimer--;
            }



            // Channeling logic for Angel
            if (IsResurrectionChanneling)
            {
                var config = ModContent.GetInstance<StatariaConfig>();

                if (Player.whoAmI == Main.myPlayer)
                {
                    bool anyTargetValid = false;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player other = Main.player[i];
                        if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                            continue;

                        bool isTeammate = false;
                        if (Player.team != 0 && Player.team == other.team)
                        {
                            isTeammate = true;
                        }
                        else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                        {
                            isTeammate = true;
                        }

                        if (!isTeammate)
                            continue;

                        var otherCleric = other.GetModPlayer<ClericPlayer>();
                        if (otherCleric.IsInSpiritForm)
                        {
                            float distance = Vector2.Distance(Player.Center, other.Center);
                            if (distance <= config.roleSettings.AngelAuraRadius)
                            {
                                anyTargetValid = true;
                                break;
                            }
                        }
                    }

                    if (!anyTargetValid)
                    {
                        // Interrupt channeling
                        IsResurrectionChanneling = false;
                        ChannelingTargetWhoAmI = -1;
                        ChannelingTimer = 0;
                        ChannelingMaxTime = 0;
                        if (Main.netMode != NetmodeID.Server)
                        {
                            CombatText.NewText(Player.Hitbox, Color.Red, "Interrupted!");
                        }
                        SyncAngelState();
                        return;
                    }

                    // Lock Angel movement/controls during channel
                    Player.velocity.X = 0;
                    if (Player.velocity.Y < 0) Player.velocity.Y = 0;
                    Player.controlLeft = false;
                    Player.controlRight = false;
                    Player.controlJump = false;
                    Player.controlUseItem = false;
                    Player.controlUseTile = false;
                }

                // Spawn vertical golden light beam descending onto all targets in range
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player other = Main.player[i];
                        if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                            continue;

                        bool isTeammate = false;
                        if (Player.team != 0 && Player.team == other.team)
                        {
                            isTeammate = true;
                        }
                        else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                        {
                            isTeammate = true;
                        }

                        if (!isTeammate)
                            continue;

                        var otherCleric = other.GetModPlayer<ClericPlayer>();
                        if (otherCleric.IsInSpiritForm)
                        {
                            float distance = Vector2.Distance(Player.Center, other.Center);
                            if (distance <= config.roleSettings.AngelAuraRadius)
                            {
                                Vector2 targetPos = other.Center;
                                for (int d = 0; d < 3; d++)
                                {
                                    Vector2 dPos = new Vector2(targetPos.X + Main.rand.NextFloat(-15f, 15f), targetPos.Y - Main.rand.NextFloat(0f, 600f));
                                    Dust dust = Dust.NewDustPerfect(dPos, DustID.GoldFlame, new Vector2(0f, Main.rand.NextFloat(3f, 7f)), 100, Color.Gold, 1.0f);
                                    dust.noGravity = true;
                                }
                            }
                        }
                    }
                }

                if (ChannelingTimer > 0)
                {
                    ChannelingTimer--;
                }

                if (Player.whoAmI == Main.myPlayer && ChannelingTimer <= 0)
                {
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player other = Main.player[i];
                            if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                                continue;

                            bool isTeammate = false;
                            if (Player.team != 0 && Player.team == other.team)
                            {
                                isTeammate = true;
                            }
                            else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                            {
                                isTeammate = true;
                            }

                            if (!isTeammate)
                                continue;

                            var otherCleric = other.GetModPlayer<ClericPlayer>();
                            if (otherCleric.IsInSpiritForm)
                            {
                                float distance = Vector2.Distance(Player.Center, other.Center);
                                if (distance <= config.roleSettings.AngelAuraRadius)
                                {
                                    otherCleric.ResurrectLocal(config.roleSettings.AngelResurrectionHealPercent, config.roleSettings.AngelResurrectionInvulTime);
                                }
                            }
                        }
                    }
                    else
                    {
                        var packet = ModContent.GetInstance<Stataria>().GetPacket();
                        packet.Write((byte)StatariaMessageType.AngelResurrect);
                        packet.Write(Player.whoAmI); // Send Angel's whoAmI
                        packet.Write(config.roleSettings.AngelResurrectionHealPercent);
                        packet.Write(config.roleSettings.AngelResurrectionInvulTime);
                        packet.Send();
                    }

                    DivineResurrectionCooldownTimer = (int)(config.roleSettings.AngelResurrectionCooldown * 60f);
                    IsResurrectionChanneling = false;
                    ChannelingTargetWhoAmI = -1;
                    ChannelingMaxTime = 0;
                    SyncAngelState();
                }
            }

            if (!IsClericActive) return;

            var configActive = ModContent.GetInstance<StatariaConfig>();
            
            regenTimer++;
            int regenIntervalTicks = (int)(configActive.roleSettings.AngelRegenInterval * 60f); // intervals are same (3s)
            
            if (regenTimer >= regenIntervalTicks)
            {
                ApplyRegeneration();
                regenTimer = 0;
            }
            
            UpdateAuraEffects();
        }

        private void UpdateAuraEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = Player.GetModPlayer<RPGPlayer>();
            bool isAngel = rpg.AscendedRoles.Contains("Cleric");
            
            float auraRadius = isAngel ? config.roleSettings.AngelAuraRadius : config.roleSettings.ClericAuraRadius;
            float healthBonus = isAngel ? config.roleSettings.AngelTeammateHealthBonus : config.roleSettings.ClericTeammateHealthBonus;
            
            HashSet<int> currentPlayersInAura = new HashSet<int>();
            bool divineInterventionActive = IsDivineInterventionActive;
            
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player otherPlayer = Main.player[i];
                if (otherPlayer == null || !otherPlayer.active || otherPlayer.dead || otherPlayer.whoAmI == Player.whoAmI)
                    continue;
                
                // Do not apply standard aura buff or health bonuses to players in Spirit Form
                if (otherPlayer.GetModPlayer<ClericPlayer>().IsInSpiritForm)
                    continue;
                
                bool isTeammate = false;
                if (Player.team != 0 && Player.team == otherPlayer.team)
                {
                    isTeammate = true;
                }
                else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && otherPlayer.team == 0)
                {
                    isTeammate = true;
                }

                if (!isTeammate)
                    continue;
                
                float distance = Vector2.Distance(Player.Center, otherPlayer.Center);
                
                if (distance <= auraRadius)
                {
                    currentPlayersInAura.Add(i);
                    
                    otherPlayer.AddBuff(ModContent.BuffType<ClericAuraBuff>(), 2);
                    
                    var otherCleric = otherPlayer.GetModPlayer<ClericPlayer>();
                    if (healthBonus > otherCleric.ReceivedTeammateHealthBonus)
                    {
                        otherCleric.ReceivedTeammateHealthBonus = healthBonus;
                    }
                    
                    if (divineInterventionActive)
                    {
                        int clericBuffIndex = Player.FindBuffIndex(ModContent.BuffType<DivineInterventionBuff>());
                        if (clericBuffIndex >= 0)
                        {
                            int remainingTime = Player.buffTime[clericBuffIndex];
                            otherPlayer.AddBuff(ModContent.BuffType<DivineInterventionBuff>(), remainingTime);
                        }
                    }
                }
            }
            
            playersInAura = currentPlayersInAura;
        }

        private void ApplyRegeneration()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = Player.GetModPlayer<RPGPlayer>();
            bool isAngel = rpg.AscendedRoles.Contains("Cleric");

            float selfRegenPercent = isAngel ? config.roleSettings.AngelSelfRegenPercent : config.roleSettings.ClericSelfRegenPercent;
            float teamRegenPercent = isAngel ? config.roleSettings.AngelTeammateRegenPercent : config.roleSettings.ClericTeammateRegenPercent;
            
            int selfHeal = (int)(Player.statLifeMax2 * selfRegenPercent / 100f);
            selfHeal = Math.Max(1, selfHeal);
            
            if (Player.statLife < Player.statLifeMax2)
            {
                Player.statLife += selfHeal;
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;
                
                if (Main.netMode != NetmodeID.Server)
                    Player.HealEffect(selfHeal, true);
            }
            
            foreach (int playerIndex in playersInAura)
            {
                Player teammate = Main.player[playerIndex];
                if (teammate == null || !teammate.active || teammate.dead)
                    continue;
                
                // Skip teammates in Spirit Form
                if (teammate.GetModPlayer<ClericPlayer>().IsInSpiritForm)
                    continue;
                
                int teammateHeal = (int)(teammate.statLifeMax2 * teamRegenPercent / 100f);
                teammateHeal = Math.Max(1, teammateHeal);
                
                if (teammate.statLife < teammate.statLifeMax2)
                {
                    teammate.statLife += teammateHeal;
                    if (teammate.statLife > teammate.statLifeMax2)
                        teammate.statLife = teammate.statLifeMax2;
                    
                    if (Main.netMode != NetmodeID.Server)
                        teammate.HealEffect(teammateHeal, false);
                }
            }
        }

        public void ActivateDivineIntervention()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int duration = (int)(config.roleSettings.DivineInterventionDuration * 60f);
            
            Player.AddBuff(ModContent.BuffType<DivineInterventionBuff>(), duration);
            
            foreach (int playerIndex in playersInAura)
            {
                Player teammate = Main.player[playerIndex];
                if (teammate != null && teammate.active && !teammate.dead)
                {
                    teammate.AddBuff(ModContent.BuffType<DivineInterventionBuff>(), duration);
                }
            }
            
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 50; i++)
                {
                    Vector2 position = Player.Center + Main.rand.NextVector2Circular(config.roleSettings.ClericAuraRadius, config.roleSettings.ClericAuraRadius);
                    Dust dust = Dust.NewDustPerfect(position, DustID.YellowTorch, Vector2.Zero, 0, Color.Gold, 1.2f);
                    dust.noGravity = true;
                    dust.fadeIn = 0.5f;
                }
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (IsBypassingSoulAnchor)
            {
                return true;
            }

            // Do not trigger Soul Anchor for the Angel player themselves
            var rpg = Player.GetModPlayer<RPGPlayer>();
            if (rpg.ActiveRole?.ID == "Cleric" && rpg.ActiveRole.Status == RoleStatus.Active && rpg.AscendedRoles.Contains("Cleric"))
            {
                return true; 
            }

            if (!IsInSpiritForm && !Player.dead && !Player.ghost)
            {
                Player angel = FindNearbyAngelTeammate();
                if (angel != null)
                {
                    var config = ModContent.GetInstance<StatariaConfig>();
                    IsInSpiritForm = true;
                    SpiritFormTimer = (int)(config.roleSettings.AngelSpiritFormDuration * 60f);
                    SpiritAngelWhoAmI = angel.whoAmI;
                    SpiritDeathReason = damageSource;
                    
                    Player.ghost = true;
                    Player.dead = true;
                    Player.statLife = 1;

                    if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
                    {
                        SyncAngelState();
                    }

                    SoundEngine.PlaySound(SoundID.Item8, Player.Center);
                    
                    for (int i = 0; i < 20; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(Player.Center, DustID.MagicMirror, Main.rand.NextVector2Circular(3f, 3f), 150, Color.Cyan, 1.2f);
                        dust.noGravity = true;
                    }

                    return false; // Prevent death
                }
            }

            return true;
        }

        private Player FindNearbyAngelTeammate()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            float closestDistance = float.MaxValue;
            Player angelPlayer = null;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player other = Main.player[i];
                if (other == null || !other.active || other.dead || other.whoAmI == Player.whoAmI)
                    continue;

                bool isTeammate = false;
                if (Player.team != 0 && Player.team == other.team)
                {
                    isTeammate = true;
                }
                else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                {
                    isTeammate = true;
                }

                if (!isTeammate)
                    continue;

                var otherRpg = other.GetModPlayer<RPGPlayer>();
                if (otherRpg?.ActiveRole?.ID == "Cleric" && otherRpg.ActiveRole.Status == RoleStatus.Active && otherRpg.AscendedRoles.Contains("Cleric"))
                {
                    float distance = Vector2.Distance(other.Center, Player.Center);
                    if (distance <= config.roleSettings.AngelAuraRadius)
                    {
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            angelPlayer = other;
                        }
                    }
                }
            }

            return angelPlayer;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (StatariaKeybinds.DivineResurrectionKey.JustPressed && !PlayerInput.WritingText)
            {
                var rpg = Player.GetModPlayer<RPGPlayer>();
                bool isAngel = rpg.ActiveRole?.ID == "Cleric" && rpg.ActiveRole.Status == RoleStatus.Active && rpg.AscendedRoles.Contains("Cleric");

                if (isAngel && DivineResurrectionCooldownTimer <= 0 && !IsResurrectionChanneling)
                {
                    bool anySpirits = false;
                    var config = ModContent.GetInstance<StatariaConfig>();

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player other = Main.player[i];
                        if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                            continue;

                        bool isTeammate = false;
                        if (Player.team != 0 && Player.team == other.team)
                        {
                            isTeammate = true;
                        }
                        else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                        {
                            isTeammate = true;
                        }

                        if (!isTeammate)
                            continue;

                        var otherCleric = other.GetModPlayer<ClericPlayer>();
                        if (otherCleric.IsInSpiritForm)
                        {
                            float distance = Vector2.Distance(Player.Center, other.Center);
                            if (distance <= config.roleSettings.AngelAuraRadius)
                            {
                                anySpirits = true;
                                break;
                            }
                        }
                    }

                    if (anySpirits)
                    {
                        IsResurrectionChanneling = true;
                        ChannelingTimer = (int)(config.roleSettings.AngelResurrectionChannelTime * 60f);
                        ChannelingMaxTime = ChannelingTimer;
                        
                        SoundEngine.PlaySound(SoundID.Item29, Player.Center); // Play sound at Angel
                        SyncAngelState();
                    }
                    else
                    {
                        if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                        {
                            CombatText.NewText(Player.Hitbox, Color.Orange, "No spirits to resurrect!");
                        }
                    }
                }
            }
        }

        public void ResurrectLocal(float healPercent, float invulTime)
        {
            IsInSpiritForm = false;
            SpiritFormTimer = 0;
            SpiritAngelWhoAmI = -1;
            Player.ghost = false;
            Player.dead = false;
            Player.respawnTimer = 0;
            
            int healLife = (int)(Player.statLifeMax2 * (healPercent / 100f));
            Player.statLife = Math.Clamp(healLife, 1, Player.statLifeMax2);
            
            int invulFrames = (int)(invulTime * 60f);
            Player.immune = true;
            Player.immuneTime = invulFrames;
            Player.SetImmuneTimeForAllTypes(invulFrames);
            
            ResurrectionInvincibilityTimer = invulFrames;
            
            SoundEngine.PlaySound(SoundID.Item4, Player.position);
            
            for (int i = 0; i < 40; i++)
            {
                Dust dust = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(6f, 6f), 100, Color.Gold, 1.5f);
                dust.noGravity = true;
            }
            
            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
            {
                SyncAngelState();
                var rpg = Player.GetModPlayer<RPGPlayer>();
                rpg.SyncPlayer(-1, Player.whoAmI, false);
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
                NetMessage.SendData(MessageID.PlayerInfo, -1, -1, null, Player.whoAmI);
            }
        }

        public override void OnEnterWorld()
        {
            ResurrectionInvincibilityTimer = 0;
            if (IsInSpiritForm)
            {
                IsInSpiritForm = false;
                SpiritFormTimer = 0;
                SpiritAngelWhoAmI = -1;
                SyncAngelState();
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (IsInSpiritForm)
            {
                Color blueGhost = new Color(100, 150, 255, 100) * 0.7f;
                drawInfo.colorArmorHead = blueGhost;
                drawInfo.colorArmorBody = blueGhost;
                drawInfo.colorArmorLegs = blueGhost;
                drawInfo.colorHair = blueGhost;
                drawInfo.colorBodySkin = blueGhost;
                drawInfo.colorEyes = blueGhost;
                drawInfo.colorEyeWhites = blueGhost;
                drawInfo.colorHead = blueGhost;
                drawInfo.colorLegs = blueGhost;
                drawInfo.colorShirt = blueGhost;
                drawInfo.colorUnderShirt = blueGhost;
                drawInfo.colorPants = blueGhost;
                drawInfo.colorShoes = blueGhost;
            }
        }

        private void UpdateDivineInterventionAuraCheck()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (!Player.HasBuff(ModContent.BuffType<DivineInterventionBuff>()))
                return;

            // If we are a Cleric and have Divine Intervention active ourselves, we keep it.
            if (IsClericActive && IsDivineInterventionActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            bool nearActiveCleric = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player other = Main.player[i];
                if (other == null || !other.active || other.dead || other.whoAmI == Player.whoAmI)
                    continue;

                bool isTeammate = false;
                if (Player.team != 0 && Player.team == other.team)
                {
                    isTeammate = true;
                }
                else if (config.roleSettings.ClericAllowAuraOnNoTeam && Player.team == 0 && other.team == 0)
                {
                    isTeammate = true;
                }

                if (!isTeammate)
                    continue;

                var otherCleric = other.GetModPlayer<ClericPlayer>();
                if (otherCleric.IsClericActive && otherCleric.IsDivineInterventionActive)
                {
                    float auraRadius = otherCleric.IsAngelActive ? config.roleSettings.AngelAuraRadius : config.roleSettings.ClericAuraRadius;
                    float distance = Vector2.Distance(Player.Center, other.Center);
                    if (distance <= auraRadius)
                    {
                        nearActiveCleric = true;
                        break;
                    }
                }
            }

            if (!nearActiveCleric)
            {
                Player.ClearBuff(ModContent.BuffType<DivineInterventionBuff>());
            }
        }
    }
}