using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria.Buffs
{
    public class GuardianAuraBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player player = Main.LocalPlayer;
            RPGPlayer rpgPlayer = player.GetModPlayer<RPGPlayer>();

            bool isGuardian = rpgPlayer?.ActiveRole?.ID == "Guardian" && rpgPlayer.ActiveRole.Status == RoleStatus.Active;

            if (isGuardian)
            {
                buffName = "Guardian Aura (Self)";
                tip = $"You are radiating protective energy\nAura radius: {config.roleSettings.GuardianAuraRadius:0.#} pixels";
            }
            else
            {
                buffName = "Guardian Aura";
                tip = $"Within a Guardian's protective aura\n+{config.roleSettings.GuardianTeammateDefenseBonus:0.#}% defense\nSharing damage with Guardian";
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var rpgPlayer = player.GetModPlayer<RPGPlayer>();
            
            bool isGuardian = rpgPlayer?.ActiveRole?.ID == "Guardian" && rpgPlayer.ActiveRole.Status == RoleStatus.Active;

            if (!isGuardian)
            {
                float defenseBonus = config.roleSettings.GuardianTeammateDefenseBonus / 100f;
                player.statDefense = player.statDefense * (1f + defenseBonus);
                
                float damageReduction = config.roleSettings.GuardianTeammateDamageReduction / 100f;
                player.endurance += damageReduction;
            }
        }
    }
}