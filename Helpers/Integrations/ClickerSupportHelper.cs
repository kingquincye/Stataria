using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Stataria
{
    public class ClickerSupportHelper : ModSystem
    {
        #region Mod Detection Variables
        public static bool ClickerClassLoaded { get; private set; }
        private static bool initialized;
        #endregion

        #region Load & Initialize
        public override void Load()
        {
            initialized = false;
            ClickerClassLoaded = ModLoader.HasMod("ClickerClass");
        }

        public override void Unload()
        {
            initialized = false;
            ClickerClassLoaded = false;
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                ClickerClassLoaded = ModLoader.HasMod("ClickerClass");
                initialized = true;
            }
            catch (Exception)
            {
                ClickerClassLoaded = false;
            }
        }
        #endregion

        #region Helper Methods
        public static bool IsClickerClassIntegrationWorking()
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return false;

            return ClickerCompat.ClickerClass != null;
        }

        public static bool IsClickerWeapon(Item item)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return false;

            return ClickerCompat.IsClickerWeapon(item);
        }

        public static float GetClickerRadius(Player player)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return 1f;

            return ClickerCompat.GetClickerRadius(player);
        }

        public static void AddClickerDamage(Player player, float amount)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return;

            ClickerCompat.SetDamageAdd(player, amount);
        }

        public static void AddClickerCrit(Player player, int amount)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return;

            ClickerCompat.SetClickerCritAdd(player, amount);
        }

        public static void AddClickerRadius(Player player, float amount)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return;

            ClickerCompat.SetClickerRadiusAdd(player, amount);
        }

        public static void ReduceClickEffectThreshold(Player player, int flatReduction)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return;

            ClickerCompat.SetClickerBonusAdd(player, flatReduction);
        }

        public static void ReduceClickEffectThresholdPercent(Player player, float percentReduction)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return;

            ClickerCompat.SetClickerBonusPercentAdd(player, percentReduction);
        }

        public static void AddFlatClickerDamage(Player player, int amount)
        {
            if (!initialized)
                Initialize();

            if (!ClickerClassLoaded)
                return;

            ClickerCompat.SetDamageFlatAdd(player, amount);
        }
        #endregion
    }
}