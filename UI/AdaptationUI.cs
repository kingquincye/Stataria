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
using Terraria.Localization;
using Stataria.Core;
using Stataria.Players;
using Stataria.UI.Elements;

namespace Stataria.UI
{
    public enum AdaptationFilter
    {
        All,
        DisabledOnly,
        Boss,
        Mob,
        Debuff,
        Environment,
        Death
    }

    public class AdaptationUI : UIState
    {
        public UIPanel mainPanel;
        private UIText titleText;
        private UIText subtitleText;
        private UITextInput searchInput;
        private UIList adaptationList;
        private UIScrollbar scrollbar;
        private UIPanel filterButtonContainer;

        private AdaptationFilter currentFilter = AdaptationFilter.All;
        private string searchText = "";

        private bool dragging = false;
        private Vector2 offset;

        public override void OnInitialize()
        {
            mainPanel = new UIPanel();
            mainPanel.Width.Set(680f, 0f);
            mainPanel.Height.Set(580f, 0f);
            mainPanel.HAlign = 0.5f;
            mainPanel.VAlign = 0.5f;
            mainPanel.SetPadding(12f);
            mainPanel.BackgroundColor = new Color(20, 26, 45, 240);
            mainPanel.BorderColor = new Color(80, 110, 170, 255);
            Append(mainPanel);

            mainPanel.OnLeftMouseDown += (evt, el) =>
            {
                if (!IsClickingOnInteractiveElement(evt.MousePosition))
                {
                    offset = new Vector2(evt.MousePosition.X - mainPanel.Left.Pixels, evt.MousePosition.Y - mainPanel.Top.Pixels);
                    dragging = true;
                }
            };
            mainPanel.OnLeftMouseUp += (evt, el) => dragging = false;

            // Title
            titleText = new UIText(Language.GetTextValue("Mods.Stataria.UI.Adaptation.Title"), 1.4f);
            if (string.IsNullOrEmpty(titleText.Text) || titleText.Text == "Mods.Stataria.UI.Adaptation.Title")
                titleText.SetText("ADAPTATION MATRIX", 1.4f, false);
            titleText.Top.Set(4f, 0f);
            titleText.HAlign = 0.5f;
            titleText.TextColor = new Color(220, 230, 255);
            mainPanel.Append(titleText);

            // Close button (X)
            var closeButton = new UITextPanel<string>("X", 0.9f, true);
            closeButton.Width.Set(30f, 0f);
            closeButton.Height.Set(30f, 0f);
            closeButton.Left.Set(-32f, 1f);
            closeButton.Top.Set(4f, 0f);
            closeButton.SetPadding(4f);
            closeButton.BackgroundColor = new Color(120, 40, 40, 220);
            closeButton.BorderColor = new Color(200, 80, 80, 255);
            closeButton.OnLeftClick += (evt, el) =>
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                StatariaUI.AdaptationUI?.SetState(null);
            };
            mainPanel.Append(closeButton);

            // Subtitle / Counter
            subtitleText = new UIText("", 0.9f);
            subtitleText.Top.Set(32f, 0f);
            subtitleText.HAlign = 0.5f;
            subtitleText.TextColor = new Color(160, 180, 220);
            mainPanel.Append(subtitleText);

            // Search Bar Input
            var placeholderText = Language.GetText("Mods.Stataria.UI.Adaptation.SearchPlaceholder");
            searchInput = new UITextInput(placeholderText, "", (text) =>
            {
                searchText = text ?? "";
                RefreshAdaptationList();
            }, 40);
            searchInput.Top.Set(56f, 0f);
            searchInput.Left.Set(10f, 0f);
            searchInput.Width.Set(-20f, 1f);
            searchInput.Height.Set(32f, 0f);
            mainPanel.Append(searchInput);

            // Filter buttons row
            filterButtonContainer = new UIPanel();
            filterButtonContainer.Top.Set(94f, 0f);
            filterButtonContainer.Left.Set(10f, 0f);
            filterButtonContainer.Width.Set(-20f, 1f);
            filterButtonContainer.Height.Set(36f, 0f);
            filterButtonContainer.SetPadding(2f);
            filterButtonContainer.BackgroundColor = new Color(15, 20, 35, 200);
            filterButtonContainer.BorderColor = new Color(50, 70, 110, 200);
            mainPanel.Append(filterButtonContainer);

            CreateFilterButtons();

