using Terraria.ModLoader;

namespace Stataria
{
    public class CritGodPlayer : ModPlayer
    {
        public bool EnableSummonCrits { get; set; }

        public override void ResetEffects()
        {
            EnableSummonCrits = false;
        }
    }
}