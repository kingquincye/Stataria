using System;
using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class LuckSystem : ModSystem
    {
        public override void Load()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config.extraLuckSettings.EnableExtraLuckSystem)
            {
                On_Player.RollLuck += RollLuck_Patch;
            }
        }

        public override void Unload()
        {
            On_Player.RollLuck -= RollLuck_Patch;
        }

        private int RollLuck_Patch(On_Player.orig_RollLuck orig, Player self, int chance)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float extraLuck = config.extraLuckSettings.BaseExtraLuck;

            var rpgPlayer = self.GetModPlayer<RPGPlayer>();
            if (rpgPlayer.RebirthAbilities.TryGetValue("EnhancedFortune", out RebirthAbility fortuneAbility) &&
                fortuneAbility.IsUnlocked &&
                fortuneAbility.AbilityData.TryGetValue("Enabled", out object isEnabledObj) &&
                isEnabledObj is bool isEnabled && isEnabled)
            {
                extraLuck += fortuneAbility.Level * config.rebirthAbilities.LuckPerAbilityLevel;
            }

            float rawLuck = self.luck + extraLuck;

            float effective;
            if (rawLuck < -1f)
                effective = -1f;
            else if (rawLuck <= 1f)
                effective = rawLuck;
            else
                effective = (float)Math.Pow(2, rawLuck) - 1f;

            if (effective < 0f)
                chance = (int)(chance * (1f + effective));
            else if (effective > 0f)
                chance = (int)(chance / (1f + effective));

            if (chance < 1)
                chance = 1;

            return Main.rand.Next(chance);
        }
    }
}