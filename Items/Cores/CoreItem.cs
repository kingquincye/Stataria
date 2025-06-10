using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Stataria;

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
                _ => ItemRarityID.White
            };
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string effect = CoreType switch
            {
                CoreType.Power => $"+{EffectValue}% Weapon Damage",
                CoreType.Force => $"+{EffectValue}% Weapon Knockback",
                CoreType.Precision => $"+{EffectValue}% Weapon Crit Chance",
                _ => ""
            };

            tooltips.Add(new TooltipLine(Mod, "CoreEffect", effect)
            {
                OverrideColor = Color.LightBlue
            });

            tooltips.Add(new TooltipLine(Mod, "CoreUsage", "Can be socketed into weapons")
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
}

public enum CoreType
{
    Power,
    Force,
    Precision
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
                _ => 0f
            },
            CoreType.Force => Tier switch
            {
                1 => config.ForceT1Effect,
                2 => config.ForceT2Effect,
                3 => config.ForceT3Effect,
                _ => 0f
            },
            CoreType.Precision => Tier switch
            {
                1 => config.PrecisionT1Effect,
                2 => config.PrecisionT2Effect,
                3 => config.PrecisionT3Effect,
                _ => 0f
            },
            _ => 0f
        };
    }

    public string GetDisplayName()
    {
        string typeName = Type switch
        {
            CoreType.Power => "Core of Power",
            CoreType.Force => "Core of Force",
            CoreType.Precision => "Core of Precision",
            _ => "Unknown Core"
        };
        return $"{typeName} T.{Tier}";
    }
}