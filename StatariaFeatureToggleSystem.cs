using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;

namespace Stataria
{
    public class StatariaFeatureToggleSystem : ModSystem
    {
        private static bool lastRebirthEnabled;
        private static bool lastCalamityIntegrationState;
        private static bool lastThoriumIntegrationState;
        private static int lastStatPointsPerLevel;
        private static float lastRebirthPointsMultiplier;
        private static int lastRebirthLevelRequirement;
        private static int lastAdditionalLevelRequirementPerRebirth;
        private static bool lastBonusPointsForExcessLevels;
        private static float lastExcessLevelPointMultiplier;
        private static bool lastRebirthBonusStatPoints;
        private static float lastRebirthStatPointsMultiplier;

        public override void Load()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            lastRebirthEnabled = config.rebirthSystem.EnableRebirthSystem;
            lastCalamityIntegrationState = false;
            lastThoriumIntegrationState = false;
            lastStatPointsPerLevel = config.generalBalance.StatPointsPerLevel;
            lastRebirthPointsMultiplier = config.rebirthSystem.RebirthPointsMultiplier;
            lastRebirthLevelRequirement = config.rebirthSystem.RebirthLevelRequirement;
            lastAdditionalLevelRequirementPerRebirth = config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
            lastBonusPointsForExcessLevels = config.rebirthSystem.BonusPointsForExcessLevels;
            lastExcessLevelPointMultiplier = config.rebirthSystem.ExcessLevelPointMultiplier;
            lastRebirthBonusStatPoints = config.rebirthSystem.EnableRebirthBonusStatPoints;
            lastRebirthStatPointsMultiplier = config.rebirthSystem.RebirthStatPointsMultiplier;
        }

        public override void OnModLoad()
        {
            CalamitySupportHelper.Initialize();
            ThoriumSupportHelper.Initialize();
            GenericModSupportHelper.Initialize();
        }

