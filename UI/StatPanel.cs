using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using System;
using Terraria.Audio;
using Terraria.ID;
using ReLogic.Graphics;
using Terraria.GameContent;
using System.Collections.Generic;
using System.Linq;

namespace Stataria
{
    public class StatPanel : UIState
    {
        public UIPanel statPanel;
        private UIText levelText;
        private UIText xpText;
        private UIText statPointsText;

        private UIPanel tooltipPanel;
        private UIText tooltipText;

        private UIText[] statTexts;
        private UITextPanel<string>[] plusButtons;
        private UITextPanel<string>[] minusButtons;
        private Dictionary<string, CheckBoxElement> autoCheckboxes = new Dictionary<string, CheckBoxElement>();
        private UITextPanel<LocalizedText> autoButton;
        private bool autoAllocationEnabled = false;

        private UITextPanel<LocalizedText> resetButton;
        private float[] holdTimers;
        private float[] holdTimersDown;
        private const float buttonRepeatDelay = 0.15f;

        private bool dragging = false;
        private Vector2 offset;

        private BulkAllocationManager bulkManager;

        private UITextPanel<LocalizedText> rebirthButton;
        private bool rebirthConfirmationShown = false;
        private UIText rebirthConfirmationText;
        private float rebirthConfirmationY;
        private bool requirementMessageShown = false;
        private float requirementMessageTimer = 0f;
        private const float RequirementMessageDuration = 3f;
        private UIText rebirthPointsText;

        private class StatDefinition
        {
            public string Name { get; set; }
            public Func<RPGPlayer, int> GetValue { get; set; }
            public Action<RPGPlayer, int> SetValue { get; set; }
            public Func<StatariaConfig, int> GetCap { get; set; }
            public Func<bool> IsModLoaded { get; set; } = () => true;
            public Func<StatariaConfig, string> GetTooltip { get; set; }

            public string GetDisplayText(RPGPlayer player)
            {
                return Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatLabel", Name, GetValue(player));
            }
        }

        private List<StatDefinition> statDefinitions = new List<StatDefinition>();

        private void InitializeStatDefinitions()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            statDefinitions.Clear();

            statDefinitions.Add(new StatDefinition
            {
                Name = "VIT",
                GetValue = player => player.VIT,
                SetValue = (player, value) => player.VIT = value,
                GetCap = cfg => cfg.statSettings.VIT_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveVIT = rpg.GetEffectiveStat("VIT");
                    var tooltips = new List<string>();

                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_MaxHealth", effectiveVIT * cfg.statSettings.VIT_HP, cfg.statSettings.VIT_HP));

                    bool isCleric = rpg.ActiveRole?.ID == "Cleric" && rpg.ActiveRole.Status == RoleStatus.Active;
                    bool isGuardian = rpg.ActiveRole?.ID == "Guardian" && rpg.ActiveRole.Status == RoleStatus.Active;

                    if (isCleric && cfg.roleSettings.ClericDisableVitRegen)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_RegenDisabled"));
                    }
                    else if (isGuardian && cfg.roleSettings.GuardianReduceVitEffects)
                    {
                        float reductionFactor = 1f - (cfg.roleSettings.GuardianVitEffectReduction / 100f);
                        if (cfg.statSettings.UseCustomHpRegen)
                        {
                            float reducedRegen = effectiveVIT * cfg.statSettings.CustomHpRegenPerVIT * reductionFactor;
                            tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_HpSecReduced", reducedRegen.ToString("0.#"), (cfg.statSettings.CustomHpRegenPerVIT * reductionFactor).ToString("0.#")));
                        }
                        else
                        {
                            float reducedRegen = effectiveVIT * 0.5f * reductionFactor;
                            tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_LifeRegenReduced", reducedRegen.ToString("0.#"), (0.5f * reductionFactor).ToString("0.#")));
                        }
                    }
                    else
                    {
                        if (cfg.statSettings.UseCustomHpRegen)
                        {
                            tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_HpSec", (effectiveVIT * cfg.statSettings.CustomHpRegenPerVIT).ToString("0.#"), cfg.statSettings.CustomHpRegenPerVIT.ToString("0.#")));
                        }
                        else
                        {
                            tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_LifeRegen", (effectiveVIT * 0.5f).ToString("0.#")));
                        }
                    }

                    if (cfg.statSettings.EnableHealingPotionBoost)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.VIT_Healing", (effectiveVIT * cfg.statSettings.HealingPotionBoostPercent).ToString("0.#"), cfg.statSettings.HealingPotionBoostPercent.ToString("0.#")));
                    }
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "STR",
                GetValue = player => player.STR,
                SetValue = (player, value) => player.STR = value,
                GetCap = cfg => cfg.statSettings.STR_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveSTR = rpg.GetEffectiveStat("STR");
                    var tooltips = new List<string>();

                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.STR_MeleeDamage", (effectiveSTR * cfg.statSettings.STR_Damage).ToString("0.#"), cfg.statSettings.STR_Damage.ToString("0.#")));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.STR_MeleeKnockback", (effectiveSTR * cfg.statSettings.STR_Knockback).ToString("0.#"), cfg.statSettings.STR_Knockback.ToString("0.#")));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.STR_MeleeArmorPen", effectiveSTR * cfg.statSettings.STR_ArmorPen, cfg.statSettings.STR_ArmorPen));

