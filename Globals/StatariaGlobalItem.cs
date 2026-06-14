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

        public override bool ConsumeItem(Item item, Player player)
        {
            var rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
            {
                if (CalamitySupportHelper.IsRogueWeapon(item))
                {
                    int effectiveRGE = rpg.GetEffectiveStat("RGE");
                    if (effectiveRGE > 0)
                    {
                        float chance = effectiveRGE * (config.modIntegration.RGE_AmmoConsumptionReduction / 100f);
                        if (Main.rand.NextFloat() < chance)
                            return false;
                    }
                }
            }

            return base.ConsumeItem(item, player);
        }

        public override bool? UseItem(Item item, Player player)
        {
            var spellweaverPlayer = player.GetModPlayer<SpellweaverPlayer>();
            if (spellweaverPlayer.IsSpellweaverActive && item.DamageType == DamageClass.Magic && item.mana > 0)
            {
                float oldCharge = spellweaverPlayer.ElementalCharge;
                spellweaverPlayer.ElementalCharge = System.Math.Min(spellweaverPlayer.MaxElementalCharge, spellweaverPlayer.ElementalCharge + item.mana);
                
                if (spellweaverPlayer.ElementalCharge > oldCharge && Main.netMode != NetmodeID.Server)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        // Spawn small sparks on charge gain
                        int d = Dust.NewDust(player.position, player.width, player.height, DustID.Electric, 0, 0, 100, default, 0.7f);
                        Main.dust[d].velocity *= 0.3f;
                        Main.dust[d].noGravity = true;
                    }
                }

                if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.SinglePlayer)
                {
                    spellweaverPlayer.SyncSpellweaverState();
                }
            }
            return null;
        }
    }
}