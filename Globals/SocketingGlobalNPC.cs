using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace Stataria
{
    public class SocketingGlobalNPC : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType != NPCID.Merchant)
                return;

            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            if (!config.EnableSocketingSystem)
                return;

            shop.Add(ModContent.ItemType<Items.Cores.CoreOfPowerT1>());
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfForceT1>());
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfPrecisionT1>());

            var evilBossCondition = new Condition("Evil Boss Defeated", () => SocketingWorld.HasDefeatedEvilBoss);
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfPowerT2>(), evilBossCondition);
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfForceT2>(), evilBossCondition);
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfPrecisionT2>(), evilBossCondition);

            shop.Add(ModContent.ItemType<Items.Cores.CoreOfPowerT3>(), Condition.Hardmode);
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfForceT3>(), Condition.Hardmode);
            shop.Add(ModContent.ItemType<Items.Cores.CoreOfPrecisionT3>(), Condition.Hardmode);
        }
    }
}