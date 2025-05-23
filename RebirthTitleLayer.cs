using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.GameContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace Stataria
{
    public class RebirthTitleLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ)
                return;

            Player player = drawInfo.drawPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.uiSettings.ShowRebirthTitle || !config.rebirthSystem.EnableRebirthSystem || rpg.RebirthCount <= 0)
                return;

            Vector2 pos = player.MountedCenter - Main.screenPosition;
            pos.Y -= 50f;

            float opacity = config.uiSettings.IndicatorOpacity;

            string rebirthText = $"Rebirth {rpg.RebirthCount}";
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(rebirthText);

            Vector2 textPos = new(pos.X - textSize.X / 2, pos.Y - textSize.Y / 2);

            Color purpleColor = new Color(190, 120, 220);
            Main.spriteBatch.DrawString(font, rebirthText, new Vector2(textPos.X + 2, textPos.Y + 2),
                Color.Black * (opacity * 0.5f));
            Main.spriteBatch.DrawString(font, rebirthText, textPos, purpleColor * opacity);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => true;
    }
}