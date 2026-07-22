using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Stataria.Players;

namespace Stataria.Globals
{
    public class AdaptationGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public string SourceNPCTargetId { get; private set; }
        public string SourceNPCName { get; private set; }
        public bool SourceNPCIsBoss { get; private set; }
        public bool HasNPCSource => !string.IsNullOrEmpty(SourceNPCTargetId);

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            IEntitySource currentSource = source;

            while (currentSource is EntitySource_Parent parent)
            {
                if (parent.Entity is NPC npcParent && npcParent.active)
                {
                    SourceNPCIsBoss = npcParent.boss;
                    AdaptationPlayer.GetNPCTargetIdAndName(npcParent, out string tId, out string tName);
                    SourceNPCTargetId = tId;
                    SourceNPCName = tName;
                    return;
                }
                else if (parent.Entity is Projectile parentProj && parentProj.active)
                {
                    if (parentProj.TryGetGlobalProjectile<AdaptationGlobalProjectile>(out var parentGlobal) && parentGlobal.HasNPCSource)
                    {
                        SourceNPCIsBoss = parentGlobal.SourceNPCIsBoss;
                        SourceNPCTargetId = parentGlobal.SourceNPCTargetId;
                        SourceNPCName = parentGlobal.SourceNPCName;
                        return;
                    }
                    break;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
