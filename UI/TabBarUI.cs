using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.Audio;
using Terraria.ID;
using System;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.Localization;

namespace Stataria
{
    public class TabBarUI : UIState
    {
        public enum TabType
        {
            Stats,
            Abilities,
            Roles,
            Socketing
        }

        private UIPanel tabPanel;
        private UITextPanel<LocalizedText>[] tabButtons;
        private TabType currentTab = TabType.Stats;
        private List<TabType> availableTabs = new List<TabType>();

        private readonly Color InactiveTabColor = new Color(100, 100, 100, 200);
        private readonly Color ActiveTabColor = new Color(63, 82, 151, 255);
        private readonly Color InactiveTabBorder = new Color(150, 150, 150, 200);
        private readonly Color ActiveTabBorder = new Color(0, 0, 0, 255);

        public TabType CurrentTab => currentTab;

        public override void OnInitialize()
        {
            tabPanel = new UIPanel();
            tabPanel.Width.Set(300f, 0f);
            tabPanel.Height.Set(40f, 0f);
            tabPanel.BackgroundColor = Color.Transparent;
            tabPanel.BorderColor = Color.Transparent;
            tabPanel.SetPadding(0f);
            Append(tabPanel);

            RefreshTabs();
        }

        private void RefreshTabs()
        {
            tabPanel.RemoveAllChildren();
            availableTabs.Clear();

            var config = ModContent.GetInstance<StatariaConfig>();

            availableTabs.Add(TabType.Stats);

            if (config.rebirthSystem.EnableRebirthSystem && config.rebirthSystem.EnableRebirthAbilities)
            {
                availableTabs.Add(TabType.Abilities);
            }

            if (config.roleSettings.EnableRoleSystem)
            {
                availableTabs.Add(TabType.Roles);
            }

            if (config.socketingSystem.EnableSocketingSystem)
            {
                availableTabs.Add(TabType.Socketing);
            }

            if (!availableTabs.Contains(currentTab))
            {
                currentTab = TabType.Stats;
            }

            CreateTabButtons();
        }

        private void CreateTabButtons()
        {
            tabButtons = new UITextPanel<LocalizedText>[availableTabs.Count];
            LocalizedText[] allTabNames = { 
                Language.GetText("Mods.Stataria.UI.TabBar.Stats"), 
                Language.GetText("Mods.Stataria.UI.TabBar.Abilities"), 
                Language.GetText("Mods.Stataria.UI.TabBar.Roles"), 
                Language.GetText("Mods.Stataria.UI.TabBar.Socketing") 
            };

            float tabWidth = 80f;
            float tabHeight = 35f;
            float spacing = 5f;
            float totalWidth = (tabWidth * availableTabs.Count) + (spacing * (availableTabs.Count - 1));
            float startX = (300f - totalWidth) / 2f;

            for (int i = 0; i < availableTabs.Count; i++)
            {
                TabType tabType = availableTabs[i];
                LocalizedText tabName = allTabNames[(int)tabType];

                tabButtons[i] = new UITextPanel<LocalizedText>(tabName, 0.9f, false)
                {
                    Width = { Pixels = tabWidth },
                    Height = { Pixels = tabHeight },
                    Top = { Pixels = 0f },
                    Left = { Pixels = startX + (tabWidth + spacing) * i },
                    BackgroundColor = tabType == currentTab ? ActiveTabColor : InactiveTabColor,
                    BorderColor = tabType == currentTab ? ActiveTabBorder : InactiveTabBorder
                };

                tabButtons[i].SetPadding(8f);

                int localTabType = (int)tabType;
                tabButtons[i].OnLeftClick += (evt, el) =>
                {
                    SwitchToTab((TabType)localTabType);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };

                tabPanel.Append(tabButtons[i]);
            }
        }

