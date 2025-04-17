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

namespace Stataria
{
    public class StatPanel : UIState
    {
        private UIPanel panel;
        private UIText levelText;
        private UIText xpText;
        private UIText statPointsText;

        // A panel for the tooltip. It will have dynamically adjusted height.
        private UIPanel tooltipPanel;
        private UIText tooltipText;

        private string[] statNames = { "VIT", "STR", "AGI", "INT", "LUC", "END", "POW", "DEX", "SPR" };
        private UIText[] statTexts;
        private UITextPanel<string>[] plusButtons;
        private UITextPanel<string>[] minusButtons;

        private UITextPanel<string> resetButton;
        private float[] holdTimers;
        private float[] holdTimersDown;
        private const float buttonRepeatDelay = 0.15f;

        private bool dragging = false;
        private Vector2 offset;

        public override void OnInitialize()
        {
            // Main panel
            panel = new UIPanel();
            // Slightly wider panel to avoid cramping the minus button
            panel.Width.Set(340f, 0f);
            panel.Height.Set(450f, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;

            // Make sure we set padding to 0 so we can place things precisely
            panel.SetPadding(0);
            panel.BackgroundColor = new Color(63, 82, 151, 200);
            panel.BorderColor = new Color(0, 0, 0, 255);
            Append(panel);

            // Enable dragging the entire panel
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

            // Level label
            levelText = new UIText("Level: 1");
            levelText.Top.Set(top, 0f);
            levelText.Left.Set(10f, 0f);
            panel.Append(levelText);
            levelText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            levelText.OnMouseOut += (evt, el) => HideTooltip();

            // Stat points label
            statPointsText = new UIText("Points: 0");
            statPointsText.Top.Set(top, 0f);
            statPointsText.Left.Set(220f, 0f);
            panel.Append(statPointsText);

            top += 30f;

            // XP label
            xpText = new UIText("XP: 0 / 100");
            xpText.Top.Set(top, 0f);
            xpText.Left.Set(10f, 0f);
            panel.Append(xpText);
            xpText.OnMouseOver += (evt, el) => ShowTooltip(GetXPSystemTooltip());
            xpText.OnMouseOut += (evt, el) => HideTooltip();

            top += 40f;

            // Create rows for each stat
            statTexts = new UIText[statNames.Length];
            plusButtons = new UITextPanel<string>[statNames.Length];
            minusButtons = new UITextPanel<string>[statNames.Length];
            holdTimers = new float[statNames.Length];
            holdTimersDown = new float[statNames.Length];

            for (int i = 0; i < statNames.Length; i++)
            {
                // Stat label
                var statLabel = new UIText(statNames[i] + ": 0");
                statLabel.Top.Set(top, 0f);
                statLabel.Left.Set(10f, 0f);
                panel.Append(statLabel);
                statTexts[i] = statLabel;

                // Plus button for stat increase
                var plusBtn = new UITextPanel<string>("+", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = top },
                    // Shift to the left a bit so we have room for minus
                    Left = { Pixels = 240f },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    // Use white-transparent for "enabled" color (we'll override in Update)
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                plusBtn.SetPadding(0f);
                int statIndex = i; // Important: Capture current value of i
                plusBtn.OnLeftClick += (evt, el) => OnStatIncrease(statIndex);
                panel.Append(plusBtn);
                plusButtons[i] = plusBtn;

                // Minus button for stat decrease
                var minusBtn = new UITextPanel<string>("-", textScale: 1.2f, large: false)
                {
                    Top = { Pixels = top },
                    Left = { Pixels = 290f },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(255, 255, 255, 50),
                    BorderColor = new Color(255, 255, 255, 50)
                };
                minusBtn.SetPadding(0f);
                // FIX: Capture the current value of i in a local variable
                int minusStatIndex = i;
                minusBtn.OnLeftClick += (evt, el) => OnStatDecrease(minusStatIndex);
                panel.Append(minusBtn);
                minusButtons[i] = minusBtn;

                // MouseOver and MouseOut events for showing/hiding tooltip
                //string tip = GetStatTooltip(statIndex);
                statLabel.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(statIndex));
                statLabel.OnMouseOut += (evt, el) => HideTooltip();
                plusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(statIndex));
                plusBtn.OnMouseOut += (evt, el) => HideTooltip();
                minusBtn.OnMouseOver += (evt, el) => ShowTooltip(GetStatTooltip(statIndex));
                minusBtn.OnMouseOut += (evt, el) => HideTooltip();

                top += 35f; // a bit more spacing
            }

