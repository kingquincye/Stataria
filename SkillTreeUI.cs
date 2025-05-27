using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;

namespace Stataria
{
    public class SkillTreeUI : UIState
    {
        private UIPanel mainPanel;
        private UIScrollbar scrollbar;
        private UIList abilityList;
        private UIText titleText;
        private UIText pointsText;
        private UITextPanel<string> resetAbilitiesButton;
        private UITextPanel<string> showHiddenButton;

        private bool dragging = false;
        private Vector2 offset;
        private bool showHiddenAbilities = false;

        private const float MaxVisibleHeight = 600f;

        public override void OnInitialize()
        {
            mainPanel = new UIPanel();
            mainPanel.Width.Set(500f, 0f);
            mainPanel.Height.Set(MaxVisibleHeight, 0f);
            mainPanel.HAlign = 0.5f;
            mainPanel.VAlign = 0.5f;
            mainPanel.SetPadding(10f);
            mainPanel.BackgroundColor = new Color(73, 94, 171, 200);
            mainPanel.BorderColor = new Color(50, 50, 150, 255);
            Append(mainPanel);

            mainPanel.OnLeftMouseDown += (evt, el) =>
            {
                if (scrollbar.ContainsPoint(evt.MousePosition))
                    return;

                offset = new Vector2(evt.MousePosition.X - mainPanel.Left.Pixels, evt.MousePosition.Y - mainPanel.Top.Pixels);
                dragging = true;
            };
            mainPanel.OnLeftMouseUp += (evt, el) => dragging = false;

            titleText = new UIText("Rebirth Abilities", 1.2f);
            titleText.Top.Set(0f, 0f);
            titleText.HAlign = 0.5f;
            mainPanel.Append(titleText);

            pointsText = new UIText("Rebirth Points: 0", 1f);
            pointsText.Top.Set(35f, 0f);
            pointsText.HAlign = 0.5f;
            pointsText.TextColor = new Color(255, 230, 100);
            mainPanel.Append(pointsText);

            abilityList = new UIList();
            abilityList.Width.Set(-25f, 1f);
            abilityList.Height.Set(-120f, 1f);
            abilityList.Top.Set(70f, 0f);
            abilityList.ListPadding = 10f;
            mainPanel.Append(abilityList);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(-140f, 1f);
            scrollbar.Top.Set(70f, 0f);
            scrollbar.Left.Set(-20f, 1f);
            mainPanel.Append(scrollbar);
            abilityList.SetScrollbar(scrollbar);

            var backButton = new UITextPanel<string>("Back", 0.9f, false);
            backButton.Width.Set(60f, 0f);
            backButton.Height.Set(30f, 0f);
            backButton.Top.Set(10f, 0f);
            backButton.Left.Set(10f, 0f);
            backButton.BackgroundColor = new Color(73, 94, 171, 255);
            backButton.OnLeftClick += (evt, el) =>
            {
                StatariaUI.SkillTreeUI.SetState(null);
                StatariaUI.StatUI.SetState(StatariaUI.Panel);
                SoundEngine.PlaySound(SoundID.MenuClose);
            };
            mainPanel.Append(backButton);

            resetAbilitiesButton = new UITextPanel<string>("RESET", 0.9f, false);
            resetAbilitiesButton.Width.Set(80f, 0f);
            resetAbilitiesButton.Height.Set(30f, 0f);
            resetAbilitiesButton.Top.Set(10f, 0f);
            resetAbilitiesButton.Left.Set(mainPanel.Width.Pixels - 100f, 0f);
            resetAbilitiesButton.BackgroundColor = new Color(180, 80, 80, 255);
            resetAbilitiesButton.OnLeftClick += (evt, el) =>
            {
                ResetAllAbilities();
                SoundEngine.PlaySound(SoundID.MenuClose);
            };
            mainPanel.Append(resetAbilitiesButton);

            showHiddenButton = new UITextPanel<string>("Show Hidden", 0.9f, false);
            showHiddenButton.Width.Set(120f, 0f);
            showHiddenButton.Height.Set(30f, 0f);
            showHiddenButton.Top.Set(mainPanel.Height.Pixels - 60f, 0f);
            showHiddenButton.HAlign = 0.5f;
            showHiddenButton.BackgroundColor = new Color(100, 100, 100, 200);
            showHiddenButton.BorderColor = new Color(150, 150, 150, 200);
            showHiddenButton.OnLeftClick += (evt, el) =>
            {
                showHiddenAbilities = !showHiddenAbilities;
                UpdateShowHiddenButton();
                RefreshAbilitiesList();
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            mainPanel.Append(showHiddenButton);
        }

        private void UpdateShowHiddenButton()
        {
            if (showHiddenAbilities)
            {
                showHiddenButton.BackgroundColor = new Color(80, 180, 80, 200);
                showHiddenButton.BorderColor = new Color(100, 255, 100, 200);
            }
            else
            {
                showHiddenButton.BackgroundColor = new Color(100, 100, 100, 200);
                showHiddenButton.BorderColor = new Color(150, 150, 150, 200);
            }
        }

        public void RefreshAbilitiesList()
        {
            abilityList.Clear();

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            if (rpg == null) return;

            pointsText.SetText($"Rebirth Points: {rpg.RebirthPoints}");

            foreach (var kvp in rpg.RebirthAbilities)
            {
                var ability = kvp.Value;
                string abilityKey = kvp.Key;

                if (abilityKey == "AutoClicker")
                {
                    var currentConfig = ModContent.GetInstance<StatariaConfig>();
                    if (!(currentConfig.modIntegration.EnableClickerClassIntegration && ClickerSupportHelper.ClickerClassLoaded))
                    {
                        continue;
                    }
                }

                if (ability.IsHidden && !showHiddenAbilities)
                    continue;

                float basePanelHeight = 70f;

                string wrappedDesc = WrapText(ability.GetCurrentEffectDescription(), 400);
                int descLines = wrappedDesc.Split('\n').Length;
                float panelHeight = basePanelHeight + descLines * 18f;

                var abilityPanel = new UIPanel
                {
                    Width = { Pixels = 0, Percent = 1f },
                    Height = { Pixels = panelHeight },
                    BackgroundColor = ability.IsUnlocked ? new Color(80, 120, 80, 200) : new Color(100, 100, 100, 200)
                };

                if (ability.IsHidden && showHiddenAbilities)
                {
                    Color bgColor = abilityPanel.BackgroundColor;
                    abilityPanel.BackgroundColor = new Color(bgColor.R, bgColor.G, bgColor.B, (int)(bgColor.A * 0.5f));
                }

                var nameText = new UIText(ability.Name, 1f);
                nameText.Top.Set(-5f, 0f);
                nameText.Left.Set(10f, 0f);
                if (ability.IsHidden && showHiddenAbilities)
                {
                    nameText.TextColor = Color.White * 0.5f;
                }
                abilityPanel.Append(nameText);

                var descText = new UIText(wrappedDesc, 0.8f);
                descText.Top.Set(20f, 0f);
                descText.Left.Set(10f, 0f);
                descText.TextColor = ability.IsHidden && showHiddenAbilities ? Color.LightGray * 0.5f : Color.LightGray;
                abilityPanel.Append(descText);

                var costText = new UIText($"Cost: {ability.Cost} RP", 0.8f);
                costText.Top.Set(panelHeight - 30f, 0f);
                costText.Left.Set(10f, 0f);
                costText.TextColor = ability.IsHidden && showHiddenAbilities ? Color.Yellow * 0.5f : Color.Yellow;
                abilityPanel.Append(costText);

                var hideButton = new UITextPanel<string>("Hide", 0.8f, false)
                {
                    Width = { Pixels = 80f },
                    Height = { Pixels = 30f },
                    Top = { Pixels = -5f },
                    Left = { Pixels = 360f },
                    BackgroundColor = ability.IsHidden ? new Color(180, 80, 80, 200) : new Color(100, 100, 100, 150),
                    BorderColor = ability.IsHidden ? new Color(255, 100, 100, 200) : new Color(150, 150, 150, 150)
                };

                hideButton.OnLeftClick += (evt, el) =>
                {
                    ability.IsHidden = !ability.IsHidden;
                    RefreshAbilitiesList();
                    SoundEngine.PlaySound(SoundID.MenuTick);

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        rpg.SyncAbilities();
                    }

                    dragging = false;
                    Main.mouseLeft = false;
                    Main.mouseLeftRelease = false;
                };

                abilityPanel.Append(hideButton);

                bool canUnlock = !ability.IsUnlocked || (ability.IsStackable && ability.Level < ability.MaxLevel);
                string buttonText = ability.IsUnlocked ? (ability.IsStackable ? "Upgrade" : "Unlocked") : "Unlock";

                var unlockButton = new UITextPanel<string>(buttonText, 0.8f, false)
                {
                    Width = { Pixels = 80f },
                    Height = { Pixels = 30f },
                    Top = { Pixels = panelHeight - 55f },
                    Left = { Pixels = 360f },
                    BackgroundColor = canUnlock && rpg.RebirthPoints >= ability.Cost ? new Color(80, 150, 80, 255) : new Color(150, 150, 150, 150)
                };

                if (canUnlock)
                {
                    unlockButton.OnLeftClick += (evt, el) =>
                    {
                        if (rpg.RebirthPoints >= ability.Cost)
                        {
                            if (ability.Unlock(rpg))
                            {
                                SoundEngine.PlaySound(SoundID.Research);
                                RefreshAbilitiesList();
                                if (Main.netMode == NetmodeID.MultiplayerClient)
                                    rpg.SyncAbilities();
                                dragging = false;
                                Main.mouseLeft = false;
                                Main.mouseLeftRelease = false;
                            }
                        }
                    };
                }

                abilityPanel.Append(unlockButton);

                if (ability.IsUnlocked && ability.AbilityType == RebirthAbilityType.Toggleable)
                {
                    bool isEnabled = ability.AbilityData.ContainsKey("Enabled") && (bool)ability.AbilityData["Enabled"];
                    var toggleButton = new UITextPanel<string>(isEnabled ? "On" : "Off", 0.8f, false)
                    {
                        Width = { Pixels = 60f },
                        Height = { Pixels = 30f },
                        Top = { Pixels = panelHeight - 55f },
                        Left = { Pixels = 280f },
                        BackgroundColor = isEnabled ? new Color(80, 180, 80, 255) : new Color(180, 80, 80, 255)
                    };

                    toggleButton.OnLeftClick += (evt, el) =>
                    {
                        bool currentState = (bool)ability.AbilityData["Enabled"];
                        ability.AbilityData["Enabled"] = !currentState;

                        toggleButton.SetText(!currentState ? "On" : "Off");
                        toggleButton.BackgroundColor = !currentState ? new Color(80, 180, 80, 255) : new Color(180, 80, 80, 255);

                        SoundEngine.PlaySound(SoundID.MenuTick);

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            rpg.SyncAbilities();
                    };

                    abilityPanel.Append(toggleButton);
                }

                abilityList.Add(abilityPanel);
            }
        }