        private void SwitchToTab(TabType newTab)
        {
            if (currentTab == newTab || !availableTabs.Contains(newTab)) return;

            var config = ModContent.GetInstance<StatariaConfig>();

            currentTab = newTab;

            var rpg = Main.LocalPlayer?.GetModPlayer<RPGPlayer>();
            if (rpg != null)
            {
                rpg.LastActiveTab = newTab;
            }

            UpdateTabAppearance();

            switch (newTab)
            {
                case TabType.Stats:
                    StatariaUI.SkillTreeUI?.SetState(null);
                    StatariaUI.RoleSelectionUI?.SetState(null);
                    StatariaUI.SocketingUI?.SetState(null);
                    StatariaUI.StatUI?.SetState(StatariaUI.Panel);
                    break;
                case TabType.Abilities:
                    if (config.rebirthSystem.EnableRebirthSystem && config.rebirthSystem.EnableRebirthAbilities)
                    {
                        StatariaUI.StatUI?.SetState(null);
                        StatariaUI.RoleSelectionUI?.SetState(null);
                        StatariaUI.SocketingUI?.SetState(null);
                        StatariaUI.SkillTreeUI?.SetState(StatariaUI.SkillTreePanel);
                        StatariaUI.SkillTreePanel?.RefreshAbilitiesList();
                    }
                    break;
                case TabType.Roles:
                    StatariaUI.StatUI?.SetState(null);
                    StatariaUI.SkillTreeUI?.SetState(null);
                    StatariaUI.SocketingUI?.SetState(null);
                    StatariaUI.RoleSelectionUI?.SetState(StatariaUI.RoleSelectionPanel);
                    StatariaUI.RoleSelectionPanel?.RefreshRolesList();
                    break;
                case TabType.Socketing:
                    if (config.socketingSystem.EnableSocketingSystem)
                    {
                        StatariaUI.StatUI?.SetState(null);
                        StatariaUI.SkillTreeUI?.SetState(null);
                        StatariaUI.RoleSelectionUI?.SetState(null);
                        StatariaUI.SocketingUI?.SetState(StatariaUI.SocketingPanel);
                        StatariaUI.SocketingPanel?.RefreshUI();
                    }
                    break;
            }
        }

        public void SetActiveTab(TabType tab)
        {
            if (currentTab != tab)
            {
                currentTab = tab;
                UpdateTabAppearance();
            }
        }

        private void UpdateTabAppearance()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (i == (int)currentTab)
                {
                    tabButtons[i].BackgroundColor = ActiveTabColor;
                    tabButtons[i].BorderColor = ActiveTabBorder;
                }
                else
                {
                    tabButtons[i].BackgroundColor = InactiveTabColor;
                    tabButtons[i].BorderColor = InactiveTabBorder;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var config = ModContent.GetInstance<StatariaConfig>();
            bool shouldRefresh = false;

            bool socketingAvailable = config.socketingSystem.EnableSocketingSystem;
            bool socketingInTabs = availableTabs.Contains(TabType.Socketing);
            if (socketingAvailable != socketingInTabs)
                shouldRefresh = true;

            bool abilitiesAvailable = config.rebirthSystem.EnableRebirthSystem && config.rebirthSystem.EnableRebirthAbilities;
            bool abilitiesInTabs = availableTabs.Contains(TabType.Abilities);
            if (abilitiesAvailable != abilitiesInTabs)
                shouldRefresh = true;

            bool rolesAvailable = config.roleSettings.EnableRoleSystem;
            bool rolesInTabs = availableTabs.Contains(TabType.Roles);
            if (rolesAvailable != rolesInTabs)
                shouldRefresh = true;

            if (shouldRefresh)
            {
                RefreshTabs();
            }

            PositionAboveActivePanel();

            bool mouseOverAnyTab = false;
            foreach (var tabButton in tabButtons)
            {
                if (tabButton.ContainsPoint(Main.MouseScreen))
                {
                    mouseOverAnyTab = true;
                    break;
                }
            }

            if (mouseOverAnyTab)
                Main.LocalPlayer.mouseInterface = true;
        }

        private void PositionAboveActivePanel()
        {
            CalculatedStyle? activePanelDimensions = null;

            if (StatariaUI.StatUI?.CurrentState != null && StatariaUI.Panel?.statPanel != null)
            {
                activePanelDimensions = StatariaUI.Panel.statPanel.GetOuterDimensions();
            }
            else if (StatariaUI.SkillTreeUI?.CurrentState != null && StatariaUI.SkillTreePanel?.skillPanel != null)
            {
                activePanelDimensions = StatariaUI.SkillTreePanel.skillPanel.GetOuterDimensions();
            }
            else if (StatariaUI.RoleSelectionUI?.CurrentState != null && StatariaUI.RoleSelectionPanel?.rolePanel != null)
            {
                activePanelDimensions = StatariaUI.RoleSelectionPanel.rolePanel.GetOuterDimensions();
            }
            else if (StatariaUI.SocketingUI?.CurrentState != null && StatariaUI.SocketingPanel?.socketingPanel != null)
            {
                activePanelDimensions = StatariaUI.SocketingPanel.socketingPanel.GetOuterDimensions();
            }

            if (activePanelDimensions.HasValue)
            {
                var dimensions = activePanelDimensions.Value;

                float tabX = dimensions.X + (dimensions.Width - tabPanel.Width.Pixels) / 2f;
                float tabY = dimensions.Y - 40f;

                tabPanel.Left.Set(tabX, 0f);
                tabPanel.Top.Set(tabY, 0f);
                tabPanel.HAlign = 0f;
                tabPanel.VAlign = 0f;

                tabPanel.Recalculate();
            }
        }
    }
}