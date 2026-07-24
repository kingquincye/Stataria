using System;
using Terraria;
using Terraria.ModLoader;
using Stataria.Core;
using Stataria.Players;

namespace Stataria.Globals
{
    public class AdaptationGlobalNPC : GlobalNPC
    {
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyOffensiveAdaptation(npc, player, ref modifiers);
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ApplyOffensiveExpGain(npc, player, hit, damageDone);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (IsPlayerAttackProjectile(projectile, out Player player))
            {
                ApplyOffensiveAdaptation(npc, player, ref modifiers);
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (IsPlayerAttackProjectile(projectile, out Player player))
            {
                ApplyOffensiveExpGain(npc, player, hit, damageDone);
            }
        }

        private static bool IsPlayerAttackProjectile(Projectile projectile, out Player player)
        {
            player = null;
            if (projectile == null || !projectile.active)
                return false;

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return false;

            player = Main.player[projectile.owner];
            if (player == null || !player.active)
                return false;

            if (!projectile.friendly || projectile.hostile || projectile.trap)
                return false;

            if (projectile.TryGetGlobalProjectile<AdaptationGlobalProjectile>(out var globalProj) && globalProj.HasNPCSource)
                return false;

            return true;
        }

        private static void ApplyOffensiveAdaptation(NPC target, Player player, ref NPC.HitModifiers modifiers)
        {
            var adaptorPlayer = player.GetModPlayer<AdaptationPlayer>();
            if (adaptorPlayer == null || !adaptorPlayer.IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config == null)
                return;

            AdaptationCategory cat = AdaptationPlayer.IsBossNPC(target) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
            AdaptationPlayer.GetNPCTargetIdAndName(target, out string targetId, out string name);

            AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: true);

            if (adaptorPlayer.InstantAdaptationMode)
            {
                adaptorPlayer.GainExp(key, 999999f);
            }

            AdaptationData data = adaptorPlayer.GetAdaptation(key);
            int maxLevel = AdaptationData.GetMaxLevel();

            if (data.Level >= maxLevel && config.roleSettings.AdaptorMaxLevelOffensiveLethal)
            {
                // Max level Offensive Adaptation = Lethal strike! Total armor penetration!
                modifiers.ArmorPenetration += 999999f;
                modifiers.Defense.Flat *= 0f;

                double realLifeMax = target.lifeMax;
                if (target.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scaling) && scaling != null && scaling.UsesCustomHP && scaling.CustomLifeMax > 0)
                {
                    realLifeMax = scaling.CustomLifeMax;
                }
                if (realLifeMax >= 1500000000.0) realLifeMax = target.life > 0 ? target.life : 1000.0;

                modifiers.FlatBonusDamage += (float)Math.Min(1500000000.0, realLifeMax * 10.0 + 999999.0);
            }
            else if (data.Level > 0)
            {
                float bonusPerLevel = config.roleSettings.AdaptorOffensiveDamageBonusPerLevel;
                modifiers.FinalDamage *= (1.0f + data.Level * bonusPerLevel);
            }
        }

        private static void ApplyOffensiveExpGain(NPC target, Player player, NPC.HitInfo hit, int damageDone)
        {
            var adaptorPlayer = player.GetModPlayer<AdaptationPlayer>();
            if (adaptorPlayer == null || !adaptorPlayer.IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config == null)
                return;

            bool isBoss = AdaptationPlayer.IsBossNPC(target);
            AdaptationCategory cat = isBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
            AdaptationPlayer.GetNPCTargetIdAndName(target, out string targetId, out string name);

            AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: true);

            float baseDamage = Math.Max((float)hit.SourceDamage, (float)damageDone);
            float hitMult = config != null ? config.roleSettings.AdaptorExpHitMultiplier : 0.5f;
            float expGain = Math.Max(25f, baseDamage * hitMult);

            if (isBoss)
            {
                expGain = Math.Max(40f, baseDamage * hitMult * 0.5f);
            }

            adaptorPlayer.GainExp(key, expGain);

            AdaptationData data = adaptorPlayer.GetAdaptation(key);
            int maxLevel = AdaptationData.GetMaxLevel();

            if (data.Level >= maxLevel && config.roleSettings.AdaptorMaxLevelOffensiveLethal && target.active && target.life > 0)
            {
                target.life = 0;
                target.HitEffect(0, 9999.0);
                target.checkDead();
            }
        }

        public static long GetXpEligibleDamage(Player player, NPC target, int damageDone)
        {
            if (player == null || target == null)
                return damageDone;

            var adaptorPlayer = player.GetModPlayer<AdaptationPlayer>();
            if (adaptorPlayer == null || !adaptorPlayer.IsAdaptorActive)
                return damageDone;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config == null || !config.roleSettings.AdaptorMaxLevelOffensiveLethal)
                return damageDone;

            AdaptationCategory cat = AdaptationPlayer.IsBossNPC(target) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
            AdaptationPlayer.GetNPCTargetIdAndName(target, out string targetId, out string name);
            AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: true);
            AdaptationData data = adaptorPlayer.GetAdaptation(key);
            int maxLevel = AdaptationData.GetMaxLevel();

            if (data.Level >= maxLevel)
            {
                double realLifeMax = target.lifeMax;
                if (target.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scaling) && scaling != null && scaling.UsesCustomHP && scaling.CustomLifeMax > 0)
                {
                    realLifeMax = scaling.CustomLifeMax;
                }
                if (realLifeMax >= 1500000000.0) realLifeMax = target.life > 0 ? target.life : 1000.0;

                long flatBonus = (long)Math.Min(1500000000.0, realLifeMax * 10.0 + 999999.0);
                long adjustedDamage = damageDone - flatBonus;

                float bonusPerLevel = config.roleSettings.AdaptorOffensiveDamageBonusPerLevel;
                float maxAdaptationMult = 1.0f + (maxLevel * bonusPerLevel);

                if (adjustedDamage <= 0)
                {
                    adjustedDamage = (long)Math.Max(1f, realLifeMax * maxAdaptationMult);
                }

                long maxAllowedXpDamage = (long)Math.Max(50.0, realLifeMax * maxAdaptationMult);
                return Math.Clamp(adjustedDamage, 1L, maxAllowedXpDamage);
            }

            return damageDone;
        }
    }
}
