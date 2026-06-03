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
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.CritGod.CritChance", config.roleSettings.CritGodCritChance),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.CritGod.ExcessCrit", config.roleSettings.CritGodExcessCritToDamage.ToString("0.##"))
                };

                if (config.roleSettings.CritGodEnableSummonCrits)
                {
                    effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.CritGod.SummonCrits"));
                }

                return string.Join("\n", effects);
            }

            if (ID == "Vampire")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Vampire.MaxHealth", config.roleSettings.VampireHealthBonus.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Vampire.MoveSpeed", config.roleSettings.VampireMovementSpeed.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Vampire.BleedChance", config.roleSettings.VampireBleedChance.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Vampire.BleedDamage", config.roleSettings.VampireBleedDamagePercent.ToString("0.##"), config.roleSettings.VampireBleedTickInterval.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Vampire.BleedHeal", config.roleSettings.VampireBleedHealPercent.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Vampire.KillHeal", config.roleSettings.VampireKillHealPercent.ToString("0.##"))
                };

                return string.Join("\n", effects);
            }

            if (ID == "Beastmaster")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Beastmaster.DamagePerMinion", config.roleSettings.BeastmasterDamagePerUniqueMinion.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Beastmaster.BonusSlots", config.roleSettings.BeastmasterBonusSlotsGained, (config.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : ""), config.roleSettings.BeastmasterSlotsPerBonusSlot)
                };

                if (config.roleSettings.BeastmasterReduceSPRSlotEfficiency)
                {
                    int sprRequirement = (int)(config.roleSettings.BeastmasterSlotsPerBonusSlot * config.roleSettings.BeastmasterSPRSlotPenaltyMultiplier);
                    effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Beastmaster.SPRSlotsReduced", config.roleSettings.BeastmasterBonusSlotsGained, (config.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : ""), sprRequirement));
                }
                else
                {
                    effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Beastmaster.SPRSlotsNormal", config.roleSettings.BeastmasterBonusSlotsGained, (config.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : ""), config.roleSettings.BeastmasterSlotsPerBonusSlot));
                }

                return string.Join("\n", effects);
            }

            if (ID == "ApexSummoner")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.ApexSummoner.MassiveBonus"),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.ApexSummoner.DamagePerUnusedSlot", config.roleSettings.ApexSummonerDamagePerUnusedSlot.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.ApexSummoner.BonusLost")
                };

                return string.Join("\n", effects);
            }

            if (ID == "BlackKnight")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.MeleeScaleINT", config.roleSettings.BlackKnightINTToMeleeDamage.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.MagicScaleSTR", config.roleSettings.BlackKnightSTRToMagicDamage.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.DarkFocusStacks", config.roleSettings.BlackKnightMaxDarkFocusStacks),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.ConsumeDarkFocus", config.roleSettings.BlackKnightDarkFocusCritChancePerStack.ToString("0.##"), config.roleSettings.BlackKnightDarkFocusCritDamagePerStack.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.ManaRestore", config.roleSettings.BlackKnightManaRestoreOnMeleeCrit),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.ArcaneSurge", config.roleSettings.BlackKnightArcaneSurgeMagicDamage.ToString("0.##"), config.roleSettings.BlackKnightArcaneSurgeDuration.ToString("0.##"))
                };

                if (config.roleSettings.BlackKnightArcaneSurgeScaleWithDamage)
                {
                    effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.BlackKnight.ArcaneSurgeScale", config.roleSettings.BlackKnightArcaneSurgeDamageScaling.ToString("0.##")));
                }

                return string.Join("\n", effects);
            }

            if (ID == "Cleric")
            {
                var rpg = Main.LocalPlayer.GetModPlayer<RPGPlayer>();
                bool isAngel = rpg.AscendedRoles.Contains("Cleric");

                if (isAngel)
                {
                    var effects = new List<string>
                    {
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.MaxHealth", config.roleSettings.AngelHealthBonus.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.DefensePenalty", config.roleSettings.AngelDefensePenalty.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.ProtectAura", config.roleSettings.AngelAuraRadius.ToString("0.#")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.AuraHealth", config.roleSettings.AngelTeammateHealthBonus.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.SelfRegen", config.roleSettings.AngelSelfRegenPercent.ToString("0.##"), config.roleSettings.AngelRegenInterval.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.TeamRegen", config.roleSettings.AngelTeammateRegenPercent.ToString("0.##"), config.roleSettings.AngelRegenInterval.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.Wings", config.roleSettings.AngelInAirMoveSpeedBonus.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.SoulAnchor", config.roleSettings.AngelSoulAnchorDamageReduction.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Angel.DivineResurrection", config.roleSettings.AngelResurrectionHealPercent.ToString("0.##"), config.roleSettings.AngelResurrectionInvulTime.ToString("0.##"), config.roleSettings.AngelResurrectionCooldown.ToString("0.##"))
                    };
                    return string.Join("\n", effects);
                }
                else
                {
                    var effects = new List<string>
                    {
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.MaxHealth", config.roleSettings.ClericHealthBonus.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.DefensePenalty", config.roleSettings.ClericDefensePenalty.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.ProtectAura", config.roleSettings.ClericAuraRadius.ToString("0.#")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.AuraHealth", config.roleSettings.ClericTeammateHealthBonus.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.SelfRegen", config.roleSettings.ClericSelfRegenPercent.ToString("0.##"), config.roleSettings.ClericRegenInterval.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.TeamRegen", config.roleSettings.ClericTeammateRegenPercent.ToString("0.##"), config.roleSettings.ClericRegenInterval.ToString("0.##")),
                        Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.DivineIntervention", config.roleSettings.DivineInterventionDuration.ToString("0.##"))
                    };

                    if (config.roleSettings.ClericDisableVitRegen)
                    {
                        effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Cleric.DisabledRegen"));
                    }

                    return string.Join("\n", effects);
                }
            }


            if (ID == "Guardian")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.MaxHealth", config.roleSettings.GuardianHealthBonus.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.Defense", config.roleSettings.GuardianDefenseBonus),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.MoveSpeedPenalty", config.roleSettings.GuardianMovementSpeedPenalty.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.DamagePenalty", config.roleSettings.GuardianDamagePenalty.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.DamageReduction", config.roleSettings.GuardianDamageReduction.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.Aura", config.roleSettings.GuardianAuraRadius.ToString("0.#")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.AuraDefense", config.roleSettings.GuardianTeammateDefenseBonus.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.AuraReduction", config.roleSettings.GuardianTeammateDamageReduction.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.ImmuneKnockback")
                };

                if (config.roleSettings.GuardianReduceVitEffects && config.roleSettings.GuardianVitEffectReduction > 0)
                {
                    effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.ReducedVIT", config.roleSettings.GuardianVitEffectReduction.ToString("0.##")));
                }

                if (config.roleSettings.GuardianDisableEndEffects)
                {
                    effects.Add(Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Guardian.DisabledEND"));
                }

                return string.Join("\n", effects);
            }

            if (ID == "Necromancer")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Necromancer.SoulReserve", config.roleSettings.NecromancerBaseSoulCapacity, config.roleSettings.NecromancerSPRPerSoul),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Necromancer.SoulDuration", config.roleSettings.NecromancerBaseSoulDuration, config.roleSettings.NecromancerSoulDurationPerSPR),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Necromancer.ThrallsLimit", config.roleSettings.NecromancerActiveThrallsLimit),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Necromancer.BoneArmor", config.roleSettings.NecromancerBoneArmorDRPerThrall),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Necromancer.ThrallDamage", config.roleSettings.NecromancerThrallBaseDamage, config.roleSettings.NecromancerThrallINTScale)
                };

                return string.Join("\n", effects);
            }

            if (ID == "Berserker")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Berserker.BloodbathDmg", config.roleSettings.BerserkerBloodbathMaxDamageBonus.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Berserker.BloodbathSpeed", config.roleSettings.BerserkerBloodbathMaxSpeedBonus.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Berserker.BloodbathImmunity", config.roleSettings.BerserkerBloodbathImmunityThreshold.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Berserker.SavageRoar", config.roleSettings.BerserkerSavageRoarDuration.ToString("0.##"), config.roleSettings.BerserkerSavageRoarCooldown.ToString("0.##"))
                };

                return string.Join("\n", effects);
            }

            if (ID == "Spellweaver")
            {
                var effects = new List<string>
                {
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Spellweaver.ManaAegis", config.roleSettings.SpellweaverManaAegisPercent.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Spellweaver.ManaCrit", config.roleSettings.SpellweaverManaCritRestorePercent.ToString("0.##")),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.Spellweaver.Discharge", config.roleSettings.SpellweaverMaxElementalCharge.ToString("0.##"), config.roleSettings.SpellweaverElementalDischargeBaseMult.ToString("0.##"), config.roleSettings.SpellweaverElementalDischargeINTScale.ToString("0.##"))
                };

                return string.Join("\n", effects);
            }

            return Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleEffects.NoEffects");
        }
    }
}