using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System.Linq;

namespace Stataria
{
    public class CalamitySupportHelper : ModSystem
    {
        #region Mod Detection Variables
        public static bool CalamityLoaded { get; private set; }
        private static Mod calamityMod;
        private static Type calamityPlayerType;
        private static Type rogueClassType;
        private static bool initialized;
        private static MethodInfo _getModPlayerMethod;
        private static Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        #endregion

        #region Calamity Access Flags
        public static bool FoundRogueClass { get; private set; }
        public static bool FoundRogueStealth { get; private set; }
        public static bool FoundRogueStealthMax { get; private set; }
        public static bool FoundAddMaxStealthCall { get; private set; }
        public static bool FoundStealthGenStandstill { get; private set; }
        public static bool FoundStealthGenMoving { get; private set; }
        public static bool FoundStealthDamage { get; private set; }
        public static bool FoundRogueVelocity { get; private set; }
        public static bool FoundRage { get; private set; }
        public static bool FoundRageMax { get; private set; }
        public static bool FoundRageDuration { get; private set; }
        public static bool FoundRageDamage { get; private set; }
        public static bool FoundAdrenaline { get; private set; }
        public static bool FoundAdrenalineMax { get; private set; }
        public static bool FoundAdrenalineDuration { get; private set; }
        public static bool FoundWearingRogueArmor { get; private set; }
        public static bool FoundStealthStrikeThisFrame { get; private set; }
        public static bool FoundStealthStrikeHalfCost { get; private set; }
        public static bool FoundStealthStrike75Cost { get; private set; }
        public static bool FoundStealthStrike90Cost { get; private set; }
        #endregion

        #region Infinite Effect Toggles
        public static bool InfiniteRageEnabled { get; private set; }
        public static bool InfiniteAdrenalineEnabled { get; private set; }
        #endregion

        #region Load & Initialize
        public override void Load()
        {
            initialized = false;
            ResetAccessFlags();

            CalamityLoaded = ModLoader.HasMod("CalamityMod");

            if (CalamityLoaded)
            {
                calamityMod = ModLoader.GetMod("CalamityMod");
            }
        }

        public override void Unload()
        {
            calamityMod = null;
            calamityPlayerType = null;
            rogueClassType = null;
            initialized = false;
            _getModPlayerMethod = null;
            _fieldCache.Clear();
            ResetAccessFlags();

            InfiniteRageEnabled = false;
            InfiniteAdrenalineEnabled = false;
        }

        private static void ResetAccessFlags()
        {
            FoundRogueClass = false;
            FoundRogueStealth = false;
            FoundRogueStealthMax = false;
            FoundStealthGenStandstill = false;
            FoundStealthGenMoving = false;
            FoundStealthDamage = false;
            FoundRogueVelocity = false;
            FoundRage = false;
            FoundRageMax = false;
            FoundRageDuration = false;
            FoundRageDamage = false;
            FoundAdrenaline = false;
            FoundAdrenalineMax = false;
            FoundAdrenalineDuration = false;
            FoundAddMaxStealthCall = false;
            FoundWearingRogueArmor = false;
            FoundStealthStrikeThisFrame = false;
            FoundStealthStrikeHalfCost = false;
            FoundStealthStrike75Cost = false;
            FoundStealthStrike90Cost = false;
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            try {
                CalamityLoaded = ModLoader.HasMod("CalamityMod");
                if (CalamityLoaded)
                {
                    calamityMod = ModLoader.GetMod("CalamityMod");

                    calamityPlayerType = calamityMod.Code.GetType("CalamityMod.CalPlayer.CalamityPlayer");

                    FoundRogueClass = true;
                }

                initialized = true;
            }
            catch (Exception) {
                CalamityLoaded = false;
            }
        }

        private static bool EnsureCalamityTypes()
        {
            if (!CalamityLoaded || calamityMod == null)
                return false;

            if (calamityPlayerType != null)
                return true;

            try {
                calamityPlayerType = calamityMod.Code.GetType("CalamityMod.CalPlayer.CalamityPlayer");
                return calamityPlayerType != null;
            }
            catch {
            }

            return false;
        }

