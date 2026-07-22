using Microsoft.Xna.Framework;
using Stataria.Core;

namespace Stataria.UI
{
    public class AdaptationNotification
    {
        public string DisplayName { get; set; }
        public AdaptationCategory Category { get; set; }
        public int Level { get; set; }
        public float ExpProgress { get; set; } // 0.0 to 1.0
        public bool IsOffensive { get; set; }
        public bool IsLevelUp { get; set; }

        public float TimeRemaining { get; set; }
        public float MaxDuration { get; set; }
        public float Alpha { get; set; } = 1.0f;

        public AdaptationNotification(string displayName, AdaptationCategory category, int level, float expProgress, bool isOffensive = false, bool isLevelUp = false, float durationSeconds = 3.5f)
        {
            DisplayName = displayName;
            Category = category;
            Level = level;
            ExpProgress = MathHelper.Clamp(expProgress, 0f, 1f);
            IsOffensive = isOffensive;
            IsLevelUp = isLevelUp;
            MaxDuration = durationSeconds;
            TimeRemaining = durationSeconds;
        }

        public bool Update(float deltaTime)
        {
            TimeRemaining -= deltaTime;

            // Fade out during the last 0.6 seconds
            if (TimeRemaining <= 0.6f)
            {
                Alpha = MathHelper.Clamp(TimeRemaining / 0.6f, 0f, 1f);
            }

            return TimeRemaining <= 0f;
        }
    }
}
