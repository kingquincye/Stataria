using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;

namespace Stataria
{
    public class RPGPlayer : ModPlayer
    {
        public int xpBarTimer = 0;
        private const int xpBarDuration = 120;
        public int levelCapMessageTimer = 0;
        private const int levelCapMessageCooldown = 1800; // 2 seconds
        public int teleportCooldownTimer = 0;
        public float breathDepletionCounter;
        private float breathMultiplier = 1f;
        public int Level = 1;
        public int XP = 0;
        public int XPToNext = 100;
        public int StatPoints = 0;

        public int VIT = 0, STR = 0, AGI = 0, INT = 0, LUC = 0, END = 0, POW = 0, DEX = 0, SPR = 0;

        public HashSet<int> rewardedBosses = new();

        public override void Initialize()
        {
            Level = 1;
            XP = 0;
            XPToNext = 100;
            StatPoints = 0;
            VIT = STR = AGI = INT = LUC = END = POW = DEX = SPR = 0;
            rewardedBosses.Clear();
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
            tag["RewardedBosses"] = new List<int>(rewardedBosses);
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
            if (tag.ContainsKey("RewardedBosses"))
                rewardedBosses = tag.Get<List<int>>("RewardedBosses").ToHashSet();
        }

        public void GainXP(int amount)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            // First: Don't do anything if we're at the level cap
            if (config.EnableLevelCap && Level >= config.LevelCapValue)
            {
                XP = XPToNext;

                if (Main.netMode != NetmodeID.Server && levelCapMessageTimer <= 0)
                {
                    CombatText.NewText(Player.Hitbox, Color.Gray, "Level Cap Reached!");
                    levelCapMessageTimer = levelCapMessageCooldown;
                }

                return; // Skip XP gain and gold text
            }

            // Safe to gain XP
            XP += amount;

            if (Main.netMode != NetmodeID.Server)
            {
                xpBarTimer = xpBarDuration;
                CombatText.NewText(Player.Hitbox, Color.Gold, $"+{amount} XP");
            }

            while (XP >= XPToNext)
            {
                if (config.EnableLevelCap && Level >= config.LevelCapValue)
                {
                    XP = XPToNext;
                    if (Main.netMode != NetmodeID.Server && levelCapMessageTimer <= 0)
                    {
                        CombatText.NewText(Player.Hitbox, Color.Gray, "Level Cap Reached!");
                        levelCapMessageTimer = levelCapMessageCooldown;
                    }
                    break;
                }

                XP -= XPToNext;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config.EnableLevelCap && Level >= config.LevelCapValue)
            {
                XP = XPToNext;
                return;
            }

            Level++;
            StatPoints++;
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
        public override void ModifyLuck(ref float luck)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            luck += LUC * config.LUC_LuckBonus;
            luck = Math.Clamp(luck, -0.7f, 1f); 
        }

        public override void ResetEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            // --- VIT ---
            Player.statLifeMax2 += VIT * config.VIT_HP;
            //Player.breathMax += VIT * config.VIT_Breath;
            breathMultiplier = 1f / (1f + VIT * 0.04f);

            // --- STR ---
            Player.GetArmorPenetration(DamageClass.Melee) += STR * config.STR_ArmorPen;

            // --- INT ---
            Player.statManaMax2 += INT * config.INT_MP;
            //Player.manaRegenBonus += INT * config.INT_ManaRegen;
            float rawReduction = INT * config.INT_ManaCostReduction / 100f;
            float diminishingReduction = 1f - (1f / (1f + rawReduction));
            Player.manaCost -= diminishingReduction;
            Player.GetArmorPenetration(DamageClass.Magic) += INT * config.INT_ArmorPen;

            // --- END ---
            /*if (config.EnableKnockbackResist)
                Player.knockBackResist += Math.Min(END * config.END_KnockbackResist / 100f, 1f);*/
            if (config.END_DefensePerX > 0)
            {
                Player.statDefense += END / config.END_DefensePerX;
            }
            Player.aggro += END * config.END_Aggro;

            // --- AGI ---
            float effectiveAGI = AGI <= 75 ? AGI : 75 + (AGI - 75) * 0.5f;
            Player.moveSpeed += effectiveAGI * (config.AGI_MoveSpeed / 100f);
            Player.GetAttackSpeed(DamageClass.Generic) += effectiveAGI * (config.AGI_AttackSpeed / 100f);
            float jumpHeightMultiplier = 1f - (float)Math.Pow(0.98, AGI);
            Player.jumpHeight += (int)(15 * jumpHeightMultiplier * config.AGI_JumpHeight);
            Player.jumpSpeedBoost += AGI * config.AGI_JumpSpeed;
            Player.autoJump = AGI >= config.AGI_JumpUnlockAt;      // Enable auto-jump at threshold

