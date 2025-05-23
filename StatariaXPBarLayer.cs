using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Stataria.UI;

namespace Stataria
{
    public class StatariaXPBarLayer : PlayerBarDrawLayer
    {
        protected override bool IsVisible(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return rpgPlayer.xpBarTimer > 0 && config.uiSettings.ShowXPBarAbovePlayer;
        }

        protected override float GetProgress(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            if (rpgPlayer.XPToNext <= 0) return 0f;
            return (float)rpgPlayer.XP / rpgPlayer.XPToNext;
        }

        protected override Color GetForegroundColor(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return Color.Gold;
        }

        protected override Vector2 GetBarPositionOffset(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return new Vector2(0f, -35f);
        }

        protected override float GetOpacity(Player player, RPGPlayer rpgPlayer, StatariaConfig config)
        {
            return MathHelper.Clamp((float)rpgPlayer.xpBarTimer / 20f, 0f, 1f);
        }
    }
}