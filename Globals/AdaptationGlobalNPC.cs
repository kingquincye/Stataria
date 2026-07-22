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

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];
                if (player != null && player.active)
                {
                    ApplyOffensiveAdaptation(npc, player, ref modifiers);
                }
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ApplyOffensiveExpGain(npc, player, hit, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];
                if (player != null && player.active)
                {
                    ApplyOffensiveExpGain(npc, player, hit, damageDone);
                }
            }
        }

        private static void ApplyOffensiveAdaptation(NPC target, Player player, ref NPC.HitModifiers modifiers)
        {
            var adaptorPlayer = player.GetModPlayer<AdaptationPlayer>();
            if (adaptorPlayer == null || !adaptorPlayer.IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config == null)
                return;

            AdaptationCategory cat = target.boss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
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
                modifiers.FlatBonusDamage += target.lifeMax * 10 + 999999;
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

            AdaptationCategory cat = target.boss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
            AdaptationPlayer.GetNPCTargetIdAndName(target, out string targetId, out string name);

            AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: true);

            float baseDamage = Math.Max((float)hit.SourceDamage, (float)damageDone);
            float hitMult = config != null ? config.roleSettings.AdaptorExpHitMultiplier : 0.5f;
            float expGain = Math.Max(25f, baseDamage * hitMult);

            if (target.boss)
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
    }
}
