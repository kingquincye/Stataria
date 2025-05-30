using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public abstract class BaseExtraAccessorySlot : ModAccessorySlot
    {
        protected abstract int RequiredAbilityLevel { get; }

        public override bool IsEnabled()
        {
            if (Player == null || !Player.active)
            {
                return false;
            }

            try
            {
                var rpgPlayer = Player.GetModPlayer<RPGPlayer>();
                if (rpgPlayer == null)
                {
                    return false;
                }

                if (rpgPlayer.RebirthAbilities != null &&
                    rpgPlayer.RebirthAbilities.TryGetValue("ExtraAccessorySlot", out RebirthAbility ability))
                {
                    return ability.IsUnlocked && ability.Level >= RequiredAbilityLevel;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public override bool CanAcceptItem(Item checkItem, AccessorySlotType context)
        {
            if (context == AccessorySlotType.FunctionalSlot)
            {
                return checkItem.accessory;
            }
            return base.CanAcceptItem(checkItem, context);
        }
    }
}