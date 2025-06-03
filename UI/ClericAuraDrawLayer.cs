using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using System;
using Terraria.ID;

namespace Stataria
{
    public class ClericAuraDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.active && !drawInfo.drawPlayer.dead;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            RPGPlayer rpgPlayer = player.GetModPlayer<RPGPlayer>();

            if (rpgPlayer?.ActiveRole?.ID != "Cleric" || rpgPlayer.ActiveRole.Status != RoleStatus.Active)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            float radius = config.roleSettings.ClericAuraRadius;

            Vector2 position = player.Center - Main.screenPosition;
            
            Color auraColor = Color.Yellow * 0.2f;
            
            for (int angle = 0; angle < 360; angle += 2)
            {
                float radian = MathHelper.ToRadians(angle);
                Vector2 offset = new Vector2(
                    (float)Math.Cos(radian) * radius,
                    (float)Math.Sin(radian) * radius
                );
                
                Vector2 dustPos = position + offset;
                
                if (Main.rand.NextBool(10) && dustPos.X > -50 && dustPos.X < Main.screenWidth + 50 && 
                    dustPos.Y > -50 && dustPos.Y < Main.screenHeight + 50)
                {
                    Dust dust = Dust.NewDustPerfect(dustPos + Main.screenPosition, 
                        DustID.YellowTorch, Vector2.Zero, 0, Color.Yellow * 0.6f, 0.8f);
                    dust.noGravity = true;
                    dust.fadeIn = 0.3f;
                    dust.velocity = Vector2.Zero;
                }
            }

            for (int i = 0; i < 20; i++)
            {
                float innerRadius = radius * Main.rand.NextFloat(0.3f, 0.9f);
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 innerPos = position + new Vector2(
                    (float)Math.Cos(angle) * innerRadius,
                    (float)Math.Sin(angle) * innerRadius
                );

                if (Main.rand.NextBool(30))
                {
                    Dust innerDust = Dust.NewDustPerfect(innerPos + Main.screenPosition,
                        DustID.YellowTorch, Vector2.Zero, 0, Color.LightYellow * 0.4f, 0.5f);
                    innerDust.noGravity = true;
                    innerDust.fadeIn = 0.2f;
                }
            }

            float lightStrength = Math.Min(radius / 500f, 1f);
            Lighting.AddLight(player.Center, 0.4f * lightStrength, 0.4f * lightStrength, 0.1f * lightStrength);
        }
    }
}