            if (AGI >= config.AGI_NoFallDamageUnlockAt)
                Player.noFallDmg = true;
            if (AGI >= config.AGI_SwimUnlockAt)
                Player.accFlipper = true;
                Player.ignoreWater = true;
            if (AGI >= config.AGI_DashUnlockAt)
                Player.dashType = 2;

            // --- LUC ---
            Player.fishingSkill += LUC * config.LUC_Fishing;
            Player.aggro -= LUC * config.LUC_AggroReduction;
            //Player.luck += LUC * config.LUC_LuckBonus;

            // --- SPR ---
            Player.maxMinions += SPR / config.SPR_MinionsPerX;
            Player.maxTurrets += SPR / config.SPR_SentriesPerX;
            Player.GetDamage(DamageClass.Summon) += SPR * (config.SPR_Damage / 100f);
            //Player.minionAttackSpeed += SPR * config.SPR_AttackSpeed / 100f;

            // --- DEX ---
            Player.pickSpeed -= DEX * config.DEX_MiningSpeed * 0.01f;
            Player.tileSpeed += DEX * config.DEX_BuildSpeed;
            Player.tileRangeX += DEX * config.DEX_Range;
            Player.tileRangeY += DEX * config.DEX_Range;
        }

        public override void PostUpdateEquips()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player.wingTimeMax += AGI * config.AGI_WingTime;
        }

        public override void PostUpdate()
        {
            if (xpBarTimer > 0)
                xpBarTimer--;
            
            if (levelCapMessageTimer > 0)
                levelCapMessageTimer--;

            Player.lifeRegen += VIT / 2;
            Player.manaRegenBonus += INT / 2;

            if (Player.wet && Player.breathCD > 0)
            {
                breathDepletionCounter += breathMultiplier;
                if (breathDepletionCounter >= 1f)
                {
                    int actualLoss = (int)breathDepletionCounter;
                    Player.breathCD -= actualLoss;
                    breathDepletionCounter -= actualLoss;
                }
            }
            else
            {
                breathDepletionCounter = 0f; // Reset when not underwater
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            if (teleportCooldownTimer > 0)
                teleportCooldownTimer--;

            if (AGI >= config.AGI_TeleportUnlockAt &&
                StatariaKeybinds.TeleportKey.JustPressed &&
                teleportCooldownTimer <= 0)
            {
                Vector2 mouseWorld = Main.MouseWorld;
                if (Collision.CanHitLine(Player.Center, 0, 0, mouseWorld, 0, 0))
                {
                    Player.Teleport(mouseWorld, 1);
                    teleportCooldownTimer = config.AGI_TeleportCooldown * 60;

                    // Add some visual & sound
                    for (int i = 0; i < 30; i++)
                    {
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror);
                    }
                    SoundEngine.PlaySound(SoundID.Item6, Player.position);
                }
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (config.EnableKnockbackResist)
            {
                float kbResist = Math.Min(END * 0.01f, 1f); // 1% per END
                modifiers.Knockback *= 1f - kbResist;
            }

            if (config.EnableDR)
            {
                float diminishingDR = 1f - (1f / (1f + END * 0.01f)); // 50% at 100 END
                modifiers.FinalDamage *= 1f - diminishingDR;
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.EnableEnemyKnockback)
                return;

            if (!info.DamageSource.TryGetCausingEntity(out Entity entity) || entity is not NPC npc)
                return;

            if (!npc.boss)
            {
                Vector2 knockbackDir = npc.Center - Player.Center;
                knockbackDir.Normalize();

                float knockbackStrength = Math.Clamp(END * config.END_EnemyKnockbackMultiplier, 2f, 12f);
                npc.velocity += knockbackDir * knockbackStrength;
            }
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float bonus = 0f;

            if (item.CountsAsClass(DamageClass.Melee))
                bonus += STR * (config.STR_Damage / 100f);

            if (item.CountsAsClass(DamageClass.Magic))
                bonus += INT * (config.INT_Damage / 100f);

            if (item.CountsAsClass(DamageClass.Ranged))
                bonus += DEX * (config.DEX_Damage / 100f);

            /*if (item.CountsAsClass(DamageClass.Summon))
                bonus += SPR * (config.SPR_Damage / 100f);*/

            if (!item.CountsAsClass(DamageClass.Melee) &&
                !item.CountsAsClass(DamageClass.Ranged) &&
                !item.CountsAsClass(DamageClass.Magic) &&
                !item.CountsAsClass(DamageClass.Summon))
            {
                bonus += POW * (config.POW_Damage / 100f);
            }
            else
            {
                bonus += POW * 0.001f;
            }

            damage += bonus;
        }

        public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (item.CountsAsClass(DamageClass.Melee))
                knockback += STR * (config.STR_Knockback / 100f);
        }

        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            crit += LUC * config.LUC_Crit;
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

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncPlayer);
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