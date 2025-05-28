using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System;

namespace Stataria
{
    public class VampirePlayer : ModPlayer
    {
        public bool IsVampireActive => GetVampireRole()?.Status == RoleStatus.Active;

        private Role GetVampireRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Vampire", out Role role) ? role : null;
        }

        public override void ResetEffects()
        {
            if (!IsVampireActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            float healthBonus = config.roleSettings.VampireHealthBonus / 100f;
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + healthBonus));

            Player.moveSpeed += config.roleSettings.VampireMovementSpeed / 100f;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsVampireActive)
            {
                TryApplyBleed(target, damageDone);
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsVampireActive && proj.owner == Player.whoAmI)
            {
                TryApplyBleed(target, damageDone);
            }
        }

        private void TryApplyBleed(NPC target, int damageDone)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (Main.rand.NextFloat() < config.roleSettings.VampireBleedChance / 100f)
            {
                int bleedDuration = (int)(config.roleSettings.VampireBleedDuration * 60f);
                target.AddBuff(ModContent.BuffType<BleedDebuff>(), bleedDuration);

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(target.Hitbox, Color.DarkRed, "Bleed!", false, false);
                }
            }

            if (target.HasBuff(ModContent.BuffType<BleedDebuff>()))
            {
                int healAmount = (int)(damageDone * config.roleSettings.VampireBleedHealPercent / 100f);
                healAmount = Math.Max(1, healAmount);

                Player.statLife += healAmount;
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;

                if (healAmount > 0 && Main.netMode != NetmodeID.Server)
                {
                    Player.HealEffect(healAmount, true);
                }
            }
        }
    }
}