        public override void OnWorldLoad()
        {
            CalamitySupportHelper.Initialize();
            ThoriumSupportHelper.Initialize();
            GenericModSupportHelper.Initialize();

            if (CalamitySupportHelper.CalamityLoaded)
            {
                CalamitySupportHelper.RunFieldValidation();
            }

            if (ThoriumSupportHelper.ThoriumLoaded)
            {
                ThoriumSupportHelper.RunFieldValidation();
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            lastCalamityIntegrationState = !(config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded);
            lastThoriumIntegrationState = !(config.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded);
        }

        public override void PostUpdateEverything()
        {
            var cfg = ModContent.GetInstance<StatariaConfig>();

            bool statPointsConfigChanged = cfg.generalBalance.StatPointsPerLevel != lastStatPointsPerLevel;

            if (statPointsConfigChanged)
            {
                if (cfg.generalBalance.EnableStatPointRecalculation)
                {
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.StatPointsRecalculating"));
                    lastStatPointsPerLevel = cfg.generalBalance.StatPointsPerLevel;

                    foreach (Player player in Main.player)
                    {
                        if (player.active)
                        {
                            var rpg = player.GetModPlayer<RPGPlayer>();
                            rpg.RecalculateStatPoints();
                        }
                    }
                }
                else
                {
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.StatPointsRecalculationDisabled"));
                    lastStatPointsPerLevel = cfg.generalBalance.StatPointsPerLevel;
                }
            }

            if (cfg.rebirthSystem.EnableRebirthSystem != lastRebirthEnabled)
            {
                StatariaUI.Panel?.ReInitializePanel();
                lastRebirthEnabled = cfg.rebirthSystem.EnableRebirthSystem;
            }

            bool rebirthConfigChanged =
                cfg.rebirthSystem.RebirthPointsMultiplier != lastRebirthPointsMultiplier ||
                cfg.rebirthSystem.RebirthLevelRequirement != lastRebirthLevelRequirement ||
                cfg.rebirthSystem.AdditionalLevelRequirementPerRebirth != lastAdditionalLevelRequirementPerRebirth ||
                cfg.rebirthSystem.BonusPointsForExcessLevels != lastBonusPointsForExcessLevels ||
                cfg.rebirthSystem.ExcessLevelPointMultiplier != lastExcessLevelPointMultiplier;

            if (rebirthConfigChanged)
            {
                if (cfg.rebirthSystem.EnableRebirthPointRecalculation)
                {
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.RebirthConfigRecalculating"));
                    lastRebirthPointsMultiplier = cfg.rebirthSystem.RebirthPointsMultiplier;
                    lastRebirthLevelRequirement = cfg.rebirthSystem.RebirthLevelRequirement;
                    lastAdditionalLevelRequirementPerRebirth = cfg.rebirthSystem.AdditionalLevelRequirementPerRebirth;
                    lastBonusPointsForExcessLevels = cfg.rebirthSystem.BonusPointsForExcessLevels;
                    lastExcessLevelPointMultiplier = cfg.rebirthSystem.ExcessLevelPointMultiplier;

                    foreach (Player player in Main.player)
                    {
                        if (player.active)
                        {
                            var rpg = player.GetModPlayer<RPGPlayer>();
                            rpg.RecalculateRebirthPoints();
                        }
                    }
                }
                else
                {
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.RebirthConfigRecalculationDisabled"));
                    lastRebirthPointsMultiplier = cfg.rebirthSystem.RebirthPointsMultiplier;
                    lastRebirthLevelRequirement = cfg.rebirthSystem.RebirthLevelRequirement;
                    lastAdditionalLevelRequirementPerRebirth = cfg.rebirthSystem.AdditionalLevelRequirementPerRebirth;
                    lastBonusPointsForExcessLevels = cfg.rebirthSystem.BonusPointsForExcessLevels;
                    lastExcessLevelPointMultiplier = cfg.rebirthSystem.ExcessLevelPointMultiplier;
                }
            }

            bool rebirthStatPointConfigChanged =
                cfg.rebirthSystem.EnableRebirthBonusStatPoints != lastRebirthBonusStatPoints ||
                cfg.rebirthSystem.RebirthStatPointsMultiplier != lastRebirthStatPointsMultiplier;

            if (rebirthStatPointConfigChanged)
            {
                if (cfg.rebirthSystem.EnableRebirthStatPointRecalculation)
                {
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.RebirthStatPointRecalculating"));
                    lastRebirthBonusStatPoints = cfg.rebirthSystem.EnableRebirthBonusStatPoints;
                    lastRebirthStatPointsMultiplier = cfg.rebirthSystem.RebirthStatPointsMultiplier;

                    foreach (Player player in Main.player)
                    {
                        if (player.active)
                        {
                            var rpg = player.GetModPlayer<RPGPlayer>();
                            rpg.RecalculateRebirthStatPoints();
                        }
                    }
                }
                else
                {
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.RebirthStatPointRecalculationDisabled"));
                    lastRebirthBonusStatPoints = cfg.rebirthSystem.EnableRebirthBonusStatPoints;
                    lastRebirthStatPointsMultiplier = cfg.rebirthSystem.RebirthStatPointsMultiplier;
                }
            }

            bool currentCalamityState = cfg.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded;
            if (currentCalamityState != lastCalamityIntegrationState)
            {
                if (StatariaUI.Panel != null)
                {
                    try
                    {
                        StatariaUI.Panel.ReInitializePanel();
                    }
                    catch (System.Exception e)
                    {
                        ModContent.GetInstance<Stataria>().Logger.Error(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.ErrorCalamityPanel", e.Message));
                    }
                }
                lastCalamityIntegrationState = currentCalamityState;
            }

            bool currentThoriumState = cfg.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded;
            if (currentThoriumState != lastThoriumIntegrationState)
            {
                if (StatariaUI.Panel != null)
                {
                    try
                    {
                        StatariaUI.Panel.ReInitializePanel();
                    }
                    catch (System.Exception e)
                    {
                        ModContent.GetInstance<Stataria>().Logger.Error(Language.GetTextValue("Mods.Stataria.Logging.StatariaFeatureToggleSystem.ErrorThoriumPanel", e.Message));
                    }
                }
                lastThoriumIntegrationState = currentThoriumState;
            }
        }
    }
}