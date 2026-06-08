using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using System.IO;

namespace Stataria
{
    public class StatariaScalingGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public float damageMult = 1f;
        public int flatDamage = 0;
        public bool hasBeenScaled = false;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.hostile || projectile.trap)
            {
                NPC sourceNPC = null;

                if (source is EntitySource_Parent parent)
                {
                    if (parent.Entity is NPC npc)
                    {
                        sourceNPC = npc;
                    }
                    else if (parent.Entity is Projectile parentProj)
                    {
                        if (parentProj.TryGetGlobalProjectile<StatariaScalingGlobalProjectile>(out var projScaling) && projScaling.hasBeenScaled)
                        {
                            damageMult = projScaling.damageMult;
                            flatDamage = projScaling.flatDamage;
                            hasBeenScaled = true;
                            return;
                        }
                    }
                }
                else if (source is EntitySource_Death death && death.Entity is NPC deadNpc)
                {
                    sourceNPC = deadNpc;
                }

                if (sourceNPC != null)
                {
                    if (sourceNPC.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var npcScaling) && npcScaling.hasBeenScaled)
                    {
                        var config = ModContent.GetInstance<StatariaConfig>();
                        if (!config.enemyScaling.EnableEnemyScaling) return;

                        damageMult = npcScaling.damageMult;
                        
                        if (config.enemyScaling.EnableFlatEnemyScaling)
                        {
                            if (sourceNPC.boss && config.enemyScaling.EnableBossScaling)
                            {
                                flatDamage = (npcScaling.Level - 1) * config.enemyScaling.FlatBossDamageScaling;
                            }
                            else if (!sourceNPC.boss)
                            {
                                flatDamage = (npcScaling.Level - 1) * config.enemyScaling.FlatEnemyDamageScaling;
                            }

                            if (npcScaling.IsElite)
                            {
                                flatDamage = (int)(flatDamage * config.enemyScaling.EliteDamageMultiplier);
                            }
                        }

                        hasBeenScaled = true;
                    }
                }
            }
        }

        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            // If we choose to use ModifyHitPlayer instead of directly modifying projectile.damage:
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config != null && !config.enemyScaling.EnableEnemyScaling) return;

            if (hasBeenScaled)
            {
                modifiers.FinalDamage *= damageMult;
                if (config.enemyScaling.EnableFlatEnemyScaling)
                {
                    modifiers.FinalDamage += flatDamage;
                }
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(hasBeenScaled);
            if (hasBeenScaled)
            {
                binaryWriter.Write(damageMult);
                binaryWriter.Write(flatDamage);
            }
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            hasBeenScaled = bitReader.ReadBit();
            if (hasBeenScaled)
            {
                damageMult = binaryReader.ReadSingle();
                flatDamage = binaryReader.ReadInt32();
            }
        }
    }
}
