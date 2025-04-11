using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Stataria
{
    public class RPGPlayer : ModPlayer
    {
        public int xpBarTimer = 0;
        private const int xpBarDuration = 120;
        public int Level = 1;
        public int XP = 0;
        public int XPToNext = 100;
        public int StatPoints = 0;

        public int VIT = 0, STR = 0, AGI = 0, INT = 0, LUC = 0, END = 0, POW = 0, DEX = 0, SPR = 0;

        public override void Initialize()
        {
            Level = 1;
            XP = 0;
            XPToNext = 100;
            StatPoints = 0;
            VIT = STR = AGI = INT = LUC = END = POW = DEX = SPR = 0;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["Level"] = Level;
            tag["XP"] = XP;
            tag["XPToNext"] = XPToNext;
            tag["StatPoints"] = StatPoints;
            tag["VIT"] = VIT; tag["STR"] = STR; tag["AGI"] = AGI;
            tag["INT"] = INT; tag["LUC"] = LUC; tag["END"] = END;
            tag["POW"] = POW; tag["DEX"] = DEX; tag["SPR"] = SPR;
        }

        public override void LoadData(TagCompound tag)
        {
            Level = tag.GetInt("Level");
            XP = tag.GetInt("XP");
            XPToNext = tag.GetInt("XPToNext");
            StatPoints = tag.GetInt("StatPoints");
            VIT = tag.GetInt("VIT"); STR = tag.GetInt("STR"); AGI = tag.GetInt("AGI");
            INT = tag.GetInt("INT"); LUC = tag.GetInt("LUC"); END = tag.GetInt("END");
            POW = tag.GetInt("POW"); DEX = tag.GetInt("DEX"); SPR = tag.GetInt("SPR");
        }

        public void GainXP(int amount)
        {
            XP += amount;

            if (Main.netMode != NetmodeID.Server)
            {
                xpBarTimer = xpBarDuration;
                CombatText.NewText(Player.Hitbox, Color.Gold, $"+{amount} XP");
            }

            while (XP >= XPToNext)
            {
                XP -= XPToNext;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            StatPoints += 1;
            XPToNext = (int)(100 * Math.Pow(Level, 1.5));

            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(Player.Hitbox, Color.LightGreen, $"Level Up! Level {Level}");
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.friendly || target.lifeMax <= 5)
                return;
            var config = ModContent.GetInstance<StatariaConfig>();
            GainXP((int)(damageDone * config.DamageXP));
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.friendly || target.lifeMax <= 5 || proj.owner != Player.whoAmI)
                return;
            var config = ModContent.GetInstance<StatariaConfig>();
            GainXP((int)(damageDone * config.DamageXP));
        }

        public override void ResetEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            Player.statLifeMax2 += VIT * config.VIT_HP;
            Player.statManaMax2 += INT * config.INT_MP;
            Player.statDefense += END / 2;
            Player.maxMinions += SPR / 10; // 1 extra minion per 10 SPR
            Player.maxTurrets += SPR / 20;   // 1 extra sentry per 20 SPR

            float effectiveAGI = AGI <= 75 ? AGI : 75 + (AGI - 75) * 0.5f;
            Player.moveSpeed += effectiveAGI * 0.01f;
        }

        public override void PostUpdate()
        {
            if (xpBarTimer > 0)
                xpBarTimer--;

            Player.lifeRegen += VIT / 2;
            Player.manaRegenBonus += INT / 2;

            float resistPercent = Math.Min(END * 0.005f, 1f);
            for (int i = 0; i < Player.buffTime.Length; i++)
            {
                int buffType = Player.buffType[i];
                if (Player.buffTime[i] > 0 && Main.debuff[buffType])
                {
                    Player.buffTime[i] = (int)(Player.buffTime[i] * (1f - resistPercent));
                }
            }

            float endBonus = 1f - (1f / (1f + END * 0.01f));
            float currentDR = Player.endurance;
            Player.endurance = currentDR + (1f - currentDR) * endBonus;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            float kbResist = Math.Min(END * 0.01f, 1f);
            modifiers.Knockback *= 1f - kbResist;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (!info.DamageSource.TryGetCausingEntity(out Entity entity) || entity is not NPC npc)
                return;

            if (!npc.boss)
            {
                Vector2 knockbackDir = npc.Center - Player.Center;
                knockbackDir.Normalize();
                float knockbackStrength = Math.Clamp(END * 0.01f * 10f, 2f, 12f);
                npc.velocity += knockbackDir * knockbackStrength;
            }
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float bonus = POW * (config.POW_Damage / 100f);

            if (item.CountsAsClass(DamageClass.Melee))
                bonus += STR * (config.STR_Damage / 100f);
            else if (item.CountsAsClass(DamageClass.Magic))
                bonus += INT * (config.INT_Damage / 100f);
            else if (item.CountsAsClass(DamageClass.Ranged))
                bonus += DEX * (config.DEX_Damage / 100f);
            else if (item.CountsAsClass(DamageClass.Summon))
                bonus += SPR * (config.SPR_Damage / 100f);

            damage += bonus;
        }

        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            crit += LUC * (config.LUC_Crit / 100f);
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            if (weapon.useAmmo > 0 && DEX > 0)
            {
                float chance = DEX * 0.01f;
                if (Main.rand.NextFloat() < chance)
                    return false;
            }
            return true;
        }

        public override float UseSpeedMultiplier(Item item)
        {
            float effectiveAGI = AGI <= 75 ? AGI : 75 + (AGI - 75) * 0.5f;
            return 1f + (effectiveAGI * 0.01f);
        }

        // --- New override for network synchronization ---
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            var packet = Mod.GetPacket();
            // Write an identifier so that the receiver knows what type of data follows.
            packet.Write((byte)StatariaMessageType.SyncPlayer);
            // Write the player index to know which player's data we’re updating.
            packet.Write(Player.whoAmI);
            packet.Write(Level);
            packet.Write(XP);
            packet.Write(XPToNext);
            packet.Write(StatPoints);
            packet.Write(VIT);
            packet.Write(STR);
            packet.Write(AGI);
            packet.Write(INT);
            packet.Write(LUC);
            packet.Write(END);
            packet.Write(POW);
            packet.Write(DEX);
            packet.Write(SPR);
            packet.Send(toWho, fromWho);
        }
    }
}