            // Adaptations Scroll List
            adaptationList = new UIList();
            adaptationList.Width.Set(-28f, 1f);
            adaptationList.Height.Set(-145f, 1f);
            adaptationList.Top.Set(136f, 0f);
            adaptationList.Left.Set(5f, 0f);
            adaptationList.ListPadding = 6f;
            mainPanel.Append(adaptationList);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(-145f, 1f);
            scrollbar.Top.Set(136f, 0f);
            scrollbar.Left.Set(-18f, 1f);
            mainPanel.Append(scrollbar);
            adaptationList.SetScrollbar(scrollbar);

            RefreshAdaptationList();
        }

        private int updateTimer = 0;

        private void CreateFilterButtons()
        {
            filterButtonContainer.RemoveAllChildren();

            (AdaptationFilter Filter, string Label, float Width, Color ActiveColor)[] filters = new[]
            {
                (AdaptationFilter.All, "All", 56f, new Color(100, 180, 255)),
                (AdaptationFilter.DisabledOnly, "Disabled Only", 105f, new Color(255, 100, 100)),
                (AdaptationFilter.Boss, "Boss", 62f, AdaptationCategory.Boss.GetCategoryColor()),
                (AdaptationFilter.Mob, "Mob", 58f, AdaptationCategory.Mob.GetCategoryColor()),
                (AdaptationFilter.Debuff, "Debuff", 75f, AdaptationCategory.Debuff.GetCategoryColor()),
                (AdaptationFilter.Environment, "Environment", 108f, AdaptationCategory.Environment.GetCategoryColor()),
                (AdaptationFilter.Death, "Death", 68f, AdaptationCategory.Death.GetCategoryColor())
            };

            float leftOffset = 4f;

            foreach (var f in filters)
            {
                bool isActive = currentFilter == f.Filter;
                var btn = new UITextPanel<string>(f.Label, 0.75f, false);
                btn.Width.Set(f.Width, 0f);
                btn.Height.Set(28f, 0f);
                btn.Left.Set(leftOffset, 0f);
                btn.Top.Set(2f, 0f);
                btn.SetPadding(2f);

                if (isActive)
                {
                    btn.BackgroundColor = new Color(50, 70, 120, 230);
                    btn.BorderColor = f.ActiveColor;
                    btn.TextColor = Color.White;
                }
                else
                {
                    btn.BackgroundColor = new Color(25, 32, 50, 180);
                    btn.BorderColor = new Color(60, 80, 110, 180);
                    btn.TextColor = new Color(170, 180, 200);
                }

                AdaptationFilter targetFilter = f.Filter;
                btn.OnLeftClick += (evt, el) =>
                {
                    currentFilter = targetFilter;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    CreateFilterButtons();
                    RefreshAdaptationList();
                };

                filterButtonContainer.Append(btn);
                leftOffset += f.Width + 4f;
            }
        }

