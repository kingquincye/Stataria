using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using System;
using Terraria.ID;
using Terraria.GameContent;

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
            bool isAngel = rpgPlayer.AscendedRoles.Contains("Cleric");
            float radius = isAngel ? config.roleSettings.AngelAuraRadius : config.roleSettings.ClericAuraRadius;

            Vector2 position = player.Center - Main.screenPosition;
            
            // Draw aura boundary sparkles
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

            // Draw tight body aura directly around the player's body
            if (Main.netMode != NetmodeID.Server && drawInfo.shadow == 0f)
            {
                bool spawnSpark = isAngel ? Main.rand.NextBool(2) : Main.rand.NextBool(5);
                if (spawnSpark)
                {
                    int dustType = isAngel ? DustID.GoldFlame : DustID.YellowTorch;
                    float dustScale = isAngel ? Main.rand.NextFloat(0.8f, 1.2f) : Main.rand.NextFloat(0.6f, 0.9f);
                    
                    Vector2 dustPos = player.position + new Vector2(
                        Main.rand.NextFloat(-2f, player.width + 2f),
                        Main.rand.NextFloat(-2f, player.height + 2f)
                    );
                    
                    Vector2 velocity = new Vector2(player.velocity.X * 0.2f, Main.rand.NextFloat(-0.5f, -1.5f));
                    
                    Dust dust = Dust.NewDustPerfect(dustPos, dustType, velocity, 130, Color.Gold, dustScale);
                    dust.noGravity = true;
                    dust.fadeIn = 0.1f;
                }
            }

            // Draw inner sparkles scattered everywhere in the protective aura
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
                    int dustType = isAngel ? DustID.GoldFlame : DustID.YellowTorch;
                    float dustScale = isAngel ? 0.8f : 0.5f;
                    Dust innerDust = Dust.NewDustPerfect(innerPos + Main.screenPosition,
                        dustType, Vector2.Zero, 0, Color.LightYellow * 0.4f, dustScale);
                    innerDust.noGravity = true;
                    innerDust.fadeIn = 0.2f;
                }
            }

            // Draw a glowing golden halo above the player's head (bobbing over time)
            if (isAngel && drawInfo.shadow == 0f)
            {
                if (DrawHelper.IsSpriteBatchActive(Main.spriteBatch))
                {
                    Texture2D pixel = TextureAssets.MagicPixel.Value;
                    Vector2 haloCenter = player.Top - new Vector2(0f, 14f) - Main.screenPosition;
                    haloCenter.Y += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 2f;
                    
                    float radiusX = 12f;
                    float radiusY = 3.5f;
                    
                    int segments = 32;
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = (float)i / segments * MathHelper.TwoPi;
                        Vector2 offset = new Vector2((float)Math.Cos(angle) * radiusX, (float)Math.Sin(angle) * radiusY);
                        Vector2 pos = haloCenter + offset;
                        
                        Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 1, (int)pos.Y - 1, 2, 2), Color.Gold * 0.9f);
                        Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 2, (int)pos.Y - 2, 4, 4), Color.Yellow * 0.25f);
                    }
                }
            }


            // Draw solid horizontal progress bar while resurrecting
            var clericPlayer = player.GetModPlayer<ClericPlayer>();
            if (isAngel && clericPlayer.IsResurrectionChanneling)
            {
                if (DrawHelper.IsSpriteBatchActive(Main.spriteBatch))
                {
                    float progress = clericPlayer.ChannelingProgress; // 0f to 1f
                    Texture2D pixel = TextureAssets.MagicPixel.Value;
                    
                    Vector2 basePos = player.MountedCenter - Main.screenPosition;
                    Vector2 pos = basePos + new Vector2(0f, -45f);
                    
                    float width = 60f;
                    float height = 8f;
                    
                    Rectangle bgRect = new Rectangle((int)(pos.X - width / 2), (int)(pos.Y - height / 2), (int)width, (int)height);
                    float fgHeight = height - 2f;
                    float fgWidth = (width - 2f) * Math.Clamp(progress, 0f, 1f);
                    Rectangle fgRect = new Rectangle((int)(bgRect.X + 1), (int)(bgRect.Y + 1), (int)fgWidth, (int)fgHeight);
                    
                    Main.spriteBatch.Draw(pixel, bgRect, Color.Black * 0.7f);
                    Main.spriteBatch.Draw(pixel, fgRect, Color.Gold);
                }
            }

            float lightStrength = Math.Min(radius / 500f, 1f);
            Lighting.AddLight(player.Center, 0.4f * lightStrength, 0.4f * lightStrength, 0.1f * lightStrength);
        }
    }
}