using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Stataria.UI;

namespace Stataria
{
    public class LastStandCooldownBar : PlayerBarDrawLayer
    {
        protected override bool IsVisible(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            if (!config.rebirthAbilities.EnableLastStandCooldownBar) return false;
            float cooldown = config.rebirthAbilities.LastStandCooldown * 60f;
            return rpgPlayer.lastStandCooldownTimer > 0 && cooldown > 0;
        }

        protected override float GetProgress(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            float cooldown = config.rebirthAbilities.LastStandCooldown * 60f;
            if (cooldown <= 0) return -1f;
            return 1f - (rpgPlayer.lastStandCooldownTimer / cooldown);
        }

        protected override Color GetForegroundColor(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return Color.LimeGreen;
        }

        protected override Vector2 GetBarPositionOffset(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return new Vector2(0f, 70f);
        }
    }
}