            // Reset stats button
            resetButton = new UITextPanel<string>("Reset Stats", textScale: 0.9f, large: false)
            {
                Top = { Pixels = top + 10f },
                Left = { Pixels = 110f },
                Width = { Pixels = 120f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(63, 82, 151, 200),
                BorderColor = new Color(0, 0, 0, 255)
            };
            resetButton.OnLeftClick += OnResetStats;
            panel.Append(resetButton);

            // Tooltip panel setup: Docked below the main panel content
            tooltipPanel = new UIPanel();
            tooltipPanel.Width.Set(320f, 0f);
            // Start with 0 height so it doesn't show initially
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Left.Set(10f, 0f);
            // Position it below everything (adjust if needed)
            tooltipPanel.Top.Set(460f, 0f);
            // Start fully transparent
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            panel.Append(tooltipPanel);

            // Tooltip text element; allow it to span the full width of the panel
            tooltipText = new UIText("", textScale: 1f);
            tooltipText.Width.Set(0, 1f);
            tooltipText.Top.Set(4f, 0f);
            tooltipText.Left.Set(4f, 0f);
            tooltipPanel.Append(tooltipText);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Block player interactions when mouse is over our panel.
            if (StatariaUI.StatUI?.CurrentState != null && panel.ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;

            // Handle dragging
            if (dragging)
            {
                Vector2 mouse = Main.MouseScreen;
                panel.Left.Set(mouse.X - offset.X, 0f);
                panel.Top.Set(mouse.Y - offset.Y, 0f);
                panel.Recalculate();
            }

            // Update UI stat values
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            levelText.SetText($"Level: {rpg.Level}");
            statPointsText.SetText($"Points: {rpg.StatPoints}");
            xpText.SetText($"XP: {rpg.XP} / {rpg.XPToNext}");

            int[] values = { rpg.VIT, rpg.STR, rpg.AGI, rpg.INT, rpg.LUC, rpg.END, rpg.POW, rpg.DEX, rpg.SPR };

            for (int i = 0; i < statNames.Length; i++)
            {
                statTexts[i].SetText($"{statNames[i]}: {values[i]}");

                // If we have points left, let the plus button be white-transparent. Else gray it out.
                if (rpg.StatPoints > 0)
                {
                    plusButtons[i].BackgroundColor = new Color(150, 150, 150, 20);
                    plusButtons[i].BorderColor = new Color(200, 200, 200, 20);
                }
                else
                {
                    // More transparent for disabled buttons
                    plusButtons[i].BackgroundColor = new Color(80, 80, 80, 100);
                    plusButtons[i].BorderColor = new Color(20, 20, 20, 150);
                }

                // If we have at least 1 point in the stat, minus is white-transparent; otherwise, gray it out.
                if (values[i] > 0)
                {
                    minusButtons[i].BackgroundColor = new Color(150, 150, 150, 20);
                    minusButtons[i].BorderColor = new Color(200, 200, 200, 20);
                }
                else
                {
                    // More transparent for disabled buttons
                    minusButtons[i].BackgroundColor = new Color(80, 80, 80, 100);
                    minusButtons[i].BorderColor = new Color(20, 20, 20, 150);
                }
            }

            // Hold-to-increase or decrease
            for (int i = 0; i < statNames.Length; i++)
            {
                if (plusButtons[i].IsMouseHovering && Main.mouseLeft)
                {
                    holdTimers[i] += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (holdTimers[i] > buttonRepeatDelay)
                    {
                        holdTimers[i] = 0f;
                        OnStatIncrease(i);
                    }
                }
                else holdTimers[i] = 0f;

                if (minusButtons[i].IsMouseHovering && Main.mouseLeft)
                {
                    holdTimersDown[i] += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (holdTimersDown[i] > buttonRepeatDelay)
                    {
                        holdTimersDown[i] = 0f;
                        OnStatDecrease(i);
                    }
                }
                else holdTimersDown[i] = 0f;
            }
        }

        // Helper: Returns the unwrapped tooltip for a stat based on its index.
        private string GetStatTooltip(int statIndex)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            switch (statIndex)
            {
                case 0: // VIT
                    string regenText;
                    if (config.UseCustomHpRegen)
                    {
                        float totalRegen = rpg.VIT * config.CustomHpRegenPerVIT;
                        float delaySec = config.CustomHpRegenDelay / 60f;
                        regenText = $"+{totalRegen:0.##} HP/sec (after {delaySec:0.#}s)";
                    }
                    else
                    {
                        regenText = $"+{rpg.VIT / 2f:0.##} HP Regen (vanilla)";
                    }

                    return $"+{config.VIT_HP} Max Health per point\n" +
                        regenText + "\n" +
                        $"Breath depletes {100f - (100f / (1f + rpg.VIT * 0.04f)):0.##}% slower";

                case 1: // STR
                    return $"+{config.STR_Damage}% Melee Damage per point\n" +
                        $"+{config.STR_Knockback}% Melee Knockback per point\n" +
                        $"+{config.STR_ArmorPen} Melee Armor Penetration per point";

                case 2: // AGI
                    string teleportKeyName = "Unknown";
                    if (StatariaKeybinds.TeleportKey != null && StatariaKeybinds.TeleportKey.GetAssignedKeys().Count > 0)
                    {
                        teleportKeyName = StatariaKeybinds.TeleportKey.GetAssignedKeys()[0];
                    }
                    
                    return $"+{config.AGI_MoveSpeed}% Movement Speed\n" +
                        $"+{config.AGI_AttackSpeed}% Attack Speed\n" +
                        $"Dash at {config.AGI_DashUnlockAt} AGI\n" +
                        $"Auto-Jump at {config.AGI_JumpUnlockAt} AGI\n" +
                        $"Water Freedom at {config.AGI_SwimUnlockAt} AGI\n" +
                        $"No Fall Damage at {config.AGI_NoFallDamageUnlockAt} AGI\n" +
                        $"Teleport ({teleportKeyName} Key) at {config.AGI_TeleportUnlockAt} AGI\n" +
                        $"+{config.AGI_JumpSpeed * 100}% Jump Speed\n" +
                        $"+{config.AGI_WingTime} Wing Flight Time";

                case 3: // INT
                    float manaCostReduction = 100f * (1f - (1f / (1f + (rpg.INT * config.INT_ManaCostReduction / 100f))));
                    return $"+{config.INT_MP} Max Mana\n" +
                        $"+{rpg.INT / 2f:0.##} Mana Regen\n" +
                        $"+{config.INT_Damage}% Magic Damage\n" +
                        $"-{manaCostReduction:0.##}% Mana Cost\n" +
                        $"+{config.INT_ArmorPen} Magic Armor Penetration";

                case 4: // LUC
                    string luckText = config.LUC_EnableLuckBonus ? $"+{config.LUC_LuckBonus * 100}% Luck\n" : "";
                    string fishingText = config.LUC_EnableFishing ? $"+{config.LUC_Fishing} Fishing Power\n" : "";
                    
                    return $"+{config.LUC_Crit}% Critical Chance\n" +
                        luckText +
                        fishingText +
                        $"-{config.LUC_AggroReduction} Aggro";

                case 5: // END
                    float drPercent = 100f * (1f - (1f / (1f + rpg.END * 0.01f)));
                    return $"+1 Defense every {config.END_DefensePerX} points\n" +
                        $"+{rpg.END}% Knockback Resistance\n" +
                        (config.EnableDR ? $"Current DR: -{drPercent:0.##}% Damage Taken\n" : "") +
                        (config.EnableEnemyKnockback ? "Knocks Back Non-Boss Enemies\n" : "") +
                        $"+{config.END_Aggro} Aggro";

                case 6: // POW
                    return $"+{config.POW_Damage}% All Damages (Not Covered By Other Stats)\n" +
                        $"+0.1% Fixed Damage (Melee, Ranged, Summon, Magic)";

                case 7: // DEX
                    string miningText = config.DEX_EnableMiningSpeed ? $"-{config.DEX_MiningSpeed}% Mining Time\n" : "";
                    string buildText = config.DEX_EnableBuildSpeed ? $"+{config.DEX_BuildSpeed}% Placement Speed\n" : "";
                    string rangeText = config.DEX_EnableRange ? $"+{config.DEX_Range} Block Reach\n" : "";
                    
                    return $"+{config.DEX_Damage}% Ranged Damage\n" +
                        $"+{config.DEX_ArmorPen} Ranged Armor Penetration\n" +
                        $"+1% Chance To No Consume Ammo\n" +
                        miningText +
                        buildText +
                        rangeText;

                case 8: // SPR
                    return $"+{config.SPR_Damage}% Summon Damage\n" +
                        $"+1 Minion per {config.SPR_MinionsPerX} SPR\n" +
                        $"+1 Sentry per {config.SPR_SentriesPerX} SPR";

                default: return "";
            }
        }

