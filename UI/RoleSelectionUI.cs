using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
using ReLogic.Graphics;
using Terraria.Localization;

namespace Stataria
{
    public class RoleSelectionUI : UIState
    {
        public UIPanel rolePanel;
        private UIText titleText;
        private UIText pointsText;
        private UIText activeRoleText;
        private UIList rolesList;
        private UIScrollbar scrollbar;

        private const float PANEL_PADDING = 15f;
        private const float SECTION_SPACING = 12f;
        private const float LINE_SPACING = 18f;

        private const float desiredWidth = 700f;
        private const float desiredHeight = 600f;

        private bool dragging = false;
        private Vector2 offset;
        private string confirmRoleID = null;
        private bool confirmAscend = false;
        private UIElement activeConfirmButton = null;

        public override void OnInitialize()
        {
            rolePanel = new UIPanel();
            rolePanel.Width.Set(700f, 0f);
            rolePanel.Height.Set(600f, 0f);
            rolePanel.HAlign = 0.5f;
            rolePanel.VAlign = 0.5f;
            rolePanel.SetPadding(15f);
            rolePanel.BackgroundColor = new Color(25, 35, 60, 240);
            rolePanel.BorderColor = new Color(100, 120, 180, 255);
            Append(rolePanel);

            rolePanel.OnLeftMouseDown += (evt, el) =>
            {
                if (!IsClickingOnInteractiveElement(evt.MousePosition))
                {
                    offset = new Vector2(evt.MousePosition.X - rolePanel.Left.Pixels, evt.MousePosition.Y - rolePanel.Top.Pixels);
                    dragging = true;
                }
            };
            rolePanel.OnLeftMouseUp += (evt, el) => dragging = false;

            titleText = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.Title"), 1.6f);
            titleText.Top.Set(5f, 0f);
            titleText.HAlign = 0.5f;
            titleText.TextColor = new Color(220, 220, 255);
            rolePanel.Append(titleText);

            pointsText = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.RebirthPoints").WithFormatArgs(0), 1.2f);
            pointsText.Top.Set(40f, 0f);
            pointsText.HAlign = 0.5f;
            pointsText.TextColor = new Color(255, 215, 100);
            rolePanel.Append(pointsText);

            activeRoleText = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.ActiveRoleNone"), 1f);
            activeRoleText.Top.Set(70f, 0f);
            activeRoleText.HAlign = 0.5f;
            activeRoleText.TextColor = new Color(100, 255, 100);
            rolePanel.Append(activeRoleText);

            rolesList = new UIList();
            rolesList.Width.Set(-25f, 1f);
            rolesList.Height.Set(-130f, 1f);
            rolesList.Top.Set(110f, 0f);
            rolesList.ListPadding = 20f;
            rolePanel.Append(rolesList);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(-150f, 1f);
            scrollbar.Top.Set(110f, 0f);
            scrollbar.Left.Set(-20f, 1f);
            rolePanel.Append(scrollbar);
            rolesList.SetScrollbar(scrollbar);

            RefreshRolesList();
        }

        public void RefreshRolesList()
        {
            rolesList.Clear();

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            if (rpg == null) return;

            pointsText.SetText(Language.GetText("Mods.Stataria.UI.RoleSelection.RebirthPoints").WithFormatArgs(rpg.RebirthPoints));

            foreach (var kvp in rpg.AvailableRoles)
            {
                var role = kvp.Value;
                var rolePanel = CreateRolePanel(role, rpg, player);
                rolesList.Add(rolePanel);
            }
        }

