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

        public override void Load()
        {
            // Only load UI on client side
            if (Main.dedServ)
                return;
            StatUI = new UserInterface();
            Panel = new StatPanel();
            Panel.Activate();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ)
                return;
            if (StatUI?.CurrentState != null)
            {
                StatUI.Update(gameTime);
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
            }
        }
    }
}