using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stataria;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Stataria.UI.Elements;

namespace Stataria.UI
{
    public class CustomConfigUIState : UIState
    {
        private UIPanel mainPanel;
        private UIPanel sidebarPanel;
        private UIPanel rightPanel;

        private UIList categoryList;
        private UIScrollbar categoryScrollbar;

        public UIList configElementsList;
        public UIScrollbar configElementsScrollbar;

        private UIPanel tooltipPanel;
        private UIText tooltipText;
        private UIText reloadWarningText;

        private UIElement configTabsList;
        public ModConfig CurrentConfig;

        private UITextPanel<Terraria.Localization.LocalizedText> closeButton;
        private UITextPanel<Terraria.Localization.LocalizedText> saveButton;
        private UITextPanel<Terraria.Localization.LocalizedText> loadButton;
        private UITextPanel<Terraria.Localization.LocalizedText> defaultButton;
        private bool _oldEscapePressed;

        private UITextInput searchInput;
        private string _searchQuery = "";
        private PropertyFieldWrapper _currentCategory;
        private UIText _statusText;
        private int _statusTimer;
        private UIElement _buttonContainer;

        // Background file dialog state
        private volatile bool _dialogPending = false;
        private string _pendingDialogPath = null;
        private Action<string> _pendingDialogAction = null;
        private UIPanel _dialogOverlay;

        /// <summary>True while a file dialog is open — custom controls check this to skip input processing.</summary>
        public static bool DialogOpen { get; private set; } = false;

        public override void OnActivate()
        {
            base.OnActivate();
            _oldEscapePressed = true;
        }

        public override void OnDeactivate()
        {
            if (Main.CurrentInputTextTakerOverride != null)
            {
                Main.CurrentInputTextTakerOverride = null;
            }
            Terraria.GameInput.PlayerInput.WritingText = false;
            Main.blockInput = false;
            base.OnDeactivate();
        }

