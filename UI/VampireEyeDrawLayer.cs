using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Stataria
{
    public class VampireEyeDrawLayer : PlayerDrawLayer
    {
        private static Dictionary<int, Color> originalEyeColors = new Dictionary<int, Color>();

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

            if (!originalEyeColors.ContainsKey(player.whoAmI))
            {
                originalEyeColors[player.whoAmI] = player.eyeColor;
            }

            if (rpgPlayer != null && rpgPlayer.ActiveRole != null &&
                rpgPlayer.ActiveRole.ID == "Vampire" && rpgPlayer.ActiveRole.Status == RoleStatus.Active &&
                config.roleSettings.VampireEnableEyeColorChange)
            {
                drawInfo.drawPlayer.eyeColor = Color.Red;
            }
            else
            {
                if (originalEyeColors.TryGetValue(player.whoAmI, out Color originalColor))
                {
                    drawInfo.drawPlayer.eyeColor = originalColor;
                }
            }
        }

        public override void Unload()
        {
            originalEyeColors?.Clear();
            originalEyeColors = null;
        }
    }
}