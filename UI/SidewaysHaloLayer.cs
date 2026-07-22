using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Stataria.Core;
using Stataria.Players;

namespace Stataria.UI
{
    public class SidewaysHaloLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.Head);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.dead || drawInfo.drawPlayer.ghost)
                return false;

            var adaptorPlayer = drawInfo.drawPlayer.GetModPlayer<AdaptationPlayer>();
            return adaptorPlayer != null && adaptorPlayer.IsAdaptorActive;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            var adaptorPlayer = player.GetModPlayer<AdaptationPlayer>();
            if (adaptorPlayer == null || !adaptorPlayer.IsAdaptorActive)
                return;

            var clientConfig = ModContent.GetInstance<StatariaClientConfig>();

            float opacity = clientConfig != null ? clientConfig.HaloOpacity : 1.0f;
            if (opacity <= 0.01f)
                return;

            // Center above player's head
            Vector2 headCenter = drawInfo.Position - Main.screenPosition + new Vector2(player.width * 0.5f, player.height * 0.15f - 18f);
            headCenter = new Vector2((int)headCenter.X, (int)headCenter.Y);

            // Ellipse dimensions for a sideways halo
            float radiusX = 20f;
            float radiusY = 7f;

            float spinAngle = adaptorPlayer.HaloRotation;

            // Draw halo ring using small glowing particles/dots in an ellipse
            int numSegments = 16;
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Color baseHaloColor = adaptorPlayer.ActiveCategory.GetCategoryColor();
            if (adaptorPlayer.HaloSpinTimer > 0)
            {
                // Pulsate brightness with crisp white glow on the ring during active spin
                float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 15f) * 0.3f + 0.7f;
                baseHaloColor = Color.Lerp(baseHaloColor, Color.White, pulse) * opacity;
            }
            else
            {
                baseHaloColor = baseHaloColor * 0.75f * opacity;
            }

            // Emit ambient lighting centered on halo above player's head (user request: soft lighting visible in dark places!)
            Vector2 worldHeadCenter = player.Center - new Vector2(0f, player.height * 0.35f + 14f);
            Lighting.AddLight(worldHeadCenter, baseHaloColor.ToVector3() * 0.45f * opacity);

            for (int i = 0; i < numSegments; i++)
            {
                float angle = (float)(i * Math.Tau / numSegments) + spinAngle;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * radiusX, (float)Math.Sin(angle) * radiusY);

                Vector2 pos = headCenter + offset;

                // Depth fade: segments further back (higher Y in screen space, smaller Sin angle) drawn slightly dimmer
                float depthScale = MathHelper.Lerp(0.7f, 1.1f, (offset.Y + radiusY) / (radiusY * 2f));
                Color dotColor = baseHaloColor * depthScale;

                Rectangle drawRect = new Rectangle((int)pos.X - 2, (int)pos.Y - 2, (int)(4 * depthScale), (int)(4 * depthScale));
                drawInfo.DrawDataCache.Add(new DrawData(pixel, drawRect, dotColor));
            }
        }
    }
}