        public override void OnInitialize()
        {
            // Background dimming
            UIPanel backgroundScreen = new UIPanel();
            backgroundScreen.Width.Set(0, 1f);
            backgroundScreen.Height.Set(0, 1f);
            backgroundScreen.BackgroundColor = new Color(15, 5, 20, 200); // Darker purple tint
            Append(backgroundScreen);

            // Main container
            mainPanel = new UIPanel();
            mainPanel.Width.Set(1300, 0f); // Widened to 1300
            mainPanel.Height.Set(750, 0f); 
            mainPanel.HAlign = 0.5f;
            mainPanel.VAlign = 0.5f;
            mainPanel.BackgroundColor = new Color(40, 25, 60) * 0.95f; // Deep Purple
            Append(mainPanel);

            // Button Container (Centers the group of buttons at the bottom)
            _buttonContainer = new UIElement();
            _buttonContainer.Width.Set(756f, 0f); // 4 buttons * 180px + 3 gaps * 12px = 756px
            _buttonContainer.Height.Set(32f, 0f);
            _buttonContainer.HAlign = 0.5f;
            _buttonContainer.VAlign = 0.5f;
            _buttonContainer.Top.Set(420f, 0f); // Positioned below the main config panel
            Append(_buttonContainer);

            float buttonWidth = 180f;
            float buttonHeight = 32f;
            float spacing = 12f;

            // Set Defaults Button
            defaultButton = new UITextPanel<Terraria.Localization.LocalizedText>(Terraria.Localization.Language.GetText("Mods.Stataria.UI.SetDefaults"), 0.8f);
            defaultButton.Width.Set(buttonWidth, 0f);
            defaultButton.Height.Set(buttonHeight, 0f);
            defaultButton.HAlign = 0f;
            defaultButton.VAlign = 0f;
            defaultButton.Left.Set(0f, 0f);
            defaultButton.BackgroundColor = new Color(120, 40, 120);
            defaultButton.OnLeftClick += (evt, element) => {
                if (DialogOpen) return;
                SetDefaultsAction();
            };
            defaultButton.OnMouseOver += (evt, element) => {
                if (DialogOpen) return;
                defaultButton.BackgroundColor = new Color(150, 70, 150);
            };
            defaultButton.OnMouseOut += (evt, element) => {
                if (DialogOpen) return;
                defaultButton.BackgroundColor = new Color(120, 40, 120);
            };
            _buttonContainer.Append(defaultButton);

            // Load Button
            loadButton = new UITextPanel<Terraria.Localization.LocalizedText>(Terraria.Localization.Language.GetText("Mods.Stataria.UI.LoadConfig"), 0.8f);
            loadButton.Width.Set(buttonWidth, 0f);
            loadButton.Height.Set(buttonHeight, 0f);
            loadButton.HAlign = 0f;
            loadButton.VAlign = 0f;
            loadButton.Left.Set(buttonWidth + spacing, 0f);
            loadButton.BackgroundColor = new Color(120, 40, 120);
            loadButton.OnLeftClick += (evt, element) => LoadConfigAction();
            loadButton.OnMouseOver += (evt, element) => {
                if (DialogOpen) return;
                loadButton.BackgroundColor = new Color(150, 70, 150);
            };
            loadButton.OnMouseOut += (evt, element) => {
                if (DialogOpen) return;
                loadButton.BackgroundColor = new Color(120, 40, 120);
            };
            _buttonContainer.Append(loadButton);

            // Save Button
            saveButton = new UITextPanel<Terraria.Localization.LocalizedText>(Terraria.Localization.Language.GetText("Mods.Stataria.UI.SaveConfig"), 0.8f);
            saveButton.Width.Set(buttonWidth, 0f);
            saveButton.Height.Set(buttonHeight, 0f);
            saveButton.HAlign = 0f;
            saveButton.VAlign = 0f;
            saveButton.Left.Set((buttonWidth + spacing) * 2f, 0f);
            saveButton.BackgroundColor = new Color(120, 40, 120);
            saveButton.OnLeftClick += (evt, element) => SaveConfigAction();
            saveButton.OnMouseOver += (evt, element) => {
                if (DialogOpen) return;
                saveButton.BackgroundColor = new Color(150, 70, 150);
            };
            saveButton.OnMouseOut += (evt, element) => {
                if (DialogOpen) return;
                saveButton.BackgroundColor = new Color(120, 40, 120);
            };
            _buttonContainer.Append(saveButton);

            // Close Button
            closeButton = new UITextPanel<Terraria.Localization.LocalizedText>(Terraria.Localization.Language.GetText("Mods.Stataria.UI.CloseCustomConfig"), 0.8f);
            closeButton.Width.Set(buttonWidth, 0f);
            closeButton.Height.Set(buttonHeight, 0f);
            closeButton.HAlign = 0f;
            closeButton.VAlign = 0f;
            closeButton.Left.Set((buttonWidth + spacing) * 3f, 0f);
            closeButton.BackgroundColor = new Color(120, 40, 120);
            closeButton.OnLeftClick += (evt, element) => {
                if (DialogOpen) return;
                SoundEngine.PlaySound(SoundID.MenuClose);
                ConfigUISystem.Instance.HideMyUI();
            };
            closeButton.OnMouseOver += (evt, element) => {
                if (DialogOpen) return;
                closeButton.BackgroundColor = new Color(150, 70, 150);
            };
            closeButton.OnMouseOut += (evt, element) => {
                if (DialogOpen) return;
                closeButton.BackgroundColor = new Color(120, 40, 120);
            };
            _buttonContainer.Append(closeButton);

            // Status Text - anchored above the button container so it never overlaps
            _statusText = new UIText("", 0.8f);
            _statusText.HAlign = 0.5f;
            _statusText.VAlign = 0f;
            _statusText.Top.Set(-20f, 0f);
            _buttonContainer.Append(_statusText);

            // Config Tabs Header
            UIPanel configTabsContainer = new UIPanel();
            configTabsContainer.Width.Set(0, 1f);
            configTabsContainer.Height.Set(45, 0f);
            configTabsContainer.BackgroundColor = new Color(30, 20, 50) * 0.9f;
            mainPanel.Append(configTabsContainer);

            configTabsList = new UIElement();
            configTabsList.Width.Set(0, 1f);
            configTabsList.Height.Set(0, 1f);
            configTabsContainer.Append(configTabsList);

            // Search Box (Top Right of configTabsContainer)
            searchInput = new UITextInput(
                Terraria.Localization.Language.GetText("Mods.Stataria.UI.SearchPlaceholder"),
                "",
                DoSearch
            );
            searchInput.Width.Set(250f, 0f);
            searchInput.Height.Set(34f, 0f);
            searchInput.HAlign = 1f;
            searchInput.VAlign = 0.5f;
            searchInput.Left.Set(-10f, 0f); // 10px padding from the right edge
            configTabsContainer.Append(searchInput);

            // Left Sidebar for Categories
            sidebarPanel = new UIPanel();
            sidebarPanel.Top.Set(50, 0f); // Make room for tabs
            sidebarPanel.Width.Set(350, 0f); 
            sidebarPanel.Height.Set(-50, 1f); 
            sidebarPanel.BackgroundColor = new Color(30, 20, 50) * 0.9f; 
            mainPanel.Append(sidebarPanel);

            // Right Area for Config Elements
            rightPanel = new UIPanel();
            rightPanel.Top.Set(50, 0f);
            rightPanel.Left.Set(360, 0f); 
            rightPanel.Width.Set(-360, 1f); 
            rightPanel.Height.Set(-200, 1f); // 50 (tabs) + 150 (tooltips)
            rightPanel.BackgroundColor = new Color(35, 25, 55) * 0.9f;
            mainPanel.Append(rightPanel);

            // Tooltip Panel Area
            tooltipPanel = new UIPanel();
            tooltipPanel.Left.Set(360, 0f);
            tooltipPanel.Top.Set(-150, 1f); 
            tooltipPanel.Width.Set(-360, 1f);
            tooltipPanel.Height.Set(150, 0f);
            tooltipPanel.BackgroundColor = new Color(25, 15, 35) * 0.95f;
            mainPanel.Append(tooltipPanel);

            tooltipText = new UIText(Terraria.Localization.Language.GetText("Mods.Stataria.UI.HoverTooltip"), 0.95f);
            tooltipText.HAlign = 0.5f;
            tooltipText.VAlign = 0.4f;
            tooltipText.IsWrapped = true;
            tooltipText.Width.Set(0, 1f);
            tooltipPanel.Append(tooltipText);

            reloadWarningText = new UIText(Terraria.Localization.Language.GetText("Mods.Stataria.UI.ReloadRequired"), 0.9f);
            reloadWarningText.TextColor = Color.LightCoral;
            reloadWarningText.HAlign = 0.5f;
            reloadWarningText.VAlign = 0.8f;
            // Initially hidden or empty, but keeping the element
            reloadWarningText.SetText("");
            tooltipPanel.Append(reloadWarningText);

            // Setup Category List (Full height of Sidebar)
            categoryList = new UIList();
            categoryList.Top.Set(5f, 0f);
            categoryList.Left.Set(5f, 0f);
            categoryList.Width.Set(-25f, 1f); // Shrink to make room for scrollbar
            categoryList.Height.Set(-10f, 1f); // 5px top and bottom margin
            categoryList.ListPadding = 5f;
            categoryList.ManualSortMethod = (elements) => { }; // FIX: Bypass UIList's aggressive auto-sorting
            sidebarPanel.Append(categoryList);

            categoryScrollbar = new UIScrollbar();
            categoryScrollbar.SetView(100f, 1000f);
            categoryScrollbar.Top.Set(5f, 0f);
            categoryScrollbar.Height.Set(-10f, 1f);
            categoryScrollbar.HAlign = 1f;
            sidebarPanel.Append(categoryScrollbar);
            categoryList.SetScrollbar(categoryScrollbar);

            // Setup Config Elements List
            configElementsList = new UIList();
            configElementsList.Width.Set(-30, 1f); // Shrink slightly to avoid scrollbar overlap
            configElementsList.Height.Set(0, 1f);
            configElementsList.ListPadding = 5f;
            configElementsList.ManualSortMethod = (elements) => { }; // FIX: Bypass UIList's aggressive auto-sorting
            rightPanel.Append(configElementsList);

            configElementsScrollbar = new UIScrollbar();
            configElementsScrollbar.SetView(100f, 1000f);
            configElementsScrollbar.Height.Set(0, 1f);
            configElementsScrollbar.HAlign = 1f;
            rightPanel.Append(configElementsScrollbar);
            configElementsList.SetScrollbar(configElementsScrollbar);
        }