        public void RefreshAdaptationList(bool preserveScroll = false)
        {
            float prevScroll = (preserveScroll && scrollbar != null) ? scrollbar.ViewPosition : 0f;
            adaptationList?.Clear();

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            var adaptor = player.GetModPlayer<AdaptationPlayer>();
            if (adaptor == null || adaptor.Adaptations == null)
                return;

            int totalAcquired = adaptor.Adaptations.Values.Count(v => v.Level > 0 || v.CurrentExp > 0f || v.Disabled);
            int totalDisabled = adaptor.Adaptations.Values.Count(v => v.Disabled);

            subtitleText.SetText($"Acquired: {totalAcquired}  |  Disabled: {totalDisabled}");

            if (totalAcquired == 0)
            {
                var emptyNotice = new UITextPanel<string>("No adaptations acquired yet!\nTake damage, fight enemies, endure debuffs or hazards to adapt.", 0.9f, false);
                emptyNotice.Width.Set(0, 0.95f);
                emptyNotice.Height.Set(70f, 0f);
                emptyNotice.HAlign = 0.5f;
                emptyNotice.Top.Set(30f, 0f);
                emptyNotice.BackgroundColor = new Color(30, 40, 65, 180);
                emptyNotice.BorderColor = new Color(70, 90, 130, 200);
                adaptationList.Add(emptyNotice);
                return;
            }

            string cleanSearch = searchText.Trim().ToLowerInvariant();

            var matchingEntries = adaptor.Adaptations.Where(kvp =>
            {
                var key = kvp.Key;
                var data = kvp.Value;

                // 0. Only show adaptations if player has actually started adapting (Level > 0 || CurrentExp > 0 || Disabled)
                if (data.Level <= 0 && data.CurrentExp <= 0f && !data.Disabled)
                    return false;

                // Mod-specific check: Calamity Mod
                bool isCalamityAdaptation = key.TargetId == "SulphurousWater" || key.TargetId == "AbyssDarkness" || key.TargetId == "AbyssPressure";
                if (isCalamityAdaptation && !CalamitySupportHelper.CalamityLoaded)
                    return false;

                // Mod-specific check: Wrath of the Gods Mod
                bool isWotgAdaptation = key.TargetId == "Erasure" || (key.TargetId != null && key.TargetId.Contains("Erasure")) || (key.DisplayName != null && key.DisplayName.Contains("Erasure"));
                if (isWotgAdaptation && !WrathOfTheGodsSupportHelper.WotGLoaded)
                    return false;

                // 1. Filter by status / category
                if (currentFilter == AdaptationFilter.DisabledOnly && !data.Disabled)
                    return false;
                if (currentFilter == AdaptationFilter.Boss && key.Category != AdaptationCategory.Boss)
                    return false;
                if (currentFilter == AdaptationFilter.Mob && key.Category != AdaptationCategory.Mob)
                    return false;
                if (currentFilter == AdaptationFilter.Debuff && key.Category != AdaptationCategory.Debuff)
                    return false;
                if (currentFilter == AdaptationFilter.Environment && key.Category != AdaptationCategory.Environment)
                    return false;
                if (currentFilter == AdaptationFilter.Death && key.Category != AdaptationCategory.Death)
                    return false;

                // 2. Search filter
                if (!string.IsNullOrEmpty(cleanSearch))
                {
                    bool matchName = key.DisplayName.ToLowerInvariant().Contains(cleanSearch);
                    bool matchId = key.TargetId.ToLowerInvariant().Contains(cleanSearch);
                    bool matchCat = key.Category.ToString().ToLowerInvariant().Contains(cleanSearch);

                    if (!matchName && !matchId && !matchCat)
                        return false;
                }

                return true;
            }).OrderByDescending(kvp => kvp.Value.Level)
              .ThenBy(kvp => kvp.Key.DisplayName)
              .ToList();

            if (matchingEntries.Count == 0)
            {
                var noMatches = new UITextPanel<string>("No adaptations match the current filter or search criteria.", 0.85f, false);
                noMatches.Width.Set(0, 0.95f);
                noMatches.Height.Set(50f, 0f);
                noMatches.HAlign = 0.5f;
                noMatches.Top.Set(20f, 0f);
                noMatches.BackgroundColor = new Color(35, 40, 55, 180);
                noMatches.BorderColor = new Color(80, 90, 110, 200);
                adaptationList.Add(noMatches);
                return;
            }

            int maxLevel = AdaptationData.GetMaxLevel();

            foreach (var kvp in matchingEntries)
            {
                var entryPanel = CreateAdaptationRow(kvp.Key, kvp.Value, adaptor, maxLevel);
                adaptationList.Add(entryPanel);
            }

            if (preserveScroll && scrollbar != null)
            {
                scrollbar.ViewPosition = prevScroll;
            }
        }

