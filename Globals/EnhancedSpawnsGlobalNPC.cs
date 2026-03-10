using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class EnhancedSpawnsGlobalNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            var rpgPlayer = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (rpgPlayer.RebirthAbilities.TryGetValue("EnhancedSpawns", out RebirthAbility enhancedSpawns) &&
                enhancedSpawns.IsUnlocked &&
                enhancedSpawns.Level > 0 &&
                enhancedSpawns.AbilityData.TryGetValue("Enabled", out object isEnabledObj) &&
                isEnabledObj is bool isEnabled && isEnabled)
            {
                float boost = enhancedSpawns.Level * config.rebirthAbilities.SpawnRatePercentPerLevel / 100f;

                spawnRate = (int)(spawnRate / (1f + boost));
                maxSpawns = (int)(maxSpawns * (1f + boost));
            }
        }
    }
}