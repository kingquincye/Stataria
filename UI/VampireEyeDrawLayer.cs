using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Stataria
{
    public class VampireEyeDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.active && !drawInfo.drawPlayer.dead;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            RPGPlayer rpgPlayer = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (rpgPlayer != null && rpgPlayer.ActiveRole != null &&
                rpgPlayer.ActiveRole.ID == "Vampire" && rpgPlayer.ActiveRole.Status == RoleStatus.Active &&
                config.roleSettings.VampireEnableEyeColorChange)
            {
                // Capture original eye color if we haven't already
                if (!rpgPlayer.OriginalEyeColor.HasValue)
                {
                    rpgPlayer.OriginalEyeColor = player.eyeColor;
                }

                drawInfo.drawPlayer.eyeColor = Color.Red;
            }
            else if (rpgPlayer != null && rpgPlayer.OriginalEyeColor.HasValue)
            {
                // Restore original eye color when Vampire is not active
                drawInfo.drawPlayer.eyeColor = rpgPlayer.OriginalEyeColor.Value;
            }
        }
    }
}