        private UIPanel CreateAdaptationRow(AdaptationKey key, AdaptationData data, AdaptationPlayer adaptor, int maxLevel)
        {
            var row = new UIPanel();
            row.Width.Set(0, 1f);
            row.Height.Set(58f, 0f);
            row.SetPadding(6f);

            bool isDisabled = data.Disabled;
            Color catColor = key.Category.GetCategoryColor();

            if (isDisabled)
            {
                row.BackgroundColor = new Color(45, 25, 30, 220);
                row.BorderColor = new Color(180, 70, 70, 230);
            }
            else
            {
                row.BackgroundColor = new Color(28, 38, 60, 210);
                row.BorderColor = catColor * 0.85f;
            }

            // Left: Category tag badge
            string categoryTag = $"[{key.Category.ToString().ToUpper()}]";
            var catBadge = new UIText(categoryTag, 0.8f);
            catBadge.Left.Set(4f, 0f);
            catBadge.Top.Set(4f, 0f);
            catBadge.TextColor = catColor;
            row.Append(catBadge);

            float catBadgeWidth = FontAssets.MouseText.Value.MeasureString(categoryTag).X * 0.8f;

            // Offensive / Defensive tag badge
            string typeTag = key.IsOffensive ? "[OFFENSE]" : "[DEFENSE]";
            var typeBadge = new UIText(typeTag, 0.75f);
            typeBadge.Left.Set(4f + catBadgeWidth + 10f, 0f);
            typeBadge.Top.Set(4f, 0f);
            typeBadge.TextColor = key.IsOffensive ? new Color(255, 215, 100) : new Color(140, 210, 255);
            row.Append(typeBadge);

            // Display Name
            string nameStr = key.DisplayName;
            var nameText = new UIText(nameStr, 1f);
            nameText.Left.Set(4f, 0f);
            nameText.Top.Set(24f, 0f);
            nameText.TextColor = isDisabled ? new Color(170, 140, 140) : Color.White;
            row.Append(nameText);

            // Target ID subtext (if different from display name)
            if (!string.IsNullOrEmpty(key.TargetId) && !key.TargetId.Equals(key.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                var subText = new UIText($"({key.TargetId})", 0.7f);
                subText.Left.Set(nameText.Left.Pixels + FontAssets.MouseText.Value.MeasureString(nameStr).X * 1f + 8f, 0f);
                subText.Top.Set(26f, 0f);
                subText.TextColor = new Color(140, 150, 170);
                row.Append(subText);
            }

            // Right side: Level indicator
            bool isMaxed = data.Level >= maxLevel;
            string levelStr = isMaxed ? $"LVL {data.Level} (MAX)" : $"LVL {data.Level}/{maxLevel}";
            var levelText = new UIText(levelStr, 0.95f);
            levelText.Left.Set(-250f, 1f);
            levelText.Top.Set(4f, 0f);
            levelText.TextColor = isMaxed ? new Color(255, 215, 100) : new Color(200, 220, 255);
            row.Append(levelText);

            // Progress percentage bar (if not maxed)
            float pct = data.GetProgressPercentage(key.Category, key.TargetId);
            string pctStr = isMaxed ? "100%" : $"{(pct * 100f):0.#}%";
            var pctText = new UIText(pctStr, 0.75f);
            pctText.Left.Set(-250f, 1f);
            pctText.Top.Set(26f, 0f);
            pctText.TextColor = isMaxed ? new Color(120, 255, 120) : new Color(180, 200, 230);
            row.Append(pctText);

            // Toggle Button (Enabled vs Disabled)
            string buttonText = isDisabled ? "DISABLED" : "ENABLED";
            var toggleBtn = new UITextPanel<string>(buttonText, 0.8f, false);
            toggleBtn.Width.Set(90f, 0f);
            toggleBtn.Height.Set(32f, 0f);
            toggleBtn.Left.Set(-100f, 1f);
            toggleBtn.Top.Set(8f, 0f);
            toggleBtn.SetPadding(4f);

            if (isDisabled)
            {
                toggleBtn.BackgroundColor = new Color(140, 40, 45, 230);
                toggleBtn.BorderColor = new Color(220, 80, 80, 255);
                toggleBtn.TextColor = Color.White;
            }
            else
            {
                toggleBtn.BackgroundColor = new Color(40, 120, 50, 230);
                toggleBtn.BorderColor = new Color(80, 200, 90, 255);
                toggleBtn.TextColor = Color.White;
            }

            toggleBtn.OnLeftClick += (evt, el) =>
            {
                bool newDisabledState = !data.Disabled;
                adaptor.SetAdaptationDisabled(key, newDisabledState);
                SoundEngine.PlaySound(SoundID.MenuTick);
                RefreshAdaptationList(preserveScroll: true);
            };

            row.Append(toggleBtn);

            return row;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mainPanel != null && mainPanel.IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (dragging)
            {
                mainPanel.Left.Set(Main.mouseX - offset.X, 0f);
                mainPanel.Top.Set(Main.mouseY - offset.Y, 0f);
                mainPanel.Recalculate();
            }

            updateTimer++;
            if (updateTimer >= 15)
            {
                updateTimer = 0;
                if (searchInput == null || !searchInput.Focused)
                {
                    RefreshAdaptationList(preserveScroll: true);
                }
            }
        }

        private bool IsClickingOnInteractiveElement(Vector2 mousePos)
        {
            if (searchInput?.ContainsPoint(mousePos) == true)
                return true;
            if (filterButtonContainer?.ContainsPoint(mousePos) == true)
                return true;
            if (scrollbar?.ContainsPoint(mousePos) == true)
                return true;
            if (adaptationList?.ContainsPoint(mousePos) == true)
                return true;
            return false;
        }
    }
}