        private UIPanel CreateRolePanel(Role role, RPGPlayer rpg, Player player)
        {
            bool isActive = role.Status == RoleStatus.Active;
            bool isDeactivated = role.Status == RoleStatus.Deactivated;
            bool canAfford = role.CanActivate(rpg);

            var panel = new UIPanel();
            panel.Width.Set(0, 1f);
            panel.SetPadding(PANEL_PADDING);

            float contentHeight = CalculateRoleContentHeight(role, rpg);
            panel.Height.Set(contentHeight, 0f);

            bool isAngelRole = role.ID == "Cleric" && rpg.AscendedRoles.Contains("Cleric");

            if (isActive)
            {
                panel.BackgroundColor = isAngelRole ? new Color(60, 50, 30, 220) : new Color(40, 80, 40, 220);
                panel.BorderColor = isAngelRole ? new Color(255, 215, 100, 255) : new Color(80, 160, 80, 255);
            }
            else if (isDeactivated)
            {
                panel.BackgroundColor = isAngelRole ? new Color(50, 45, 30, 220) : new Color(80, 80, 40, 220);
                panel.BorderColor = isAngelRole ? new Color(200, 180, 80, 255) : new Color(160, 160, 80, 255);
            }
            else if (role.Status == RoleStatus.Locked)
            {
                panel.BackgroundColor = new Color(40, 40, 40, 180);
                panel.BorderColor = new Color(80, 80, 80, 220);
            }
            else
            {
                panel.BackgroundColor = isAngelRole ? new Color(45, 40, 60, 200) : new Color(45, 60, 85, 200);
                panel.BorderColor = isAngelRole ? new Color(180, 140, 220, 255) : new Color(80, 110, 150, 255);
            }

            float currentY = 0f;

            var nameText = new UIText(role.Name.ToUpper(), 1.4f);
            nameText.Top.Set(currentY, 0f);
            nameText.Left.Set(0f, 0f);
            nameText.TextColor = isAngelRole ? new Color(255, 230, 150) : (isActive ? new Color(255, 215, 100) :
                            (isDeactivated ? new Color(255, 255, 100) :
                            (role.Status == RoleStatus.Locked ? new Color(140, 140, 140) :
                                new Color(220, 220, 255))));
            panel.Append(nameText);
            currentY += 30f;

            if (isActive)
            {
                var activeIndicator = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.IndicatorActive"), 1f);
                activeIndicator.Top.Set(currentY, 0f);
                activeIndicator.Left.Set(0f, 0f);
                activeIndicator.TextColor = isAngelRole ? new Color(255, 215, 100) : new Color(100, 255, 100);
                panel.Append(activeIndicator);
                currentY += 25f;
            }
            else if (isDeactivated)
            {
                var deactivatedIndicator = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.IndicatorDeactivated"), 1f);
                deactivatedIndicator.Top.Set(currentY, 0f);
                deactivatedIndicator.Left.Set(0f, 0f);
                deactivatedIndicator.TextColor = isAngelRole ? new Color(220, 200, 100) : new Color(255, 255, 100);
                panel.Append(deactivatedIndicator);
                currentY += 25f;
            }

            var separator = new UIText(" ─────────────────────────────────────────────── ", 1f);
            separator.Top.Set(currentY - 5f, 0f);
            separator.HAlign = 0.5f;
            separator.TextColor = isAngelRole ? new Color(200, 160, 80, 150) : (isActive ? new Color(80, 160, 80, 150) :
                                (isDeactivated ? new Color(160, 160, 80, 150) :
                                (role.Status == RoleStatus.Locked ? new Color(80, 80, 80, 150) :
                                new Color(80, 110, 150, 150))));
            panel.Append(separator);
            currentY += 12f;


            string wrappedDescription = WrapText(role.Description, 620f, 0.95f);
            var descText = new UIText(wrappedDescription, 0.95f);
            descText.Top.Set(currentY, 0f);
            descText.Left.Set(0f, 0f);
            descText.Width.Set(-10f, 1f);
            descText.TextColor = role.Status == RoleStatus.Locked ?
                                new Color(140, 140, 140) : new Color(200, 200, 220);
            panel.Append(descText);
            currentY += GetTextHeight(wrappedDescription, 0.95f) + SECTION_SPACING;

            if (!string.IsNullOrEmpty(role.FlavorText))
            {
                string wrappedFlavorText = WrapText(role.FlavorText, 620f, 0.85f);
                var flavorText = new UIText(wrappedFlavorText, 0.85f);
                flavorText.Top.Set(currentY, 0f);
                flavorText.Left.Set(15f, 0f);
                flavorText.Width.Set(-25f, 1f);
                flavorText.TextColor = role.Status == RoleStatus.Locked ?
                                    new Color(120, 120, 100) : new Color(220, 200, 150);
                panel.Append(flavorText);
                currentY += GetTextHeight(wrappedFlavorText, 0.85f) + SECTION_SPACING;
            }

            var effectsHeader = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.EffectsHeader"), 1.1f);
            effectsHeader.Top.Set(currentY, 0f);
            effectsHeader.Left.Set(0f, 0f);
            effectsHeader.TextColor = role.Status == RoleStatus.Locked ?
                                    new Color(120, 120, 120) : new Color(255, 200, 100);
            panel.Append(effectsHeader);
            currentY += 25f;

            string effectsDesc = role.GetEffectsDescription();
            string wrappedEffects = WrapText(effectsDesc, 620f, 0.9f);
            var effectsText = new UIText(wrappedEffects, 0.9f);
            effectsText.Top.Set(currentY, 0f);
            effectsText.Left.Set(15f, 0f);
            effectsText.Width.Set(-25f, 1f);
            effectsText.TextColor = role.Status == RoleStatus.Locked ?
                                new Color(120, 120, 120) : new Color(220, 220, 240);
            panel.Append(effectsText);
            currentY += GetTextHeight(wrappedEffects, 0.9f) + SECTION_SPACING;

            if (isActive)
            {
                bool showAscendButton = role.ID == "Cleric" && !rpg.AscendedRoles.Contains("Cleric");

                var statusPanel = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.RoleSelection.DeactivateButton"), 1f, false);
                statusPanel.Height.Set(35f, 0f);
                statusPanel.Top.Set(currentY, 0f);
                statusPanel.BackgroundColor = new Color(120, 80, 40, 200);
                statusPanel.BorderColor = new Color(200, 140, 80, 255);
                statusPanel.SetPadding(8f);

                if (showAscendButton)
                {
                    statusPanel.Width.Set(180f, 0f);
                    statusPanel.HAlign = 0.5f;
                    statusPanel.Left.Set(-100f, 0f);
                }
                else
                {
                    statusPanel.Width.Set(200f, 0f);
                    statusPanel.HAlign = 0.5f;
                    statusPanel.Left.Set(0f, 0f);
                }

                statusPanel.OnLeftClick += (evt, el) =>
                {
                    if (rpg.DeactivateRole())
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        RefreshRolesList();
                    }
                };
                panel.Append(statusPanel);

                if (showAscendButton)
                {
                    var config = ModContent.GetInstance<StatariaConfig>();
                    int requiredRebirths = config.roleSettings.AngelRebirthRequirement;
                    int requiredPoints = config.roleSettings.AngelUnlockCost;
                    bool meetsRequirements = rpg.RebirthCount >= requiredRebirths && rpg.RebirthPoints >= requiredPoints;

                    LocalizedText buttonText = confirmAscend ? Language.GetText("Mods.Stataria.UI.RoleSelection.ConfirmAscend") : Language.GetText("Mods.Stataria.UI.RoleSelection.AscendButton");
                    var ascendButton = new UITextPanel<LocalizedText>(buttonText, 1f, false);
                    ascendButton.Width.Set(180f, 0f);
                    ascendButton.Height.Set(35f, 0f);
                    ascendButton.HAlign = 0.5f;
                    ascendButton.Left.Set(100f, 0f);
                    ascendButton.Top.Set(currentY, 0f);
                    ascendButton.SetPadding(8f);

                    if (meetsRequirements)
                    {
                        ascendButton.BackgroundColor = new Color(180, 140, 40, 220); // Golden yellow
                        ascendButton.BorderColor = new Color(255, 215, 100, 255);
                        ascendButton.TextColor = Color.White;

                        ascendButton.OnLeftClick += (evt, el) =>
                        {
                            if (!confirmAscend)
                            {
                                confirmAscend = true;
                                confirmRoleID = null;
                                activeConfirmButton = ascendButton;
                                SoundEngine.PlaySound(SoundID.MenuTick);
                                RefreshRolesList();
                                return;
                            }
                            confirmAscend = false;
                            activeConfirmButton = null;
                            rpg.RebirthPoints -= requiredPoints;
                            rpg.AscendedRoles.Add("Cleric");
                            rpg.UpdateAscendedRoleProperties();

                            SoundEngine.PlaySound(SoundID.Item29, player.Center);

                            if (Main.netMode != NetmodeID.Server)
                            {
                                for (int i = 0; i < 50; i++)
                                {
                                    Dust dust = Dust.NewDustPerfect(player.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(8f, 8f), 100, Color.Gold, 1.5f);
                                    dust.noGravity = true;
                                }
                            }

                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                rpg.SyncPlayer(-1, player.whoAmI, false);
                                player.GetModPlayer<ClericPlayer>().SyncAngelState();
                            }

                            RefreshRolesList();
                        };
                    }
                    else
                    {
                        ascendButton.BackgroundColor = new Color(70, 70, 70, 180);
                        ascendButton.BorderColor = new Color(120, 120, 120, 200);
                        ascendButton.TextColor = new Color(160, 160, 160);
                    }

                    ascendButton.OnUpdate += (el) =>
                    {
                        if (el.IsMouseHovering)
                        {
                            string tooltip = meetsRequirements
                                ? Language.GetTextValue("Mods.Stataria.UI.RoleSelection.AscendReady")
                                : Language.GetTextValue("Mods.Stataria.UI.RoleSelection.AscendRequirements", requiredRebirths, requiredPoints);
                            Main.instance.MouseText(tooltip);
                        }
                    };

                    panel.Append(ascendButton);
                }
            }

