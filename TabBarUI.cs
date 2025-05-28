using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.Audio;
using Terraria.ID;
using System;

namespace Stataria
{
    public class TabBarUI : UIState
    {
        public enum TabType
        {
            Stats,
            Abilities,
            Roles
        }

        private UIPanel tabPanel;
        private UITextPanel<string>[] tabButtons;
        private TabType currentTab = TabType.Stats;

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

            tabButtons = new UITextPanel<string>[3];
            string[] tabNames = { "Stats", "Abilities", "Roles" };
            float tabWidth = 80f;
            float tabHeight = 35f;
            float spacing = 5f;
            float startX = (300f - (tabWidth * 3 + spacing * 2)) / 2f;

            for (int i = 0; i < 3; i++)
            {
                int tabIndex = i;
                TabType tabType = (TabType)i;

                tabButtons[i] = new UITextPanel<string>(tabNames[i], 0.9f, false)
                {
                    Width = { Pixels = tabWidth },
                    Height = { Pixels = tabHeight },
                    Top = { Pixels = 0f },
                    Left = { Pixels = startX + (tabWidth + spacing) * i },
                    BackgroundColor = i == 0 ? ActiveTabColor : InactiveTabColor,
                    BorderColor = i == 0 ? ActiveTabBorder : InactiveTabBorder
                };

                tabButtons[i].SetPadding(8f);

                tabButtons[i].OnLeftClick += (evt, el) =>
                {
                    SwitchToTab(tabType);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };

                tabPanel.Append(tabButtons[i]);
            }
        }

        private void SwitchToTab(TabType newTab)
        {
            if (currentTab == newTab) return;

            currentTab = newTab;
            UpdateTabAppearance();

            switch (newTab)
            {
                case TabType.Stats:
                    StatariaUI.SkillTreeUI?.SetState(null);
                    StatariaUI.RoleSelectionUI?.SetState(null);
                    StatariaUI.StatUI?.SetState(StatariaUI.Panel);
                    break;
                case TabType.Abilities:
                    StatariaUI.StatUI?.SetState(null);
                    StatariaUI.RoleSelectionUI?.SetState(null);
                    StatariaUI.SkillTreeUI?.SetState(StatariaUI.SkillTreePanel);
                    StatariaUI.SkillTreePanel?.RefreshAbilitiesList();
                    break;
                case TabType.Roles:
                    StatariaUI.StatUI?.SetState(null);
                    StatariaUI.SkillTreeUI?.SetState(null);
                    StatariaUI.RoleSelectionUI?.SetState(StatariaUI.RoleSelectionPanel);
                    StatariaUI.RoleSelectionPanel?.RefreshRolesList();
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