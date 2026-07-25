using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Stataria.UI;
using ReLogic.Graphics;

namespace Stataria
{
    public class StatariaUI : ModSystem
    {
        internal static UserInterface StatUI;
        internal static StatPanel Panel;
        internal static UserInterface SkillTreeUI;
        internal static SkillTreeUI SkillTreePanel;
        internal static UserInterface XPVerificationUI;
        internal static UserInterface RoleSelectionUI;
        internal static RoleSelectionUI RoleSelectionPanel;
        internal static UserInterface TabBarInterface;
        internal static TabBarUI TabBarPanel;
        internal static UserInterface SocketingUI;
        internal static SocketingUI SocketingPanel;
        internal static UserInterface AdaptationNotificationUserInterface;
        internal static AdaptationNotificationUI AdaptationNotificationPanel;
        internal static UserInterface AdaptationUI;
        internal static AdaptationUI AdaptationPanel;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            StatUI = new UserInterface();
            Panel = new StatPanel();
            Panel.Activate();
            SkillTreeUI = new UserInterface();
            SkillTreePanel = new SkillTreeUI();
            SkillTreePanel.Activate();
            XPVerificationUI = new UserInterface();
            RoleSelectionUI = new UserInterface();
            RoleSelectionPanel = new RoleSelectionUI();
            RoleSelectionPanel.Activate();
            TabBarInterface = new UserInterface();
            TabBarPanel = new TabBarUI();
            TabBarPanel.Activate();
            SocketingUI = new UserInterface();
            SocketingPanel = new SocketingUI();
            SocketingPanel.Activate();

            AdaptationUI = new UserInterface();
            AdaptationPanel = new AdaptationUI();
            AdaptationPanel.Activate();

            AdaptationNotificationUserInterface = new UserInterface();
            AdaptationNotificationPanel = new AdaptationNotificationUI();
            AdaptationNotificationPanel.Activate();
            AdaptationNotificationUserInterface.SetState(AdaptationNotificationPanel);

            Main.OnResolutionChanged += OnResolutionChanged;
        }

        public override void Unload()
        {
            if (!Main.dedServ)
            {
                Main.OnResolutionChanged -= OnResolutionChanged;
            }

            StatUI = null;
            Panel = null;
            SkillTreeUI = null;
            SkillTreePanel = null;
            XPVerificationUI = null;
            RoleSelectionUI = null;
            RoleSelectionPanel = null;
            TabBarInterface = null;
            TabBarPanel = null;
            SocketingUI = null;
            SocketingPanel = null;
            AdaptationUI = null;
            AdaptationPanel = null;
            AdaptationNotificationUserInterface = null;
            AdaptationNotificationPanel = null;
        }

        private static void OnResolutionChanged(Vector2 size)
        {
            StatUI?.Recalculate();
            SkillTreeUI?.Recalculate();
            XPVerificationUI?.Recalculate();
            RoleSelectionUI?.Recalculate();
            SocketingUI?.Recalculate();
            AdaptationUI?.Recalculate();
            TabBarInterface?.Recalculate();
            AdaptationNotificationUserInterface?.Recalculate();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ)
                return;

