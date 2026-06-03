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
            var clericPlayer = player.GetModPlayer<ClericPlayer>();

            bool isCleric = rpgPlayer?.ActiveRole?.ID == "Cleric" && rpgPlayer.ActiveRole.Status == RoleStatus.Active;
            bool isAngel = isCleric && rpgPlayer.AscendedRoles.Contains("Cleric");

            if (isCleric)
            {
                if (isAngel)
                {
                    buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.AngelNameSelf");
                    tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.AngelTooltipSelf", config.roleSettings.AngelAuraRadius.ToString("0.#"));
                }
                else
                {
                    buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianNameSelf");
                    tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianTooltipSelf", config.roleSettings.ClericAuraRadius.ToString("0.#"));
                }
            }
            else
            {
                bool isAngelAura = clericPlayer.ReceivedTeammateHealthBonus >= config.roleSettings.AngelTeammateHealthBonus;
                if (isAngelAura)
                {
                    buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.AngelNameTeammate");
                    tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.AngelTooltipTeammate", clericPlayer.ReceivedTeammateHealthBonus.ToString("0.#"));
                }
                else
                {
                    buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianNameTeammate");
                    tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.ClericAuraBuff.GuardianTooltipTeammate", clericPlayer.ReceivedTeammateHealthBonus.ToString("0.#"));
                }
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Health bonus is now applied inside ClericPlayer.ResetEffects to avoid replication
        }
    }
}