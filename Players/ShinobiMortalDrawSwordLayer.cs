using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Stataria
{
    public class ShinobiMortalDrawSwordLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.ArmOverItem);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer.dead || !drawPlayer.active) return false;

            if (!SekirariaSupportHelper.SekirariaLoaded) return false;

            var shinobiPlayer = drawPlayer.GetModPlayer<ShinobiPlayer>();
            return shinobiPlayer.IsShinobiActive && shinobiPlayer.MortalDrawAnimationTimer > 0 && SekirariaSupportHelper.HasParrySword(drawPlayer, out _);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;

            try
            {
                Texture2D texture = ModContent.Request<Texture2D>("Sekiraria/Items/ParrySword/ParrySword").Value;
                if (texture == null) return;

                // Position in screen space
                Vector2 drawPos = drawPlayer.itemLocation - Main.screenPosition;
                drawPos.Y += drawPlayer.gfxOffY;

                SpriteEffects effects = SpriteEffects.None;
                Vector2 origin = new Vector2(0f, texture.Height);

                if (drawPlayer.direction == -1)
                {
                    effects = SpriteEffects.FlipHorizontally;
                    origin = new Vector2(texture.Width, texture.Height);
                }

                Color drawColor = drawPlayer.GetImmuneAlpha(Lighting.GetColor((int)(drawPlayer.Center.X / 16f), (int)(drawPlayer.Center.Y / 16f)), drawInfo.shadow);

                DrawData drawData = new DrawData(
                    texture,
                    drawPos,
                    null,
                    drawColor,
                    drawPlayer.itemRotation,
                    origin,
                    1f,
                    effects,
                    0
                );

                drawInfo.DrawDataCache.Add(drawData);
            }
            catch (System.Exception)
            {
                // Fallback safety
            }
        }
    }
}
