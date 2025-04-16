using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.GameContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Stataria
{
    public class TeleportCooldownBar : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Main.dedServ)
                return;

            Player player = drawInfo.drawPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (rpg.AGI < config.AGI_TeleportUnlockAt || !config.EnableTeleportCooldownBar)
                return;

            float cooldown = config.AGI_TeleportCooldown * 60f;
            if (rpg.teleportCooldownTimer <= 0 || cooldown <= 0)
                return;

            Vector2 pos = player.MountedCenter - Main.screenPosition;
            pos.Y += 60f; // Below the player

            float width = 60f;
            float height = 6f;
            float progress = 1f - (rpg.teleportCooldownTimer / cooldown);
            float opacity = 1f;

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Rectangle bg = new((int)(pos.X - width / 2), (int)(pos.Y - height / 2), (int)width, (int)height);
            Rectangle fg = new(bg.X, bg.Y, (int)(width * progress), (int)height);

            Main.spriteBatch.Draw(pixel, bg, Color.Black * 0.5f * opacity);
            Main.spriteBatch.Draw(pixel, fg, Color.BlueViolet * opacity);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => true;
    }
}