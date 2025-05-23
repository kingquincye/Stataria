using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.GameContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Stataria.UI
{
    public abstract class PlayerBarDrawLayer : PlayerDrawLayer
    {
        protected abstract bool IsVisible(Player player, RPGPlayer rpgPlayer, StatariaConfig config);
        protected abstract float GetProgress(Player player, RPGPlayer rpgPlayer, StatariaConfig config);
        protected abstract Color GetForegroundColor(Player player, RPGPlayer rpgPlayer, StatariaConfig config);
        protected abstract Vector2 GetBarPositionOffset(Player player, RPGPlayer rpgPlayer, StatariaConfig config);
        protected virtual float GetBarWidth(Player player, RPGPlayer rpgPlayer, StatariaConfig config) => 60f;
        protected virtual float GetBarHeight(Player player, RPGPlayer rpgPlayer, StatariaConfig config) => 6f;
        protected virtual float GetOpacity(Player player, RPGPlayer rpgPlayer, StatariaConfig config) => 1f;
        protected virtual Color GetBackgroundColor(Player player, RPGPlayer rpgPlayer, StatariaConfig config) => Color.Black * 0.5f;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ)
                return;

            Player player = drawInfo.drawPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!IsVisible(player, rpg, config))
                return;

            float progress = GetProgress(player, rpg, config);
            if (progress < 0f) return;

            Vector2 basePos = player.MountedCenter - Main.screenPosition;
            Vector2 offset = GetBarPositionOffset(player, rpg, config);
            Vector2 pos = basePos + offset;

            float width = GetBarWidth(player, rpg, config);
            float height = GetBarHeight(player, rpg, config);
            float opacity = GetOpacity(player, rpg, config);
            Color bgColor = GetBackgroundColor(player, rpg, config);
            Color fgColor = GetForegroundColor(player, rpg, config);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Rectangle bgRect = new((int)(pos.X - width / 2), (int)(pos.Y - height / 2), (int)width, (int)height);
            Rectangle fgRect = new(bgRect.X, bgRect.Y, (int)(width * Math.Clamp(progress, 0f, 1f)), (int)height);

            Main.spriteBatch.Draw(pixel, bgRect, bgColor * opacity);
            Main.spriteBatch.Draw(pixel, fgRect, fgColor * opacity);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
             if (Main.dedServ) return false;
             return true;
        }
    }
}