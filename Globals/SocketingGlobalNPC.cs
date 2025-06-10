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

            shop.Add<Items.Cores.CoreOfPowerT1>();
            shop.Add<Items.Cores.CoreOfForceT1>();
            shop.Add<Items.Cores.CoreOfPrecisionT1>();

            shop.Add<Items.Cores.CoreOfPowerT2>(Condition.DownedEowOrBoc);
            shop.Add<Items.Cores.CoreOfForceT2>(Condition.DownedEowOrBoc);
            shop.Add<Items.Cores.CoreOfPrecisionT2>(Condition.DownedEowOrBoc);

            shop.Add<Items.Cores.CoreOfPowerT3>(Condition.Hardmode);
            shop.Add<Items.Cores.CoreOfForceT3>(Condition.Hardmode);
            shop.Add<Items.Cores.CoreOfPrecisionT3>(Condition.Hardmode);
        }
    }
}