                    bool isBlackKnight = rpg.ActiveRole?.ID == "BlackKnight" && rpg.ActiveRole.Status == RoleStatus.Active;
                    if (isBlackKnight)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.STR_BlackKnight", (effectiveSTR * cfg.roleSettings.BlackKnightSTRToMagicDamage).ToString("0.#"), cfg.roleSettings.BlackKnightSTRToMagicDamage.ToString("0.#")));
                    }

                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "AGI",
                GetValue = player => player.AGI,
                SetValue = (player, value) => player.AGI = value,
                GetCap = cfg => cfg.statSettings.AGI_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveAGI = rpg.GetEffectiveStat("AGI");
                    float diminishedAGI = effectiveAGI <= 50 ? effectiveAGI : 50 + (effectiveAGI - 50) * 0.5f;
                    return Language.GetTextValue("Mods.Stataria.UI.StatPanel.AGI_MoveSpeed", (diminishedAGI * (cfg.statSettings.AGI_MoveSpeed / 100f)).ToString("P1"), (cfg.statSettings.AGI_MoveSpeed / 100f).ToString("P1")) + "\n" +
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.AGI_AttackSpeed", (diminishedAGI * (cfg.statSettings.AGI_AttackSpeed / 100f)).ToString("P1"), (cfg.statSettings.AGI_AttackSpeed / 100f).ToString("P1")) + "\n" +
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.AGI_WingTime", effectiveAGI * cfg.statSettings.AGI_WingTime, cfg.statSettings.AGI_WingTime) + "\n" +
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.AGI_ImprovedJump");
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "INT",
                GetValue = player => player.INT,
                SetValue = (player, value) => player.INT = value,
                GetCap = cfg => cfg.statSettings.INT_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveINT = rpg.GetEffectiveStat("INT");
                    float rawReduction = effectiveINT * cfg.statSettings.INT_ManaCostReduction / 100f;
                    float diminishingReduction = 1f - (1f / (1f + rawReduction));
                    var tooltips = new List<string>();

                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.INT_MaxMana", effectiveINT * cfg.statSettings.INT_MP, cfg.statSettings.INT_MP));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.INT_ManaRegen", (effectiveINT * 0.5f).ToString("0.#")));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.INT_MagicDamage", (effectiveINT * cfg.statSettings.INT_Damage).ToString("0.#"), cfg.statSettings.INT_Damage.ToString("0.#")));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.INT_ManaCost", diminishingReduction.ToString("P1")));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.INT_MagicArmorPen", effectiveINT * cfg.statSettings.INT_ArmorPen, cfg.statSettings.INT_ArmorPen));

                    bool isBlackKnight = rpg.ActiveRole?.ID == "BlackKnight" && rpg.ActiveRole.Status == RoleStatus.Active;
                    if (isBlackKnight)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.INT_BlackKnight", (effectiveINT * cfg.roleSettings.BlackKnightINTToMeleeDamage).ToString("0.#"), cfg.roleSettings.BlackKnightINTToMeleeDamage.ToString("0.#")));
                    }

                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "LUC",
                GetValue = player => player.LUC,
                SetValue = (player, value) => player.LUC = value,
                GetCap = cfg => cfg.statSettings.LUC_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveLUC = rpg.GetEffectiveStat("LUC");
                    var tooltips = new List<string>
                    {
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.LUC_Crit", (effectiveLUC * cfg.statSettings.LUC_Crit).ToString("0.#"), cfg.statSettings.LUC_Crit.ToString("0.#"))
                    };
                    if (cfg.statSettings.LUC_EnableLuckBonus)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.LUC_Luck", (effectiveLUC * cfg.statSettings.LUC_LuckBonus).ToString("0.##"), cfg.statSettings.LUC_LuckBonus.ToString("0.##")));
                    }
                    if (cfg.statSettings.LUC_EnableFishing)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.LUC_Fishing", effectiveLUC * cfg.statSettings.LUC_Fishing, cfg.statSettings.LUC_Fishing));
                    }
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.LUC_Aggro", effectiveLUC * cfg.statSettings.LUC_AggroReduction, cfg.statSettings.LUC_AggroReduction));
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "END",
                GetValue = player => player.END,
                SetValue = (player, value) => player.END = value,
                GetCap = cfg => cfg.statSettings.END_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveEND = rpg.GetEffectiveStat("END");
                    var tooltips = new List<string>();
                    bool isGuardian = rpg.ActiveRole?.ID == "Guardian" && rpg.ActiveRole.Status == RoleStatus.Active;

                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_Defense", (effectiveEND * cfg.statSettings.END_Defense).ToString("0.#"), cfg.statSettings.END_Defense.ToString("0.#")));

                    if (isGuardian)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_ImmuneKnockback"));
                    }
                    else if (cfg.statSettings.EnableKnockbackResist)
                    {
                        float knockbackResist = Math.Min(effectiveEND * cfg.statSettings.END_KnockbackResistPerPoint, 100f);
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_KnockbackResist", knockbackResist.ToString("0.#"), cfg.statSettings.END_KnockbackResistPerPoint.ToString("0.#")));
                    }

                    if (isGuardian && cfg.roleSettings.GuardianDisableEndEffects)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_DamageReductionDisabled"));
                    }
                    else if (cfg.statSettings.EnableDR)
                    {
                        float drPercent = 100f * (1f - (1f / (1f + effectiveEND * (config.statSettings.END_DRPerPoint / 100f))));
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_DamageReduction", drPercent.ToString("0.#")));
                    }

                    if (cfg.statSettings.EnableEnemyKnockback)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_KnocksBackNonBosses"));
                    }
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.END_Aggro", effectiveEND * cfg.statSettings.END_Aggro, cfg.statSettings.END_Aggro));
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "POW",
                GetValue = player => player.POW,
                SetValue = (player, value) => player.POW = value,
                GetCap = cfg => cfg.statSettings.POW_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectivePOW = rpg.GetEffectiveStat("POW");
                    var tooltips = new List<string>
                    {
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.POW_GeneralDamage", (effectivePOW * cfg.statSettings.POW_Damage).ToString("0.#"), cfg.statSettings.POW_Damage.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.POW_OtherDamage", (effectivePOW * 0.1f).ToString("0.#"))
                    };
                    if (cfg.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.POW_RageDamage", (effectivePOW * cfg.modIntegration.POW_RageDamage).ToString("0.#"), cfg.modIntegration.POW_RageDamage.ToString("0.#")));
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.POW_RageDuration", (Math.Min(effectivePOW * cfg.modIntegration.POW_RageDuration, cfg.modIntegration.POW_MaxRageDurationBonus) / 60f).ToString("0.#"), (cfg.modIntegration.POW_RageDuration / 60f).ToString("0.#"), (cfg.modIntegration.POW_MaxRageDurationBonus / 60f).ToString("0.#")));
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.POW_AdrenalineDuration", (effectivePOW * cfg.modIntegration.POW_AdrenalineDuration / 60f).ToString("0.#"), (cfg.modIntegration.POW_AdrenalineDuration / 60f).ToString("0.#")));
                    }
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "DEX",
                GetValue = player => player.DEX,
                SetValue = (player, value) => player.DEX = value,
                GetCap = cfg => cfg.statSettings.DEX_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveDEX = rpg.GetEffectiveStat("DEX");
                    return Language.GetTextValue("Mods.Stataria.UI.StatPanel.DEX_RangedDamage", (effectiveDEX * cfg.statSettings.DEX_Damage).ToString("0.#"), cfg.statSettings.DEX_Damage.ToString("0.#")) + "\n" +
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.DEX_RangedArmorPen", effectiveDEX * cfg.statSettings.DEX_ArmorPen, cfg.statSettings.DEX_ArmorPen) + "\n" +
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.DEX_AmmoSave", (effectiveDEX * cfg.statSettings.DEX_AmmoConservation).ToString("0.#"), cfg.statSettings.DEX_AmmoConservation.ToString("0.#"));
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "SPR",
                GetValue = player => player.SPR,
                SetValue = (player, value) => player.SPR = value,
                GetCap = cfg => cfg.statSettings.SPR_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveSPR = rpg.GetEffectiveStat("SPR");
                    int minionSlots = effectiveSPR / cfg.statSettings.SPR_MinionsPerX;
                    int sentrySlots = effectiveSPR / cfg.statSettings.SPR_SentriesPerX;
                    var tooltips = new List<string>();

                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.SPR_SummonDamage", (effectiveSPR * cfg.statSettings.SPR_Damage).ToString("0.#"), cfg.statSettings.SPR_Damage.ToString("0.#")));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.SPR_MinionSlots", minionSlots, (minionSlots != 1 ? "s" : ""), cfg.statSettings.SPR_MinionsPerX));
                    tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.SPR_SentrySlots", sentrySlots, (sentrySlots != 1 ? "s" : ""), cfg.statSettings.SPR_SentriesPerX));

                    bool isBeastmaster = rpg.ActiveRole?.ID == "Beastmaster" && rpg.ActiveRole.Status == RoleStatus.Active;
                    bool isApexSummoner = rpg.ActiveRole?.ID == "ApexSummoner" && rpg.ActiveRole.Status == RoleStatus.Active;

                    if (isBeastmaster)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.SPR_BeastmasterSlots", cfg.roleSettings.BeastmasterBonusSlotsGained, (cfg.roleSettings.BeastmasterBonusSlotsGained > 1 ? "s" : ""), cfg.roleSettings.BeastmasterSlotsPerBonusSlot));
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.SPR_BeastmasterDamage", cfg.roleSettings.BeastmasterDamagePerUniqueMinion.ToString("0.#")));
                    }

                    if (isApexSummoner)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.SPR_ApexSummonerDamage", cfg.roleSettings.ApexSummonerDamagePerUnusedSlot.ToString("0.#")));
                    }

                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "TCH",
                GetValue = player => player.TCH,
                SetValue = (player, value) => player.TCH = value,
                GetCap = cfg => cfg.statSettings.TCH_Cap,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveTCH = rpg.GetEffectiveStat("TCH");
                    var tooltips = new List<string>();
                    if (cfg.statSettings.TCH_EnableMiningSpeed)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.TCH_MiningSpeed", (effectiveTCH * cfg.statSettings.TCH_MiningSpeed).ToString("0.#"), cfg.statSettings.TCH_MiningSpeed.ToString("0.#")));
                    }
                    if (cfg.statSettings.TCH_EnableBuildSpeed)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.TCH_BuildSpeed", (effectiveTCH * cfg.statSettings.TCH_BuildSpeed).ToString("0.#"), cfg.statSettings.TCH_BuildSpeed.ToString("0.#")));
                    }
                    if (cfg.statSettings.TCH_EnableRange)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.TCH_TilesReach", effectiveTCH * cfg.statSettings.TCH_Range, cfg.statSettings.TCH_Range));
                    }
                    return tooltips.Count > 0 ? string.Join("\n", tooltips) : Language.GetTextValue("Mods.Stataria.UI.StatPanel.TCH_NoEffects");
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "RGE",
                GetValue = player => player.RGE,
                SetValue = (player, value) => player.RGE = value,
                GetCap = cfg => cfg.statSettings.RGE_Cap,
                IsModLoaded = () => config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveRGE = rpg.GetEffectiveStat("RGE");
                    var tooltips = new List<string>
                    {
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_RogueDamage", (effectiveRGE * cfg.modIntegration.RGE_Damage).ToString("0.#"), cfg.modIntegration.RGE_Damage.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_MaxStealth", (effectiveRGE * cfg.modIntegration.RGE_MaxStealthPerPoint).ToString("0.#"), cfg.modIntegration.RGE_MaxStealthPerPoint.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_Velocity", (effectiveRGE * cfg.modIntegration.RGE_Velocity).ToString("0.#"), cfg.modIntegration.RGE_Velocity.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_AmmoCost", (effectiveRGE * cfg.modIntegration.RGE_AmmoConsumptionReduction).ToString("0.#"), cfg.modIntegration.RGE_AmmoConsumptionReduction.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_StealthRegen", (effectiveRGE * cfg.modIntegration.RGE_StealthRegenBonus).ToString("0.#"), cfg.modIntegration.RGE_StealthRegenBonus.ToString("0.#"))
                    };
                    if (cfg.modIntegration.RGE_EnableStealthConsumptionReduction)
                    {
                        if (effectiveRGE >= cfg.modIntegration.RGE_StealthConsumptionReductionThreshold) tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_Stealth50"));
                        else if (effectiveRGE >= cfg.modIntegration.RGE_StealthConsumption75Threshold) tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_Stealth75"));
                        else if (effectiveRGE >= cfg.modIntegration.RGE_StealthConsumption90Threshold) tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.RGE_Stealth90"));
                    }
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "BRD",
                GetValue = player => player.BRD,
                SetValue = (player, value) => player.BRD = value,
                GetCap = cfg => cfg.statSettings.BRD_Cap,
                IsModLoaded = () => config.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveBRD = rpg.GetEffectiveStat("BRD");
                    var tooltips = new List<string>
                    {
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.BRD_SymphonicDamage", (effectiveBRD * cfg.modIntegration.BRD_Damage).ToString("0.#"), cfg.modIntegration.BRD_Damage.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.BRD_SymphonicArmorPen", effectiveBRD * cfg.modIntegration.BRD_ArmorPen, cfg.modIntegration.BRD_ArmorPen)
                    };
                    if (cfg.modIntegration.BRD_PointsPerMaxInspiration > 0)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.BRD_MaxInspiration", cfg.modIntegration.BRD_PointsPerMaxInspiration));
                    }
                    if (cfg.modIntegration.BRD_EnableEmpowermentBoost && cfg.modIntegration.BRD_EmpowermentDuration > 0)
                    {
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.BRD_EmpowermentDuration", (effectiveBRD * cfg.modIntegration.BRD_EmpowermentDuration).ToString("0.#"), cfg.modIntegration.BRD_EmpowermentDuration.ToString("0.#")));
                    }
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "HLR",
                GetValue = player => player.HLR,
                SetValue = (player, value) => player.HLR = value,
                GetCap = cfg => cfg.statSettings.HLR_Cap,
                IsModLoaded = () => config.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveHLR = rpg.GetEffectiveStat("HLR");
                    var tooltips = new List<string>
                    {
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.HLR_RadiantDamage", (effectiveHLR * cfg.modIntegration.HLR_Damage).ToString("0.#"), cfg.modIntegration.HLR_Damage.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.HLR_RadiantArmorPen", effectiveHLR * cfg.modIntegration.HLR_ArmorPen, cfg.modIntegration.HLR_ArmorPen)
                    };
                    if (cfg.modIntegration.HLR_PointsPerEffectPoint > 0)
                    {
                        int effectivePoints = effectiveHLR / cfg.modIntegration.HLR_PointsPerEffectPoint;
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.HLR_HealingPower", effectivePoints * cfg.modIntegration.HLR_HealingPower, cfg.modIntegration.HLR_HealingPower, cfg.modIntegration.HLR_PointsPerEffectPoint));
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.HLR_ImprovedLifeRecovery"));
                    }
                    return string.Join("\n", tooltips);
                }
            });

            statDefinitions.Add(new StatDefinition
            {
                Name = "CLK",
                GetValue = player => player.CLK,
                SetValue = (player, value) => player.CLK = value,
                GetCap = cfg => cfg.statSettings.CLK_Cap,
                IsModLoaded = () => config.modIntegration.EnableClickerClassIntegration && ClickerSupportHelper.ClickerClassLoaded,
                GetTooltip = cfg =>
                {
                    var player = Main.LocalPlayer;
                    var rpg = player.GetModPlayer<RPGPlayer>();
                    int effectiveCLK = rpg.GetEffectiveStat("CLK");
                    var tooltips = new List<string>
                    {
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.CLK_ClickDamage", (effectiveCLK * cfg.modIntegration.CLK_Damage).ToString("0.#"), cfg.modIntegration.CLK_Damage.ToString("0.#")),
                        Language.GetTextValue("Mods.Stataria.UI.StatPanel.CLK_ClickRadius", (effectiveCLK * cfg.modIntegration.CLK_Radius).ToString("0.#"), cfg.modIntegration.CLK_Radius.ToString("0.#"))
                    };
                    float perPointFactor = cfg.modIntegration.CLK_EffectThreshold / 100f;
                    float linearReduction = effectiveCLK * perPointFactor;
                    if (linearReduction > 0)
                    {
                        float effectiveReduction = 100f * (1f - (1f / (1f + linearReduction)));
                        tooltips.Add(Language.GetTextValue("Mods.Stataria.UI.StatPanel.CLK_EffectsDiminishing", effectiveReduction.ToString("0.#")));
                    }
                    return string.Join("\n", tooltips);
                }
            });
            
            GenericModSupportHelper.Initialize();
            var visibleMods = GenericModSupportHelper.GetVisibleMods();

            foreach (var modDef in visibleMods)
            {
                string statName = modDef.StatName;
                string displayName = modDef.DisplayName;
                
                statDefinitions.Add(new StatDefinition
                {
                    Name = statName,
                    GetValue = player => GetGenericModStatValue(player, statName),
                    SetValue = (player, value) => SetGenericModStatValue(player, statName, value),
                    GetCap = cfg => GetGenericModStatCap(cfg, statName),
                    IsModLoaded = () => modDef.ShouldShowStat(),
                    GetTooltip = cfg => GetGenericModTooltip(cfg, statName, displayName)
                });
            }
        }

        private static int GetGenericModStatValue(RPGPlayer player, string statName)
        {
            return statName switch
            {
                "BLH" => player.BLH,
                "HNT" => player.HNT,
                "GMB" => player.GMB,
                "SHM" => player.SHM,
                "THR" => player.THR,
                _ => 0
            };
        }

        private static void SetGenericModStatValue(RPGPlayer player, string statName, int value)
        {
            switch (statName)
            {
                case "BLH": player.BLH = value; break;
                case "HNT": player.HNT = value; break;
                case "GMB": player.GMB = value; break;
                case "SHM": player.SHM = value; break;
                case "THR": player.THR = value; break;
            }
        }

        private static int GetGenericModStatCap(StatariaConfig cfg, string statName)
        {
            return statName switch
            {
                "BLH" => cfg.statSettings.BLH_Cap,
                "HNT" => cfg.statSettings.HNT_Cap,
                "GMB" => cfg.statSettings.GMB_Cap,
                "SHM" => cfg.statSettings.SHM_Cap,
                "THR" => cfg.statSettings.THR_Cap,
                _ => 1000
            };
        }

        private static string GetGenericModTooltip(StatariaConfig cfg, string statName, string displayName)
        {
            var player = Main.LocalPlayer;
            var rpg = player.GetModPlayer<RPGPlayer>();
            int effectiveStat = rpg.GetEffectiveStat(statName);
            float damageBonus = rpg.GetGenericModDamageBonus(statName, cfg);
            
            return Language.GetTextValue("Mods.Stataria.UI.StatPanel.Generic_Damage", (damageBonus * 100f).ToString("0.#"), displayName, GetDamagePerPoint(cfg, statName).ToString("0.#"));
        }

        private static float GetDamagePerPoint(StatariaConfig cfg, string statName)
        {
            return statName switch
            {
                "BLH" => cfg.modIntegration.BLH_Damage,
                "HNT" => cfg.modIntegration.HNT_Damage,
                "GMB" => cfg.modIntegration.GMB_Damage,
                "SHM" => cfg.modIntegration.SHM_Damage,
                "THR" => cfg.modIntegration.THR_Damage,
                _ => 0.5f
            };
        }

        private List<StatDefinition> GetActiveStats()
        {
            return statDefinitions.Where(stat => stat.IsModLoaded()).ToList();
        }

        public override void OnInitialize()
        {
            InitializeStatDefinitions();

            statPanel = new UIPanel();

            float baseWidth = 340f;
            float heightPerStat = 35f;

            var config = ModContent.GetInstance<StatariaConfig>();
            var activeStats = GetActiveStats();

            int totalStats = activeStats.Count;
            int maxStatsPerColumn = 10;
            int numColumns = (int)Math.Ceiling((float)totalStats / maxStatsPerColumn);
            int numRows = Math.Min(totalStats, maxStatsPerColumn);

            float columnWidth = baseWidth;
            float totalWidth = columnWidth * numColumns;

            statPanel.Width.Set(totalWidth, 0f);
            statPanel.HAlign = 0.5f;
            statPanel.VAlign = 0.5f;

            statPanel.SetPadding(0);
            statPanel.BackgroundColor = new Color(63, 82, 151, 200);
            statPanel.BorderColor = new Color(0, 0, 0, 255);
            Append(statPanel);

            statPanel.OnLeftMouseDown += (evt, el) =>
            {
                if (!IsClickingOnInteractiveElement(evt.MousePosition))
                {
                    offset = new Vector2(evt.MousePosition.X - statPanel.Left.Pixels, evt.MousePosition.Y - statPanel.Top.Pixels);
                    dragging = true;
                }
            };
            statPanel.OnLeftMouseUp += (evt, el) =>
            {
                dragging = false;
            };

            float top = 10f;

            levelText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.LevelText").WithFormatArgs(1));
            levelText.Top.Set(top, 0f);
            levelText.Left.Set(10f, 0f);
            statPanel.Append(levelText);
            levelText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            levelText.OnMouseOut += (evt, el) => HideTooltip();

            statPointsText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.PointsText").WithFormatArgs(0));
            statPointsText.Top.Set(top, 0f);
            statPointsText.Left.Set(statPanel.Width.Pixels - 120f, 0f);
            statPanel.Append(statPointsText);

            top += 30f;

            xpText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.XPText").WithFormatArgs(0, 100));
            xpText.Top.Set(top, 0f);
            xpText.Left.Set(10f, 0f);
            statPanel.Append(xpText);
            xpText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            xpText.OnMouseOut += (evt, el) => HideTooltip();

            rebirthPointsText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.RPText").WithFormatArgs(0));
            rebirthPointsText.Top.Set(top, 0f);
            rebirthPointsText.Left.Set(statPanel.Width.Pixels - 120f, 0f);
            rebirthPointsText.TextColor = new Color(190, 120, 190);
            statPanel.Append(rebirthPointsText);

            top += 40f;

            statTexts = new UIText[totalStats];
            plusButtons = new UITextPanel<string>[totalStats];
            minusButtons = new UITextPanel<string>[totalStats];
            holdTimers = new float[totalStats];
            holdTimersDown = new float[totalStats];
            autoCheckboxes = new Dictionary<string, CheckBoxElement>();

            for (int i = 0; i < totalStats; i++)
            {
                int column = i / maxStatsPerColumn;
                int row = i % maxStatsPerColumn;

                float columnOffset = column * columnWidth;
                float rowTop = top + (row * heightPerStat);

                var stat = activeStats[i];

                CheckBoxElement checkbox = new CheckBoxElement();
                checkbox.Top.Set(rowTop, 0f);
                checkbox.Left.Set(10f + columnOffset, 0f);
                string statName = stat.Name;
                checkbox.OnCheckChanged += (isChecked) =>
                {
                    var player = Main.LocalPlayer;
                    if (player == null || !player.active) return;

                    var rpg = player.GetModPlayer<RPGPlayer>();
                    if (isChecked)
                        rpg.AutoAllocateStats.Add(statName);
                    else
                        rpg.AutoAllocateStats.Remove(statName);

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        rpg.SyncPlayer(-1, player.whoAmI, false);
                };
                autoCheckboxes[stat.Name] = checkbox;
                statPanel.Append(checkbox);

                var statLabel = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.StatLabel").WithFormatArgs(stat.Name, 0));
                statLabel.Top.Set(rowTop + 5f, 0f);
                statLabel.Left.Set(40f + columnOffset, 0f);
                statPanel.Append(statLabel);
                statTexts[i] = statLabel;

                var minusBtn = new UITextPanel<string>("-", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 240f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                minusBtn.SetPadding(0f);
                int minusStatIndex = i;
                minusBtn.OnLeftClick += (evt, el) => OnStatDecrease(minusStatIndex);
                statPanel.Append(minusBtn);
                minusButtons[i] = minusBtn;

                var plusBtn = new UITextPanel<string>("+", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 290f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                plusBtn.SetPadding(0f);
                int localStatIndex = i;
                plusBtn.OnLeftClick += (evt, el) => OnStatIncrease(localStatIndex);
                statPanel.Append(plusBtn);
                plusButtons[i] = plusBtn;

                statLabel.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                statLabel.OnMouseOut += (evt, el) => HideTooltip();
                minusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                minusBtn.OnMouseOut += (evt, el) => HideTooltip();
                plusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                plusBtn.OnMouseOut += (evt, el) => HideTooltip();
                checkbox.OnMouseOver += (evt, el) => ShowTooltip(Language.GetTextValue("Mods.Stataria.UI.StatPanel.AutoAllocateTooltip"));
                checkbox.OnMouseOut += (evt, el) => HideTooltip();
            }

            float bottomControlsTop = top + (numRows * heightPerStat) + 10f;

            resetButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.StatPanel.ResetStats"), textScale: 0.9f, large: false)
            {
                Top = { Pixels = bottomControlsTop },
                Left = { Pixels = (totalWidth - 120f) / 2 },
                Width = { Pixels = 120f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(63, 82, 151, 200),
                BorderColor = new Color(0, 0, 0, 255)
            };
            resetButton.OnLeftClick += OnResetStats;
            resetButton.OnMouseOver += (evt, el) =>
            {
                ShowTooltip(Language.GetTextValue("Mods.Stataria.UI.StatPanel.ResetStatsTooltip"));
            };
            resetButton.OnMouseOut += (evt, el) => HideTooltip();
            statPanel.Append(resetButton);

            float rebirthButtonY = bottomControlsTop + 45f;
            if (config.rebirthSystem.EnableRebirthSystem)
            {
                rebirthButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.StatPanel.Rebirth"), textScale: 0.9f, large: false)
                {
                    Top = { Pixels = rebirthButtonY },
                    Left = { Pixels = (totalWidth - 120f) / 2 },
                    Width = { Pixels = 120f },
                    Height = { Pixels = 30f },
                    BackgroundColor = new Color(150, 90, 150, 200),
                    BorderColor = new Color(190, 120, 190, 255)
                };
                rebirthButton.OnLeftClick += OnRebirthButtonClick;
                statPanel.Append(rebirthButton);

                rebirthConfirmationText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.ConfirmRebirthPrompt"), 0.9f)
                {
                    Top = { Pixels = rebirthButtonY + 40f },
                    Left = { Pixels = 20f },
                    TextColor = Color.Red
                };
                rebirthConfirmationY = rebirthButtonY + 45f;
                bottomControlsTop = rebirthButtonY + 50f;
            }

            bulkManager = new BulkAllocationManager();
            float bulkBaseY;
            if (config.rebirthSystem.EnableRebirthSystem)
                bulkBaseY = bottomControlsTop;
            else
                bulkBaseY = bottomControlsTop + resetButton.Height.Pixels + 10f;

            bulkManager.Initialize(statPanel, bulkBaseY);

            autoButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.StatPanel.Auto"), textScale: 0.9f, large: false)
            {
                Top = { Pixels = bulkBaseY + 70f },
                Left = { Pixels = (totalWidth - 100f) / 2 },
                Width = { Pixels = 100f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(100, 100, 100, 200),
                BorderColor = new Color(150, 150, 150, 200)
            };
            autoButton.OnLeftClick += (evt, el) =>
            {
                var player = Main.LocalPlayer;
                if (player == null || !player.active) return;

                var rpg = player.GetModPlayer<RPGPlayer>();
                rpg.AutoAllocateEnabled = !rpg.AutoAllocateEnabled;
                autoAllocationEnabled = rpg.AutoAllocateEnabled;
                UpdateAutoButton();

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    rpg.SyncPlayer(-1, player.whoAmI, false);

                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            autoButton.OnMouseOver += (evt, el) => ShowTooltip(Language.GetTextValue("Mods.Stataria.UI.StatPanel.AutoTooltip"));
            autoButton.OnMouseOut += (evt, el) => HideTooltip();
            statPanel.Append(autoButton);

            float panelHeight = bulkBaseY + 120f;
            statPanel.Height.Set(panelHeight, 0f);
            statPanel.Recalculate();

            float tooltipY = panelHeight + 10f;
            tooltipPanel = new UIPanel();
            tooltipPanel.Width.Set(Math.Min(totalWidth - 20f, 600f), 0f);
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Left.Set(10f, 0f);
            tooltipPanel.Top.Set(tooltipY, 0f);
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            statPanel.Append(tooltipPanel);
            tooltipPanel.Recalculate();

            tooltipText = new UIText("", textScale: 1f);
            tooltipText.Width.Set(0, 1f);
            tooltipText.Top.Set(4f, 0f);
            tooltipText.Left.Set(4f, 0f);
            tooltipPanel.Append(tooltipText);
        }

        public void ReInitializePanel()
        {
            if (statDefinitions.Count == 0)
            {
                InitializeStatDefinitions();
            }

            var config = ModContent.GetInstance<StatariaConfig>();
            var activeStats = GetActiveStats();

            int totalStats = activeStats.Count;
            int maxStatsPerColumn = 10;
            int numColumns = (int)Math.Ceiling((float)totalStats / maxStatsPerColumn);
            int numRows = Math.Min(totalStats, maxStatsPerColumn);

            float columnWidth = 340f;
            float totalWidth = columnWidth * numColumns;
            float heightPerStat = 35f;

            statPanel.Width.Set(totalWidth, 0f);

            if (statTexts.Length != totalStats)
            {
                Array.Resize(ref statTexts, totalStats);
                Array.Resize(ref plusButtons, totalStats);
                Array.Resize(ref minusButtons, totalStats);
                Array.Resize(ref holdTimers, totalStats);
                Array.Resize(ref holdTimersDown, totalStats);
            }

            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            Dictionary<string, bool> checkedStats = new Dictionary<string, bool>();

            foreach (var kvp in autoCheckboxes)
            {
                checkedStats[kvp.Key] = kvp.Value.IsChecked;
            }

            autoCheckboxes.Clear();
            statPanel.RemoveAllChildren();

            float top = 10f;

            levelText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.LevelText").WithFormatArgs(1));
            levelText.Top.Set(top, 0f);
            levelText.Left.Set(10f, 0f);
            statPanel.Append(levelText);
            levelText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            levelText.OnMouseOut += (evt, el) => HideTooltip();

            statPointsText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.PointsText").WithFormatArgs(0));
            statPointsText.Top.Set(top, 0f);
            statPointsText.Left.Set(totalWidth - 120f, 0f);
            statPanel.Append(statPointsText);

            top += 30f;

            xpText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.XPText").WithFormatArgs(0, 100));
            xpText.Top.Set(top, 0f);
            xpText.Left.Set(10f, 0f);
            statPanel.Append(xpText);
            xpText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            xpText.OnMouseOut += (evt, el) => HideTooltip();

            rebirthPointsText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.RPText").WithFormatArgs(0));
            rebirthPointsText.Top.Set(top, 0f);
            rebirthPointsText.Left.Set(totalWidth - 120f, 0f);
            rebirthPointsText.TextColor = new Color(190, 120, 190);
            statPanel.Append(rebirthPointsText);

            top += 40f;

            for (int i = 0; i < totalStats; i++)
            {
                int column = i / maxStatsPerColumn;
                int row = i % maxStatsPerColumn;

                float columnOffset = column * columnWidth;
                float rowTop = top + (row * heightPerStat);

                var stat = activeStats[i];

                CheckBoxElement checkbox = new CheckBoxElement();
                checkbox.Top.Set(rowTop, 0f);
                checkbox.Left.Set(10f + columnOffset, 0f);
                string statName = stat.Name;
                checkbox.OnCheckChanged += (isChecked) =>
                {
                    var localPlayer = Main.LocalPlayer;
                    if (localPlayer == null || !localPlayer.active) return;

                    var localRpg = localPlayer.GetModPlayer<RPGPlayer>();
                    if (isChecked)
                        localRpg.AutoAllocateStats.Add(statName);
                    else
                        localRpg.AutoAllocateStats.Remove(statName);

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        localRpg.SyncPlayer(-1, localPlayer.whoAmI, false);
                };

                bool shouldBeChecked = rpg.AutoAllocateStats.Contains(statName);
                if (!shouldBeChecked && checkedStats.ContainsKey(statName))
                {
                    shouldBeChecked = checkedStats[statName];
                }
                checkbox.IsChecked = shouldBeChecked;

                autoCheckboxes[statName] = checkbox;
                statPanel.Append(checkbox);

                var statLabel = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.StatLabel").WithFormatArgs(statName, 0));
                statLabel.Top.Set(rowTop + 5f, 0f);
                statLabel.Left.Set(40f + columnOffset, 0f);
                statPanel.Append(statLabel);
                statTexts[i] = statLabel;

                var minusBtn = new UITextPanel<string>("-", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 240f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                minusBtn.SetPadding(0f);
                int minusStatIndex = i;
                minusBtn.OnLeftClick += (evt, el) => OnStatDecrease(minusStatIndex);
                statPanel.Append(minusBtn);
                minusButtons[i] = minusBtn;

                var plusBtn = new UITextPanel<string>("+", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 290f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                plusBtn.SetPadding(0f);
                int localStatIndex = i;
                plusBtn.OnLeftClick += (evt, el) => OnStatIncrease(localStatIndex);
                statPanel.Append(plusBtn);
                plusButtons[i] = plusBtn;

                statLabel.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                statLabel.OnMouseOut += (evt, el) => HideTooltip();
                plusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                plusBtn.OnMouseOut += (evt, el) => HideTooltip();
                minusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                minusBtn.OnMouseOut += (evt, el) => HideTooltip();
                checkbox.OnMouseOver += (evt, el) => ShowTooltip(Language.GetTextValue("Mods.Stataria.UI.StatPanel.AutoAllocateTooltip"));
                checkbox.OnMouseOut += (evt, el) => HideTooltip();
            }

            float bottomControlsTop = top + (numRows * heightPerStat) + 10f;

            resetButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.StatPanel.ResetStats"), textScale: 0.9f, large: false)
            {
                Top = { Pixels = bottomControlsTop },
                Left = { Pixels = (totalWidth - 120f) / 2 },
                Width = { Pixels = 120f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(63, 82, 151, 200),
                BorderColor = new Color(0, 0, 0, 255)
            };
            resetButton.OnLeftClick += OnResetStats;
            resetButton.OnMouseOver += (evt, el) =>
            {
                ShowTooltip(Language.GetTextValue("Mods.Stataria.UI.StatPanel.ResetStatsTooltip"));
            };
            resetButton.OnMouseOut += (evt, el) => HideTooltip();
            statPanel.Append(resetButton);

            float rebirthButtonY = bottomControlsTop + 45f;
            if (config.rebirthSystem.EnableRebirthSystem)
            {
                rebirthButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.StatPanel.Rebirth"), textScale: 0.9f, large: false)
                {
                    Top = { Pixels = rebirthButtonY },
                    Left = { Pixels = (totalWidth - 120f) / 2 },
                    Width = { Pixels = 120f },
                    Height = { Pixels = 30f },
                    BackgroundColor = new Color(150, 90, 150, 200),
                    BorderColor = new Color(190, 120, 190, 255)
                };
                rebirthButton.OnLeftClick += OnRebirthButtonClick;
                statPanel.Append(rebirthButton);

                rebirthConfirmationText = new UIText(Language.GetText("Mods.Stataria.UI.StatPanel.ConfirmRebirthPrompt"), 0.9f)
                {
                    Top = { Pixels = rebirthButtonY + 40f },
                    Left = { Pixels = 20f },
                    TextColor = Color.Red
                };
                rebirthConfirmationY = rebirthButtonY + 45f;
                bottomControlsTop = rebirthButtonY + 50f;
            }

            bulkManager = new BulkAllocationManager();
            float bulkBaseY;
            if (config.rebirthSystem.EnableRebirthSystem)
                bulkBaseY = bottomControlsTop;
            else
                bulkBaseY = bottomControlsTop + resetButton.Height.Pixels + 10f;

            bulkManager.Initialize(statPanel, bulkBaseY);

            autoButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.StatPanel.Auto"), textScale: 0.9f, large: false)
            {
                Top = { Pixels = bulkBaseY + 70f },
                Left = { Pixels = (totalWidth - 100f) / 2 },
                Width = { Pixels = 100f },
                Height = { Pixels = 30f },
                BackgroundColor = rpg.AutoAllocateEnabled ? new Color(80, 180, 80, 200) : new Color(100, 100, 100, 200),
                BorderColor = rpg.AutoAllocateEnabled ? new Color(100, 255, 100, 200) : new Color(150, 150, 150, 200)
            };
            autoButton.OnLeftClick += (evt, el) =>
            {
                var localPlayer = Main.LocalPlayer;
                if (localPlayer == null || !localPlayer.active) return;

                var localRpg = localPlayer.GetModPlayer<RPGPlayer>();
                localRpg.AutoAllocateEnabled = !localRpg.AutoAllocateEnabled;
                autoAllocationEnabled = localRpg.AutoAllocateEnabled;
                UpdateAutoButton();

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    localRpg.SyncPlayer(-1, localPlayer.whoAmI, false);

                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            autoButton.OnMouseOver += (evt, el) => ShowTooltip(Language.GetTextValue("Mods.Stataria.UI.StatPanel.AutoTooltip"));
            autoButton.OnMouseOut += (evt, el) => HideTooltip();
            statPanel.Append(autoButton);

            autoAllocationEnabled = rpg.AutoAllocateEnabled;
            UpdateAutoButton();

            float panelHeight = bulkBaseY + 120f;
            statPanel.Height.Set(panelHeight, 0f);
            statPanel.Recalculate();

            float tooltipY = panelHeight + 10f;
            tooltipPanel = new UIPanel();
            tooltipPanel.Width.Set(Math.Min(totalWidth - 20f, 600f), 0f);
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Left.Set(10f, 0f);
            tooltipPanel.Top.Set(tooltipY, 0f);
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            statPanel.Append(tooltipPanel);
            tooltipPanel.Recalculate();

            tooltipText = new UIText("", textScale: 1f);
            tooltipText.Width.Set(0, 1f);
            tooltipText.Top.Set(4f, 0f);
            tooltipText.Left.Set(4f, 0f);
            tooltipPanel.Append(tooltipText);

            statPanel.Recalculate();
        }

        private bool IsClickingOnInteractiveElement(Vector2 mousePosition)
        {
            foreach (var button in plusButtons)
            {
                if (button?.ContainsPoint(mousePosition) == true)
                    return true;
            }
            foreach (var button in minusButtons)
            {
                if (button?.ContainsPoint(mousePosition) == true)
                    return true;
            }
            if (resetButton?.ContainsPoint(mousePosition) == true)
                return true;
            if (rebirthButton?.ContainsPoint(mousePosition) == true)
                return true;
            if (autoButton?.ContainsPoint(mousePosition) == true)
                return true;

            foreach (var checkbox in autoCheckboxes.Values)
            {
                if (checkbox?.ContainsPoint(mousePosition) == true)
                    return true;
            }

            return false;
        }

        private void OnRebirthButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            int currentLevelRequirement = config.rebirthSystem.RebirthLevelRequirement;

            if (config.rebirthSystem.IncreaseLevelRequirement && rpg.RebirthCount > 0)
            {
                currentLevelRequirement += rpg.RebirthCount * config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
            }

            if (rpg.Level < currentLevelRequirement)
            {
                rebirthConfirmationText.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.RequireLevelPrompt").WithFormatArgs(currentLevelRequirement));
                rebirthConfirmationText.TextColor = Color.Red;
                if (!statPanel.HasChild(rebirthConfirmationText))
                    statPanel.Append(rebirthConfirmationText);
                requirementMessageShown = true;
                requirementMessageTimer = RequirementMessageDuration;
                return;
            }

            if (!rebirthConfirmationShown)
            {
                rebirthConfirmationText.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.ConfirmRebirthPrompt"));
                rebirthConfirmationText.TextColor = Color.Red;
                statPanel.Append(rebirthConfirmationText);
                rebirthConfirmationShown = true;

                rebirthButton.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.ConfirmRebirth"), 0.9f, large: false);
            }
            else
            {
                rpg.PerformRebirth();

                rebirthConfirmationShown = false;
                statPanel.RemoveChild(rebirthConfirmationText);
                rebirthButton.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.Rebirth"), 0.9f, large: false);

                SoundEngine.PlaySound(SoundID.Item4);
            }
        }

        private void UpdateAutoButton()
        {
            if (autoAllocationEnabled)
            {
                autoButton.BackgroundColor = new Color(80, 180, 80, 200);
                autoButton.BorderColor = new Color(100, 255, 100, 200);
            }
            else
            {
                autoButton.BackgroundColor = new Color(100, 100, 100, 200);
                autoButton.BorderColor = new Color(150, 150, 150, 200);
            }
        }

        private string GetStatTooltip(int statIndex)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            var activeStats = GetActiveStats();

            if (statIndex < 0 || statIndex >= activeStats.Count)
                return Language.GetTextValue("Mods.Stataria.UI.StatPanel.UnknownStat");

            var stat = activeStats[statIndex];

            return stat.GetTooltip(config);
        }

        private string GetXPSystemTooltip()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            string tooltip = Language.GetTextValue("Mods.Stataria.UI.StatPanel.XPInfoTitle") + "\n";

            int baseStatPoints = config.generalBalance.StatPointsPerLevel;
            int bonusStatPoints = 0;

            if (config.rebirthSystem.EnableRebirthBonusStatPoints && rpg.RebirthCount > 0)
            {
                bonusStatPoints = (int)(baseStatPoints * rpg.RebirthCount * config.rebirthSystem.RebirthStatPointsMultiplier);
            }

            if (bonusStatPoints > 0)
            {
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatPointsPerLevelBonus", baseStatPoints, bonusStatPoints, baseStatPoints + bonusStatPoints) + "\n";
            }
            else
            {
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatPointsPerLevel", baseStatPoints) + "\n";
            }
            tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.DamageXP", config.generalBalance.DamageXP.ToString("0.##")) + "\n";
            tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.KillXP", config.generalBalance.KillXP.ToString("0.##")) + "\n";
            tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.BossXPTitle") + " ";
            if (config.generalBalance.UseFlatBossXP)
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.BossXPFlat", config.generalBalance.DefaultFlatBossXP) + "\n";
            else
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.BossXPPercent", config.generalBalance.BossXP) + "\n";

            if (config.rebirthSystem.EnableRebirthSystem && rpg.RebirthCount > 0)
            {
                float bonus = rpg.RebirthCount * config.rebirthSystem.RebirthXPMultiplier;
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.RebirthXPBonus", bonus.ToString("P0"), rpg.RebirthCount, config.rebirthSystem.RebirthXPMultiplier.ToString("0.##")) + "\n";
            }

            string capText = Language.GetTextValue("Mods.Stataria.UI.StatPanel.LevelCapNone") + "\n";

            if (config.rebirthSystem.EnableDynamicRebirthLevelCap)
            {
                int nextRebirthRequirement = config.rebirthSystem.RebirthLevelRequirement +
                                        (rpg.RebirthCount * config.rebirthSystem.AdditionalLevelRequirementPerRebirth);
                int dynamicLevelCap = (int)(nextRebirthRequirement * config.rebirthSystem.DynamicRebirthLevelCapMultiplier);
                capText = Language.GetTextValue("Mods.Stataria.UI.StatPanel.DynamicLevelCap", dynamicLevelCap) + "\n";
            }
            else if (config.generalBalance.EnableLevelCap)
            {
                capText = Language.GetTextValue("Mods.Stataria.UI.StatPanel.LevelCap", config.generalBalance.LevelCapValue) + "\n";
            }
            tooltip += capText;

            if (config.statSettings.EnableStatCaps)
            {
                if (config.rebirthSystem.EnableProgressiveStatCaps && rpg.RebirthCount > 0)
                {
                    float capMultiplier = 1f + (rpg.RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                    tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatCapsProgressive", capMultiplier.ToString("F2")) + "\n";
                }
                else
                {
                    tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatCapsBase") + "\n";
                }
            }

            if (config.multiplayerSettings.SplitKillXP)
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.XPSplit") + "\n";

            if (config.multiplayerSettings.EnableXPProximity)
                tooltip += Language.GetTextValue("Mods.Stataria.UI.StatPanel.XPProximity", config.multiplayerSettings.XPProximityRange);

            return tooltip;
        }

        private string WrapText(string text, float maxWidth, float textScale = 1f)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string[] words = text.Split(' ');
            var sb = new System.Text.StringBuilder();
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = (currentLine.Length == 0) ? word : currentLine + " " + word;
                Vector2 size = font.MeasureString(testLine) * textScale;
                if (size.X > maxWidth)
                {
                    if (currentLine.Length == 0)
                    {
                        if (word.Length > 40)
                        {
                            int midpoint = word.Length / 2;
                            int breakpoint = word.IndexOf('/', midpoint);
                            if (breakpoint < 0) breakpoint = word.IndexOf('-', midpoint);
                            if (breakpoint < 0) breakpoint = word.IndexOf('.', midpoint);
                            if (breakpoint < 0) breakpoint = word.IndexOf('_', midpoint);
                            if (breakpoint < 0) breakpoint = midpoint;

                            sb.AppendLine(word.Substring(0, breakpoint + 1));
                            currentLine = word.Substring(breakpoint + 1);
                        }
                        else
                        {
                            sb.AppendLine(word);
                            currentLine = "";
                        }
                    }
                    else
                    {
                        sb.AppendLine(currentLine);
                        currentLine = word;
                    }
                }
                else
                {
                    currentLine = testLine;
                }
            }
            if (currentLine.Length > 0)
                sb.Append(currentLine);
            return sb.ToString();
        }

        private void ShowTooltip(string description)
        {
            tooltipPanel.BackgroundColor = new Color(33, 43, 79, 200);
            tooltipPanel.BorderColor = new Color(255, 255, 255, 200);

            float innerWidth = tooltipPanel.GetInnerDimensions().Width;
            string wrappedText = WrapText(description, innerWidth, 1f);
            tooltipText.SetText(wrappedText);
            tooltipText.Recalculate();

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float lineHeight = font.LineSpacing * 1f;
            int lineCount = wrappedText.Split('\n').Length;
            float totalTextHeight = lineCount * lineHeight;

            float padding = tooltipPanel.PaddingTop + tooltipPanel.PaddingBottom;
            tooltipPanel.Height.Set(totalTextHeight + padding + 8f, 0f);
            tooltipPanel.Recalculate();
        }

        private void HideTooltip()
        {
            tooltipText.SetText("");
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Recalculate();
        }

        private void OnStatIncrease(int index)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            var activeStats = GetActiveStats();

            if (index < 0 || index >= activeStats.Count)
                return;

            var stat = activeStats[index];

            int amount = bulkManager.GetCurrentAmount();
            amount = Math.Min(amount, rpg.StatPoints);

            if (amount <= 0)
                return;

            if (config.statSettings.EnableStatCaps)
            {
                int currentBaseStat = stat.GetValue(rpg);
                int cap = stat.GetCap(config);

                if (cap != -1)
                {
                    if (config.rebirthSystem.EnableProgressiveStatCaps && rpg.RebirthCount > 0)
                    {
                        float capMultiplier = 1f + (rpg.RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                        cap = (int)(cap * capMultiplier);
                    }

                    int effectiveCurrentStat = rpg.GetEffectiveStat(stat.Name);

                    if (effectiveCurrentStat >= cap)
                    {
                        return;
                    }

                    int ghostBonus = rpg.GhostStats.TryGetValue(stat.Name, out int ghost) ? ghost : 0;
                    int maxUsefulBaseStat = cap - ghostBonus;

                    if (currentBaseStat + amount > maxUsefulBaseStat)
                    {
                        amount = Math.Max(0, maxUsefulBaseStat - currentBaseStat);
                    }

                    if (amount <= 0)
                        return;
                }
            }

            int currentValue = stat.GetValue(rpg);
            stat.SetValue(rpg, currentValue + amount);

            rpg.StatPoints -= amount;
            SoundEngine.PlaySound(SoundID.MenuTick);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                rpg.SyncPlayer(-1, player.whoAmI, false);
            }
        }

        private void OnStatDecrease(int index)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            var activeStats = GetActiveStats();

            if (index < 0 || index >= activeStats.Count)
                return;

            var stat = activeStats[index];

            int amount = bulkManager.GetCurrentAmount();
            int currentValue = stat.GetValue(rpg);

            amount = Math.Min(amount, currentValue);

            if (amount <= 0)
                return;

            stat.SetValue(rpg, currentValue - amount);

            rpg.StatPoints += amount;
            SoundEngine.PlaySound(SoundID.MenuTick);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                rpg.SyncPlayer(-1, player.whoAmI, false);
            }
        }

        private void OnResetStats(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            if (!rpg.PerformRespec(out string reason))
            {
                Main.NewText(reason, Color.Red);
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            var activeStats = GetActiveStats();

            int total = 0;
            foreach (var stat in activeStats)
            {
                total += stat.GetValue(rpg);
                stat.SetValue(rpg, 0);
            }

            rpg.StatPoints += total;
            SoundEngine.PlaySound(SoundID.MenuClose);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                rpg.SyncPlayer(-1, player.whoAmI, false);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (statPanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (StatariaUI.StatUI?.CurrentState == null)
                return;

            if (dragging)
            {
                Vector2 mouse = Main.MouseScreen;
                statPanel.Left.Set(mouse.X - offset.X, 0f);
                statPanel.Top.Set(mouse.Y - offset.Y, 0f);
                statPanel.Recalculate();
            }

            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            levelText.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.LevelText").WithFormatArgs(rpg.Level));
            statPointsText.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.PointsText").WithFormatArgs(rpg.StatPoints));
            xpText.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.XPText").WithFormatArgs(rpg.XP.ToString("N0"), rpg.XPToNext.ToString("N0")));

            autoAllocationEnabled = rpg.AutoAllocateEnabled;
            UpdateAutoButton();

            foreach (var kvp in autoCheckboxes)
            {
                kvp.Value.IsChecked = rpg.AutoAllocateStats.Contains(kvp.Key);
            }

            var activeStats = GetActiveStats();

            for (int i = 0; i < activeStats.Count && i < statTexts.Length; i++)
            {
                var stat = activeStats[i];
                int value = stat.GetValue(rpg);

                if (statTexts[i] != null)
                {
                    string displayText = Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatLabel", stat.Name, value);

                    if (config.rebirthSystem.EnableGhostStats &&
                        rpg.GhostStats.TryGetValue(stat.Name, out int ghostValue) &&
                        ghostValue > 0)
                    {
                        displayText = Language.GetTextValue("Mods.Stataria.UI.StatPanel.StatLabelGhost", stat.Name, value, ghostValue);
                    }

                    statTexts[i].SetText(displayText);

                    bool canAdd = rpg.StatPoints > 0;
                    if (canAdd && config.statSettings.EnableStatCaps)
                    {
                        int cap = stat.GetCap(config);

                        if (cap != -1)
                        {
                            if (config.rebirthSystem.EnableProgressiveStatCaps && rpg.RebirthCount > 0)
                            {
                                float capMultiplier = 1f + (rpg.RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                                cap = (int)(cap * capMultiplier);
                            }

                            int effectiveStat = rpg.GetEffectiveStat(stat.Name);
                            canAdd = effectiveStat < cap;

                            if (canAdd && rpg.GhostStats.TryGetValue(stat.Name, out int ghostBonus))
                            {
                                int maxUsefulBaseStat = cap - ghostBonus;
                                canAdd = value < maxUsefulBaseStat;
                            }
                        }
                    }


                    plusButtons[i].BackgroundColor = canAdd
                        ? new Color(150, 150, 150, 20)
                        : new Color(80, 80, 80, 100);

                    plusButtons[i].BorderColor = canAdd
                        ? new Color(200, 200, 200, 20)
                        : new Color(20, 20, 20, 150);

                    bool canReduce = value > 0;
                    minusButtons[i].BackgroundColor = canReduce
                        ? new Color(150, 150, 150, 20)
                        : new Color(80, 80, 80, 100);

                    minusButtons[i].BorderColor = canReduce
                        ? new Color(200, 200, 200, 20)
                        : new Color(20, 20, 20, 150);
                }
            }

            for (int i = 0; i < activeStats.Count && i < statTexts.Length; i++)
            {
                if (plusButtons[i] != null && plusButtons[i].IsMouseHovering && Main.mouseLeft)
                {
                    holdTimers[i] += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (holdTimers[i] > buttonRepeatDelay)
                    {
                        holdTimers[i] = 0f;
                        OnStatIncrease(i);
                    }
                }
                else if (i < holdTimers.Length)
                {
                    holdTimers[i] = 0f;
                }

                if (minusButtons[i] != null && minusButtons[i].IsMouseHovering && Main.mouseLeft)
                {
                    holdTimersDown[i] += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (holdTimersDown[i] > buttonRepeatDelay)
                    {
                        holdTimersDown[i] = 0f;
                        OnStatDecrease(i);
                    }
                }
                else if (i < holdTimersDown.Length)
                {
                    holdTimersDown[i] = 0f;
                }
            }

            if (requirementMessageShown)
            {
                requirementMessageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                bool clickedElsewhere =
                    Main.mouseLeft
                    && statPanel.ContainsPoint(Main.MouseScreen)
                    && (rebirthButton == null || !rebirthButton.ContainsPoint(Main.MouseScreen))
                    && (rebirthConfirmationText == null || !rebirthConfirmationText.ContainsPoint(Main.MouseScreen));

                if (requirementMessageTimer <= 0f || clickedElsewhere)
                {
                    requirementMessageShown = false;
                    statPanel.RemoveChild(rebirthConfirmationText);
                }
            }

            if (rebirthConfirmationShown &&
                ((Main.mouseLeft && !rebirthButton.ContainsPoint(Main.MouseScreen) &&
                !rebirthConfirmationText.ContainsPoint(Main.MouseScreen)) ||
                Main.gameMenu))
            {
                rebirthConfirmationShown = false;
                statPanel.RemoveChild(rebirthConfirmationText);
                rebirthButton.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.Rebirth"), 0.9f, large: false);
            }


            if (resetButton != null)
            {
                if (rpg.CanRespec(out string _))
                {
                    resetButton.BackgroundColor = new Color(63, 82, 151, 200);
                    resetButton.BorderColor = new Color(0, 0, 0, 255);
                }
                else
                {
                    resetButton.BackgroundColor = new Color(100, 100, 100, 150);
                    resetButton.BorderColor = new Color(150, 150, 150, 150);
                }
            }

            if (rebirthButton != null)
            {
                int currentLevelRequirement = config.rebirthSystem.RebirthLevelRequirement;

                if (config.rebirthSystem.IncreaseLevelRequirement && rpg.RebirthCount > 0)
                {
                    currentLevelRequirement += rpg.RebirthCount * config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
                }

                if (rpg.Level >= currentLevelRequirement)
                {
                    rebirthButton.BackgroundColor = new Color(150, 90, 150, 200);
                    rebirthButton.BorderColor = new Color(190, 120, 190, 255);
                }
                else
                {
                    rebirthButton.BackgroundColor = new Color(100, 100, 100, 150);
                    rebirthButton.BorderColor = new Color(150, 150, 150, 150);
                }

                if (rebirthPointsText != null)
                {
                    rebirthPointsText.SetText(Language.GetText("Mods.Stataria.UI.StatPanel.RPText").WithFormatArgs(rpg.RebirthPoints));
                }
            }
        }
    }
}