            else if (isDeactivated)
            {
                var reactivatePanel = new UITextPanel<LocalizedText>(Language.GetText("Mods.Stataria.UI.RoleSelection.ReactivateButton"), 1f, false);
                reactivatePanel.Width.Set(200f, 0f);
                reactivatePanel.Height.Set(35f, 0f);
                reactivatePanel.HAlign = 0.5f;
                reactivatePanel.Top.Set(currentY, 0f);
                reactivatePanel.BackgroundColor = new Color(80, 120, 80, 200);
                reactivatePanel.BorderColor = new Color(140, 200, 140, 255);
                reactivatePanel.SetPadding(8f);

                reactivatePanel.OnLeftClick += (evt, el) =>
                {
                    if (rpg.SwitchToRole(role.ID))
                    {
                        SoundEngine.PlaySound(SoundID.Research);
                        RefreshRolesList();
                    }
                };

                panel.Append(reactivatePanel);
            }
            else if (role.Status == RoleStatus.Locked)
            {
                var lockedPanel = new UIPanel();
                lockedPanel.Width.Set(250f, 0f);
                lockedPanel.Height.Set(35f, 0f);
                lockedPanel.HAlign = 0.5f;
                lockedPanel.Top.Set(currentY, 0f);
                lockedPanel.BackgroundColor = new Color(80, 40, 40, 200);
                lockedPanel.BorderColor = new Color(160, 80, 80, 255);
                lockedPanel.SetPadding(0f);

                var lockedText = new UIText(Language.GetText("Mods.Stataria.UI.RoleSelection.LockedRole"), 0.9f);
                lockedText.HAlign = 0.5f;
                lockedText.VAlign = 0.5f;
                lockedText.TextColor = new Color(255, 120, 120);
                lockedPanel.Append(lockedText);
                panel.Append(lockedPanel);
            }
            else
            {
                int cost = role.GetCurrentSwitchCost(rpg);
                LocalizedText buttonText = confirmRoleID == role.ID ? Language.GetText("Mods.Stataria.UI.RoleSelection.ConfirmSwitch") : (cost > 0 ? Language.GetText("Mods.Stataria.UI.RoleSelection.SwitchCostButton").WithFormatArgs(cost) : Language.GetText("Mods.Stataria.UI.RoleSelection.SelectFreeButton"));

                var switchButton = new UITextPanel<LocalizedText>(buttonText, 1f, false);
                switchButton.Width.Set(250f, 0f);
                switchButton.Height.Set(40f, 0f);
                switchButton.HAlign = 0.5f;
                switchButton.Top.Set(currentY, 0f);

                if (canAfford)
                {
                    switchButton.BackgroundColor = new Color(60, 120, 60, 220);
                    switchButton.BorderColor = new Color(100, 200, 100, 255);
                    switchButton.OnLeftClick += (evt, el) =>
                    {
                        if (confirmRoleID != role.ID)
                        {
                            confirmRoleID = role.ID;
                            confirmAscend = false;
                            activeConfirmButton = switchButton;
                            SoundEngine.PlaySound(SoundID.MenuTick);
                            RefreshRolesList();
                            return;
                        }
                        confirmRoleID = null;
                        activeConfirmButton = null;
                        if (rpg.SwitchToRole(role.ID))
                        {
                            SoundEngine.PlaySound(SoundID.Research);
                            RefreshRolesList();
                        }
                    };
                }
                else
                {
                    switchButton.BackgroundColor = new Color(60, 60, 60, 180);
                    switchButton.BorderColor = new Color(120, 120, 120, 200);
                    switchButton.TextColor = new Color(160, 160, 160);
                }

                panel.Append(switchButton);
            }