        private string GetXPSystemTooltip()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            string tooltip = "XP System Info:\n";
            
            tooltip += $"Damage XP: {config.DamageXP:0.##}x damage dealt\n";
            tooltip += $"Kill XP: {config.KillXP:0.##}x enemy max health\n";
            tooltip += $"Boss XP: ";
            if (config.UseFlatBossXP)
                tooltip += $"{config.DefaultFlatBossXP} flat XP\n";
            else
                tooltip += $"{config.BossXP}% of next level\n";
            
            if (config.SplitKillXP)
                tooltip += "XP is split evenly among all eligible players\n";
            
            if (config.EnableXPProximity)
                tooltip += $"Players must be within {config.XPProximityRange} pixels of enemies to get XP";
            
            return tooltip;
        }

        // Helper: Wrap a string to fit a maximum pixel width using the MouseText font.
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
                        // Single word exceeds max width; force-break.
                        sb.AppendLine(word);
                        currentLine = "";
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

        // Show the tooltip with word-wrap and dynamic height adjustment.
        private void ShowTooltip(string description)
        {
            tooltipPanel.BackgroundColor = new Color(33, 43, 79, 200);
            tooltipPanel.BorderColor = new Color(255, 255, 255, 200);

            float innerWidth = tooltipPanel.GetInnerDimensions().Width;
            string wrappedText = WrapText(description, innerWidth, 1f);
            tooltipText.SetText(wrappedText);
            tooltipText.Recalculate();

            // Calculate dynamic height manually
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float lineHeight = font.LineSpacing * 1f; // textScale = 1f
            int lineCount = wrappedText.Split('\n').Length;
            float totalTextHeight = lineCount * lineHeight;

            float padding = tooltipPanel.PaddingTop + tooltipPanel.PaddingBottom;
            tooltipPanel.Height.Set(totalTextHeight + padding + 8f, 0f); // Extra margin
            tooltipPanel.Recalculate();
        }

