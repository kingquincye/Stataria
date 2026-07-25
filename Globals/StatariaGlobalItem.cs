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

        public override void ModifyTooltips(Item item, System.Collections.Generic.List<TooltipLine> tooltips)
        {
            var player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            var rpg = player.GetModPlayer<RPGPlayer>();

            if (item.damage > 0)
            {
                TooltipLine damageLine = tooltips.Find(x => x.Name == "Damage" && x.Mod == "Terraria");
                if (damageLine != null)
                {
                    double trueDamage = rpg.GetTrueWeaponDamage(item);
                    if (trueDamage > 1000000.0)
                    {
                        var config = ModContent.GetInstance<StatariaConfig>();
                        if (trueDamage < 2000000000.0 || config.advanced.EnableCustomPlayerDamage)
                        {
                            string[] parts = damageLine.Text.Split(' ', 2);
                            if (parts.Length > 1)
                            {
                                damageLine.Text = $"{RPGPlayer.FormatBigDamage(trueDamage)} {parts[1]}";
                            }
                        }
                    }
                }
            }

            var adaptor = player.GetModPlayer<Players.AdaptationPlayer>();
            if (adaptor != null && adaptor.IsAdaptorActive)
            {
                if (item != null && item.ModItem != null && item.ModItem.Mod != null && item.ModItem.Mod.Name == "NoxusBoss" && item.ModItem.Name == "EmptinessSprayer")
                {
                    string tooltipText = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Tooltip.AdaptorEmptinessSprayer");
                    if (string.IsNullOrEmpty(tooltipText) || tooltipText == "Mods.Stataria.Tooltip.AdaptorEmptinessSprayer")
                    {
                        tooltipText = "[Adaptor] \"Deletes lesser beings\"... What happens when absolute erasure meets absolute adaptation?";
                    }

                    var line = new TooltipLine(Mod, "AdaptorEmptinessSprayer", tooltipText)
                    {
                        OverrideColor = new Microsoft.Xna.Framework.Color(220, 160, 255)
                    };
                    tooltips.Add(line);
                }
            }
        }
    }
}