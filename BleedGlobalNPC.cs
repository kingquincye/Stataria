using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace Stataria
{
    public class BleedGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool isBleedActive = false;
        private float bleedTimer = 0f;
        private int dustTimer = 0;

        public override void ResetEffects(NPC npc)
        {
            isBleedActive = false;
        }

        public override void PostAI(NPC npc)
        {
            if (!isBleedActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            bleedTimer += 1f;
            if (bleedTimer >= config.roleSettings.VampireBleedTickInterval * 60f)
            {
                int bleedDamage = (int)(npc.lifeMax * config.roleSettings.VampireBleedDamagePercent / 100f);
                bleedDamage = Math.Max(1, bleedDamage);

                npc.life -= bleedDamage;

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(npc.Hitbox, Color.DarkRed, bleedDamage, false, false);
                }

                if (npc.life <= 0)
                {
                    npc.life = 0;
                    npc.checkDead();
                }

                bleedTimer = 0f;
            }

            dustTimer++;
            if (dustTimer >= 20)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPos = npc.position + new Vector2(
                        Main.rand.Next(npc.width),
                        Main.rand.Next(npc.height)
                    );

                    Dust dust = Dust.NewDustDirect(dustPos, 4, 4, DustID.Blood);
                    dust.velocity = Vector2.Zero;
                    dust.scale = 0.8f;
                    dust.alpha = 100;
                    dust.noGravity = true;
                }
                dustTimer = 0;
            }
        }
    }
}