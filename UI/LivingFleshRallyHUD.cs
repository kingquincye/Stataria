using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace Stataria.UI
{
    public class LivingFleshRallyHUD : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (Main.dedServ) return false;

            Player player = drawInfo.drawPlayer;
            if (player == null || !player.active || player.dead) return false;

            var lfPlayer = player.GetModPlayer<LivingFleshPlayer>();
            if (lfPlayer == null || !lfPlayer.IsLivingFleshActive) return false;

            // Suppress during clone rendering — the bar would appear at the clone's position otherwise
            if (lfPlayer.IsDrawingClone) return false;

            // Do not draw the overhead floating bar if the custom Stataria resource bar is currently active/selected
            var customDisplay = ModContent.GetInstance<StatariaResourceDisplaySet>();
            if (customDisplay != null && customDisplay.Selected) return false;

            var clientConfig = ModContent.GetInstance<StatariaClientConfig>();
            if (clientConfig == null || !clientConfig.EnableVanillaHUDFloatingRallyBar) return false;

            return true;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ || !DrawHelper.IsSpriteBatchActive(Main.spriteBatch))
                return;

            Player player = drawInfo.drawPlayer;
            var lfPlayer = player.GetModPlayer<LivingFleshPlayer>();

            // Setup sizes
            float width = 60f;
            float height = 6f;
            Vector2 pos = player.MountedCenter - Main.screenPosition + new Vector2(0f, -42f);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Background
            Rectangle bgRect = new((int)(pos.X - width / 2), (int)(pos.Y - height / 2), (int)width, (int)height);
            Main.spriteBatch.Draw(pixel, bgRect, Color.Black * 0.5f);

            // 1. Current Health segment (solid Red)
            float hpPercent = player.statLifeMax2 > 0 ? Math.Clamp((float)player.statLife / player.statLifeMax2, 0f, 1f) : 0f;
            int hpWidth = (int)(width * hpPercent);
            if (hpWidth > 0)
            {
                Main.spriteBatch.Draw(pixel, new Rectangle(bgRect.X, bgRect.Y, hpWidth, (int)height), Color.Red);
            }

            // 2. Rallyable Health segment (translucent Red)
            if (lfPlayer.RallyableHealth > 0 && lfPlayer.RallyTimer > 0)
            {
                float rallyPercent = player.statLifeMax2 > 0 ? Math.Clamp((float)lfPlayer.RallyableHealth / player.statLifeMax2, 0f, 1f) : 0f;
                int rallyWidth = (int)(width * rallyPercent);

                if (hpWidth + rallyWidth > width)
                {
                    rallyWidth = (int)width - hpWidth;
                }

                if (rallyWidth > 0)
                {
                    Color rallyColor = Color.Red * 0.4f; // Translucent red
                    Main.spriteBatch.Draw(pixel, new Rectangle(bgRect.X + hpWidth, bgRect.Y, rallyWidth, (int)height), rallyColor);
                }
            }
        }
    }
}
