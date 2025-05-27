using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
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
        private UIPanel panel;
        private UIText levelText;
        private UIText xpText;
        private UIText statPointsText;

        private UIPanel tooltipPanel;
        private UIText tooltipText;

        private string[] statNames;
        private UIText[] statTexts;
        private UITextPanel<string>[] plusButtons;
        private UITextPanel<string>[] minusButtons;
        private Dictionary<string, CheckBoxElement> autoCheckboxes = new Dictionary<string, CheckBoxElement>();
        private UITextPanel<string> autoButton;
        private bool autoAllocationEnabled = false;

        private UITextPanel<string> resetButton;
        private float[] holdTimers;
        private float[] holdTimersDown;
        private const float buttonRepeatDelay = 0.15f;

        private bool dragging = false;
        private Vector2 offset;

        private BulkAllocationManager bulkManager;

        private UITextPanel<string> rebirthButton;
        private bool rebirthConfirmationShown = false;
        private UIText rebirthConfirmationText;
        private float rebirthConfirmationY;
        private bool requirementMessageShown = false;
        private float requirementMessageTimer = 0f;
        private const float RequirementMessageDuration = 3f;
        private UIText rebirthPointsText;
        private UITextPanel<string> skillTreeButton;

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
                return $"{Name}: {GetValue(player)}";
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

                    tooltips.Add($"+{effectiveVIT * cfg.statSettings.VIT_HP} Max Health (+{cfg.statSettings.VIT_HP} per point)");

                    if (cfg.statSettings.UseCustomHpRegen)
                    {
                        tooltips.Add($"+{effectiveVIT * cfg.statSettings.CustomHpRegenPerVIT:0.#} HP/sec (+{cfg.statSettings.CustomHpRegenPerVIT:0.#} per point)");
                    }
                    else
                    {
                        tooltips.Add($"+{effectiveVIT * 0.5f:0.#} Life Regen (+0.5 per point)");
                    }

                    if (cfg.statSettings.EnableHealingPotionBoost)
                    {
                        tooltips.Add($"+{effectiveVIT * cfg.statSettings.HealingPotionBoostPercent:0.#}% Healing (+{cfg.statSettings.HealingPotionBoostPercent:0.#}% per point)");
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
                    return $"+{effectiveSTR * cfg.statSettings.STR_Damage:0.#}% Melee Damage (+{cfg.statSettings.STR_Damage:0.#}% per point)" +
                        $"\n+{effectiveSTR * cfg.statSettings.STR_Knockback:0.#}% Melee Knockback (+{cfg.statSettings.STR_Knockback:0.#}% per point)" +
                        $"\n+{effectiveSTR * cfg.statSettings.STR_ArmorPen} Melee Armor Pen. (+{cfg.statSettings.STR_ArmorPen} per point)";
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
                    return $"+{diminishedAGI * (cfg.statSettings.AGI_MoveSpeed / 100f):P1} Movement Speed (+{cfg.statSettings.AGI_MoveSpeed / 100f:P1} per point, diminished)" +
                        $"\n+{diminishedAGI * (cfg.statSettings.AGI_AttackSpeed / 100f):P1} Attack Speed (+{cfg.statSettings.AGI_AttackSpeed / 100f:P1} per point, diminished)" +
                        $"\n+{effectiveAGI * cfg.statSettings.AGI_WingTime} Wing Time (+{cfg.statSettings.AGI_WingTime} per point)" +
                        "\nImproved Jump Height & Speed";
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
                    return $"+{effectiveINT * cfg.statSettings.INT_MP} Max Mana (+{cfg.statSettings.INT_MP} per point)" +
                        $"\n+{effectiveINT * 0.5f:0.#} Mana Regen (+0.5 per point)" +
                        $"\n+{effectiveINT * cfg.statSettings.INT_Damage:0.#}% Magic Damage (+{cfg.statSettings.INT_Damage:0.#}% per point)" +
                        $"\n-{diminishingReduction:P1} Mana Cost (Diminishing)" +
                        $"\n+{effectiveINT * cfg.statSettings.INT_ArmorPen} Magic Armor Pen. (+{cfg.statSettings.INT_ArmorPen} per point)";
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
                        $"+{effectiveLUC * cfg.statSettings.LUC_Crit:0.#}% Critical Chance (+{cfg.statSettings.LUC_Crit:0.#}% per point)"
                    };
                    if (cfg.statSettings.LUC_EnableLuckBonus)
                    {
                        tooltips.Add($"+{effectiveLUC * cfg.statSettings.LUC_LuckBonus:0.##} Luck (+{cfg.statSettings.LUC_LuckBonus:0.##} per point)");
                    }
                    if (cfg.statSettings.LUC_EnableFishing)
                    {
                        tooltips.Add($"+{effectiveLUC * cfg.statSettings.LUC_Fishing} Fishing Power (+{cfg.statSettings.LUC_Fishing} per point)");
                    }
                    tooltips.Add($"-{effectiveLUC * cfg.statSettings.LUC_AggroReduction} Aggro (-{cfg.statSettings.LUC_AggroReduction} per point)");
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
                    var tooltips = new List<string>
                    {
                        $"+{effectiveEND / cfg.statSettings.END_DefensePerX} Defense (+1 per {cfg.statSettings.END_DefensePerX} points)"
                    };
                    if (cfg.statSettings.EnableKnockbackResist)
                    {
                        tooltips.Add($"+{Math.Min(effectiveEND, 100):0.#}% Knockback Resist (+1% per point)");
                    }
                    if (cfg.statSettings.EnableDR)
                    {
                        float drPercent = 100f * (1f - (1f / (1f + effectiveEND * 0.01f)));
                        tooltips.Add($"-{drPercent:0.#}% Damage Taken (Diminishing)");
                    }
                    if (cfg.statSettings.EnableEnemyKnockback)
                    {
                        tooltips.Add("Knocks Back Non-Bosses");
                    }
                    tooltips.Add($"+{effectiveEND * cfg.statSettings.END_Aggro} Aggro (+{cfg.statSettings.END_Aggro} per point)");
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
                        $"+{effectivePOW * cfg.statSettings.POW_Damage:0.#}% General Damage (+{cfg.statSettings.POW_Damage:0.#}% per point)",
                        $"+{effectivePOW * 0.1f:0.#}% Other Damage Types (+0.1% per point)"
                    };
                    if (cfg.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
                    {
                        tooltips.Add($"+{effectivePOW * cfg.modIntegration.POW_RageDamage:0.#}% Rage Damage (+{cfg.modIntegration.POW_RageDamage:0.#}% per point)");
                        tooltips.Add($"+{Math.Min(effectivePOW * cfg.modIntegration.POW_RageDuration, cfg.modIntegration.POW_MaxRageDurationBonus) / 60f:0.#}s Rage Duration (+{cfg.modIntegration.POW_RageDuration / 60f:0.#}s per point, max {cfg.modIntegration.POW_MaxRageDurationBonus / 60f:0.#}s)");
                        tooltips.Add($"+{effectivePOW * cfg.modIntegration.POW_AdrenalineDuration / 60f:0.#}s Adrenaline Duration (+{cfg.modIntegration.POW_AdrenalineDuration / 60f:0.#}s per point)");
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
                    return $"+{effectiveDEX * cfg.statSettings.DEX_Damage:0.#}% Ranged Damage (+{cfg.statSettings.DEX_Damage:0.#}% per point)" +
                        $"\n+{effectiveDEX * cfg.statSettings.DEX_ArmorPen} Ranged Armor Pen. (+{cfg.statSettings.DEX_ArmorPen} per point)" +
                        $"\n+{effectiveDEX * cfg.statSettings.DEX_AmmoConservation:0.#}% Ammo Save Chance (+{cfg.statSettings.DEX_AmmoConservation:0.#}% per point)";
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
                    return $"+{effectiveSPR * cfg.statSettings.SPR_Damage:0.#}% Summon Damage (+{cfg.statSettings.SPR_Damage:0.#}% per point)" +
                        $"\n+{minionSlots} Minion Slot{(minionSlots != 1 ? "s" : "")} (+1 per {cfg.statSettings.SPR_MinionsPerX} points)" +
                        $"\n+{sentrySlots} Sentry Slot{(sentrySlots != 1 ? "s" : "")} (+1 per {cfg.statSettings.SPR_SentriesPerX} points)";
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
                        tooltips.Add($"+{effectiveTCH * cfg.statSettings.TCH_MiningSpeed:0.#}% Mining Speed (+{cfg.statSettings.TCH_MiningSpeed:0.#}% per point)");
                    }
                    if (cfg.statSettings.TCH_EnableBuildSpeed)
                    {
                        tooltips.Add($"+{effectiveTCH * cfg.statSettings.TCH_BuildSpeed:0.#}% Build Speed (+{cfg.statSettings.TCH_BuildSpeed:0.#}% per point)");
                    }
                    if (cfg.statSettings.TCH_EnableRange)
                    {
                        tooltips.Add($"+{effectiveTCH * cfg.statSettings.TCH_Range} Tiles Reach (+{cfg.statSettings.TCH_Range} per point)");
                    }
                    return tooltips.Count > 0 ? string.Join("\n", tooltips) : "No active TCH effects.";
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
                        $"+{effectiveRGE * cfg.modIntegration.RGE_Damage:0.#}% Rogue Damage (+{cfg.modIntegration.RGE_Damage:0.#}% per point)",
                        $"+{effectiveRGE * cfg.modIntegration.RGE_MaxStealthPerPoint:0.#} Max Stealth (+{cfg.modIntegration.RGE_MaxStealthPerPoint:0.#} per point)",
                        $"+{effectiveRGE * cfg.modIntegration.RGE_Velocity:0.#}% Rogue Velocity (+{cfg.modIntegration.RGE_Velocity:0.#}% per point)",
                        $"-{effectiveRGE * cfg.modIntegration.RGE_AmmoConsumptionReduction:0.#}% Rogue Ammo Cost (-{cfg.modIntegration.RGE_AmmoConsumptionReduction:0.#}% per point)"
                    };
                    if (cfg.modIntegration.RGE_EnableStealthConsumptionReduction)
                    {
                        if (effectiveRGE >= cfg.modIntegration.RGE_StealthConsumptionReductionThreshold) tooltips.Add("Stealth Strikes cost 50%");
                        else if (effectiveRGE >= cfg.modIntegration.RGE_StealthConsumption75Threshold) tooltips.Add("Stealth Strikes cost 75%");
                        else if (effectiveRGE >= cfg.modIntegration.RGE_StealthConsumption85Threshold) tooltips.Add("Stealth Strikes cost 85%");
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
                        $"+{effectiveBRD * cfg.modIntegration.BRD_Damage:0.#}% Symphonic Damage (+{cfg.modIntegration.BRD_Damage:0.#}% per point)",
                        $"+{effectiveBRD * cfg.modIntegration.BRD_ArmorPen} Symphonic Armor Pen. (+{cfg.modIntegration.BRD_ArmorPen} per point)"
                    };
                    if (cfg.modIntegration.BRD_PointsPerMaxInspiration > 0)
                    {
                        tooltips.Add($"Max Inspiration: +1 per {cfg.modIntegration.BRD_PointsPerMaxInspiration} points (Thorium total caps at 30)");
                    }
                    if (cfg.modIntegration.BRD_EnableEmpowermentBoost && cfg.modIntegration.BRD_EmpowermentDuration > 0)
                    {
                        tooltips.Add($"+{effectiveBRD * cfg.modIntegration.BRD_EmpowermentDuration:0.#}s Empowerment Duration (+{cfg.modIntegration.BRD_EmpowermentDuration:0.#}s per point)");
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
                        $"+{effectiveHLR * cfg.modIntegration.HLR_Damage:0.#}% Radiant Damage (+{cfg.modIntegration.HLR_Damage:0.#}% per point)",
                        $"+{effectiveHLR * cfg.modIntegration.HLR_ArmorPen} Radiant Armor Pen. (+{cfg.modIntegration.HLR_ArmorPen} per point)"
                    };
                    if (cfg.modIntegration.HLR_PointsPerEffectPoint > 0)
                    {
                        int effectivePoints = effectiveHLR / cfg.modIntegration.HLR_PointsPerEffectPoint;
                        tooltips.Add($"+{effectivePoints * cfg.modIntegration.HLR_HealingPower} Healing Power (+{cfg.modIntegration.HLR_HealingPower} per {cfg.modIntegration.HLR_PointsPerEffectPoint} points)");
                        tooltips.Add("Improved Life Recovery");
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
                        $"+{effectiveCLK * cfg.modIntegration.CLK_Damage:0.#}% Click Damage (+{cfg.modIntegration.CLK_Damage:0.#}% per point)",
                        $"+{effectiveCLK * cfg.modIntegration.CLK_Radius:0.#} Click Radius Units (+{cfg.modIntegration.CLK_Radius:0.#} units per point)"
                    };
                    float perPointFactor = cfg.modIntegration.CLK_EffectThreshold / 100f;
                    float linearReduction = effectiveCLK * perPointFactor;
                    if (linearReduction > 0)
                    {
                        float effectiveReduction = 100f * (1f - (1f / (1f + linearReduction)));
                        tooltips.Add($"-{effectiveReduction:0.#}% Clicks for Effects (Diminishing)");
                    }
                    return string.Join("\n", tooltips);
                }
            });
        }

        private List<StatDefinition> GetActiveStats()
        {
            return statDefinitions.Where(stat => stat.IsModLoaded()).ToList();
        }

        public override void OnInitialize()
        {
            InitializeStatDefinitions();

            panel = new UIPanel();

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

            panel.Width.Set(totalWidth, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;

            panel.SetPadding(0);
            panel.BackgroundColor = new Color(63, 82, 151, 200);
            panel.BorderColor = new Color(0, 0, 0, 255);
            Append(panel);

            panel.OnLeftMouseDown += (evt, el) =>
            {
                offset = new Vector2(evt.MousePosition.X - panel.Left.Pixels, evt.MousePosition.Y - panel.Top.Pixels);
                dragging = true;
            };
            panel.OnLeftMouseUp += (evt, el) =>
            {
                dragging = false;
            };

            float top = 10f;

            levelText = new UIText("Level: 1");
            levelText.Top.Set(top, 0f);
            levelText.Left.Set(10f, 0f);
            panel.Append(levelText);
            levelText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            levelText.OnMouseOut += (evt, el) => HideTooltip();

            statPointsText = new UIText("Points: 0");
            statPointsText.Top.Set(top, 0f);
            statPointsText.Left.Set(panel.Width.Pixels - 120f, 0f);
            panel.Append(statPointsText);

            top += 30f;

            xpText = new UIText("XP: 0 / 100");
            xpText.Top.Set(top, 0f);
            xpText.Left.Set(10f, 0f);
            panel.Append(xpText);
            xpText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            xpText.OnMouseOut += (evt, el) => HideTooltip();

            rebirthPointsText = new UIText("RP: 0");
            rebirthPointsText.Top.Set(top, 0f);
            rebirthPointsText.Left.Set(panel.Width.Pixels - 120f, 0f);
            rebirthPointsText.TextColor = new Color(190, 120, 190);
            panel.Append(rebirthPointsText);

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
                panel.Append(checkbox);

                var statLabel = new UIText(stat.Name + ": 0");
                statLabel.Top.Set(rowTop + 5f, 0f);
                statLabel.Left.Set(40f + columnOffset, 0f);
                panel.Append(statLabel);
                statTexts[i] = statLabel;

                var plusBtn = new UITextPanel<string>("+", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 240f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                plusBtn.SetPadding(0f);
                int localStatIndex = i;
                plusBtn.OnLeftClick += (evt, el) => OnStatIncrease(localStatIndex);
                panel.Append(plusBtn);
                plusButtons[i] = plusBtn;

                var minusBtn = new UITextPanel<string>("-", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 290f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                minusBtn.SetPadding(0f);
                int minusStatIndex = i;
                minusBtn.OnLeftClick += (evt, el) => OnStatDecrease(minusStatIndex);
                panel.Append(minusBtn);
                minusButtons[i] = minusBtn;

                statLabel.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                statLabel.OnMouseOut += (evt, el) => HideTooltip();
                plusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                plusBtn.OnMouseOut += (evt, el) => HideTooltip();
                minusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                minusBtn.OnMouseOut += (evt, el) => HideTooltip();
                checkbox.OnMouseOver += (evt, el) => ShowTooltip("Check to auto-allocate points to this stat");
                checkbox.OnMouseOut += (evt, el) => HideTooltip();
            }

            float bottomControlsTop = top + (numRows * heightPerStat) + 10f;

            resetButton = new UITextPanel<string>("Reset Stats", textScale: 0.9f, large: false)
            {
                Top = { Pixels = bottomControlsTop },
                Left = { Pixels = (totalWidth - 120f) / 2 },
                Width = { Pixels = 120f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(63, 82, 151, 200),
                BorderColor = new Color(0, 0, 0, 255)
            };
            resetButton.OnLeftClick += OnResetStats;
            panel.Append(resetButton);

            float rebirthButtonY = bottomControlsTop + 45f;
            if (config.rebirthSystem.EnableRebirthSystem)
            {
                rebirthButton = new UITextPanel<string>("Rebirth", textScale: 0.9f, large: false)
                {
                    Top = { Pixels = rebirthButtonY },
                    Left = { Pixels = (totalWidth - 120f) / 2 },
                    Width = { Pixels = 120f },
                    Height = { Pixels = 30f },
                    BackgroundColor = new Color(150, 90, 150, 200),
                    BorderColor = new Color(190, 120, 190, 255)
                };
                rebirthButton.OnLeftClick += OnRebirthButtonClick;
                panel.Append(rebirthButton);

                skillTreeButton = new UITextPanel<string>("Abilities", textScale: 0.9f, large: false)
                {
                    Top = { Pixels = rebirthButtonY + 45f },
                    Left = { Pixels = (totalWidth - 120f) / 2 },
                    Width = { Pixels = 120f },
                    Height = { Pixels = 30f },
                    BackgroundColor = new Color(80, 150, 80, 200),
                    BorderColor = new Color(100, 180, 100, 255)
                };
                skillTreeButton.OnLeftClick += OnSkillTreeButtonClick;
                panel.Append(skillTreeButton);

                rebirthConfirmationText = new UIText("Are you sure you want to Rebirth?", 0.9f)
                {
                    Top = { Pixels = rebirthButtonY + 85f },
                    Left = { Pixels = 20f },
                    TextColor = Color.Red
                };
                rebirthConfirmationY = rebirthButtonY + 45f;
                bottomControlsTop = rebirthButtonY + 100f;
            }

            bulkManager = new BulkAllocationManager();
            float bulkBaseY;
            if (config.rebirthSystem.EnableRebirthSystem)
                bulkBaseY = bottomControlsTop;
            else
                bulkBaseY = bottomControlsTop + resetButton.Height.Pixels + 10f;

            bulkManager.Initialize(panel, bulkBaseY);

            autoButton = new UITextPanel<string>("Auto", textScale: 0.9f, large: false)
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
            autoButton.OnMouseOver += (evt, el) => ShowTooltip("Toggle automatic point allocation to checked stats");
            autoButton.OnMouseOut += (evt, el) => HideTooltip();
            panel.Append(autoButton);

            float panelHeight = bulkBaseY + 120f;
            panel.Height.Set(panelHeight, 0f);
            panel.Recalculate();

            float tooltipY = panelHeight + 10f;
            tooltipPanel = new UIPanel();
            tooltipPanel.Width.Set(Math.Min(totalWidth - 20f, 600f), 0f);
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Left.Set(10f, 0f);
            tooltipPanel.Top.Set(tooltipY, 0f);
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            panel.Append(tooltipPanel);
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

            panel.Width.Set(totalWidth, 0f);

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
            panel.RemoveAllChildren();

            float top = 10f;

            levelText = new UIText("Level: 1");
            levelText.Top.Set(top, 0f);
            levelText.Left.Set(10f, 0f);
            panel.Append(levelText);
            levelText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            levelText.OnMouseOut += (evt, el) => HideTooltip();

            statPointsText = new UIText("Points: 0");
            statPointsText.Top.Set(top, 0f);
            statPointsText.Left.Set(totalWidth - 120f, 0f);
            panel.Append(statPointsText);

            top += 30f;

            xpText = new UIText("XP: 0 / 100");
            xpText.Top.Set(top, 0f);
            xpText.Left.Set(10f, 0f);
            panel.Append(xpText);
            xpText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            xpText.OnMouseOut += (evt, el) => HideTooltip();

            rebirthPointsText = new UIText("RP: 0");
            rebirthPointsText.Top.Set(top, 0f);
            rebirthPointsText.Left.Set(totalWidth - 120f, 0f);
            rebirthPointsText.TextColor = new Color(190, 120, 190);
            panel.Append(rebirthPointsText);

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
                panel.Append(checkbox);

                var statLabel = new UIText(statName + ": 0");
                statLabel.Top.Set(rowTop + 5f, 0f);
                statLabel.Left.Set(40f + columnOffset, 0f);
                panel.Append(statLabel);
                statTexts[i] = statLabel;

                var plusBtn = new UITextPanel<string>("+", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 240f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                plusBtn.SetPadding(0f);
                int localStatIndex = i;
                plusBtn.OnLeftClick += (evt, el) => OnStatIncrease(localStatIndex);
                panel.Append(plusBtn);
                plusButtons[i] = plusBtn;

                var minusBtn = new UITextPanel<string>("-", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = rowTop },
                    Left = { Pixels = 290f + columnOffset },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                minusBtn.SetPadding(0f);
                int minusStatIndex = i;
                minusBtn.OnLeftClick += (evt, el) => OnStatDecrease(minusStatIndex);
                panel.Append(minusBtn);
                minusButtons[i] = minusBtn;

                statLabel.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                statLabel.OnMouseOut += (evt, el) => HideTooltip();
                plusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                plusBtn.OnMouseOut += (evt, el) => HideTooltip();
                minusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(localStatIndex));
                minusBtn.OnMouseOut += (evt, el) => HideTooltip();
                checkbox.OnMouseOver += (evt, el) => ShowTooltip("Check to auto-allocate points to this stat");
                checkbox.OnMouseOut += (evt, el) => HideTooltip();
            }

            float bottomControlsTop = top + (numRows * heightPerStat) + 10f;

            resetButton = new UITextPanel<string>("Reset Stats", textScale: 0.9f, large: false)
            {
                Top = { Pixels = bottomControlsTop },
                Left = { Pixels = (totalWidth - 120f) / 2 },
                Width = { Pixels = 120f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(63, 82, 151, 200),
                BorderColor = new Color(0, 0, 0, 255)
            };
            resetButton.OnLeftClick += OnResetStats;
            panel.Append(resetButton);

            float rebirthButtonY = bottomControlsTop + 45f;
            if (config.rebirthSystem.EnableRebirthSystem)
            {
                rebirthButton = new UITextPanel<string>("Rebirth", textScale: 0.9f, large: false)
                {
                    Top = { Pixels = rebirthButtonY },
                    Left = { Pixels = (totalWidth - 120f) / 2 },
                    Width = { Pixels = 120f },
                    Height = { Pixels = 30f },
                    BackgroundColor = new Color(150, 90, 150, 200),
                    BorderColor = new Color(190, 120, 190, 255)
                };
                rebirthButton.OnLeftClick += OnRebirthButtonClick;
                panel.Append(rebirthButton);

                skillTreeButton = new UITextPanel<string>("Abilities", textScale: 0.9f, large: false)
                {
                    Top = { Pixels = rebirthButtonY + 45f },
                    Left = { Pixels = (totalWidth - 120f) / 2 },
                    Width = { Pixels = 120f },
                    Height = { Pixels = 30f },
                    BackgroundColor = new Color(80, 150, 80, 200),
                    BorderColor = new Color(100, 180, 100, 255)
                };
                skillTreeButton.OnLeftClick += OnSkillTreeButtonClick;
                panel.Append(skillTreeButton);

                rebirthConfirmationText = new UIText("Are you sure you want to Rebirth?", 0.9f)
                {
                    Top = { Pixels = rebirthButtonY + 85f },
                    Left = { Pixels = 20f },
                    TextColor = Color.Red
                };
                rebirthConfirmationY = rebirthButtonY + 45f;
                bottomControlsTop = rebirthButtonY + 100f;
            }

            bulkManager = new BulkAllocationManager();
            float bulkBaseY;
            if (config.rebirthSystem.EnableRebirthSystem)
                bulkBaseY = bottomControlsTop;
            else
                bulkBaseY = bottomControlsTop + resetButton.Height.Pixels + 10f;

            bulkManager.Initialize(panel, bulkBaseY);

            autoButton = new UITextPanel<string>("Auto", textScale: 0.9f, large: false)
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
            autoButton.OnMouseOver += (evt, el) => ShowTooltip("Toggle automatic point allocation to checked stats");
            autoButton.OnMouseOut += (evt, el) => HideTooltip();
            panel.Append(autoButton);

            autoAllocationEnabled = rpg.AutoAllocateEnabled;
            UpdateAutoButton();

            float panelHeight = bulkBaseY + 120f;
            panel.Height.Set(panelHeight, 0f);
            panel.Recalculate();

            float tooltipY = panelHeight + 10f;
            tooltipPanel = new UIPanel();
            tooltipPanel.Width.Set(Math.Min(totalWidth - 20f, 600f), 0f);
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Left.Set(10f, 0f);
            tooltipPanel.Top.Set(tooltipY, 0f);
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            panel.Append(tooltipPanel);
            tooltipPanel.Recalculate();

            tooltipText = new UIText("", textScale: 1f);
            tooltipText.Width.Set(0, 1f);
            tooltipText.Top.Set(4f, 0f);
            tooltipText.Left.Set(4f, 0f);
            tooltipPanel.Append(tooltipText);

            panel.Recalculate();
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
                rebirthConfirmationText.SetText($"Requires level {currentLevelRequirement}!");
                rebirthConfirmationText.TextColor = Color.Red;
                if (!panel.HasChild(rebirthConfirmationText))
                    panel.Append(rebirthConfirmationText);
                requirementMessageShown = true;
                requirementMessageTimer = RequirementMessageDuration;
                return;
            }

            if (!rebirthConfirmationShown)
            {
                rebirthConfirmationText.SetText("Are you sure you want to Rebirth?");
                rebirthConfirmationText.TextColor = Color.Red;
                panel.Append(rebirthConfirmationText);
                rebirthConfirmationShown = true;

                rebirthButton.SetText("Confirm Rebirth");
            }
            else
            {
                rpg.PerformRebirth();

                rebirthConfirmationShown = false;
                panel.RemoveChild(rebirthConfirmationText);
                rebirthButton.SetText("Rebirth");

                SoundEngine.PlaySound(SoundID.Item4);
            }
        }

        private void OnSkillTreeButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            StatariaUI.StatUI.SetState(null);
            StatariaUI.SkillTreeUI.SetState(StatariaUI.SkillTreePanel);
            StatariaUI.SkillTreePanel.RefreshAbilitiesList();
            SoundEngine.PlaySound(SoundID.MenuOpen);
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
                return "Unknown Stat";

            var stat = activeStats[statIndex];

            return stat.GetTooltip(config);
        }

        private string GetXPSystemTooltip()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            string tooltip = "XP System Info:\n";

            int baseStatPoints = config.generalBalance.StatPointsPerLevel;
            int bonusStatPoints = 0;

            if (config.rebirthSystem.EnableRebirthBonusStatPoints && rpg.RebirthCount > 0)
            {
                bonusStatPoints = (int)(baseStatPoints * rpg.RebirthCount * config.rebirthSystem.RebirthStatPointsMultiplier);
            }

            if (bonusStatPoints > 0)
            {
                tooltip += $"Stat Points per Level: {baseStatPoints} + {bonusStatPoints} (Rebirth Bonus) = {baseStatPoints + bonusStatPoints}\n";
            }
            else
            {
                tooltip += $"Stat Points per Level: {baseStatPoints}\n";
            }
            tooltip += $"Damage XP: {config.generalBalance.DamageXP:0.##}x damage dealt\n";
            tooltip += $"Kill XP: {config.generalBalance.KillXP:0.##}x enemy max health\n";
            tooltip += $"Boss XP: ";
            if (config.generalBalance.UseFlatBossXP)
                tooltip += $"{config.generalBalance.DefaultFlatBossXP} flat XP\n";
            else
                tooltip += $"{config.generalBalance.BossXP}% of next level\n";

            if (config.rebirthSystem.EnableRebirthSystem && rpg.RebirthCount > 0)
            {
                float bonus = rpg.RebirthCount * config.rebirthSystem.RebirthXPMultiplier;
                tooltip += $"Rebirth XP Bonus: +{bonus:P0} ({rpg.RebirthCount} × {config.rebirthSystem.RebirthXPMultiplier:0.##})\n";
            }

            string capText = "Level Cap: None (or very high)\n";

            if (config.rebirthSystem.EnableDynamicRebirthLevelCap)
            {
                int nextRebirthRequirement = config.rebirthSystem.RebirthLevelRequirement +
                                        (rpg.RebirthCount * config.rebirthSystem.AdditionalLevelRequirementPerRebirth);
                int dynamicLevelCap = (int)(nextRebirthRequirement * config.rebirthSystem.DynamicRebirthLevelCapMultiplier);
                capText = $"Dynamic Level Cap (Current Cycle): {dynamicLevelCap}\n";
            }
            else if (config.generalBalance.EnableLevelCap)
            {
                capText = $"Level Cap: {config.generalBalance.LevelCapValue}\n";
            }
            tooltip += capText;

            if (config.statSettings.EnableStatCaps)
            {
                if (config.rebirthSystem.EnableProgressiveStatCaps && rpg.RebirthCount > 0)
                {
                    float capMultiplier = 1f + (rpg.RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                    tooltip += $"Stat Caps: Base × {capMultiplier:F2} (Progressive)\n";
                }
                else
                {
                    tooltip += "Stat Caps: Base values\n";
                }
            }

            if (config.multiplayerSettings.SplitKillXP)
                tooltip += "XP is split evenly among all eligible players\n";

            if (config.multiplayerSettings.EnableXPProximity)
                tooltip += $"Players must be within {config.multiplayerSettings.XPProximityRange} pixels of enemies to get XP";

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

            int currentValue = stat.GetValue(rpg);
            stat.SetValue(rpg, currentValue + amount);

            rpg.StatPoints -= amount;
            SoundEngine.PlaySound(SoundID.MenuTick);
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
        }

        private void OnResetStats(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            var activeStats = GetActiveStats();

            int total = 0;
            foreach (var stat in activeStats)
            {
                total += stat.GetValue(rpg);
                stat.SetValue(rpg, 0);
            }

            rpg.StatPoints += total;
            SoundEngine.PlaySound(SoundID.MenuClose);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (StatariaUI.StatUI?.CurrentState == null)
                return;

            if (dragging)
            {
                Vector2 mouse = Main.MouseScreen;
                panel.Left.Set(mouse.X - offset.X, 0f);
                panel.Top.Set(mouse.Y - offset.Y, 0f);
                panel.Recalculate();
            }

            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            levelText.SetText($"Level: {rpg.Level}");
            statPointsText.SetText($"Points: {rpg.StatPoints}");
            xpText.SetText($"XP: {rpg.XP:N0} / {rpg.XPToNext:N0}");

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
                    string displayText = $"{stat.Name}: {value}";

                    if (config.rebirthSystem.EnableGhostStats &&
                        rpg.GhostStats.TryGetValue(stat.Name, out int ghostValue) &&
                        ghostValue > 0)
                    {
                        displayText = $"{stat.Name}: {value} (+{ghostValue})";
                    }

                    statTexts[i].SetText(displayText);

                    bool canAdd = rpg.StatPoints > 0;
                    if (canAdd && config.statSettings.EnableStatCaps)
                    {
                        int cap = stat.GetCap(config);

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
                    && panel.ContainsPoint(Main.MouseScreen)
                    && (rebirthButton == null || !rebirthButton.ContainsPoint(Main.MouseScreen))
                    && (rebirthConfirmationText == null || !rebirthConfirmationText.ContainsPoint(Main.MouseScreen));

                if (requirementMessageTimer <= 0f || clickedElsewhere)
                {
                    requirementMessageShown = false;
                    panel.RemoveChild(rebirthConfirmationText);
                }
            }

            if (rebirthConfirmationShown &&
                ((Main.mouseLeft && !rebirthButton.ContainsPoint(Main.MouseScreen) &&
                !rebirthConfirmationText.ContainsPoint(Main.MouseScreen)) ||
                Main.gameMenu))
            {
                rebirthConfirmationShown = false;
                panel.RemoveChild(rebirthConfirmationText);
                rebirthButton.SetText("Rebirth");
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
                    rebirthPointsText.SetText($"RP: {rpg.RebirthPoints}");
                }
            }
        }
    }
}