        private void ResetAllAbilities()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            if (rpg == null) return;

            int refundedPoints = 0;

            foreach (var kvp in rpg.RebirthAbilities)
            {
                RebirthAbility ability = kvp.Value;

                if (ability.IsUnlocked)
                {
                    if (ability.IsStackable)
                    {
                        refundedPoints += ability.Cost * ability.Level;
                        ability.Level = 0;
                    }
                    else
                    {
                        refundedPoints += ability.Cost;
                    }

                    ability.IsUnlocked = false;

                    if (ability.AbilityType == RebirthAbilityType.Toggleable &&
                        ability.AbilityData.ContainsKey("Enabled"))
                    {
                        ability.AbilityData["Enabled"] = false;
                    }
                }
            }

            rpg.RebirthPoints += refundedPoints;

            if (refundedPoints > 0)
            {
                CombatText.NewText(player.Hitbox, Color.Gold, $"+{refundedPoints} RP Refunded!");
            }

            RefreshAbilitiesList();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                rpg.SyncAbilities();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mainPanel.ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;

            if (dragging)
            {
                Vector2 mouse = Main.MouseScreen;
                mainPanel.Left.Set(mouse.X - offset.X, 0f);
                mainPanel.Top.Set(mouse.Y - offset.Y, 0f);
                mainPanel.Recalculate();
            }

            Player player = Main.LocalPlayer;
            if (player != null && player.active)
            {
                RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
                if (rpg != null)
                    pointsText.SetText($"Rebirth Points: {rpg.RebirthPoints}");
            }
        }

        private string WrapText(string text, int maxWidth)
        {
            string result = "";
            string[] words = text.Split(' ');
            float lineWidth = 0f;
            float spaceWidth = FontAssets.MouseText.Value.MeasureString(" ").X;

            foreach (string word in words)
            {
                float wordWidth = FontAssets.MouseText.Value.MeasureString(word).X;
                if (lineWidth + wordWidth + spaceWidth > maxWidth)
                {
                    result += "\n" + word;
                    lineWidth = wordWidth + spaceWidth;
                }
                else
                {
                    if (result.Length > 0)
                        result += " ";
                    result += word;
                    lineWidth += wordWidth + spaceWidth;
                }
            }

            return result;
        }
    }
}