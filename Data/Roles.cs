using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria;

namespace Stataria
{
    public enum RoleStatus
    {
        Available,
        Active,
        Locked,
        Deactivated
    }

    public class Role
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FlavorText { get; set; }
        public int SwitchCost { get; set; }
        public RoleStatus Status { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public Dictionary<string, object> Effects { get; set; }

        public Role(string id, string name, string description, string flavorText, int switchCost = 0)
        {
            ID = id;
            Name = name;
            Description = description;
            FlavorText = flavorText;
            SwitchCost = switchCost;
            Status = RoleStatus.Available;
            Requirements = new Dictionary<string, object>();
            Effects = new Dictionary<string, object>();
        }

        public bool CanActivate(RPGPlayer player)
        {
            if (Status == RoleStatus.Locked)
            {
                if (Requirements.ContainsKey("RebirthRequired"))
                {
                    int requiredRebirths = (int)Requirements["RebirthRequired"];
                    return player.RebirthCount >= requiredRebirths;
                }
                return false;
            }

            if (Status == RoleStatus.Active)
                return false;

            if (Status == RoleStatus.Deactivated)
                return true;

            return player.RebirthPoints >= GetCurrentSwitchCost(player);
        }

        public int GetCurrentSwitchCost(RPGPlayer player)
        {
            if (player.ActiveRole == null || Status == RoleStatus.Deactivated)
                return 0;

            var config = ModContent.GetInstance<StatariaConfig>();
            int baseCost = config.roleSettings.BaseSwitchCost;
            float multiplier = 1f + (player.RoleSwitchCount * config.roleSettings.SwitchCostMultiplier);
            return (int)(baseCost * multiplier);
        }

        public string GetEffectsDescription()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (ID == "CritGod")
            {
                var effects = new List<string>
                {
                    $"• +{config.roleSettings.CritGodCritChance}% Critical Strike Chance",
                    $"• Excess Critical Strike Chance (over 100%) converts to +{config.roleSettings.CritGodExcessCritToDamage:0.##}% Critical Strike Damage per 1% excess"
                };

                if (config.roleSettings.CritGodEnableSummonCrits)
                {
                    effects.Add("• Your summons can now critically strike");
                }

                return string.Join("\n", effects);
            }

            if (ID == "Vampire")
            {
                var effects = new List<string>
                {
                    $"• +{config.roleSettings.VampireHealthBonus:0.##}% Max Health",
                    $"• +{config.roleSettings.VampireMovementSpeed:0.##}% Movement Speed",
                    $"• {config.roleSettings.VampireBleedChance:0.##}% chance to inflict Bleed on attack",
                    $"• Bleed deals {config.roleSettings.VampireBleedDamagePercent:0.##}% of enemy's max health per {config.roleSettings.VampireBleedTickInterval:0.##}s",
                    $"• Heal for {config.roleSettings.VampireBleedHealPercent:0.##}% of damage dealt to bleeding enemies",
                    $"• Heal for {config.roleSettings.VampireKillHealPercent:0.##}% of max health on enemy kill"
                };

                return string.Join("\n", effects);
            }

            if (ID == "Beastmaster")
            {
                var effects = new List<string>
                {
                    $"• +{config.roleSettings.BeastmasterDamagePerUniqueMinion:0.##}% Summon Damage per unique active minion type",
                    $"• +{config.roleSettings.BeastmasterBonusSlotsGained} Minion Slot{(config.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : "")} per {config.roleSettings.BeastmasterSlotsPerBonusSlot} base minion slots"
                };

                if (config.roleSettings.BeastmasterReduceSPRSlotEfficiency)
                {
                    int sprRequirement = (int)(config.roleSettings.BeastmasterSlotsPerBonusSlot * config.roleSettings.BeastmasterSPRSlotPenaltyMultiplier);
                    effects.Add($"• +{config.roleSettings.BeastmasterBonusSlotsGained} Minion Slot{(config.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : "")} per {sprRequirement} SPR-gained minion slots (reduced efficiency)");
                }
                else
                {
                    effects.Add($"• +{config.roleSettings.BeastmasterBonusSlotsGained} Minion Slot{(config.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : "")} per {config.roleSettings.BeastmasterSlotsPerBonusSlot} SPR-gained minion slots (normal efficiency)");
                }

                return string.Join("\n", effects);
            }

            if (ID == "ApexSummoner")
            {
                var effects = new List<string>
                {
                    "• Massive summon damage bonus when you have exactly one type of minion active",
                    $"• +{config.roleSettings.ApexSummonerDamagePerUnusedSlot:0.##}% Summon Damage per unused minion slot",
                    "• Bonus is lost if multiple minion types are summoned"
                };

                return string.Join("\n", effects);
            }

            if (ID == "BlackKnight")
            {
                var effects = new List<string>
                {
                    $"• Melee weapons gain +{config.roleSettings.BlackKnightINTToMeleeDamage:0.##}% damage per INT",
                    $"• Magic weapons gain +{config.roleSettings.BlackKnightSTRToMagicDamage:0.##}% damage per STR",
                    $"• Magic crits grant Dark Focus stacks (max {config.roleSettings.BlackKnightMaxDarkFocusStacks})",
                    $"• Melee attacks consume Dark Focus: +{config.roleSettings.BlackKnightDarkFocusCritChancePerStack:0.##}% crit chance and +{config.roleSettings.BlackKnightDarkFocusCritDamagePerStack:0.##}% crit damage per stack",
                    $"• Melee crits restore {config.roleSettings.BlackKnightManaRestoreOnMeleeCrit} mana",
                    $"• Melee crits grant Arcane Surge: +{config.roleSettings.BlackKnightArcaneSurgeMagicDamage:0.##}% magic damage for {config.roleSettings.BlackKnightArcaneSurgeDuration:0.##}s"
                };

                if (config.roleSettings.BlackKnightArcaneSurgeScaleWithDamage)
                {
                    effects.Add($"• Arcane Surge scales with melee crit damage (+{config.roleSettings.BlackKnightArcaneSurgeDamageScaling:0.##}% per damage point)");
                }

                return string.Join("\n", effects);
            }

            if (ID == "Cleric")
            {
                var effects = new List<string>
                {
                    $"• +{config.roleSettings.ClericHealthBonus:0.##}% Max Health",
                    $"• -{config.roleSettings.ClericDefensePenalty:0.##}% Defense",
                    $"• Radiates protective aura (radius: {config.roleSettings.ClericAuraRadius:0.#} pixels)",
                    $"• Teammates in aura: +{config.roleSettings.ClericTeammateHealthBonus:0.##}% max health",
                    $"• Self regeneration: {config.roleSettings.ClericSelfRegenPercent:0.##}% max health per {config.roleSettings.ClericRegenInterval:0.##}s",
                    $"• Teammate regeneration: {config.roleSettings.ClericTeammateRegenPercent:0.##}% max health per {config.roleSettings.ClericRegenInterval:0.##}s",
                    $"• Divine Intervention: Grants debuff immunity to team within aura for {config.roleSettings.DivineInterventionDuration:0.##}s"
                };

                if (config.roleSettings.ClericDisableVitRegen)
                {
                    effects.Add("• VIT regeneration effects disabled for balance");
                }

                return string.Join("\n", effects);
            }

            return "No effects defined.";
        }
    }
}