            return panel;
        }

        private bool IsClickingOnInteractiveElement(Vector2 mousePosition)
        {
            if (rolesList?.ContainsPoint(mousePosition) == true)
                return true;

            if (scrollbar?.ContainsPoint(mousePosition) == true)
                return true;

            return false;
        }

        private float CalculateRoleContentHeight(Role role, RPGPlayer rpg)
        {
            float height = PANEL_PADDING * 2;

            height += 30f;

            if (role.Status == RoleStatus.Active || role.Status == RoleStatus.Deactivated)
                height += 25f;

            height += 12f;

            string wrappedDesc = WrapText(role.Description, 620f, 0.95f);
            height += GetTextHeight(wrappedDesc, 0.95f) + SECTION_SPACING;

            if (!string.IsNullOrEmpty(role.FlavorText))
            {
                string wrappedFlavorText = WrapText(role.FlavorText, 620f, 0.85f);
                height += GetTextHeight(wrappedFlavorText, 0.85f) + SECTION_SPACING;
            }

            height += 25f;

            string effectsDesc = role.GetEffectsDescription();
            string wrappedEffects = WrapText(effectsDesc, 620f, 0.9f);
            height += GetTextHeight(wrappedEffects, 0.9f) + SECTION_SPACING;

            height += 40f;

            return Math.Max(height, 180f);
        }

        private string WrapText(string text, float maxWidth, float textScale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string[] words = text.Split(' ');
            var lines = new List<string>();
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                Vector2 size = font.MeasureString(testLine) * textScale;

                if (size.X > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            return string.Join("\n", lines);
        }

        private float GetTextHeight(string text, float textScale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            int lineCount = text.Split('\n').Length;
            return lineCount * font.LineSpacing * textScale;
        }

        public void ResetConfirmation()
        {
            if (confirmRoleID != null || confirmAscend)
            {
                confirmRoleID = null;
                confirmAscend = false;
                activeConfirmButton = null;
                RefreshRolesList();
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if ((confirmRoleID != null || confirmAscend) && Main.mouseLeft)
            {
                if (activeConfirmButton == null || !activeConfirmButton.ContainsPoint(Main.MouseScreen))
                {
                    confirmRoleID = null;
                    confirmAscend = false;
                    activeConfirmButton = null;
                    RefreshRolesList();
                }
            }

            if (rolePanel.ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;

            if (dragging)
            {
                Vector2 mouse = Main.MouseScreen;
                rolePanel.Left.Set(mouse.X - offset.X, 0f);
                rolePanel.Top.Set(mouse.Y - offset.Y, 0f);
                rolePanel.Recalculate();
            }

            Player player = Main.LocalPlayer;
            if (player != null && player.active)
            {
                RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
                if (rpg != null)
                {
                    pointsText.SetText(Language.GetText("Mods.Stataria.UI.RoleSelection.RebirthPoints").WithFormatArgs(rpg.RebirthPoints));

                    if (rpg.ActiveRole != null)
                    {
                        if (rpg.ActiveRole.Status == RoleStatus.Active)
                        {
                            activeRoleText.SetText(Language.GetText("Mods.Stataria.UI.RoleSelection.ActiveRole").WithFormatArgs(rpg.ActiveRole.Name));
                        }
                        else if (rpg.ActiveRole.Status == RoleStatus.Deactivated)
                        {
                            activeRoleText.SetText(Language.GetText("Mods.Stataria.UI.RoleSelection.ActiveRoleDeactivated").WithFormatArgs(rpg.ActiveRole.Name));
                        }
                    }
                    else
                    {
                        activeRoleText.SetText(Language.GetText("Mods.Stataria.UI.RoleSelection.ActiveRoleNone"));
                    }
                }
            }
        }

        public override void Recalculate()
        {
            if (rolePanel != null)
            {
                float targetWidth = Math.Min(desiredWidth, Main.screenWidth - 20f);
                float targetHeight = Math.Min(desiredHeight, Main.screenHeight - 70f);

                rolePanel.Width.Set(targetWidth, 0f);
                rolePanel.Height.Set(targetHeight, 0f);

                float baseX = (Main.screenWidth - targetWidth) * 0.5f;
                float baseY = (Main.screenHeight - targetHeight) * 0.5f;

                float clampedLeft = Math.Clamp(rolePanel.Left.Pixels, -baseX + 10f, Math.Max(-baseX + 10f, baseX - 10f));
                float minY = 50f - baseY;
                float maxY = baseY - 10f;
                float clampedTop = Math.Clamp(rolePanel.Top.Pixels, minY, Math.Max(minY, maxY));

                rolePanel.Left.Set(clampedLeft, 0f);
                rolePanel.Top.Set(clampedTop, 0f);
            }
            base.Recalculate();
        }
    }
}