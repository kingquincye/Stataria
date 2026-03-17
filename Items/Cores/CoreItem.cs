using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Stataria;
using Terraria.Localization;

namespace Stataria.Items.Cores
{
    public abstract class CoreItem : ModItem
    {
        public abstract CoreType CoreType { get; }
        public abstract int Tier { get; }
        public abstract float EffectValue { get; }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 999;
            Item.value = GetCoreValue();
            Item.rare = GetCoreRarity();
            Item.useStyle = ItemUseStyleID.None;
        }

        private int GetCoreValue()
        {
            return Tier switch
            {
                1 => Item.buyPrice(0, 0, 50, 0),
                2 => Item.buyPrice(0, 10, 0, 0),
                3 => Item.buyPrice(1, 0, 0, 0),
                4 => Item.buyPrice(2, 0, 0, 0),
                _ => 0
            };
        }

        private int GetCoreRarity()
        {
            return Tier switch
            {
                1 => ItemRarityID.White,
                2 => ItemRarityID.Green,
                3 => ItemRarityID.Orange,
                4 => ItemRarityID.LightRed,
                _ => ItemRarityID.White
            };
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string effect;
            string usage;
            switch (CoreType)
            {
                case CoreType.Power:
                    effect = Language.GetTextValue("Mods.Stataria.CoreItem.EffectPower", EffectValue);
                    usage = Language.GetTextValue("Mods.Stataria.CoreItem.UsageWeapon");
                    break;
                case CoreType.Force:
                    effect = Language.GetTextValue("Mods.Stataria.CoreItem.EffectForce", EffectValue);
                    usage = Language.GetTextValue("Mods.Stataria.CoreItem.UsageWeapon");
                    break;
                case CoreType.Precision:
                    effect = Language.GetTextValue("Mods.Stataria.CoreItem.EffectPrecision", EffectValue);
                    usage = Language.GetTextValue("Mods.Stataria.CoreItem.UsageWeapon");
                    break;
                case CoreType.Defense:
                    effect = Language.GetTextValue("Mods.Stataria.CoreItem.EffectDefense", EffectValue);
                    usage = Language.GetTextValue("Mods.Stataria.CoreItem.UsageArmor");
                    break;
                case CoreType.Vitality:
                    effect = Language.GetTextValue("Mods.Stataria.CoreItem.EffectVitality", EffectValue);
                    usage = Language.GetTextValue("Mods.Stataria.CoreItem.UsageArmor");
                    break;
                case CoreType.Evasion:
                    effect = Language.GetTextValue("Mods.Stataria.CoreItem.EffectEvasion", EffectValue);
                    usage = Language.GetTextValue("Mods.Stataria.CoreItem.UsageArmor");
                    break;
                default:
                    effect = "";
                    usage = "";
                    break;
            }

            tooltips.Add(new TooltipLine(Mod, "CoreEffect", effect)
            {
                OverrideColor = Color.LightBlue
            });

            tooltips.Add(new TooltipLine(Mod, "CoreUsage", usage)
            {
                OverrideColor = Color.Gray
            });
        }
    }

    public class CoreOfPowerT1 : CoreItem
    {
        public override CoreType CoreType => CoreType.Power;
        public override int Tier => 1;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PowerT1Effect;
    }

    public class CoreOfPowerT2 : CoreItem
    {
        public override CoreType CoreType => CoreType.Power;
        public override int Tier => 2;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PowerT2Effect;
    }

    public class CoreOfPowerT3 : CoreItem
    {
        public override CoreType CoreType => CoreType.Power;
        public override int Tier => 3;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PowerT3Effect;
    }

    public class CoreOfPowerT4 : CoreItem
    {
        public override CoreType CoreType => CoreType.Power;
        public override int Tier => 4;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PowerT4Effect;
    }

    public class CoreOfForceT1 : CoreItem
    {
        public override CoreType CoreType => CoreType.Force;
        public override int Tier => 1;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.ForceT1Effect;
    }

    public class CoreOfForceT2 : CoreItem
    {
        public override CoreType CoreType => CoreType.Force;
        public override int Tier => 2;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.ForceT2Effect;
    }

    public class CoreOfForceT3 : CoreItem
    {
        public override CoreType CoreType => CoreType.Force;
        public override int Tier => 3;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.ForceT3Effect;
    }

    public class CoreOfForceT4 : CoreItem
    {
        public override CoreType CoreType => CoreType.Force;
        public override int Tier => 4;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.ForceT4Effect;
    }

    public class CoreOfPrecisionT1 : CoreItem
    {
        public override CoreType CoreType => CoreType.Precision;
        public override int Tier => 1;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PrecisionT1Effect;
    }

    public class CoreOfPrecisionT2 : CoreItem
    {
        public override CoreType CoreType => CoreType.Precision;
        public override int Tier => 2;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PrecisionT2Effect;
    }

    public class CoreOfPrecisionT3 : CoreItem
    {
        public override CoreType CoreType => CoreType.Precision;
        public override int Tier => 3;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PrecisionT3Effect;
    }

    public class CoreOfPrecisionT4 : CoreItem
    {
        public override CoreType CoreType => CoreType.Precision;
        public override int Tier => 4;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.PrecisionT4Effect;
    }

    public class CoreOfDefenseT1 : CoreItem
    {
        public override CoreType CoreType => CoreType.Defense;
        public override int Tier => 1;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.DefenseT1Effect;
    }

    public class CoreOfDefenseT2 : CoreItem
    {
        public override CoreType CoreType => CoreType.Defense;
        public override int Tier => 2;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.DefenseT2Effect;
    }

    public class CoreOfDefenseT3 : CoreItem
    {
        public override CoreType CoreType => CoreType.Defense;
        public override int Tier => 3;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.DefenseT3Effect;
    }

    public class CoreOfDefenseT4 : CoreItem
    {
        public override CoreType CoreType => CoreType.Defense;
        public override int Tier => 4;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.DefenseT4Effect;
    }

    public class CoreOfVitalityT1 : CoreItem
    {
        public override CoreType CoreType => CoreType.Vitality;
        public override int Tier => 1;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.VitalityT1Effect;
    }

    public class CoreOfVitalityT2 : CoreItem
    {
        public override CoreType CoreType => CoreType.Vitality;
        public override int Tier => 2;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.VitalityT2Effect;
    }

    public class CoreOfVitalityT3 : CoreItem
    {
        public override CoreType CoreType => CoreType.Vitality;
        public override int Tier => 3;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.VitalityT3Effect;
    }

    public class CoreOfVitalityT4 : CoreItem
    {
        public override CoreType CoreType => CoreType.Vitality;
        public override int Tier => 4;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.VitalityT4Effect;
    }

    public class CoreOfEvasionT1 : CoreItem
    {
        public override CoreType CoreType => CoreType.Evasion;
        public override int Tier => 1;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.EvasionT1Effect;
    }

    public class CoreOfEvasionT2 : CoreItem
    {
        public override CoreType CoreType => CoreType.Evasion;
        public override int Tier => 2;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.EvasionT2Effect;
    }

    public class CoreOfEvasionT3 : CoreItem
    {
        public override CoreType CoreType => CoreType.Evasion;
        public override int Tier => 3;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.EvasionT3Effect;
    }

    public class CoreOfEvasionT4 : CoreItem
    {
        public override CoreType CoreType => CoreType.Evasion;
        public override int Tier => 4;
        public override float EffectValue => ModContent.GetInstance<StatariaConfig>().socketingSystem.EvasionT4Effect;
    }
}

