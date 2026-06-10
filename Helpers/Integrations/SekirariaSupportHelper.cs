using System;
using System.Reflection;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class SekirariaSupportHelper : ModSystem
    {
        public static bool SekirariaLoaded { get; private set; }
        private static Mod sekirariaMod;
        private static Type sekirariaPlayerType;
        private static bool initialized;
        private static MethodInfo getModPlayerMethod;

        public override void Load()
        {
            initialized = false;
            SekirariaLoaded = ModLoader.HasMod("Sekiraria");
            if (SekirariaLoaded)
            {
                sekirariaMod = ModLoader.GetMod("Sekiraria");
            }
        }

        public override void Unload()
        {
            sekirariaMod = null;
            sekirariaPlayerType = null;
            getModPlayerMethod = null;
            initialized = false;
            SekirariaLoaded = false;
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                SekirariaLoaded = ModLoader.HasMod("Sekiraria");
                if (SekirariaLoaded)
                {
                    sekirariaMod = ModLoader.GetMod("Sekiraria");
                    sekirariaPlayerType = sekirariaMod.Code.GetType("Sekiraria.Common.Players.SekirariaPlayer");
                }
                initialized = true;
            }
            catch (Exception)
            {
                SekirariaLoaded = false;
            }
        }

        private static object GetSekirariaPlayer(Player player)
        {
            if (!initialized)
                Initialize();

            if (!SekirariaLoaded || sekirariaPlayerType == null)
                return null;

            try
            {
                if (getModPlayerMethod == null)
                {
                    getModPlayerMethod = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == "GetModPlayer" && m.IsGenericMethod && m.GetParameters().Length == 0);
                }

                if (getModPlayerMethod != null)
                {
                    MethodInfo genericMethod = getModPlayerMethod.MakeGenericMethod(sekirariaPlayerType);
                    return genericMethod.Invoke(player, null);
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error getting SekirariaPlayer: " + e.Message);
            }

            return null;
        }

        public static void AddPlayerPostureMaxBonus(Player player, float amount)
        {
            if (!initialized)
                Initialize();

            if (!SekirariaLoaded || sekirariaMod == null)
                return;

            try
            {
                sekirariaMod.Call("AddPlayerPostureMaxBonus", player, amount);
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Warn($"Error calling AddPlayerPostureMaxBonus: {e.Message}");
            }
        }

        public static void AddPlayerPostureDamageFlatBonus(Player player, float amount)
        {
            if (!initialized)
                Initialize();

            if (!SekirariaLoaded || sekirariaMod == null)
                return;

            try
            {
                sekirariaMod.Call("AddPlayerPostureDamageFlatBonus", player, amount);
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Warn($"Error calling AddPlayerPostureDamageFlatBonus: {e.Message}");
            }
        }

        public static void AddPlayerBlockDamageMultiplier(Player player, float amount)
        {
            if (!initialized)
                Initialize();

            if (!SekirariaLoaded || sekirariaMod == null)
                return;

            try
            {
                sekirariaMod.Call("AddPlayerBlockDamageMultiplier", player, amount);
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Warn($"Error calling AddPlayerBlockDamageMultiplier: {e.Message}");
            }
        }

        public static void SyncPlayerPostureMax(Player player)
        {
            if (!initialized)
                Initialize();

            if (!SekirariaLoaded || sekirariaPlayerType == null || sekirariaMod == null)
                return;

            try
            {
                object sPlayer = GetSekirariaPlayer(player);
                if (sPlayer == null)
                    return;

                var maxField = sekirariaPlayerType.GetField("playerPostureMax", BindingFlags.Instance | BindingFlags.Public);
                var maxBonusField = sekirariaPlayerType.GetField("playerPostureMaxBonus", BindingFlags.Instance | BindingFlags.Public);

                if (maxField != null && maxBonusField != null)
                {
                    float baseMax = 100f;
                    Type configType = sekirariaMod.Code.GetType("Sekiraria.SekirariaConfig");
                    if (configType != null)
                    {
                        MethodInfo getInstanceMethod = typeof(ModContent).GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(m => m.Name == "GetInstance" && m.IsGenericMethod && m.GetParameters().Length == 0);
                        if (getInstanceMethod != null)
                        {
                            object configInstance = getInstanceMethod.MakeGenericMethod(configType).Invoke(null, null);
                            if (configInstance != null)
                            {
                                var baseMaxField = configType.GetField("PlayerMaxPosture", BindingFlags.Instance | BindingFlags.Public);
                                if (baseMaxField != null)
                                {
                                    baseMax = (float)baseMaxField.GetValue(configInstance);
                                }
                            }
                        }
                    }

                    float bonus = (float)maxBonusField.GetValue(sPlayer);
                    maxField.SetValue(sPlayer, baseMax + bonus);
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error syncing player posture max: " + e.Message);
            }
        }
    }
}
