using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.GameContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Stataria
{
    public class StatariaXPBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ)
                return;
            Player player = drawInfo.drawPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();

            if (rpg.xpBarTimer <= 0)
                return;

            Vector2 pos = player.MountedCenter - Main.screenPosition;
            pos.Y -= 60f;

            float width = 60f;
            float height = 6f;
            float progress = (float)rpg.XP / rpg.XPToNext;
            float opacity = MathHelper.Clamp((float)rpg.xpBarTimer / 20f, 0f, 1f);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Rectangle bg = new((int)(pos.X - width / 2), (int)(pos.Y - height / 2), (int)width, (int)height);
            Rectangle fg = new(bg.X, bg.Y, (int)(width * progress), (int)height);

            Main.spriteBatch.Draw(pixel, bg, Color.Black * 0.5f * opacity);
            Main.spriteBatch.Draw(pixel, fg, Color.Gold * opacity);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (Main.dedServ)
                return false;
            return true;
        }
    }
}