public enum CoreType
{
    Power,
    Force,
    Precision,
    Defense,
    Vitality,
    Evasion
}

public struct SocketedCore
{
    public CoreType Type { get; set; }
    public int Tier { get; set; }
    public int Count { get; set; }

    public SocketedCore(CoreType type, int tier, int count = 1)
    {
        Type = type;
        Tier = tier;
        Count = count;
    }

    public float GetEffectValue()
    {
        var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
        return Type switch
        {
            CoreType.Power => Tier switch
            {
                1 => config.PowerT1Effect,
                2 => config.PowerT2Effect,
                3 => config.PowerT3Effect,
                4 => config.PowerT4Effect,
                _ => 0f
            },
            CoreType.Force => Tier switch
            {
                1 => config.ForceT1Effect,
                2 => config.ForceT2Effect,
                3 => config.ForceT3Effect,
                4 => config.ForceT4Effect,
                _ => 0f
            },
            CoreType.Precision => Tier switch
            {
                1 => config.PrecisionT1Effect,
                2 => config.PrecisionT2Effect,
                3 => config.PrecisionT3Effect,
                4 => config.PrecisionT4Effect,
                _ => 0f
            },
            CoreType.Defense => Tier switch
            {
                1 => config.DefenseT1Effect,
                2 => config.DefenseT2Effect,
                3 => config.DefenseT3Effect,
                4 => config.DefenseT4Effect,
                _ => 0f
            },
            CoreType.Vitality => Tier switch
            {
                1 => config.VitalityT1Effect,
                2 => config.VitalityT2Effect,
                3 => config.VitalityT3Effect,
                4 => config.VitalityT4Effect,
                _ => 0f
            },
            CoreType.Evasion => Tier switch
            {
                1 => config.EvasionT1Effect,
                2 => config.EvasionT2Effect,
                3 => config.EvasionT3Effect,
                4 => config.EvasionT4Effect,
                _ => 0f
            },
            _ => 0f
        };
    }

    public string GetDisplayName()
    {
        string typeName = Type switch
        {
            CoreType.Power => Language.GetTextValue("Mods.Stataria.CoreItem.CoreOfPower"),
            CoreType.Force => Language.GetTextValue("Mods.Stataria.CoreItem.CoreOfForce"),
            CoreType.Precision => Language.GetTextValue("Mods.Stataria.CoreItem.CoreOfPrecision"),
            CoreType.Defense => Language.GetTextValue("Mods.Stataria.CoreItem.CoreOfDefense"),
            CoreType.Vitality => Language.GetTextValue("Mods.Stataria.CoreItem.CoreOfVitality"),
            CoreType.Evasion => Language.GetTextValue("Mods.Stataria.CoreItem.CoreOfEvasion"),
            _ => Language.GetTextValue("Mods.Stataria.CoreItem.UnknownCore")
        };
        return Language.GetTextValue("Mods.Stataria.CoreItem.CoreNameTier", typeName, Tier);
    }
}