        public override void Recalculate()
        {
            if (mainPanel != null)
            {
                float targetWidth = Math.Min(1300f, Main.screenWidth - 40f);
                float targetHeight = Math.Min(750f, Main.screenHeight - 120f);

                mainPanel.Width.Set(targetWidth, 0f);
                mainPanel.Height.Set(targetHeight, 0f);

                if (_buttonContainer != null)
                {
                    _buttonContainer.VAlign = 0.5f;
                    _buttonContainer.Top.Set(targetHeight / 2f + 40f, 0f);
                }

                // _statusText is a child of _buttonContainer, no separate positioning needed
            }
            base.Recalculate();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (tooltipText != null)
            {
                tooltipText.VAlign = 0.4f;
                tooltipText.SetText(Terraria.Localization.Language.GetText("Mods.Stataria.UI.HoverTooltip"), 0.95f, false);
            }
            reloadWarningText?.SetText("");
            base.Draw(spriteBatch);

            // Keyboard Escape handling during Draw (since typing state is processed during Draw phase)
            bool escapePressed = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
            if (!DialogOpen && escapePressed && !_oldEscapePressed)
            {
                if (!Main.inputTextEscape)
                {
                    ConfigUISystem.Instance.HideMyUI();
                }
            }
            _oldEscapePressed = escapePressed;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);

            if (_statusTimer > 0)
            {
                _statusTimer--;
                if (_statusTimer <= 0)
                {
                    _statusText?.SetText("");
                }
            }

            // Process background file dialog result on the main thread
            if (_dialogPending && _pendingDialogPath != null)
            {
                string path = _pendingDialogPath;
                Action<string> action = _pendingDialogAction;
                _pendingDialogPath = null;
                _pendingDialogAction = null;
                _dialogPending = false;
                DialogOpen = false;
                action?.Invoke(path);
            }

            // Handle overlay visibility based on DialogOpen
            if (DialogOpen)
            {
                if (_dialogOverlay == null || _dialogOverlay.Parent == null)
                {
                    ShowDialogOverlay();
                }
                
                // Dim all buttons
                if (defaultButton != null) defaultButton.BackgroundColor = new Color(60, 20, 60);
                if (loadButton != null) loadButton.BackgroundColor = new Color(60, 20, 60);
                if (saveButton != null) saveButton.BackgroundColor = new Color(60, 20, 60);
                if (closeButton != null) closeButton.BackgroundColor = new Color(60, 20, 60);
            }
            else
            {
                if (_dialogOverlay != null && _dialogOverlay.Parent != null)
                {
                    HideDialogOverlay();
                }

                // Restore all buttons background if not hovered
                if (defaultButton != null && !defaultButton.IsMouseHovering) defaultButton.BackgroundColor = new Color(120, 40, 120);
                if (loadButton != null && !loadButton.IsMouseHovering) loadButton.BackgroundColor = new Color(120, 40, 120);
                if (saveButton != null && !saveButton.IsMouseHovering) saveButton.BackgroundColor = new Color(120, 40, 120);
                if (closeButton != null && !closeButton.IsMouseHovering) closeButton.BackgroundColor = new Color(120, 40, 120);
            }

