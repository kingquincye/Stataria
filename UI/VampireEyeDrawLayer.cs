using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            if (rpgPlayer != null && rpgPlayer.ActiveRole != null &&
                rpgPlayer.ActiveRole.ID == "Vampire" && rpgPlayer.ActiveRole.Status == RoleStatus.Active)
            {
                drawInfo.drawPlayer.eyeColor = Color.Red;
            }
        }
    }
}