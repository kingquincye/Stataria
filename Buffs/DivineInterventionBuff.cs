using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace Stataria.Buffs
{
    public class DivineInterventionBuff : ModBuff
    {
        private static readonly HashSet<int> ExemptDebuffs = new HashSet<int>
        {
            BuffID.ManaSickness,
            BuffID.PotionSickness,
            BuffID.ChaosState,
            BuffID.Suffocation,
            BuffID.Tipsy,
            BuffID.WaterCandle,
            BuffID.ShadowCandle,
            BuffID.NoBuilding,
            BuffID.NeutralHunger,
            BuffID.Hunger,
            BuffID.Starving,
            BuffID.BrainOfConfusionBuff,
            BuffID.Shimmer
        };

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            Player player = Main.LocalPlayer;

            if (player != null)
            {
                int buffIndex = player.FindBuffIndex(Type);
                if (buffIndex >= 0)
                {
                    float timeLeft = player.buffTime[buffIndex] / 60f;
                    buffName = $"Divine Intervention ({timeLeft:0.0}s)";
                    tip = "Protected from harmful debuffs by divine grace";
                }
            }
        }
        
        public override void Update(Player player, ref int buffIndex)
        {
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffType = player.buffType[i];
                if (buffType > 0 && Main.debuff[buffType] && !ExemptDebuffs.Contains(buffType))
                {
                    player.DelBuff(i);
                    i--;
                }
            }
        }
    }
}