        // Hide tooltip by clearing text and making background/border transparent.
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

            if (rpg.StatPoints <= 0)
                return;

            switch (index)
            {
                case 0: rpg.VIT++; break;
                case 1: rpg.STR++; break;
                case 2: rpg.AGI++; break;
                case 3: rpg.INT++; break;
                case 4: rpg.LUC++; break;
                case 5: rpg.END++; break;
                case 6: rpg.POW++; break;
                case 7: rpg.DEX++; break;
                case 8: rpg.SPR++; break;
            }

            rpg.StatPoints--;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void OnStatDecrease(int index)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            switch (index)
            {
                case 0: if (rpg.VIT > 0) { rpg.VIT--; rpg.StatPoints++; } break;
                case 1: if (rpg.STR > 0) { rpg.STR--; rpg.StatPoints++; } break;
                case 2: if (rpg.AGI > 0) { rpg.AGI--; rpg.StatPoints++; } break;
                case 3: if (rpg.INT > 0) { rpg.INT--; rpg.StatPoints++; } break;
                case 4: if (rpg.LUC > 0) { rpg.LUC--; rpg.StatPoints++; } break;
                case 5: if (rpg.END > 0) { rpg.END--; rpg.StatPoints++; } break;
                case 6: if (rpg.POW > 0) { rpg.POW--; rpg.StatPoints++; } break;
                case 7: if (rpg.DEX > 0) { rpg.DEX--; rpg.StatPoints++; } break;
                case 8: if (rpg.SPR > 0) { rpg.SPR--; rpg.StatPoints++; } break;
            }

            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void OnResetStats(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            int total = rpg.VIT + rpg.STR + rpg.AGI + rpg.INT + rpg.LUC + rpg.END + rpg.POW + rpg.DEX + rpg.SPR;
            rpg.VIT = rpg.STR = rpg.AGI = rpg.INT = rpg.LUC = rpg.END = rpg.POW = rpg.DEX = rpg.SPR = 0;
            rpg.StatPoints += total;
            SoundEngine.PlaySound(SoundID.MenuClose);
        }
    }
}