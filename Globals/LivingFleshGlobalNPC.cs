using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Stataria.Projectiles;
using Stataria.Buffs;

namespace Stataria
{
    public class LivingFleshGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // Apply regen buff to nearby players with Living Flesh active
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead)
                {
                    var lfPlayer = p.GetModPlayer<LivingFleshPlayer>();
                    if (lfPlayer.IsLivingFleshActive)
                    {
                        float distance = Vector2.Distance(npc.Center, p.Center);
                        if (!config.roleSettings.EnableRoleProximity || distance <= config.roleSettings.RoleProximityRange)
                        {
                            int duration = (int)(config.roleSettings.LivingFleshKillRegenDuration * 60f);
                            p.AddBuff(ModContent.BuffType<LivingFleshRegenBuff>(), duration);
                        }
                    }
                }
            }
        }
    }
}
