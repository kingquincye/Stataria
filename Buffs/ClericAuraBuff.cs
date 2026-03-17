using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria.Buffs
{
    public class ClericAuraBuff : ModBuff
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

            bool isCleric = rpgPlayer?.ActiveRole?.ID == "Cleric" && rpgPlayer.ActiveRole.Status == RoleStatus.Active;

            if (isCleric)
            {
                buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianNameSelf");
                tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianTooltipSelf", config.roleSettings.ClericAuraRadius.ToString("0.#"));
            }
            else
            {
                buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianNameTeammate");
                tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianTooltipTeammate", config.roleSettings.ClericTeammateHealthBonus.ToString("0.#"));
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var rpgPlayer = player.GetModPlayer<RPGPlayer>();
            
            bool isCleric = rpgPlayer?.ActiveRole?.ID == "Cleric" && rpgPlayer.ActiveRole.Status == RoleStatus.Active;
            
            if (!isCleric)
            {
                float healthBonus = config.roleSettings.ClericTeammateHealthBonus / 100f;
                player.statLifeMax2 = (int)(player.statLifeMax2 * (1f + healthBonus));
            }
        }
    }
}