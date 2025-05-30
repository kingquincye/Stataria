using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Stataria.UI;

namespace Stataria
{
    public class TeleportCooldownBar : PlayerBarDrawLayer
    {
        protected override bool IsVisible(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            if (!config.rebirthAbilities.EnableTeleportCooldownBar) return false;
            float cooldown = config.rebirthAbilities.TeleportCooldown * 60f;
            return rpgPlayer.teleportCooldownTimer > 0 && cooldown > 0;
        }

        protected override float GetProgress(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            float cooldown = config.rebirthAbilities.TeleportCooldown * 60f;
            if (cooldown <= 0) return -1f;
            return 1f - (rpgPlayer.teleportCooldownTimer / cooldown);
        }

        protected override Color GetForegroundColor(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return Color.BlueViolet;
        }

        protected override Vector2 GetBarPositionOffset(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return new Vector2(0f, 60f);
        }
    }
}