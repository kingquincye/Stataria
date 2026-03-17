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
                buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.GuardianAuraBuff.GuardianNameSelf");
                tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.GuardianAuraBuff.GuardianTooltipSelf", config.roleSettings.GuardianAuraRadius.ToString("0.#"));
            }
            else
            {
                buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.GuardianAuraBuff.GuardianNameTeammate");
                tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.GuardianAuraBuff.GuardianTooltipTeammate", config.roleSettings.GuardianTeammateDefenseBonus.ToString("0.#"));
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