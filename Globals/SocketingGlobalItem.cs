using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Stataria
{
    public class SocketingGlobalItem : GlobalItem
    {
        public List<SocketedCore> SocketedCores = new List<SocketedCore>();
        public int MaxSlots = 0;
        public int ExpandedSlots = 0;

        public override bool InstancePerEntity => true;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults(Item item)
        {
            if (IsWeapon(item) || IsArmor(item))
            {
                MaxSlots = GetBaseSlots(item);
            }
        }

        public override void SaveData(Item item, TagCompound tag)
        {
            if (SocketedCores.Count > 0)
            {
                var coreList = new List<TagCompound>();
                foreach (var core in SocketedCores)
                {
                    coreList.Add(new TagCompound
                    {
                        ["Type"] = (int)core.Type,
                        ["Tier"] = core.Tier,
                        ["Count"] = core.Count
                    });
                }
                tag["SocketedCores"] = coreList;
            }

            if (ExpandedSlots > 0)
                tag["ExpandedSlots"] = ExpandedSlots;
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            if (tag.ContainsKey("SocketedCores"))
            {
                var coreList = tag.Get<List<TagCompound>>("SocketedCores");
                SocketedCores.Clear();
                foreach (var coreTag in coreList)
                {
                    SocketedCores.Add(new SocketedCore(
                        (CoreType)coreTag.GetInt("Type"),
                        coreTag.GetInt("Tier"),
                        coreTag.GetInt("Count")
                    ));
                }
            }

            ExpandedSlots = tag.GetInt("ExpandedSlots");

            if (IsWeapon(item) || IsArmor(item))
            {
                MaxSlots = GetBaseSlots(item) + ExpandedSlots;
            }
        }

        public override void UpdateEquip(Item item, Player player)
        {
            float defenseBonus = GetTotalCoreEffect(CoreType.Defense);
            if (defenseBonus > 0)
            {
                player.statDefense += (int)defenseBonus;
            }
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            float powerBonus = GetTotalCoreEffect(CoreType.Power);
            if (powerBonus > 0)
            {
                damage += powerBonus / 100f;
            }
        }

        public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
        {
            if (!CanReceiveKnockback(item))
                return;

            float forceBonus = GetTotalCoreEffect(CoreType.Force);
            if (forceBonus > 0)
            {
                knockback += forceBonus / 100f;
            }
        }

        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!CanReceiveCrit(item))
                return;

            float precisionBonus = GetTotalCoreEffect(CoreType.Precision);
            if (precisionBonus > 0)
            {
                crit += precisionBonus;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!(IsWeapon(item) || IsArmor(item)) || SocketedCores.Count == 0)
                return;

            var configClient = ModContent.GetInstance<StatariaClientConfig>();
            if (!configClient.ShowSocketedCoresInTooltip)
                return;

            tooltips.Add(new TooltipLine(Mod, "SocketedCoresHeader", "Socketed Cores:")
            {
                OverrideColor = Color.Gold
            });

            foreach (var core in SocketedCores)
            {
                string effect = core.Type switch
                {
                    CoreType.Power => $"+{core.GetEffectValue() * core.Count:0.#}% Damage",
                    CoreType.Force => CanReceiveKnockback(item) ? $"+{core.GetEffectValue() * core.Count:0.#}% Knockback" : "",
                    CoreType.Precision => CanReceiveCrit(item) ? $"+{core.GetEffectValue() * core.Count:0.#}% Crit" : "",
                    CoreType.Defense => $"+{core.GetEffectValue() * core.Count:0.#} Defense",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(effect))
                {
                    tooltips.Add(new TooltipLine(Mod, $"Core_{core.Type}_{core.Tier}",
                        $"  {core.GetDisplayName()} x{core.Count} - {effect}")
                    {
                        OverrideColor = Color.LightBlue
                    });
                }
            }
        }

        public static bool IsWeapon(Item item)
        {
            return item.DamageType != DamageClass.Default;
        }

        public static bool IsArmor(Item item)
        {
            return !item.accessory && (item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1);
        }

        public static bool CanReceiveDamage(Item item)
        {
            return item.damage > 0;
        }

        public static bool CanReceiveKnockback(Item item)
        {
            return item.knockBack > 0 && !item.CountsAsClass(DamageClass.Summon);
        }

        public static bool CanReceiveCrit(Item item)
        {
            return item.damage > 0 && item.crit >= 0 && !item.CountsAsClass(DamageClass.Summon);
        }

        public static int GetBaseSlots(Item item)
        {
            return item.rare switch
            {
                ItemRarityID.Gray or
                ItemRarityID.White or
                ItemRarityID.Blue or
                ItemRarityID.Green => 1,

                ItemRarityID.Orange or
                ItemRarityID.LightRed or
                ItemRarityID.Pink or
                ItemRarityID.LightPurple => 2,

                ItemRarityID.Lime or
                ItemRarityID.Yellow or
                ItemRarityID.Cyan or
                ItemRarityID.Red or
                ItemRarityID.Purple => 3,

                _ when item.rare > ItemRarityID.Purple => 3,

                _ => 1
            };
        }

        public int GetUsedSlots()
        {
            return SocketedCores.Sum(core => core.Count);
        }

        public int GetAvailableSlots()
        {
            return MaxSlots - GetUsedSlots();
        }

        public bool CanAttachCore(CoreType type, Item item, Player player)
        {
            if (GetAvailableSlots() <= 0)
                return false;

            return type switch
            {
                CoreType.Power => CanReceiveDamage(item),
                CoreType.Force => CanReceiveKnockback(item),
                CoreType.Precision => CanReceiveCrit(item),
                CoreType.Defense => IsArmor(item),
                _ => false
            };
        }

        public void AttachCore(CoreType type, int tier)
        {
            var existingCore = SocketedCores.FirstOrDefault(c => c.Type == type && c.Tier == tier);
            if (existingCore.Count > 0)
            {
                var index = SocketedCores.FindIndex(c => c.Type == type && c.Tier == tier);
                SocketedCores[index] = new SocketedCore(type, tier, existingCore.Count + 1);
            }
            else
            {
                SocketedCores.Add(new SocketedCore(type, tier, 1));
            }
        }

        public bool ExtractCore(CoreType type, int tier)
        {
            var core = SocketedCores.FirstOrDefault(c => c.Type == type && c.Tier == tier);
            if (core.Count <= 0)
                return false;

            var index = SocketedCores.FindIndex(c => c.Type == type && c.Tier == tier);
            if (core.Count > 1)
            {
                SocketedCores[index] = new SocketedCore(type, tier, core.Count - 1);
            }
            else
            {
                SocketedCores.RemoveAt(index);
            }
            return true;
        }

        public void ExpandSlots()
        {
            ExpandedSlots++;
            MaxSlots++;
        }

        public int GetExpandCost()
        {
            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            float multiplier = 1f + (ExpandedSlots * (config.ExpandCostIncrease / 100f));
            return (int)(config.BaseExpandCost * multiplier);
        }

        public float GetTotalCoreEffect(CoreType type)
        {
            return SocketedCores
                .Where(core => core.Type == type)
                .Sum(core => core.GetEffectValue() * core.Count);
        }

        public static void SyncSocketedItem(Player player, Item item, int itemSlot = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            if (!(IsWeapon(item) || IsArmor(item)) || item.IsAir)
                return;

            var socketingData = item.GetGlobalItem<SocketingGlobalItem>();

            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncSocketedItem);
            packet.Write(player.whoAmI);
            packet.Write(itemSlot);
            packet.Write(socketingData.SocketedCores.Count);

            foreach (var core in socketingData.SocketedCores)
            {
                packet.Write((int)core.Type);
                packet.Write(core.Tier);
                packet.Write(core.Count);
            }

            packet.Write(socketingData.ExpandedSlots);
            packet.Send();
        }

        public static int FindItemSlotInInventory(Player player, Item targetItem)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i] == targetItem)
                    return i;
            }
            return -1;
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(SocketedCores.Count);
            foreach (var core in SocketedCores)
            {
                writer.Write((int)core.Type);
                writer.Write(core.Tier);
                writer.Write(core.Count);
            }
            writer.Write(ExpandedSlots);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            int coreCount = reader.ReadInt32();
            SocketedCores.Clear();

            for (int i = 0; i < coreCount; i++)
            {
                CoreType type = (CoreType)reader.ReadInt32();
                int tier = reader.ReadInt32();
                int count = reader.ReadInt32();
                SocketedCores.Add(new SocketedCore(type, tier, count));
            }

            ExpandedSlots = reader.ReadInt32();
            MaxSlots = GetBaseSlots(item) + ExpandedSlots;
        }
    }
}