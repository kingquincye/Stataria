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

        public static bool HasParrySword(Player player, out Item parrySwordItem)
        {
            parrySwordItem = null;
            if (!initialized)
                Initialize();

            if (!SekirariaLoaded || sekirariaPlayerType == null)
                return false;

            try
            {
                object sPlayer = GetSekirariaPlayer(player);
                if (sPlayer == null)
                    return false;

                var method = sekirariaPlayerType.GetMethod("HasParrySword", BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    object[] parameters = new object[] { null };
                    bool result = (bool)method.Invoke(sPlayer, parameters);
                    if (result && parameters[0] != null)
                    {
                        if (parameters[0] is ModItem modItem)
                        {
                            parrySwordItem = modItem.Item;
                        }
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error checking HasParrySword: " + e.Message);
            }

            return false;
        }

        private static Type postureNPCType;
        private static MethodInfo getGlobalNPCMethod;

        public static object GetPostureNPC(NPC npc)
        {
            if (!initialized) Initialize();
            if (!SekirariaLoaded) return null;

            try
            {
                if (postureNPCType == null)
                {
                    postureNPCType = sekirariaMod.Code.GetType("Sekiraria.Common.NPCs.PostureNPC");
                }
                if (postureNPCType == null) return null;

                if (getGlobalNPCMethod == null)
                {
                    getGlobalNPCMethod = typeof(NPC).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == "GetGlobalNPC" && m.IsGenericMethod && m.GetParameters().Length == 0);
                }

                if (getGlobalNPCMethod != null)
                {
                    MethodInfo genericMethod = getGlobalNPCMethod.MakeGenericMethod(postureNPCType);
                    return genericMethod.Invoke(npc, null);
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error getting PostureNPC: " + e.Message);
            }
            return null;
        }

        public static bool IsPostureBroken(NPC npc)
        {
            if (!SekirariaLoaded) return false;
            object postureNPC = GetPostureNPC(npc);
            if (postureNPC == null) return false;

            try
            {
                var findMasterMethod = postureNPC.GetType().GetMethod("FindMasterNPC", BindingFlags.Instance | BindingFlags.Public);
                if (findMasterMethod != null)
                {
                    NPC master = findMasterMethod.Invoke(postureNPC, new object[] { npc }) as NPC;
                    if (master != null && master.active)
                    {
                        object masterPostureNPC = GetPostureNPC(master);
                        if (masterPostureNPC != null)
                        {
                            var isBrokenField = masterPostureNPC.GetType().GetField("isBroken", BindingFlags.Instance | BindingFlags.Public);
                            if (isBrokenField != null)
                            {
                                return (bool)isBrokenField.GetValue(masterPostureNPC);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error checking IsPostureBroken: " + e.Message);
            }
            return false;
        }

        public static void ResetPostureStun(NPC npc)
        {
            if (!SekirariaLoaded) return;
            object postureNPC = GetPostureNPC(npc);
            if (postureNPC == null) return;

            try
            {
                var findMasterMethod = postureNPC.GetType().GetMethod("FindMasterNPC", BindingFlags.Instance | BindingFlags.Public);
                if (findMasterMethod != null)
                {
                    NPC master = findMasterMethod.Invoke(postureNPC, new object[] { npc }) as NPC;
                    if (master != null && master.active)
                    {
                        object masterPostureNPC = GetPostureNPC(master);
                        if (masterPostureNPC != null)
                        {
                            var isBrokenField = masterPostureNPC.GetType().GetField("isBroken", BindingFlags.Instance | BindingFlags.Public);
                            var currentPostureField = masterPostureNPC.GetType().GetField("currentPosture", BindingFlags.Instance | BindingFlags.Public);
                            var brokenTimerField = masterPostureNPC.GetType().GetField("brokenTimer", BindingFlags.Instance | BindingFlags.Public);

                            if (isBrokenField != null) isBrokenField.SetValue(masterPostureNPC, false);
                            if (currentPostureField != null) currentPostureField.SetValue(masterPostureNPC, 0f);
                            if (brokenTimerField != null) brokenTimerField.SetValue(masterPostureNPC, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error in ResetPostureStun: " + e.Message);
            }
        }

        public static void PerformExecutionOnNPC(Player player, NPC npc)
        {
            if (!initialized) Initialize();
            if (!SekirariaLoaded || sekirariaMod == null) return;
            try
            {
                Type execSystemType = sekirariaMod.Code.GetType("Sekiraria.Common.Systems.ExecutionSystem");
                if (execSystemType != null)
                {
                    var method = execSystemType.GetMethod("PerformExecutionSlash", BindingFlags.Static | BindingFlags.Public);
                    if (method != null)
                    {
                        method.Invoke(null, new object[] { player, npc });
                    }
                }

                // Reset posture stun on the target master NPC
                ResetPostureStun(npc);
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error performing execution on NPC: " + e.Message);
            }
        }

        public static bool IsPlayerExecutingNPC(Player player, NPC npc)
        {
            if (!initialized) Initialize();
            if (!SekirariaLoaded || sekirariaPlayerType == null) return false;
            try
            {
                object sPlayer = GetSekirariaPlayer(player);
                if (sPlayer == null) return false;

                var timerField = sekirariaPlayerType.GetField("executionStrikeTimer", BindingFlags.Instance | BindingFlags.Public);
                var targetField = sekirariaPlayerType.GetField("executionStrikeTargetIndex", BindingFlags.Instance | BindingFlags.Public);

                if (timerField != null && targetField != null)
                {
                    int timer = (int)timerField.GetValue(sPlayer);
                    int targetIndex = (int)targetField.GetValue(sPlayer);

                    if (timer > 0 && targetIndex >= 0 && targetIndex < Main.maxNPCs)
                    {
                        NPC targetNPC = Main.npc[targetIndex];
                        if (targetNPC == npc || targetNPC.realLife == npc.whoAmI || npc.realLife == targetNPC.whoAmI)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error checking IsPlayerExecutingNPC: " + e.Message);
            }
            return false;
        }
    }
}
