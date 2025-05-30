using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Stataria
{
    public class ThoriumSupportHelper : ModSystem
    {
        #region Mod Detection Variables
        public static bool ThoriumLoaded { get; private set; }
        private static Mod thoriumMod;
        private static bool initialized;
        #endregion

        #region Load & Initialize
        public override void Load()
        {
            initialized = false;
            ThoriumLoaded = ModLoader.HasMod("ThoriumMod");

            if (ThoriumLoaded)
            {
                thoriumMod = ModLoader.GetMod("ThoriumMod");
            }
        }

        public override void Unload()
        {
            thoriumMod = null;
            initialized = false;
            ThoriumLoaded = false;
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                ThoriumLoaded = ModLoader.HasMod("ThoriumMod");
                if (ThoriumLoaded)
                {
                    thoriumMod = ModLoader.GetMod("ThoriumMod");
                }

                initialized = true;
            }
            catch (Exception)
            {
                ThoriumLoaded = false;
            }
        }

        public static void RunFieldValidation()
        {
            if (!ThoriumLoaded || thoriumMod == null)
                return;
        }
        #endregion

        #region ModCall Access Methods - Only what's used in RPGPlayer
        public static object CallAddBardInspirationMax(Player player, int amount)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null || amount <= 0)
                return null;

            try
            {
                var result = thoriumMod.Call("BonusBardInspirationMax", player, amount);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static object CallBonusBardEmpowermentDuration(Player player, short ticksToAdd)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null || ticksToAdd <= 0)
                return null;

            try
            {
                var result = thoriumMod.Call("BonusBardEmpowermentDuration", player, ticksToAdd);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static object CallBonusHealerHealBonus(Player player, int amount)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null)
                return null;

            try
            {
                var result = thoriumMod.Call("BonusHealerHealBonus", player, amount);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static object CallBonusLifeRecovery(Player player, int amount)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null)
                return null;

            try
            {
                var result = thoriumMod.Call("BonusLifeRecovery", player, amount);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static object CallBonusLifeRecoveryIntervalReduction(Player player, int amount)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null)
                return null;

            try
            {
                var result = thoriumMod.Call("BonusLifeRecoveryIntervalReduction", player, amount);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static DamageClass GetHealerDamageClass()
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null)
                return DamageClass.Generic;

            try
            {
                if (thoriumMod.TryFind("HealerDamage", out DamageClass healerClass))
                {
                    return healerClass;
                }
            }
            catch
            {
            }

            return DamageClass.Generic;
        }
        #endregion

        #region Item Detection Methods - Used for weapon stat boosting
        public static bool IsSymphonicWeapon(Item item)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || thoriumMod == null || item == null || item.damage <= 0 || item.accessory)
                return false;

            try
            {
                var result = thoriumMod.Call("IsBardWeapon", item);
                if (result != null)
                {
                    if (result is ValueTuple<bool, byte> tuple)
                    {
                        return tuple.Item1;
                    }
                    else if (result is bool isBardWeapon)
                    {
                        return isBardWeapon;
                    }
                }

                if (item.DamageType != null)
                {
                    string damageTypeName = item.DamageType.GetType().Name;
                    if (damageTypeName.Contains("Bard") || damageTypeName.Contains("Symphonic"))
                        return true;
                }

                if (item.ModItem != null)
                {
                    string className = item.ModItem.GetType().FullName;
                    if (className != null &&
                        (className.Contains(".Bard.") ||
                        className.Contains(".Symphonic.") ||
                        className.Contains("BardItem")))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRadiantWeapon(Item item)
        {
            if (!initialized)
                Initialize();

            if (!ThoriumLoaded || item == null || item.damage <= 0 || item.accessory)
                return false;

            try
            {
                DamageClass healerDamageClass = GetHealerDamageClass();

                if (healerDamageClass != DamageClass.Generic && item.DamageType == healerDamageClass)
                    return true;

                if (item.ModItem != null)
                {
                    string className = item.ModItem.GetType().FullName;
                    if (className != null &&
                        (className.Contains(".Healer.") ||
                         className.Contains(".Radiant.") ||
                         className.Contains("HealerItem")))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Utility Methods
        public static bool IsThoriumIntegrationWorking()
        {
            if (!initialized)
                Initialize();

            return ThoriumLoaded && thoriumMod != null;
        }
        #endregion
    }
}