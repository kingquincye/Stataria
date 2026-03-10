using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Stataria.Buffs;
using System;
using Terraria.Localization;
using Terraria.DataStructures;

namespace Stataria
{
    public class GuardianPlayer : ModPlayer
    {
        private HashSet<int> playersInAura = new HashSet<int>();
        public bool IsGuardianActive => GetGuardianRole()?.Status == RoleStatus.Active;

        private Role GetGuardianRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Guardian", out Role role) ? role : null;
        }

        public override void ResetEffects()
        {
            if (!IsGuardianActive)
            {
                playersInAura.Clear();
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            float healthBonus = config.roleSettings.GuardianHealthBonus / 100f;
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + healthBonus));

            Player.statDefense += config.roleSettings.GuardianDefenseBonus;

            float speedPenalty = config.roleSettings.GuardianMovementSpeedPenalty / 100f;
            Player.moveSpeed *= (1f - speedPenalty);

            float damagePenalty = config.roleSettings.GuardianDamagePenalty / 100f;
            Player.GetDamage(DamageClass.Generic) *= (1f - damagePenalty);

            Player.noKnockback = true;

            Player.AddBuff(ModContent.BuffType<GuardianAuraBuff>(), 2);
        }

        public override void PostUpdate()
        {
            if (!IsGuardianActive) return;

            UpdateAuraEffects();
        }

        private void UpdateAuraEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            HashSet<int> currentPlayersInAura = new HashSet<int>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player otherPlayer = Main.player[i];
                if (otherPlayer == null || !otherPlayer.active || otherPlayer.dead || otherPlayer.whoAmI == Player.whoAmI)
                    continue;

                if (otherPlayer.team == 0 || otherPlayer.team != Player.team)
                    continue;

                float distance = Vector2.Distance(Player.Center, otherPlayer.Center);

                if (distance <= config.roleSettings.GuardianAuraRadius)
                {
                    currentPlayersInAura.Add(i);
                    otherPlayer.AddBuff(ModContent.BuffType<GuardianAuraBuff>(), 2);
                }
            }

            playersInAura = currentPlayersInAura;
        }

        public bool IsPlayerInAura(int playerIndex)
        {
            return IsGuardianActive && playersInAura.Contains(playerIndex);
        }
    }
}