            if (!Main.gameMenu)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.blockInput = true;
                Main.LocalPlayer.delayUseItem = true;
            }
        }

        public void PopulateConfigsTabs()
        {
            if (searchInput != null)
            {
                searchInput.Text = "";
            }
            _searchQuery = "";

            configTabsList.RemoveAllChildren();
            categoryList.Clear();
            configElementsList.Clear();

            var mod = ModContent.GetInstance<ConfigUISystem>().Mod;
            var configsField = typeof(Terraria.ModLoader.Config.ConfigManager).GetField("Configs", BindingFlags.Static | BindingFlags.NonPublic);
            if (configsField != null)
            {
                var allConfigs = configsField.GetValue(null) as IDictionary<Mod, List<ModConfig>>;
                if (allConfigs != null && allConfigs.TryGetValue(mod, out var configs))
                {
                    ModConfig firstConfig = null;
                    float currentLeft = 0f;
                    foreach (var config in configs)
                    {
                        if (firstConfig == null) firstConfig = config;
                        AddConfigTab(config, ref currentLeft);
                    }

                    if (firstConfig != null)
                    {
                        SelectConfig(firstConfig);
                    }
                }
            }
        }

        private void AddConfigTab(ModConfig config, ref float currentLeft)
        {
            string label = Terraria.Localization.Language.GetTextValue(config.DisplayName.Key);
            UITextPanel<string> tab = new UITextPanel<string>(label, 0.8f);
            tab.BackgroundColor = new Color(50, 30, 80);
            
            // Measure string to size tab horizontally
            float textWidth = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(label).X * 0.8f;
            tab.Width.Set(textWidth + 25f, 0f);
            tab.Height.Set(35f, 0f);
            tab.Left.Set(currentLeft, 0f);
            tab.VAlign = 0.5f;

            currentLeft += textWidth + 35f; // padding between tabs
            
            tab.OnMouseOver += (evt, element) => {
                if (DialogOpen) return;
                if (CurrentConfig != config) tab.BackgroundColor = new Color(100, 50, 150);
            };
            tab.OnMouseOut += (evt, element) => {
                if (DialogOpen) return;
                if (CurrentConfig != config) tab.BackgroundColor = new Color(50, 30, 80);
            };
            tab.OnLeftClick += (evt, element) => {
                if (DialogOpen) return;
                SoundEngine.PlaySound(SoundID.MenuTick);
                SelectConfig(config);
            };
            configTabsList.Append(tab);
        }

        private void SelectConfig(ModConfig config)
        {
            CurrentConfig = config;
            
            var mod = ModContent.GetInstance<ConfigUISystem>().Mod;
            var configsField = typeof(Terraria.ModLoader.Config.ConfigManager).GetField("Configs", BindingFlags.Static | BindingFlags.NonPublic);
            if (configsField != null)
            {
                var allConfigs = configsField.GetValue(null) as IDictionary<Mod, List<ModConfig>>;
                if (allConfigs != null && allConfigs.TryGetValue(mod, out var configs))
                {
                    int index = 0;
                    foreach (UIElement el in configTabsList.Children)
                    {
                        if (el is UITextPanel<string> p) 
                        {
                            if (configs[index] == config) p.BackgroundColor = new Color(130, 60, 180);
                            else p.BackgroundColor = new Color(50, 30, 80);
                        }
                        index++;
                    }
                }
            }

            PopulateCategories();
        }

        public void PopulateCategories()
        {
            categoryList.Clear();
            configElementsList.Clear();
            
            if (CurrentConfig == null) return;

            var properties = Terraria.ModLoader.Config.ConfigManager.GetFieldsAndProperties(CurrentConfig).ToList();
            
            bool hasGeneralSettings = false;
            List<PropertyFieldWrapper> categoryProperties = new List<PropertyFieldWrapper>();

            foreach (PropertyFieldWrapper prop in properties)
            {
                // Only include properties distinctly declared in the specific ModConfig
                if (prop.MemberInfo.DeclaringType != CurrentConfig.GetType()) continue;
                if (prop.Name == "Mode") continue; // Skip ModConfig Mode property
                if (prop.Name == "OpenMenuButton") continue; // Skip our custom button config
                
                bool isCategory = prop.Type.IsClass && prop.Type != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.Type);
                if (isCategory)
                {
                    categoryProperties.Add(prop);
                }
                else
                {
                    hasGeneralSettings = true;
                }
            }

            // Create Uncategorized Tab if there are non-category root fields
            if (hasGeneralSettings)
            {
                AddCategoryTab(Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.Uncategorized"), null);
            }

            foreach (PropertyFieldWrapper prop in categoryProperties)
            {
                string key = $"Mods.{CurrentConfig.Mod.Name}.Configs.{CurrentConfig.Name}.{prop.Name}.Label";
                string localized = Terraria.Localization.Language.GetTextValue(key);
                string formattedName = localized != key ? localized : FormatCamelCase(prop.Name);
                
                AddCategoryTab(formattedName, prop);
            }

            // Automatically open Uncategorized or the first category tab
            if (hasGeneralSettings)
            {
                OpenCategory(null);
            }
            else if (categoryProperties.Count > 0)
            {
                OpenCategory(categoryProperties[0]);
            }
        }
        
        private string FormatCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            string result = Regex.Replace(str, "([a-z])([A-Z])", "$1 $2");
            result = result.Replace("_", " ");
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        private void AddCategoryTab(string name, PropertyFieldWrapper categoryProperty)
        {
            UITextPanel<string> catTab = new UITextPanel<string>(name, 0.8f);
            catTab.Width.Set(-25, 1f); // Shrink to make room for scrollbar
            catTab.BackgroundColor = new Color(50, 30, 80); // Purple unselected
            catTab.OnMouseOver += (evt, element) => {
                if (DialogOpen) return;
                catTab.BackgroundColor = new Color(100, 50, 150); // Hover bright purple
            };
            catTab.OnMouseOut += (evt, element) => {
                if (DialogOpen) return;
                catTab.BackgroundColor = new Color(50, 30, 80);
            };
            catTab.OnLeftClick += (evt, element) => {
                if (DialogOpen) return;
                SoundEngine.PlaySound(SoundID.MenuTick);
                OpenCategory(categoryProperty);
                
                // Keep the selected category highlighted
                foreach(UIElement el in categoryList)
                {
                    if (el is UITextPanel<string> p) p.BackgroundColor = new Color(50, 30, 80);
                }
                catTab.BackgroundColor = new Color(130, 60, 180); // Selected bright purple
            };
            categoryList.Add(catTab);
        }

        private void OpenCategory(PropertyFieldWrapper categoryProperty)
        {
            _currentCategory = categoryProperty;
            UpdateConfigElementsList();

            // Clear search query on category switch if desired, or keep it. Let's clear search query when selecting a category to make it intuitive.
            if (searchInput != null && !string.IsNullOrEmpty(_searchQuery))
            {
                searchInput.Text = "";
                _searchQuery = "";
            }
        }

        public void DoSearch(string query)
        {
            _searchQuery = query;
            UpdateConfigElementsList();

            // If we are searching, we clear the active category highlighting to avoid confusion
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                foreach (UIElement el in categoryList)
                {
                    if (el is UITextPanel<string> p) p.BackgroundColor = new Color(50, 30, 80);
                }
            }
        }

        private bool MatchesSearch(string query, string label, string tooltip, string fieldName)
        {
            if (string.IsNullOrEmpty(query)) return true;

            query = query.ToLowerInvariant();

            if (label != null && label.ToLowerInvariant().Contains(query)) return true;
            if (tooltip != null && tooltip.ToLowerInvariant().Contains(query)) return true;
            if (fieldName != null && fieldName.ToLowerInvariant().Contains(query)) return true;

            return false;
        }

        public void UpdateConfigElementsList()
        {
            configElementsList.Clear();

            // Reset scrollbar position when switching categories or searching
            if (configElementsScrollbar != null)
            {
                configElementsScrollbar.ViewPosition = 0f;
            }

            if (CurrentConfig == null) return;

            // Define tooltip action
            Action<string, bool> onHover = (tt, r) => {
                string text = string.IsNullOrEmpty(tt) ? Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.HoverTooltip") : tt;
                float scale = 0.95f;
                if (!string.IsNullOrEmpty(tt) && tt.Length > 100)
                {
                    scale = Math.Max(0.55f, 0.95f - (tt.Length - 100) * 0.001f);
                }
                tooltipText.VAlign = (tt != null && tt.Length > 150) ? (r ? 0.02f : 0.08f) : 0.4f;
                tooltipText.SetText(text, scale, false);
                reloadWarningText.SetText(r ? Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.ReloadRequired") : "");
                tooltipText.Recalculate();
                tooltipPanel.Recalculate();
            };

            if (string.IsNullOrEmpty(_searchQuery))
            {
                PopulateCategoryElements(_currentCategory, onHover);
            }
            else
            {
                PopulateSearchElements(_searchQuery, onHover);
            }
        }

        private void PopulateCategoryElements(PropertyFieldWrapper categoryProperty, Action<string, bool> onHover)
        {
            object categoryInstance;
            List<PropertyFieldWrapper> fieldsToDisplay = new List<PropertyFieldWrapper>();

            if (categoryProperty == null)
            {
                // We are looking at "Uncategorized" (Root fields)
                categoryInstance = CurrentConfig;
                var allProperties = Terraria.ModLoader.Config.ConfigManager.GetFieldsAndProperties(CurrentConfig).ToList();
                foreach (var prop in allProperties)
                {
                    if (prop.MemberInfo.DeclaringType != CurrentConfig.GetType()) continue;
                    if (prop.Name == "Mode") continue;
                    if (prop.Name == "OpenMenuButton") continue;

                    bool isCategory = prop.Type.IsClass && prop.Type != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.Type);
                    if (!isCategory)
                    {
                        fieldsToDisplay.Add(prop);
                    }
                }
            }
            else
            {
                categoryInstance = categoryProperty.GetValue(CurrentConfig);
                if (categoryInstance == null) return;
                fieldsToDisplay = Terraria.ModLoader.Config.ConfigManager.GetFieldsAndProperties(categoryInstance).ToList();
            }

            foreach (var field in fieldsToDisplay)
            {
                AddConfigElement(field, categoryInstance, onHover);
            }
        }

        private void PopulateSearchElements(string query, Action<string, bool> onHover)
        {
            var properties = Terraria.ModLoader.Config.ConfigManager.GetFieldsAndProperties(CurrentConfig).ToList();

            // 1. Uncategorized (Root fields)
            List<PropertyFieldWrapper> uncategorizedFields = new List<PropertyFieldWrapper>();
            foreach (var prop in properties)
            {
                if (prop.MemberInfo.DeclaringType != CurrentConfig.GetType()) continue;
                if (prop.Name == "Mode" || prop.Name == "OpenMenuButton") continue;

                bool isCategory = prop.Type.IsClass && prop.Type != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.Type);
                if (!isCategory)
                {
                    uncategorizedFields.Add(prop);
                }
            }

            // Filter Uncategorized
            List<PropertyFieldWrapper> matchingUncategorized = new List<PropertyFieldWrapper>();
            foreach (var field in uncategorizedFields)
            {
                string typeName = CurrentConfig.GetType().Name;
                string labelKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Label";
                string tooltipKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Tooltip";

                string localizedLabel = Terraria.Localization.Language.GetTextValue(labelKey);
                string localizedTooltip = Terraria.Localization.Language.GetTextValue(tooltipKey);

                string formattedFieldName = localizedLabel != labelKey ? localizedLabel : FormatCamelCase(field.Name);
                string tooltipString = localizedTooltip != tooltipKey ? localizedTooltip : "";

                if (MatchesSearch(query, formattedFieldName, tooltipString, field.Name))
                {
                    matchingUncategorized.Add(field);
                }
            }

            if (matchingUncategorized.Count > 0)
            {
                configElementsList.Add(new UI.Elements.UIHeader(Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.Uncategorized")));
                foreach (var field in matchingUncategorized)
                {
                    AddConfigElement(field, CurrentConfig, onHover);
                }
            }

            // 2. Categories
            foreach (var prop in properties)
            {
                if (prop.MemberInfo.DeclaringType != CurrentConfig.GetType()) continue;
                if (prop.Name == "Mode" || prop.Name == "OpenMenuButton") continue;

                bool isCategory = prop.Type.IsClass && prop.Type != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.Type);
                if (isCategory)
                {
                    object categoryInstance = prop.GetValue(CurrentConfig);
                    if (categoryInstance == null) continue;

                    string catLabelKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{CurrentConfig.Name}.{prop.Name}.Label";
                    string catLocalized = Terraria.Localization.Language.GetTextValue(catLabelKey);
                    string categoryName = catLocalized != catLabelKey ? catLocalized : FormatCamelCase(prop.Name);

                    var catFields = Terraria.ModLoader.Config.ConfigManager.GetFieldsAndProperties(categoryInstance).ToList();
                    List<PropertyFieldWrapper> matchingCatFields = new List<PropertyFieldWrapper>();

                    foreach (var field in catFields)
                    {
                        string typeName = categoryInstance.GetType().Name;
                        string labelKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Label";
                        string tooltipKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Tooltip";

                        string localizedLabel = Terraria.Localization.Language.GetTextValue(labelKey);
                        string localizedTooltip = Terraria.Localization.Language.GetTextValue(tooltipKey);

                        string formattedFieldName = localizedLabel != labelKey ? localizedLabel : FormatCamelCase(field.Name);
                        string tooltipString = localizedTooltip != tooltipKey ? localizedTooltip : "";

                        if (MatchesSearch(query, formattedFieldName, tooltipString, field.Name))
                        {
                            matchingCatFields.Add(field);
                        }
                    }

                    if (matchingCatFields.Count > 0)
                    {
                        configElementsList.Add(new UI.Elements.UIHeader(categoryName));
                        foreach (var field in matchingCatFields)
                        {
                            AddConfigElement(field, categoryInstance, onHover);
                        }
                    }
                }
            }
        }

        private void AddConfigElement(PropertyFieldWrapper field, object categoryInstance, Action<string, bool> onHover)
        {
            string typeName = categoryInstance.GetType().Name;
            string labelKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Label";
            string tooltipKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Tooltip";

            string localizedLabel = Terraria.Localization.Language.GetTextValue(labelKey);
            string localizedTooltip = Terraria.Localization.Language.GetTextValue(tooltipKey);

            string formattedFieldName = localizedLabel != labelKey ? localizedLabel : FormatCamelCase(field.Name);
            string tooltipString = localizedTooltip != tooltipKey ? localizedTooltip : "";

            var headerAttr = field.MemberInfo.GetCustomAttribute<Terraria.ModLoader.Config.HeaderAttribute>();

            if (headerAttr != null)
            {
                string headerId = "";
                try
                {
                    var t = headerAttr.GetType();
                    string[] possibleFields = { "identifier", "key" };
                    foreach (string f in possibleFields)
                    {
                        var fieldInfo = t.GetField(f, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldInfo != null && fieldInfo.FieldType == typeof(string))
                        {
                            headerId = (string)fieldInfo.GetValue(headerAttr);
                            if (!string.IsNullOrEmpty(headerId)) break;
                        }
                    }

                    if (string.IsNullOrEmpty(headerId))
                    {
                        string str = headerAttr.ToString();
                        if (str != t.FullName)
                        {
                            headerId = str;
                        }
                    }
                }
                catch (Exception)
                {
                }

                if (string.IsNullOrEmpty(headerId))
                {
                    headerId = field.Name;
                }

                string headerKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.Headers.{headerId}";
                string localizedHeader = Terraria.Localization.Language.GetTextValue(headerKey);
                if (localizedHeader == headerKey)
                {
                    localizedHeader = headerId;
                }

                configElementsList.Add(new UI.Elements.UIHeader(localizedHeader));
            }

            bool reloadRequired = field.MemberInfo.GetCustomAttribute<ReloadRequiredAttribute>() != null;

            if (field.Type == typeof(bool))
            {
                configElementsList.Add(new UI.Elements.UIToggle(formattedFieldName, field, categoryInstance, CurrentConfig, tooltipString, reloadRequired, onHover));
            }
            else if (field.Type == typeof(float))
            {
                float min = 0f; float max = 100f; float step = 0.01f;
                var rangeAttr = field.MemberInfo.GetCustomAttribute<System.ComponentModel.DataAnnotations.RangeAttribute>();
                if (rangeAttr != null)
                {
                    min = Convert.ToSingle(rangeAttr.Minimum);
                    max = Convert.ToSingle(rangeAttr.Maximum);
                }
                else
                {
                    var tmlRangeAttr = field.MemberInfo.GetCustomAttribute<Terraria.ModLoader.Config.RangeAttribute>();
                    if (tmlRangeAttr != null)
                    {
                        min = Convert.ToSingle(tmlRangeAttr.Min);
                        max = Convert.ToSingle(tmlRangeAttr.Max);
                    }
                }

                configElementsList.Add(new UI.Elements.UIFloatSliderInput(formattedFieldName, field, categoryInstance, min, max, step, CurrentConfig, tooltipString, reloadRequired, onHover));
            }
            else if (field.Type == typeof(int))
            {
                int min = 0; int max = 100; int step = 1;
                var tmlRangeAttr = field.MemberInfo.GetCustomAttribute<Terraria.ModLoader.Config.RangeAttribute>();
                if (tmlRangeAttr != null)
                {
                    min = Convert.ToInt32(tmlRangeAttr.Min);
                    max = Convert.ToInt32(tmlRangeAttr.Max);
                }
                configElementsList.Add(new UI.Elements.UIIntSliderInput(formattedFieldName, field, categoryInstance, min, max, step, CurrentConfig, tooltipString, reloadRequired, onHover));
            }
            else if (field.Type.IsGenericType && field.Type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            {
                bool showAddHeld = field.Name == "DesperadoRicochetBlacklist" || field.Name == "DesperadoExtraProjectileBlacklist";
                configElementsList.Add(new UI.Elements.UIListEditor(formattedFieldName, field, categoryInstance, CurrentConfig, tooltipString, reloadRequired, showAddHeld, onHover));
            }
            else if (field.Type == typeof(string))
            {
                var optionStringsAttr = field.MemberInfo.GetCustomAttribute<Terraria.ModLoader.Config.OptionStringsAttribute>();
                if (optionStringsAttr != null)
                {
                    configElementsList.Add(new UI.Elements.UIStringSelector(formattedFieldName, field, categoryInstance, optionStringsAttr.OptionLabels, CurrentConfig, tooltipString, reloadRequired, onHover));
                }
            }
        }

        private void ShowWindowsFileDialogAsync(bool save, string title, string filter, Action<string> onResult)
        {
            if (_dialogPending)
                return; // Already waiting on a dialog, ignore extra clicks

            _dialogPending = true;
            DialogOpen = true;

            Task.Run(() =>
            {
                try
                {
                    if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        // Fallback path for non-Windows platforms
                        _pendingDialogPath = Path.Combine(Main.SavePath, "ModConfigs", "Stataria_Custom_Profile.json");
                        _pendingDialogAction = onResult;
                        return;
                    }

                    string initialDir = Path.Combine(Main.SavePath, "ModConfigs");
                    if (!Directory.Exists(initialDir))
                        Directory.CreateDirectory(initialDir);

                    string base64Path = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(initialDir));

                    string command = $"[System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms') | Out-Null; " +
                                     $"$d = New-Object System.Windows.Forms.{(save ? "SaveFileDialog" : "OpenFileDialog")}; " +
                                     $"$d.Filter = '{filter}'; " +
                                     $"$d.Title = '{title}'; " +
                                     $"$d.InitialDirectory = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String('{base64Path}')); " +
                                     // Use a transparent TopMost owner form so the dialog always appears above the game
                                     $"$owner = New-Object System.Windows.Forms.Form; $owner.TopMost = $true; $owner.ShowInTaskbar = $false; $owner.Opacity = 0; $owner.Show(); " +
                                     $"$r = $d.ShowDialog($owner); $owner.Dispose(); if ($r -eq 'OK') {{ Write-Output $d.FileName }}";

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        // Signal the main thread to process the result next Update tick
                        _pendingDialogPath = output.Trim();
                        _pendingDialogAction = onResult;
                    }
                }
                catch (Exception ex)
                {
                    _pendingDialogPath = "";
                    _pendingDialogAction = _ => ShowStatusMessage("Error opening file dialog: " + ex.Message, Color.Red);
                }
            });
        }


        private void ShowStatusMessage(string message, Color color)
        {
            if (_statusText != null)
            {
                _statusText.SetText(message);
                _statusText.TextColor = color;
                _statusTimer = 300; // Show for 5 seconds (60 ticks * 5)
            }
        }

        private void ShowDialogOverlay()
        {
            if (_dialogOverlay == null)
            {
                _dialogOverlay = new UIPanel();
                _dialogOverlay.BackgroundColor = new Color(15, 5, 25, 180);
                _dialogOverlay.BorderColor = Color.Transparent;
                _dialogOverlay.IgnoresMouseInteraction = false; // Block mouse events!

                var overlayText = new UIText("File dialog is open...", 1.2f);
                overlayText.HAlign = 0.5f;
                overlayText.VAlign = 0.45f;
                overlayText.TextColor = Color.LightGray;
                _dialogOverlay.Append(overlayText);
            }

            _dialogOverlay.Left.Set(0f, 0f);
            _dialogOverlay.Top.Set(0f, 0f);
            _dialogOverlay.Width.Set(Main.screenWidth, 0f);
            _dialogOverlay.Height.Set(Main.screenHeight, 0f);

            Append(_dialogOverlay);
            _dialogOverlay.Recalculate();
        }

        private void HideDialogOverlay()
        {
            if (_dialogOverlay != null && _dialogOverlay.Parent != null)
            {
                RemoveChild(_dialogOverlay);
            }
        }

        private void SaveConfigAction()
        {
            if (CurrentConfig == null || _dialogPending) return;

            ShowWindowsFileDialogAsync(true, "Save Config Preset", "JSON Files (*.json)|*.json", selectedPath =>
            {
                if (string.IsNullOrEmpty(selectedPath)) return;

                try
                {
                    // Serialize config then inject our branded signature so only Stataria can load it back
                    var jObject = Newtonsoft.Json.Linq.JObject.FromObject(CurrentConfig);
                    jObject["$statariaConfig"] = CurrentConfig.GetType().Name;
                    string json = jObject.ToString(Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(selectedPath, json);
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    ShowStatusMessage($"Successfully saved to {Path.GetFileName(selectedPath)}!", Color.LimeGreen);
                }
                catch (Exception ex)
                {
                    ShowStatusMessage("Failed to save: " + ex.Message, Color.Red);
                }
            });
        }

        private void LoadConfigAction()
        {
            if (CurrentConfig == null || _dialogPending) return;

            ShowWindowsFileDialogAsync(false, "Load Config Preset", "JSON Files (*.json)|*.json", selectedPath =>
            {
                if (string.IsNullOrEmpty(selectedPath) || !File.Exists(selectedPath)) return;

                try
                {
                    string json = File.ReadAllText(selectedPath);
                    var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);

                    // Check for Stataria's branded signature key
                    if (!jObject.ContainsKey("$statariaConfig"))
                    {
                        ShowStatusMessage("Not a Stataria config file!", Color.Red);
                        return;
                    }

                    // Check that the signature matches the currently active config tab
                    string savedConfigType = jObject["$statariaConfig"].ToString();
                    string currentConfigType = CurrentConfig.GetType().Name;
                    if (savedConfigType != currentConfigType)
                    {
                        ShowStatusMessage($"Wrong config type! This file is for '{savedConfigType}'.", Color.Red);
                        return;
                    }

                    // Remove the signature key before populating so it doesn't cause issues
                    jObject.Remove("$statariaConfig");
                    ClearConfigCollections(CurrentConfig);
                    Newtonsoft.Json.JsonConvert.PopulateObject(jObject.ToString(), CurrentConfig);

                    var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (saveMethod != null) saveMethod.Invoke(null, new object[] { CurrentConfig });

                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    UpdateConfigElementsList();
                    ShowStatusMessage($"Successfully loaded from {Path.GetFileName(selectedPath)}!", Color.LimeGreen);
                }
                catch (Exception ex)
                {
                    ShowStatusMessage("Failed to load: " + ex.Message, Color.Red);
                }
            });
        }

        private void SetDefaultsAction()
        {
            if (CurrentConfig == null || DialogOpen) return;

            try
            {
                object defaultInstance = Activator.CreateInstance(CurrentConfig.GetType());
                string defaultJson = Newtonsoft.Json.JsonConvert.SerializeObject(defaultInstance);
                ClearConfigCollections(CurrentConfig);
                Newtonsoft.Json.JsonConvert.PopulateObject(defaultJson, CurrentConfig);

                var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (saveMethod != null) saveMethod.Invoke(null, new object[] { CurrentConfig });

                SoundEngine.PlaySound(SoundID.MenuOpen);
                UpdateConfigElementsList();
                ShowStatusMessage("Successfully restored default settings!", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Failed to reset defaults: " + ex.Message, Color.Red);
            }
        }

        private void ClearConfigCollections(object obj)
        {
            if (obj == null) return;
            var type = obj.GetType();

            if (obj is System.Collections.IList list)
            {
                list.Clear();
                return;
            }

            var assembly = typeof(CustomConfigUIState).Assembly;

            // Properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;

                if (typeof(System.Collections.IList).IsAssignableFrom(prop.PropertyType))
                {
                    var val = prop.GetValue(obj) as System.Collections.IList;
                    if (val != null)
                    {
                        val.Clear();
                    }
                }
                else if (prop.PropertyType.IsClass && prop.PropertyType.Assembly == assembly)
                {
                    var val = prop.GetValue(obj);
                    if (val != null)
                    {
                        ClearConfigCollections(val);
                    }
                }
            }

            // Fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (typeof(System.Collections.IList).IsAssignableFrom(field.FieldType))
                {
                    var val = field.GetValue(obj) as System.Collections.IList;
                    if (val != null)
                    {
                        val.Clear();
                    }
                }
                else if (field.FieldType.IsClass && field.FieldType.Assembly == assembly)
                {
                    var val = field.GetValue(obj);
                    if (val != null)
                    {
                        ClearConfigCollections(val);
                    }
                }
            }
        }
    }
}