        private static void ValidateCalamityFieldAccess()
        {
            try
            {
                ResetAccessFlags();

                if (!EnsureCalamityTypes())
                    return;

                FoundRogueClass = true;

                // Check fields directly from the Type instead of requiring a dummy player
                bool CheckField(string name) => calamityPlayerType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;

                try { FoundRogueStealth = CheckField("rogueStealth"); } catch { }
                try { FoundRogueStealthMax = CheckField("rogueStealthMax"); } catch { }
                try { FoundStealthGenStandstill = CheckField("stealthGenStandstill"); } catch { }
                try { FoundStealthGenMoving = CheckField("stealthGenMoving"); } catch { }
                try { FoundStealthDamage = CheckField("stealthDamage"); } catch { }
                try { FoundRogueVelocity = CheckField("rogueVelocity"); } catch { }
                try { FoundRage = CheckField("rage"); } catch { }
                try { FoundRageMax = CheckField("rageMax"); } catch { }
                try { FoundRageDuration = CheckField("RageDuration"); } catch { }
                try { FoundRageDamage = CheckField("RageDamageBoost"); } catch { }
                try { FoundAdrenaline = CheckField("adrenaline"); } catch { }
                try { FoundAdrenalineMax = CheckField("adrenalineMax"); } catch { }
                try { FoundAdrenalineDuration = CheckField("AdrenalineDuration"); } catch { }
                try { FoundWearingRogueArmor = CheckField("wearingRogueArmor"); } catch { }
                try { FoundStealthStrikeThisFrame = CheckField("stealthStrikeThisFrame"); } catch { }
                try { FoundStealthStrikeHalfCost = CheckField("stealthStrikeHalfCost"); } catch { }
                try { FoundStealthStrike75Cost = CheckField("stealthStrike75Cost"); } catch { }
                try { FoundStealthStrike90Cost = CheckField("stealthStrike90Cost"); } catch { }
            }
            catch (Exception)
            {
            }
        }

        public static void RunFieldValidation()
        {
            ValidateCalamityFieldAccess();
        }
        #endregion

        #region Calamity Field Access Methods
        public static object GetCalamityPlayer(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || calamityPlayerType == null)
                return null;

            try
            {
                if (_getModPlayerMethod == null)
                {
                    _getModPlayerMethod = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == "GetModPlayer" && m.IsGenericMethod && m.GetParameters().Length == 0);
                }

                if (_getModPlayerMethod != null)
                {
                    MethodInfo genericMethod = _getModPlayerMethod.MakeGenericMethod(calamityPlayerType);
                    return genericMethod.Invoke(player, null);
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<Stataria>().Logger.Error("Error getting CalamityPlayer: " + e.Message);
            }

            return null;
        }

