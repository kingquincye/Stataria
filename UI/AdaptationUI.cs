using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
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

        // Virtualization & Caching fields
        private List<KeyValuePair<AdaptationKey, AdaptationData>> cachedMatchingEntries = new List<KeyValuePair<AdaptationKey, AdaptationData>>();
        private readonly List<UIAdaptationRow> rowPool = new List<UIAdaptationRow>();
        private UIElement topSpacer;
        private UIElement bottomSpacer;
        private const float RowStep = 64f; // 58f row height + 6f ListPadding
        private const int MaxVisibleRows = 14;
        private int lastFirstIndex = -1;
        private float lastScrollPosition = -1f;
        private int lastKnownAdaptationCount = -1;
        private int updateTimer = 0;

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
            adaptationList.ManualSortMethod = (elements) => { }; // Bypass UIList's automatic sorting
            mainPanel.Append(adaptationList);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(-145f, 1f);
            scrollbar.Top.Set(136f, 0f);
            scrollbar.Left.Set(-18f, 1f);
            mainPanel.Append(scrollbar);
            adaptationList.SetScrollbar(scrollbar);

            RefreshAdaptationList();
        }

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
            rowPool.Clear();
            lastFirstIndex = -1;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            var adaptor = player.GetModPlayer<AdaptationPlayer>();
            if (adaptor == null || adaptor.Adaptations == null)
                return;

            lastKnownAdaptationCount = adaptor.Adaptations.Count;

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
                cachedMatchingEntries.Clear();
                return;
            }

            string cleanSearch = searchText.Trim().ToLowerInvariant();

            cachedMatchingEntries = adaptor.Adaptations.Where(kvp =>
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

            if (cachedMatchingEntries.Count == 0)
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

            int poolSize = Math.Min(MaxVisibleRows, cachedMatchingEntries.Count);
            int maxLevel = AdaptationData.GetMaxLevel();

            topSpacer = new UIElement();
            topSpacer.Width.Set(0, 1f);
            topSpacer.Height.Set(0, 0f);
            adaptationList.Add(topSpacer);

            for (int i = 0; i < poolSize; i++)
            {
                var kvp = cachedMatchingEntries[i];
                var row = new UIAdaptationRow(kvp.Key, kvp.Value, maxLevel, (targetKey) =>
                {
                    if (adaptor.Adaptations.TryGetValue(targetKey, out var latestData))
                    {
                        bool newDisabledState = !latestData.Disabled;
                        adaptor.SetAdaptationDisabled(targetKey, newDisabledState);
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        RefreshAdaptationList(preserveScroll: true);
                    }
                });
                rowPool.Add(row);
                adaptationList.Add(row);
            }

            bottomSpacer = new UIElement();
            bottomSpacer.Width.Set(0, 1f);
            bottomSpacer.Height.Set(0, 0f);
            adaptationList.Add(bottomSpacer);

            UpdateVirtualScroll(force: true);

            if (preserveScroll && scrollbar != null)
            {
                scrollbar.ViewPosition = prevScroll;
            }
        }

        private void UpdateVirtualScroll(bool force = false)
        {
            if (cachedMatchingEntries == null || cachedMatchingEntries.Count == 0 || rowPool.Count == 0)
                return;

            float currentScroll = scrollbar != null ? scrollbar.ViewPosition : 0f;
            int totalCount = cachedMatchingEntries.Count;
            int visibleCount = rowPool.Count;
            int maxFirstIndex = Math.Max(0, totalCount - visibleCount);
            int firstIndex = Math.Max(0, Math.Min(maxFirstIndex, (int)Math.Floor(currentScroll / RowStep)));

            if (force || firstIndex != lastFirstIndex)
            {
                lastFirstIndex = firstIndex;

                float topH = firstIndex * RowStep;
                float bottomH = Math.Max(0f, (totalCount - firstIndex - visibleCount) * RowStep);

                if (topSpacer != null)
                    topSpacer.Height.Set(topH, 0f);
                if (bottomSpacer != null)
                    bottomSpacer.Height.Set(bottomH, 0f);

                adaptationList?.Recalculate();
            }

            lastScrollPosition = currentScroll;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            var adaptor = player.GetModPlayer<AdaptationPlayer>();
            if (adaptor == null || adaptor.Adaptations == null)
                return;

            int totalAcquired = adaptor.Adaptations.Values.Count(v => v.Level > 0 || v.CurrentExp > 0f || v.Disabled);
            int totalDisabled = adaptor.Adaptations.Values.Count(v => v.Disabled);
            subtitleText?.SetText($"Acquired: {totalAcquired}  |  Disabled: {totalDisabled}");

            int maxLevel = AdaptationData.GetMaxLevel();

            for (int i = 0; i < visibleCount; i++)
            {
                int dataIndex = firstIndex + i;
                if (dataIndex < totalCount)
                {
                    var key = cachedMatchingEntries[dataIndex].Key;
                    if (adaptor.Adaptations.TryGetValue(key, out var data))
                    {
                        rowPool[i].Bind(key, data, maxLevel);
                    }
                }
            }
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

            Player player = Main.LocalPlayer;
            if (player != null && player.active)
            {
                var adaptor = player.GetModPlayer<AdaptationPlayer>();
                if (adaptor != null && adaptor.Adaptations != null)
                {
                    if (lastKnownAdaptationCount != adaptor.Adaptations.Count)
                    {
                        RefreshAdaptationList(preserveScroll: true);
                        return;
                    }
                }
            }

            float currentScroll = scrollbar != null ? scrollbar.ViewPosition : 0f;
            if (Math.Abs(currentScroll - lastScrollPosition) > 0.01f)
            {
                UpdateVirtualScroll();
            }

            updateTimer++;
            if (updateTimer >= 15)
            {
                updateTimer = 0;
                UpdateVirtualScroll();
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
