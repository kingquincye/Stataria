using System;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Stataria.Core
{
    public class AdaptationData
    {
        public int Level { get; set; }
        public float CurrentExp { get; set; }

        public AdaptationData(int level = 0, float currentExp = 0f)
        {
            int maxLevel = GetMaxLevel();
            Level = Math.Clamp(level, 0, maxLevel);
            CurrentExp = Math.Max(0f, currentExp);
        }

        public static int GetMaxLevel()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            return config != null ? Math.Max(1, config.roleSettings.AdaptorMaxLevel) : 10;
        }

        public static float GetRequiredExp(AdaptationCategory category, int currentLevel, string targetId = "")
        {
            int maxLevel = GetMaxLevel();
            if (currentLevel >= maxLevel)
                return 0f;

            int levelFactor = currentLevel + 1;
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config == null)
                return 100f * levelFactor;

            var rSettings = config.roleSettings;

            float baseExp = category switch
            {
                AdaptationCategory.Boss => rSettings.AdaptorBaseExpBoss,
                AdaptationCategory.Mob => rSettings.AdaptorBaseExpMob,
                AdaptationCategory.Debuff => rSettings.AdaptorBaseExpDebuff,
                AdaptationCategory.Environment => targetId switch
                {
                    "Darkness" => rSettings.AdaptorBaseExpDarkness,
                    "FallDamage" => rSettings.AdaptorBaseExpFallDamage,
                    "Breath" => rSettings.AdaptorBaseExpBreathlessness,
                    "Lava" => rSettings.AdaptorBaseExpLava,
                    "Knockback" => rSettings.AdaptorBaseExpKnockback,
                    _ => rSettings.AdaptorBaseExpEnvironment
                },
                AdaptationCategory.Death => rSettings.AdaptorBaseExpDeath,
                _ => 100f
            };

            float multiplier = category switch
            {
                AdaptationCategory.Boss => 1.5f,
                AdaptationCategory.Death => 2.0f,
                _ => 1.0f
            };

            return baseExp * levelFactor * multiplier;
        }

        public float GetMaxExp(AdaptationCategory category, string targetId = "")
        {
            return GetRequiredExp(category, Level, targetId);
        }

        public float GetProgressPercentage(AdaptationCategory category, string targetId = "")
        {
            int maxLevel = GetMaxLevel();
            if (Level >= maxLevel)
                return 1.0f;

            float maxExp = GetMaxExp(category, targetId);
            if (maxExp <= 0f)
                return 1.0f;

            return Math.Clamp(CurrentExp / maxExp, 0f, 1.0f);
        }

        /// <summary>
        /// Adds experience towards this adaptation. Returns true if a level-up occurred.
        /// </summary>
        public bool AddExp(AdaptationCategory category, float expAmount, out int levelsGained, string targetId = "")
        {
            levelsGained = 0;
            int maxLevel = GetMaxLevel();
            if (Level >= maxLevel || expAmount <= 0f)
                return false;

            CurrentExp += expAmount;
            bool didLevelUp = false;

            while (Level < maxLevel)
            {
                float required = GetRequiredExp(category, Level, targetId);
                if (CurrentExp >= required)
                {
                    CurrentExp -= required;
                    Level++;
                    levelsGained++;
                    didLevelUp = true;
                }
                else
                {
                    break;
                }
            }

            if (Level >= maxLevel)
            {
                CurrentExp = 0f;
            }

            return didLevelUp;
        }

        public TagCompound Save()
        {
            return new TagCompound
            {
                ["level"] = Level,
                ["exp"] = CurrentExp
            };
        }

        public static AdaptationData Load(TagCompound tag)
        {
            int level = tag.GetInt("level");
            float exp = tag.GetFloat("exp");
            return new AdaptationData(level, exp);
        }
    }
}
