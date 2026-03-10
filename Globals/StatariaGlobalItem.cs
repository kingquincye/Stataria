using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria
{
    public class StatariaGlobalItem : GlobalItem
    {
        public override void ModifyItemScale(Item item, Player player, ref float scale)
        {
            var rpg = player.GetModPlayer<RPGPlayer>();

            if (rpg.RebirthAbilities.TryGetValue("GiantsGrip", out RebirthAbility ability) && ability.IsUnlocked)
            {
                if (item.CountsAsClass(DamageClass.Melee))
                {
                    scale *= 1.33f;
                }
            }
        }

        public override bool OnPickup(Item item, Player player)
        {
            var rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (rpg.RebirthAbilities.TryGetValue("GoldenTouch", out RebirthAbility ability) && ability.IsUnlocked && ability.Level > 0)
            {
                if (item.type >= ItemID.CopperCoin && item.type <= ItemID.PlatinumCoin)
                {
                    float multiplier = 1.0f + (ability.Level * config.rebirthAbilities.GoldenTouchPercentPerLevel / 100f);

                    long newStackLong = (long)(item.stack * multiplier);
                    int newStack = (int)System.Math.Min(newStackLong, item.maxStack);

                    item.stack = newStack;
                }
            }

            return true;
        }


    }
}