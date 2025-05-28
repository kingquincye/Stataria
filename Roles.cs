using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria;

namespace Stataria
{
    public enum RoleStatus
    {
        Available,
        Active,
        Locked
    }

    public class Role
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FlavorText { get; set; }
        public int SwitchCost { get; set; }
        public RoleStatus Status { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public Dictionary<string, object> Effects { get; set; }

        public Role(string id, string name, string description, string flavorText, int switchCost = 0)
        {
            ID = id;
            Name = name;
            Description = description;
            FlavorText = flavorText;
            SwitchCost = switchCost;
            Status = RoleStatus.Available;
            Requirements = new Dictionary<string, object>();
            Effects = new Dictionary<string, object>();
        }

        public bool CanActivate(RPGPlayer player)
        {
            if (Status == RoleStatus.Locked)
            {
                if (Requirements.ContainsKey("RebirthRequired"))
                {
                    int requiredRebirths = (int)Requirements["RebirthRequired"];
                    return player.RebirthCount >= requiredRebirths;
                }
                return false;
            }

            if (Status == RoleStatus.Active)
                return false;

            return player.RebirthPoints >= GetCurrentSwitchCost(player);
        }

        public int GetCurrentSwitchCost(RPGPlayer player)
        {
            if (player.ActiveRole == null)
                return 0;

            var config = ModContent.GetInstance<StatariaConfig>();
            int baseCost = config.roleSettings.BaseSwitchCost;
            float multiplier = 1f + (player.RoleSwitchCount * config.roleSettings.SwitchCostMultiplier);
            return (int)(baseCost * multiplier);
        }

        public string GetEffectsDescription()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (ID == "CritGod")
            {
                var effects = new List<string>
                {
                    $"• +{config.roleSettings.CritGodCritChance}% Global Critical Strike Chance",
                    $"• Excess Critical Strike Chance (over 100%) converts to +{config.roleSettings.CritGodExcessCritToDamage:0.##}% Critical Strike Damage per 1% excess",
                    "• Your summons can now critically strike"
                };

                return string.Join("\n", effects);
            }

            return "No effects defined.";
        }
    }
}