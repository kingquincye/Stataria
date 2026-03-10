using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class CritGodPlayer : ModPlayer
    {
        public bool EnableSummonCrits { get; set; }

        public override void ResetEffects()
        {
            EnableSummonCrits = false;
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!EnableSummonCrits)
                return;

            bool isSummonProjectile = proj.minion || proj.sentry || proj.DamageType == DamageClass.Summon;

            if (!isSummonProjectile || proj.owner != Player.whoAmI)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = Player.GetModPlayer<RPGPlayer>();

            float critChance = config.roleSettings.CritGodCritChance;
            critChance += rpg.GetEffectiveStat("LUC") * config.statSettings.LUC_Crit;

            if (Main.rand.NextFloat(100f) < critChance)
            {
                modifiers.SetCrit();
            }
        }
    }
}