        private static FieldInfo GetFieldInfoCached(string fieldName)
        {
            if (_fieldCache.TryGetValue(fieldName, out FieldInfo field))
                return field;

            if (calamityPlayerType != null)
            {
                field = calamityPlayerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    _fieldCache[fieldName] = field;
                }
            }
            return field;
        }

        public static T GetFieldValue<T>(Player player, string fieldName, T defaultValue = default)
        {
            try
            {
                object calPlayer = GetCalamityPlayer(player);
                if (calPlayer == null)
                    return defaultValue;

                FieldInfo field = GetFieldInfoCached(fieldName);

                if (field == null)
                    return defaultValue;

                object value = field.GetValue(calPlayer);
                if (value == null)
                    return defaultValue;

                return (T)value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool SetFieldValue<T>(Player player, string fieldName, T value)
        {
            try
            {
                object calPlayer = GetCalamityPlayer(player);
                if (calPlayer == null)
                    return false;

                FieldInfo field = GetFieldInfoCached(fieldName);

                if (field == null)
                    return false;

                field.SetValue(calPlayer, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Stealth System Access
        public static float GetRogueStealth(Player player)
        {
            if (!initialized)
                Initialize();

            if (!EnsureCalamityTypes())
                return 0f;

            try {
                object calPlayer = GetCalamityPlayer(player);
                if (calPlayer == null)
                    return 0f;

                FieldInfo field = calamityPlayerType.GetField("rogueStealth",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field == null)
                    return 0f;

                FoundRogueStealth = true;
                return (float)field.GetValue(calPlayer);
            }
            catch {
                return 0f;
            }
        }

        public static float GetRogueStealthMax(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRogueStealthMax)
                return 0f;

            return GetFieldValue<float>(player, "rogueStealthMax");
        }

        public static object CallAddMaxStealth(Player player, float amount)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || calamityMod == null || amount == 0)
                return null;

            try
            {
                var result = calamityMod.Call("AddMaxStealth", player, amount);
                FoundAddMaxStealthCall = true;
                return result;
            }
            catch (Exception e)
            {
                if (!FoundAddMaxStealthCall)
                {
                    ModContent.GetInstance<Stataria>()?.Logger.Warn($"Error calling AddMaxStealth: {e.Message}. This warning will not repeat.");
                    FoundAddMaxStealthCall = false;
                }
                return null;
            }
        }

        public static float GetStealthGenStandstill(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundStealthGenStandstill)
                return 1f;

            return GetFieldValue<float>(player, "stealthGenStandstill", 1f);
        }

        public static float GetStealthGenMoving(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundStealthGenMoving)
                return 1f;

            return GetFieldValue<float>(player, "stealthGenMoving", 1f);
        }

        public static float GetStealthDamage(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundStealthDamage)
                return 0f;

            return GetFieldValue<float>(player, "stealthDamage");
        }

        public static float GetRogueVelocity(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRogueVelocity)
                return 1f;

            return GetFieldValue<float>(player, "rogueVelocity", 1f);
        }

        public static bool SetRogueStealth(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRogueStealth)
                return false;

            return SetFieldValue(player, "rogueStealth", value);
        }

        public static bool SetStealthGenStandstill(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundStealthGenStandstill)
                return false;

            return SetFieldValue(player, "stealthGenStandstill", value);
        }

        public static bool SetStealthGenMoving(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundStealthGenMoving)
                return false;

            return SetFieldValue(player, "stealthGenMoving", value);
        }

        public static bool SetStealthDamage(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundStealthDamage)
                return false;

            return SetFieldValue(player, "stealthDamage", value);
        }

        public static bool SetRogueVelocity(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRogueVelocity)
                return false;

            return SetFieldValue(player, "rogueVelocity", value);
        }
        #endregion

        #region Rage System Access
        public static float GetRage(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRage)
                return 0f;

            return GetFieldValue<float>(player, "rage");
        }

        public static float GetRageMax(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRageMax)
                return 100f;

            return GetFieldValue<float>(player, "rageMax", 100f);
        }

        public static int GetRageDuration(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRageDuration)
                return 300;

            return GetFieldValue<int>(player, "RageDuration", 300);
        }

        public static float GetRageDamageBoost(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRageDamage)
                return 0.5f;

            return GetFieldValue<float>(player, "RageDamageBoost", 0.5f);
        }

        public static bool SetRage(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRage)
                return false;

            return SetFieldValue(player, "rage", value);
        }

        public static bool SetRageDuration(Player player, int value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRageDuration)
                return false;

            return SetFieldValue(player, "RageDuration", value);
        }

        public static bool SetRageDamageBoost(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundRageDamage)
                return false;

            return SetFieldValue(player, "RageDamageBoost", value);
        }
        #endregion

        #region Adrenaline System Access
        public static float GetAdrenaline(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundAdrenaline)
                return 0f;

            return GetFieldValue<float>(player, "adrenaline");
        }

        public static float GetAdrenalineMax(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundAdrenalineMax)
                return 100f;

            return GetFieldValue<float>(player, "adrenalineMax", 100f);
        }

        public static int GetAdrenalineDuration(Player player)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundAdrenalineDuration)
                return 300;

            return GetFieldValue<int>(player, "AdrenalineDuration", 300);
        }

        public static bool SetAdrenaline(Player player, float value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundAdrenaline)
                return false;

            return SetFieldValue(player, "adrenaline", value);
        }

        public static bool SetAdrenalineDuration(Player player, int value)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || !FoundAdrenalineDuration)
                return false;

            return SetFieldValue(player, "AdrenalineDuration", value);
        }
        #endregion

        #region Utility Methods
        public static bool IsRogueWeapon(Item item)
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded || item == null || item.damage <= 0 || item.accessory)
                return false;

            try
            {
                if (rogueClassType == null)
                {
                    rogueClassType = calamityMod.Code.GetType("CalamityMod.RogueDamageClass");
                }

                if (item.ModItem != null && item.ModItem.GetType().Namespace?.Contains("Rogue") == true)
                {
                    return true;
                }

                if (item.DamageType != null)
                {
                    string damageTypeName = item.DamageType.GetType().Name;
                    if (damageTypeName.Contains("Rogue") || damageTypeName.Contains("Throwing"))
                        return true;

                    string damageTypeString = item.DamageType.ToString();
                    if (damageTypeString.Contains("Rogue") || damageTypeString.Contains("Throwing"))
                        return true;
                }

                if (calamityMod != null && item.ModItem?.Mod == calamityMod)
                {
                    if (rogueClassType != null)
                    {

                        Type calGlobalItem = calamityMod.Code.GetType("CalamityMod.Items.CalamityGlobalItem");
                        if (calGlobalItem != null)
                        {
                            var methods = calGlobalItem.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                .Where(m => m.Name.Contains("Counts") || m.Name.Contains("Rogue"));

                            foreach (var method in methods)
                            {
                                if (method.GetParameters().Length == 1 &&
                                    method.GetParameters()[0].ParameterType == typeof(Item))
                                {
                                    try
                                    {
                                        object result = method.Invoke(null, new object[] { item });
                                        if (result is bool boolResult && boolResult)
                                            return true;
                                    }
                                    catch {  }
                                }
                            }
                        }
                    }

                    Type modItemType = item.ModItem.GetType();
                    MethodInfo tooltipMethod = modItemType.GetMethod("ModifyTooltips",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (tooltipMethod != null)
                    {
                        try
                        {
                            PropertyInfo nameProperty = item.ModItem.GetType().GetProperty("DisplayName");
                            if (nameProperty != null)
                            {
                                object localizedTextObj = nameProperty.GetValue(item.ModItem);
                                string displayName = localizedTextObj?.ToString();
                                if (displayName != null &&
                                    (displayName.ToLower().Contains("rogue") ||
                                    displayName.ToLower().Contains("throwing")))
                                    return true;
                            }
                        }
                        catch {  }
                    }

                    string className = modItemType.FullName;
                    if (className != null &&
                        (className.Contains(".Rogue.") ||
                        className.Contains(".Throwing.")))
                        return true;

                    string shortClassName = modItemType.Name;
                    if (shortClassName.Contains("Rogue") ||
                        shortClassName.Contains("Throwing") ||
                        shortClassName.Contains("Stealth"))
                        return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsCalamityIntegrationWorking()
        {
            if (!initialized)
                Initialize();

            if (!CalamityLoaded)
                return false;

            bool rogueFieldsOk = FoundRogueClass && FoundRogueStealth && FoundRogueStealthMax &&
                                FoundStealthGenStandstill && FoundStealthGenMoving &&
                                FoundStealthDamage && FoundRogueVelocity;

            bool rageFieldsOk = FoundRage && FoundRageMax && FoundRageDuration && FoundRageDamage;

            bool adrenalineFieldsOk = FoundAdrenaline && FoundAdrenalineMax && FoundAdrenalineDuration;

            return rogueFieldsOk && rageFieldsOk && adrenalineFieldsOk;
        }

        public static void ToggleInfiniteRage()
        {
            InfiniteRageEnabled = !InfiniteRageEnabled;
        }

        public static void ToggleInfiniteAdrenaline()
        {
            InfiniteAdrenalineEnabled = !InfiniteAdrenalineEnabled;
        }
        #endregion
    }
}