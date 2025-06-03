using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Stataria.UI;

namespace Stataria
{
    public class DivineInterventionCooldownBar : PlayerBarDrawLayer
    {
        protected override bool IsVisible(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            if (!config.roleSettings.EnableDivineInterventionCooldownBar) return false;
            
            if (rpgPlayer.ActiveRole?.ID != "Cleric" || rpgPlayer.ActiveRole.Status != RoleStatus.Active) 
                return false;
                
            float cooldown = config.roleSettings.DivineInterventionCooldown * 60f;
            return rpgPlayer.divineInterventionCooldownTimer > 0 && cooldown > 0;
        }

        protected override float GetProgress(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            float cooldown = config.roleSettings.DivineInterventionCooldown * 60f;
            if (cooldown <= 0) return -1f;
            return 1f - (rpgPlayer.divineInterventionCooldownTimer / cooldown);
        }

        protected override Color GetForegroundColor(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return Color.Gold;
        }

        protected override Vector2 GetBarPositionOffset(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return new Vector2(0f, 80f);
        }
    }
}