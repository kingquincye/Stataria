using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;
using Stataria.Buffs;

namespace Stataria
{
    public class DesperadoPlayer : ModPlayer
    {
        public int ShowdownCooldownTimer = 0;

        public bool IsDesperadoActive => GetDesperadoRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;
        public bool IsShowdownActive => Player.HasBuff(ModContent.BuffType<ShowdownBuff>());

        private Role GetDesperadoRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Desperado", out Role role) ? role : null;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ShowdownCooldownTimer"] = ShowdownCooldownTimer;
        }

        public override void LoadData(TagCompound tag)
        {
            ShowdownCooldownTimer = tag.ContainsKey("ShowdownCooldownTimer") ? tag.GetInt("ShowdownCooldownTimer") : 0;
        }

        public override void Initialize()
        {
            ShowdownCooldownTimer = 0;
        }

        public override void ResetEffects()
        {
            if (!IsDesperadoActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // Lock Tempo stacks at maximum during Showdown
            if (IsShowdownActive)
            {
                int maxStacks = config.roleSettings.DesperadoMaxTempoStacks;
                int maxBuffTime = maxStacks * 60;
                int buffType = ModContent.BuffType<DesperadoTempoBuff>();
                int buffIndex = Player.FindBuffIndex(buffType);
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = maxBuffTime;
                }
                else
                {
                    Player.AddBuff(buffType, maxBuffTime);
                }
            }

            int stacks = GetTempoStacks();
            if (stacks > 0)
            {
                float speedBonus = stacks * (config.roleSettings.DesperadoTempoAttackSpeedPerStack / 100f);
                Player.GetAttackSpeed(DamageClass.Ranged) += speedBonus;
            }

            if (IsShowdownActive)
            {
                Player.GetCritChance(DamageClass.Ranged) += config.roleSettings.DesperadoShowdownCritChance;
            }
        }

        public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (!IsDesperadoActive)
                return;

            if (item.CountsAsClass(DamageClass.Ranged))
            {
                int stacks = GetTempoStacks();
                var config = ModContent.GetInstance<StatariaConfig>();
                float velBonus = stacks * (config.roleSettings.DesperadoTempoVelocityPerStack / 100f);
                velocity *= (1f + velBonus);
            }
        }

        public override void PreUpdate()
        {
            if (ShowdownCooldownTimer > 0)
                ShowdownCooldownTimer--;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsDesperadoActive && item.CountsAsClass(DamageClass.Ranged))
            {
                AddTempoStack();
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsDesperadoActive && proj.owner == Player.whoAmI && proj.CountsAsClass(DamageClass.Ranged))
            {
                AddTempoStack();
            }
        }

        private void AddTempoStack()
        {
            if (IsShowdownActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            int maxStacks = config.roleSettings.DesperadoMaxTempoStacks;
            int buffType = ModContent.BuffType<DesperadoTempoBuff>();
            int buffIndex = Player.FindBuffIndex(buffType);

            if (buffIndex >= 0)
            {
                int newTime = Player.buffTime[buffIndex] + 60;
                Player.buffTime[buffIndex] = Math.Min(newTime, maxStacks * 60);
            }
            else
            {
                Player.AddBuff(buffType, 60);
            }
        }

        public int GetTempoStacks()
        {
            if (!IsDesperadoActive)
                return 0;

            int buffType = ModContent.BuffType<DesperadoTempoBuff>();
            int buffIndex = Player.FindBuffIndex(buffType);
            if (buffIndex >= 0)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                return Math.Min((Player.buffTime[buffIndex] + 59) / 60, config.roleSettings.DesperadoMaxTempoStacks);
            }
            return 0;
        }

        public void ActivateShowdown()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int duration = (int)(config.roleSettings.DesperadoShowdownDuration * 60f);
            ShowdownCooldownTimer = (int)(config.roleSettings.DesperadoShowdownCooldown * 60f);

            Player.AddBuff(ModContent.BuffType<ShowdownBuff>(), duration);

            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Player.position);
                CombatText.NewText(Player.Hitbox, Color.Orange, "SHOWDOWN!", true);

                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.CopperCoin, vel, 0, default, 1.5f);
                    d.noGravity = true;
                }
            }
        }
    }
}
