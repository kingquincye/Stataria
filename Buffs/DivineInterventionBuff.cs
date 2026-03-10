using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using System;

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
            var config = ModContent.GetInstance<StatariaConfig>();

            var exemptBuffs = new HashSet<int>(ExemptDebuffs);

            foreach (string entry in config.roleSettings.DivineInterventionExemptBuffs)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;

                if (int.TryParse(entry.Trim(), out int buffId))
                {
                    exemptBuffs.Add(buffId);
                }
                else
                {
                    string buffName = entry.Trim();
                    for (int i = 0; i < BuffLoader.BuffCount; i++)
                    {
                        ModBuff modBuff = BuffLoader.GetBuff(i);
                        if (modBuff != null && modBuff.Name.Equals(buffName, StringComparison.OrdinalIgnoreCase))
                        {
                            exemptBuffs.Add(i);
                            break;
                        }

                        if (Lang.GetBuffName(i).Equals(buffName, StringComparison.OrdinalIgnoreCase))
                        {
                            exemptBuffs.Add(i);
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffType = player.buffType[i];
                if (buffType > 0 && Main.debuff[buffType] && !exemptBuffs.Contains(buffType))
                {
                    player.DelBuff(i);
                    i--;
                }
            }
        }
    }
}