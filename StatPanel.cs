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

        private UITextPanel<string> resetButton;

        private bool dragging = false;
        private Vector2 offset;

        public override void OnInitialize()
        {
            // Main panel
            panel = new UIPanel();
            panel.Width.Set(300f, 0f);
            panel.Height.Set(400f, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;

            panel.BackgroundColor = new Color(63, 82, 151, 200);
            panel.BorderColor = new Color(0, 0, 0, 255);
            Append(panel);

            // Enable dragging the entire panel
            panel.SetPadding(0);
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

            // Stat points label
            statPointsText = new UIText("Points: 0");
            statPointsText.Top.Set(top, 0f);
            statPointsText.Left.Set(200f, 0f);
            panel.Append(statPointsText);

            top += 30f;

            // XP label
            xpText = new UIText("XP: 0 / 100");
            xpText.Top.Set(top, 0f);
            xpText.Left.Set(10f, 0f);
            panel.Append(xpText);

            top += 40f;

            // Create rows for each stat
            statTexts = new UIText[statNames.Length];
            plusButtons = new UITextPanel<string>[statNames.Length];

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
                    Left = { Pixels = 220f },
                    Width = { Pixels = 40f },
                    Height = { Pixels = 25f },
                    BackgroundColor = new Color(63, 82, 151, 200),
                    BorderColor = new Color(0, 0, 0, 255)
                };
                plusBtn.SetPadding(0f);
                int statIndex = i;
                plusBtn.OnLeftClick += (evt, el) => OnStatIncrease(statIndex);
                panel.Append(plusBtn);
                plusButtons[i] = plusBtn;

                // MouseOver and MouseOut events for showing/hiding tooltip
                string tip = GetStatTooltip(statIndex);
                statLabel.OnMouseOver += (evt, el) => ShowTooltip(tip);
                statLabel.OnMouseOut += (evt, el) => HideTooltip();
                plusBtn.OnMouseOver += (evt, el) => ShowTooltip(tip);
                plusBtn.OnMouseOut += (evt, el) => HideTooltip();

                top += 30f;
            }

            // Reset stats button
            resetButton = new UITextPanel<string>("Reset Stats", textScale: 0.9f, large: false)
            {
                Top = { Pixels = top + 10f },
                Left = { Pixels = 80f },
                Width = { Pixels = 120f },
                Height = { Pixels = 30f },
                BackgroundColor = new Color(63, 82, 151, 200),
                BorderColor = new Color(0, 0, 0, 255)
            };
            resetButton.OnLeftClick += OnResetStats;
            panel.Append(resetButton);

            // Tooltip panel setup: Docked below the main panel content
            tooltipPanel = new UIPanel();
            tooltipPanel.Width.Set(280f, 0f);
            // Start with 0 height so it doesn't show initially
            tooltipPanel.Height.Set(0f, 0f);
            tooltipPanel.Left.Set(10f, 0f);
            // Position it just below the stats area (you may adjust the Y position as needed)
            tooltipPanel.Top.Set(420f, 0f);
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

                if (rpg.StatPoints > 0)
                {
                    plusButtons[i].BackgroundColor = new Color(63, 82, 151, 200);
                    plusButtons[i].BorderColor = new Color(0, 0, 0, 255);
                }
                else
                {
                    plusButtons[i].BackgroundColor = new Color(80, 80, 80, 200);
                    plusButtons[i].BorderColor = new Color(20, 20, 20, 255);
                }
            }
        }

        // Helper: Returns the unwrapped tooltip for a stat based on its index.
        private string GetStatTooltip(int statIndex)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            switch (statIndex)
            {
                case 0: return $"VIT: +{config.VIT_HP} Max Health, Increased Health Regen (VIT/2)";
                case 1: return $"STR: +{config.STR_Damage}% Melee Damage";
                case 2: return "AGI: +1% Movement/Attack Speed (diminishing returns after 75+ points)";
                case 3: return $"INT: +{config.INT_MP} Max Mana, Increased Mana Regen (INT/2), +{config.INT_Damage}% Magic Damage";
                case 4: return $"LUC: +{(config.LUC_Crit / 100f):0.##}% Crit Chance";
                case 5: return "END: +1 Defense (every 2 points), Increased Damage Reduction (cannot reach 100%), +1% Debuff/Knockback Resist, Knocks Back Enemies (excl. bosses)";
                case 6: return $"POW: +{config.POW_Damage}% All Damage";
                case 7: return $"DEX: +{config.DEX_Damage}% Ranged Damage, +1% Chance To Not Consume Ammo";
                case 8: return $"SPR: +{config.SPR_Damage}% Summon Damage, Increased Minion (every 10 points) & Sentry (every 20 points) Slots";
                default: return "";
            }
        }

        // Helper: Wrap a string to fit a maximum pixel width using the MouseText font.
        // This method splits the string word by word and inserts newline characters where needed.
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
            float lineHeight = font.LineSpacing * 1f; // Assuming textScale is 1f
            int lineCount = wrappedText.Split('\n').Length;
            float totalTextHeight = lineCount * lineHeight;

            float padding = tooltipPanel.PaddingTop + tooltipPanel.PaddingBottom;
            tooltipPanel.Height.Set(totalTextHeight + padding + 8f, 0f); // Extra margin
            tooltipPanel.Recalculate();
        }

        // Hide tooltip by clearing text and making background and border transparent.
        private void HideTooltip()
        {
            tooltipText.SetText("");
            tooltipPanel.BackgroundColor = Color.Transparent;
            tooltipPanel.BorderColor = Color.Transparent;
            // Optionally collapse the panel height
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