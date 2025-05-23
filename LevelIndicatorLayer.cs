using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.GameContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace Stataria
{
    public class LevelIndicatorLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ)
                return;

            Player player = drawInfo.drawPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.uiSettings.ShowLevelIndicator)
                return;

            Vector2 pos = player.MountedCenter - Main.screenPosition;
            pos.Y -= 70f;

            float opacity = config.uiSettings.IndicatorOpacity;

            string levelText = $"Lv.{rpg.Level}";
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(levelText);

            Vector2 textPos = new(pos.X - textSize.X / 2, pos.Y - textSize.Y / 2);

            Main.spriteBatch.DrawString(font, levelText, new Vector2(textPos.X + 2, textPos.Y + 2),
                Color.Black * (opacity * 0.5f));
            Main.spriteBatch.DrawString(font, levelText, textPos, Color.White * opacity);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => true;
    }
}