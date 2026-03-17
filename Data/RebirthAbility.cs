using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria;

namespace Stataria
{
    public enum RebirthAbilityType
    {
        Passive,
        Toggleable,
        Active,
        Enhancement
    }

    public class RebirthAbility
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsStackable { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public RebirthAbilityType AbilityType { get; set; }
        public bool IsHidden { get; set; }

        public Dictionary<string, object> AbilityData { get; set; }

        public RebirthAbility(string name, string description, int cost, bool isStackable = false, int maxLevel = 1)
        {
            Name = name;
            Description = description;
            Cost = cost;
            IsUnlocked = false;
            IsStackable = isStackable;
            Level = 0;
            MaxLevel = maxLevel;
            AbilityType = RebirthAbilityType.Passive;
            AbilityData = new Dictionary<string, object>();
        }

        public bool Unlock(RPGPlayer player)
        {
            if (IsUnlocked && !IsStackable)
                return false;

            if (IsStackable && Level >= MaxLevel)
                return false;

            if (player.RebirthPoints < Cost)
                return false;

            player.RebirthPoints -= Cost;

            if (!IsUnlocked)
                IsUnlocked = true;

            if (IsStackable)
                Level++;

            return true;
        }

        public virtual string GetCurrentEffectDescription()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            string description = Description;

            if (Name == "Last Stand")
            {
                description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.LastStand", config.rebirthAbilities.LastStandHealPercent, config.rebirthAbilities.LastStandImmunityTime, config.rebirthAbilities.LastStandCooldown);
            }
            else if (Name == "Extra Accessory Slot")
            {
                int maxLevel = Math.Min(config.rebirthAbilities.MaxExtraAccessorySlots, 29);
                description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.ExtraAccessorySlot");
                MaxLevel = maxLevel;
            }
            else if (Name == "Golden Touch")
            {
                int maxLevel = config.rebirthAbilities.MaxGoldenTouchLevel;
                description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.GoldenTouch", config.rebirthAbilities.GoldenTouchPercentPerLevel);
                MaxLevel = maxLevel;
            }
            else if (Name == "Teleport")
            {
                description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.Teleport", config.rebirthAbilities.TeleportCooldown);
            }
            else if (Name == "Enhanced Spawns")
            {
                int maxLevel = config.rebirthAbilities.MaxEnhancedSpawnsLevel;
                description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.EnhancedSpawns", config.rebirthAbilities.SpawnRatePercentPerLevel);
                MaxLevel = maxLevel;
            }
            else if (Name == "Auto-Clicker")
            {
                var player = Main.LocalPlayer;
                int maxLevel = config.rebirthAbilities.AutoClickerMaxLevel;
                if (player != null)
                {
                    var localConfig = ModContent.GetInstance<StatariaConfig>();
                    int displayLevel = IsUnlocked && Level > 0 ? Level : 1;

                    float currentSpeedFactor = localConfig.rebirthAbilities.AutoClickerSpeedFactorAtLevel1 +
                                            ((displayLevel - 1) * localConfig.rebirthAbilities.AutoClickerSpeedFactorImprovementPerLevel);
                    currentSpeedFactor = Math.Max(2f, currentSpeedFactor);
                    currentSpeedFactor = Math.Min(120f, currentSpeedFactor);

                    bool effectsArePreventedByConfig = localConfig.rebirthAbilities.AutoClickerPreventsEffects;

                    float cps = 60f / currentSpeedFactor;
                    string preventString = effectsArePreventedByConfig ? Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.Prevented") : Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.Allowed");
                    description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.AutoClickerDetail", cps.ToString("F1"), preventString);
                }
                else
                {
                    description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.AutoClickerDefault");
                }
                MaxLevel = maxLevel;
            }
            else if (Name == "Enhanced Fortune")
            {
                float luckPerLevel = config.rebirthAbilities.LuckPerAbilityLevel;
                int maxLevel = config.rebirthAbilities.MaxEnhancedFortuneLevel;
                description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.EnhancedFortune", luckPerLevel);
                MaxLevel = maxLevel;
            }

            if (IsUnlocked && IsStackable)
            {
                return Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityText.LevelInfo", description, Level, MaxLevel);
            }

            return description;
        }

        public TagCompound Save()
        {
            var tag = new TagCompound();
            tag["Name"] = Name;
            tag["IsUnlocked"] = IsUnlocked;
            tag["Level"] = Level;
            tag["IsHidden"] = IsHidden;

            if (AbilityType == RebirthAbilityType.Toggleable && AbilityData.ContainsKey("Enabled"))
            {
                tag["IsEnabled"] = (bool)AbilityData["Enabled"];
            }

            return tag;
        }

        public void Load(TagCompound tag)
        {
            IsUnlocked = tag.GetBool("IsUnlocked");
            Level = tag.GetInt("Level");
            IsHidden = tag.GetBool("IsHidden");

            if (AbilityType == RebirthAbilityType.Toggleable && tag.ContainsKey("IsEnabled"))
            {
                AbilityData["Enabled"] = tag.GetBool("IsEnabled");
            }
        }
    }
}