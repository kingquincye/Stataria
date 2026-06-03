using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace Stataria
{
    public class BerserkerPlayer : ModPlayer
    {
        public int SavageRoarTimer = 0;
        public int SavageRoarCooldownTimer = 0;

        public bool IsBerserkerActive => GetBerserkerRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;
        public bool IsSavageRoarActive => SavageRoarTimer > 0;

        private Role GetBerserkerRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Berserker", out Role role) ? role : null;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["SavageRoarCooldownTimer"] = SavageRoarCooldownTimer;
        }

        public override void LoadData(TagCompound tag)
        {
            SavageRoarCooldownTimer = tag.ContainsKey("SavageRoarCooldownTimer") ? tag.GetInt("SavageRoarCooldownTimer") : 0;
        }

        public override void Initialize()
        {
            SavageRoarTimer = 0;
            SavageRoarCooldownTimer = 0;
        }

        public override void ResetEffects()
        {
            if (!IsBerserkerActive)
            {
                SavageRoarTimer = 0;
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            // Bloodbath Passive
            float missingLifePct = 1f - ((float)Player.statLife / Player.statLifeMax2);
            missingLifePct = Math.Clamp(missingLifePct, 0f, 1f);

            float dmgBoost = (config.roleSettings.BerserkerBloodbathMaxDamageBonus / 100f) * missingLifePct;
            float speedBoost = (config.roleSettings.BerserkerBloodbathMaxSpeedBonus / 100f) * missingLifePct;

            Player.GetDamage(DamageClass.Melee) += dmgBoost;
            Player.GetAttackSpeed(DamageClass.Melee) += speedBoost;

            float immunityThreshold = config.roleSettings.BerserkerBloodbathImmunityThreshold / 100f;
            if (((float)Player.statLife / Player.statLifeMax2) < immunityThreshold)
            {
                Player.noKnockback = true;
                Player.buffImmune[BuffID.Slow] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Weak] = true;
                Player.buffImmune[BuffID.Burning] = true;
                Player.buffImmune[BuffID.OgreSpit] = true;
            }

            // Savage Roar Active: defense reduced to 0
            if (IsSavageRoarActive)
            {
                Player.statDefense = Player.statDefense * 0f;
            }
        }

        public override void PostUpdate()
        {
            if (!IsBerserkerActive)
                return;

            if (SavageRoarTimer > 0)
            {
                SavageRoarTimer--;
                
                // Red particles swirling around player when roar is active
                if (Main.rand.NextBool(3))
                {
                    int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.RedTorch, 0, 0, 100, default, 1.5f);
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }

            if (SavageRoarCooldownTimer > 0)
            {
                SavageRoarCooldownTimer--;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (IsBerserkerActive && IsSavageRoarActive)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
                {
                    if (Player.statLife <= info.Damage)
                    {
                        info.Damage = Math.Max(0, Player.statLife - 1);
                    }
                };
            }
        }

        public void ActivateSavageRoar()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            SavageRoarTimer = (int)(config.roleSettings.BerserkerSavageRoarDuration * 60f);
            SavageRoarCooldownTimer = (int)(config.roleSettings.BerserkerSavageRoarCooldown * 60f);

            // Roar visual: burst of red dust and sound
            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, Player.position);
                CombatText.NewText(Player.Hitbox, Color.Red, "Savage Roar!", true);

                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.Blood, vel, 0, default, 1.8f);
                    d.noGravity = true;
                }
            }

            // Sync to other players in multiplayer
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncSavageRoar();
            }
        }

        public void SyncSavageRoar(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncBerserkerSavageRoar);
            packet.Write(Player.whoAmI);
            packet.Write(SavageRoarTimer);
            packet.Write(SavageRoarCooldownTimer);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncSavageRoar(toWho, fromWho);
        }
    }
}
