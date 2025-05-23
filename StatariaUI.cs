using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaUI : ModSystem
    {
        internal static UserInterface StatUI;
        internal static StatPanel Panel;
        internal static UserInterface SkillTreeUI;
        internal static SkillTreeUI SkillTreePanel;
        internal static UserInterface XPVerificationUI;

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
            }
        }
    }
}