            if (StatUI?.CurrentState != null)
            {
                StatUI.Update(gameTime);
            }
            if (SkillTreeUI?.CurrentState != null)
            {
                SkillTreeUI.Update(gameTime);
            }
            if (XPVerificationUI?.CurrentState != null)
            {
                XPVerificationUI.Update(gameTime);
            }
            if (RoleSelectionUI?.CurrentState != null)
            {
                RoleSelectionUI.Update(gameTime);
            }
            if (SocketingUI?.CurrentState != null)
            {
                SocketingUI.Update(gameTime);
            }
            if (AdaptationUI?.CurrentState != null)
            {
                AdaptationUI.Update(gameTime);
            }
            if (AdaptationNotificationUserInterface?.CurrentState != null)
            {
                AdaptationNotificationUserInterface.Update(gameTime);
            }
            if (TabBarInterface?.CurrentState != null)
            {
                TabBarInterface.Update(gameTime);

                if (StatUI?.CurrentState != null)
                    TabBarPanel?.SetActiveTab(TabBarUI.TabType.Stats);
                else if (SkillTreeUI?.CurrentState != null)
                    TabBarPanel?.SetActiveTab(TabBarUI.TabType.Abilities);
                else if (RoleSelectionUI?.CurrentState != null)
                    TabBarPanel?.SetActiveTab(TabBarUI.TabType.Roles);
                else if (SocketingUI?.CurrentState != null)
                    TabBarPanel?.SetActiveTab(TabBarUI.TabType.Socketing);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ)
                return;
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Stat Panel",
                    delegate
                    {
                        if (StatUI?.CurrentState != null)
                        {
                            StatUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Adaptations",
                    delegate
                    {
                        if (AdaptationUI?.CurrentState != null)
                        {
                            AdaptationUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Skill Tree",
                    delegate
                    {
                        if (SkillTreeUI?.CurrentState != null)
                        {
                            SkillTreeUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: XP Verification",
                    delegate
                    {
                        if (XPVerificationUI?.CurrentState != null)
                        {
                            XPVerificationUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Role Selection",
                    delegate
                    {
                        if (RoleSelectionUI?.CurrentState != null)
                        {
                            RoleSelectionUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Socketing",
                    delegate
                    {
                        if (SocketingUI?.CurrentState != null)
                        {
                            SocketingUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Tab Bar",
                    delegate
                    {
                        if (TabBarInterface?.CurrentState != null)
                        {
                            TabBarInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Necromancer Souls",
                    delegate
                    {
                        NecromancerUI.Draw(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Spellweaver Charge",
                    delegate
                    {
                        SpellweaverUI.Draw(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Berserker Vignette",
                    delegate
                    {
                        Player player = Main.LocalPlayer;
                        if (player != null && player.active && !player.dead)
                        {
                            var berserkerPlayer = player.GetModPlayer<BerserkerPlayer>();
                            if (berserkerPlayer.IsBerserkerActive && berserkerPlayer.IsSavageRoarActive)
                            {
                                int maxRoarTimer = (int)(ModContent.GetInstance<StatariaConfig>().roleSettings.BerserkerSavageRoarDuration * 60f);
                                if (maxRoarTimer > 0)
                                {
                                    float ratio = (float)berserkerPlayer.SavageRoarTimer / maxRoarTimer;
                                    float pulse = 0.5f + 0.15f * (float)Math.Sin(Main.timeForVisualEffects * 0.15f);
                                    float baseOpacity = Math.Clamp(ratio * pulse * 1.5f, 0f, 0.7f);

                                    if (baseOpacity > 0f)
                                    {
                                        Texture2D pixel = TextureAssets.MagicPixel.Value;
                                        int maxDepth = 120;
                                        int step = 4;
                                        for (int i = 0; i < maxDepth; i += step)
                                        {
                                            float opacity = baseOpacity * (float)Math.Pow((float)(maxDepth - i) / maxDepth, 1.5f);
                                            Color color = Color.Red * opacity;
                                            // Top
                                            Main.spriteBatch.Draw(pixel, new Rectangle(i, i, Main.screenWidth - 2 * i, step), color);
                                            // Bottom
                                            Main.spriteBatch.Draw(pixel, new Rectangle(i, Main.screenHeight - step - i, Main.screenWidth - 2 * i, step), color);
                                            // Left
                                            Main.spriteBatch.Draw(pixel, new Rectangle(i, i + step, step, Main.screenHeight - 2 * i - 2 * step), color);
                                            // Right
                                            Main.spriteBatch.Draw(pixel, new Rectangle(Main.screenWidth - step - i, i + step, step, Main.screenHeight - 2 * i - 2 * step), color);
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Role Cooldowns",
                    delegate
                    {
                        RoleCooldownUI.Draw(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Adaptation Notifications",
                    delegate
                    {
                        if (AdaptationNotificationUserInterface?.CurrentState != null)
                        {
                            AdaptationNotificationUserInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Stataria: Spirit Indicators",
                    delegate
                    {
                        DrawSpiritIndicators(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.Game)
                );
            }
        }

        public static void ToggleAdaptationUI()
        {
            if (AdaptationUI == null || AdaptationPanel == null)
                return;

            if (AdaptationUI.CurrentState != null)
            {
                AdaptationUI.SetState(null);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
            }
            else
            {
                StatUI?.SetState(null);
                SkillTreeUI?.SetState(null);
                RoleSelectionUI?.SetState(null);
                SocketingUI?.SetState(null);
                TabBarInterface?.SetState(null);

                AdaptationPanel.RefreshAdaptationList();
                AdaptationUI.SetState(AdaptationPanel);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
            }
        }

        private static void DrawSpiritIndicators(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active)
                    continue;

                var clericPlayer = player.GetModPlayer<ClericPlayer>();
                if (clericPlayer != null && clericPlayer.IsInSpiritForm)
                {
                    Vector2 pos = player.MountedCenter - Main.screenPosition;
                    pos.Y -= 65f;

                    string titleText = "Spirit Anchor";
                    float timerSeconds = clericPlayer.SpiritFormTimer / 60f;
                    string timerText = timerSeconds.ToString("0.0") + "s";
                    string fullText = $"{player.name} - {titleText} ({timerText})";

                    var font = FontAssets.MouseText.Value;
                    Vector2 textSize = font.MeasureString(fullText);
                    Vector2 textPos = new Vector2(pos.X - textSize.X / 2f, pos.Y - textSize.Y / 2f);

                    int teamIndex = Math.Clamp(player.team, 0, Main.teamColor.Length - 1);
                    Color textColor = Main.teamColor[teamIndex];
                    if (player.team == 0)
                    {
                        textColor = new Color(200, 220, 255);
                    }

                    spriteBatch.DrawString(font, fullText, new Vector2(textPos.X + 1f, textPos.Y + 1f), Color.Black * 0.7f);
                    spriteBatch.DrawString(font, fullText, textPos, textColor);
                }
            }
        }
    }
}