using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Stataria
{
    public class ModDefinition
    {
        public string ModName { get; set; }
        public string StatName { get; set; }
        public string DisplayName { get; set; }
        public List<string> DamageClassNames { get; set; } = new List<string>();
        public List<string> TooltipKeywords { get; set; } = new List<string>();
        public List<string> NamespacePatterns { get; set; } = new List<string>();
        public List<string> ItemNamePatterns { get; set; } = new List<string>();
        public bool IsLegacyOnly { get; set; } = false;
        public Func<bool> CustomDetection { get; set; }

        public bool IsModLoaded => ModLoader.HasMod(ModName);

        public bool ShouldShowStat()
        {
            if (!IsModLoaded) return false;
            if (IsLegacyOnly) return HasWorkingImplementation();
            return true;
        }

        private bool HasWorkingImplementation()
        {
            if (!IsModLoaded) return false;

            Mod mod = ModLoader.GetMod(ModName);
            if (mod == null) return false;

            foreach (string className in DamageClassNames)
            {
                if (mod.TryFind(className, out DamageClass _))
                    return true;
            }

            return TooltipKeywords.Count > 0 || NamespacePatterns.Count > 0;
        }
    }

    public class GenericModSupportHelper : ModSystem
    {
        private static List<ModDefinition> supportedMods = new List<ModDefinition>();
        private static Dictionary<string, Mod> modCache = new Dictionary<string, Mod>();
        private static bool initialized = false;

        public override void Load()
        {
            initialized = false;
            supportedMods.Clear();
            modCache.Clear();
        }

        public override void Unload()
        {
            supportedMods.Clear();
            modCache.Clear();
            initialized = false;
        }

        public static void Initialize()
        {
            if (initialized) return;

            supportedMods.Clear();
            InitializeModDefinitions();
            CacheLoadedMods();
            initialized = true;
        }

        private static void InitializeModDefinitions()
        {
            supportedMods.Add(new ModDefinition
            {
                ModName = "VitalityMod",
                StatName = "BLH",
                DisplayName = "Blood Hunter",
                DamageClassNames = { "MalevolentDamage", "BloodHunterDamage" },
                TooltipKeywords = { "malevolent damage" },
                NamespacePatterns = { ".BloodHunter.", ".Malevolent." },
                ItemNamePatterns = { "BloodHunter", "Malevolent" }
            });

            supportedMods.Add(new ModDefinition
            {
                ModName = "MetroidMod",
                StatName = "HNT",
                DisplayName = "Hunter",
                DamageClassNames = { "HunterDamage", "HunterDamageClass" },
                TooltipKeywords = { "hunter damage" },
                NamespacePatterns = { ".Hunter.", ".Metroid." },
                ItemNamePatterns = { "Hunter" }
            });

            supportedMods.Add(new ModDefinition
            {
                ModName = "OrchidMod",
                StatName = "GMB",
                DisplayName = "Gambler",
                DamageClassNames = { "GamblingDamage", "GamblerDamage" },
                TooltipKeywords = { "gambling damage" },
                NamespacePatterns = { ".Gambler.", ".Gambling." },
                ItemNamePatterns = { "Gambler", "Gambling" }
            });

            supportedMods.Add(new ModDefinition
            {
                ModName = "OrchidMod",
                StatName = "SHM",
                DisplayName = "Shaman",
                DamageClassNames = { "ShamanicDamage", "ShamanDamage" },
                TooltipKeywords = { "shamanic damage" },
                NamespacePatterns = { ".Shaman.", ".Shamanic." },
                ItemNamePatterns = { "Shaman", "Shamanic" },
                IsLegacyOnly = true
            });

            supportedMods.Add(new ModDefinition
            {
                ModName = "ThoriumMod",
                StatName = "THR",
                DisplayName = "Thrower",
                DamageClassNames = { "ThrowingDamage", "ThrowerDamage" },
                TooltipKeywords = { "throwing damage" },
                NamespacePatterns = { ".Throwing.", ".Thrower." },
                ItemNamePatterns = { "Throwing", "Thrower" }
            });
        }

        private static void CacheLoadedMods()
        {
            modCache.Clear();
            foreach (var modDef in supportedMods)
            {
                if (modDef.IsModLoaded && !modCache.ContainsKey(modDef.ModName))
                {
                    modCache[modDef.ModName] = ModLoader.GetMod(modDef.ModName);
                }
            }
        }

        public static List<ModDefinition> GetVisibleMods()
        {
            if (!initialized) Initialize();
            return supportedMods.Where(mod => mod.ShouldShowStat()).ToList();
        }

        public static bool IsWeaponOfType(Item item, ModDefinition modDef)
        {
            if (!initialized) Initialize();

            if (item == null || item.damage <= 0 || item.accessory)
                return false;

            if (!modDef.IsModLoaded)
                return false;

            try
            {
                if (item.DamageType != null && modDef.DamageClassNames.Count > 0)
                {
                    string damageTypeName = item.DamageType.GetType().Name;
                    string damageTypeString = item.DamageType.ToString();

                    foreach (string className in modDef.DamageClassNames)
                    {
                        if (damageTypeName.Contains(className) || damageTypeString.Contains(className))
                            return true;
                    }
                }

                if (item.ModItem?.Mod?.Name == modDef.ModName)
                {
                    if (modDef.NamespacePatterns.Count > 0)
                    {
                        string itemNamespace = item.ModItem.GetType().Namespace ?? "";
                        foreach (string pattern in modDef.NamespacePatterns)
                        {
                            if (itemNamespace.Contains(pattern))
                                return true;
                        }
                    }

                    if (modDef.ItemNamePatterns.Count > 0)
                    {
                        string className = item.ModItem.GetType().Name;
                        foreach (string pattern in modDef.ItemNamePatterns)
                        {
                            if (className.Contains(pattern))
                                return true;
                        }
                    }

                    if (modDef.TooltipKeywords.Count > 0)
                    {
                        return CheckTooltipKeywords(item, modDef.TooltipKeywords);
                    }
                }

                if (modDef.CustomDetection != null)
                {
                    return modDef.CustomDetection();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckTooltipKeywords(Item item, List<string> keywords)
        {
            try
            {
                string itemName = item.Name?.ToLower() ?? "";

                foreach (string keyword in keywords)
                {
                    if (itemName.Contains(keyword.ToLower()))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static ModDefinition GetModDefinitionForWeapon(Item item)
        {
            if (!initialized) Initialize();

            foreach (var modDef in supportedMods)
            {
                if (IsWeaponOfType(item, modDef))
                    return modDef;
            }
            return null;
        }

        public static bool IsGenericModIntegrationWorking()
        {
            if (!initialized) Initialize();
            return supportedMods.Any(mod => mod.IsModLoaded);
        }

        public static void RegisterModSupport(ModDefinition modDefinition)
        {
            if (!initialized) Initialize();

            if (!supportedMods.Any(m => m.ModName == modDefinition.ModName && m.StatName == modDefinition.StatName))
            {
                supportedMods.Add(modDefinition);

                if (modDefinition.IsModLoaded && !modCache.ContainsKey(modDefinition.ModName))
                {
                    modCache[modDefinition.ModName] = ModLoader.GetMod(modDefinition.ModName);
                }
            }
        }
    }
}