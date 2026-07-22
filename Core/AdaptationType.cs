using Microsoft.Xna.Framework;

namespace Stataria.Core
{
    public enum AdaptationCategory
    {
        Mob,
        Boss,
        Debuff,
        Environment,
        Death
    }

    public static class AdaptationCategoryExtensions
    {
        public static Color GetCategoryColor(this AdaptationCategory category)
        {
            return category switch
            {
                AdaptationCategory.Boss => new Color(255, 200, 60),       // Crimson & Gold highlight
                AdaptationCategory.Mob => new Color(180, 220, 255),       // Cyan / Silver
                AdaptationCategory.Debuff => new Color(130, 235, 120),    // Acid Green
                AdaptationCategory.Environment => new Color(255, 140, 50), // Amber / Fire Orange
                AdaptationCategory.Death => new Color(220, 200, 255),     // Cosmic Purple / White
                _ => Color.White
            };
        }
    }
}
