using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stataria;
using System;
using System.Linq;
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

        // Absolute single source of truth for the UI's activity status
        public static bool IsUIActive { get; private set; }

        public override void OnActivate()
        {
            base.OnActivate();
            IsUIActive = true;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            IsUIActive = false;
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

            // Close Button
            UITextPanel<string> closeButton = new UITextPanel<string>("Close Custom Config");
            closeButton.HAlign = 0.5f;
            closeButton.VAlign = 0.5f; // Center screen relative
            closeButton.Top.Set(415, 0f); // 375 (half of 750) + 40 margin
            closeButton.BackgroundColor = new Color(120, 40, 120); // Bright Purple
            closeButton.OnLeftClick += (evt, element) => {
                SoundEngine.PlaySound(SoundID.MenuClose);
                ConfigUISystem.Instance.HideMyUI();
            };
            closeButton.OnMouseOver += (evt, element) => closeButton.BackgroundColor = new Color(150, 70, 150);
            closeButton.OnMouseOut += (evt, element) => closeButton.BackgroundColor = new Color(120, 40, 120);
            Append(closeButton);

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

            tooltipText = new UIText("Hover over a setting to see its description.", 0.95f);
            tooltipText.HAlign = 0.5f;
            tooltipText.VAlign = 0.4f;
            tooltipText.IsWrapped = true;
            tooltipText.Width.Set(0, 1f);
            tooltipPanel.Append(tooltipText);

            reloadWarningText = new UIText("A reload is required for this setting to take effect.", 0.9f);
            reloadWarningText.TextColor = Color.LightCoral;
            reloadWarningText.HAlign = 0.5f;
            reloadWarningText.VAlign = 0.8f;
            // Initially hidden or empty, but keeping the element
            reloadWarningText.SetText("");
            tooltipPanel.Append(reloadWarningText);

            // Setup Category List
            categoryList = new UIList();
            categoryList.Width.Set(0, 1f);
            categoryList.Height.Set(0, 1f);
            categoryList.ListPadding = 5f;
            categoryList.ManualSortMethod = (elements) => { }; // FIX: Bypass UIList's aggressive auto-sorting
            sidebarPanel.Append(categoryList);

            categoryScrollbar = new UIScrollbar();
            categoryScrollbar.SetView(100f, 1000f);
            categoryScrollbar.Height.Set(0, 1f);
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

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (!Main.gameMenu)
            {
                // Prevent the player from using items while interacting with the UI (Crucial for multiplayer safety!)
                Main.LocalPlayer.mouseInterface = true;
                
                // Extra input bleed suppression mappings
                Main.blockInput = true;
                Main.LocalPlayer.delayUseItem = true;
            }
        }

        public void PopulateConfigsTabs()
        {
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
            
            tab.OnMouseOver += (evt, element) => { if (CurrentConfig != config) tab.BackgroundColor = new Color(100, 50, 150); };
            tab.OnMouseOut += (evt, element) => { if (CurrentConfig != config) tab.BackgroundColor = new Color(50, 30, 80); };
            tab.OnLeftClick += (evt, element) => {
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
                AddCategoryTab("Uncategorized", null);
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
            catTab.OnMouseOver += (evt, element) => catTab.BackgroundColor = new Color(100, 50, 150); // Hover bright purple
            catTab.OnMouseOut += (evt, element) => {
                catTab.BackgroundColor = new Color(50, 30, 80);
            };
            catTab.OnLeftClick += (evt, element) => {
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
            configElementsList.Clear();

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

            // Define tooltip action
            Action<string, bool> onHover = (tt, r) => {
                tooltipText.SetText(string.IsNullOrEmpty(tt) ? "Hover over a setting to see its description." : tt);
                reloadWarningText.SetText(r ? "A reload is required for this setting to take effect." : "");
            };

            foreach (var field in fieldsToDisplay)
            {
                string typeName = categoryInstance.GetType().Name;
                string labelKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Label";
                string tooltipKey = $"Mods.{CurrentConfig.Mod.Name}.Configs.{typeName}.{field.Name}.Tooltip";
                
                string localizedLabel = Terraria.Localization.Language.GetTextValue(labelKey);
                string localizedTooltip = Terraria.Localization.Language.GetTextValue(tooltipKey);
                
                string formattedFieldName = localizedLabel != labelKey ? localizedLabel : FormatCamelCase(field.Name);
                string tooltipString = localizedTooltip != tooltipKey ? localizedTooltip : "";

                bool reloadRequired = field.MemberInfo.GetCustomAttribute<ReloadRequiredAttribute>() != null;

                if (field.Type == typeof(bool))
                {
                    configElementsList.Add(new UI.Elements.UIToggle(formattedFieldName, field, categoryInstance, CurrentConfig, tooltipString, reloadRequired, onHover));
                }
                else if (field.Type == typeof(float))
                {
                    // Default range mapping, or we could read the [Range] attribute
                    float min = 0f; float max = 100f; float step = 0.01f;
                    var rangeAttr = field.MemberInfo.GetCustomAttribute<System.ComponentModel.DataAnnotations.RangeAttribute>();
                    if (rangeAttr != null)
                    {
                         min = Convert.ToSingle(rangeAttr.Minimum);
                         max = Convert.ToSingle(rangeAttr.Maximum);
                    }
                    else
                    {
                         // Try to get tML Range attribute which might be used
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
                    configElementsList.Add(new UI.Elements.UIListEditor(formattedFieldName, field, categoryInstance, CurrentConfig, tooltipString, reloadRequired, onHover));
                }
            }
        }
    }
}