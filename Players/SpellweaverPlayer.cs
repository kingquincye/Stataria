using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using Stataria.Projectiles;

namespace Stataria
{
    public class SpellweaverPlayer : ModPlayer
    {
        public float ElementalCharge = 0f;

        public float MaxElementalCharge
        {
            get
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                int intStat = Player.GetModPlayer<RPGPlayer>().GetEffectiveStat("INT");
                // Base capacity + INT * Scale
                return config.roleSettings.SpellweaverMaxElementalCharge + intStat * config.roleSettings.SpellweaverElementalDischargeINTScale;
            }
        }

        public bool IsSpellweaverActive => GetSpellweaverRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;

        private Role GetSpellweaverRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Spellweaver", out Role role) ? role : null;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["SpellweaverElementalCharge"] = ElementalCharge;
        }

        public override void LoadData(TagCompound tag)
        {
            ElementalCharge = tag.ContainsKey("SpellweaverElementalCharge") ? tag.GetFloat("SpellweaverElementalCharge") : 0f;
        }

        public override void Initialize()
        {
            ElementalCharge = 0f;
        }

        public override void ResetEffects()
        {
            if (!IsSpellweaverActive)
            {
                ElementalCharge = 0f;
                return;
            }
        }

        public override void PostUpdate()
        {
            if (!IsSpellweaverActive)
                return;

            // Visual indicator: glowing hands when charge is built up
            if (Main.netMode != NetmodeID.Server && ElementalCharge > 0)
            {
                float chargePct = ElementalCharge / MaxElementalCharge;
                if (Main.rand.NextFloat() < chargePct * 0.4f)
                {
                    // Left hand glow approx
                    Vector2 leftHand = Player.Center + new Vector2(-10f * Player.direction, -2f);
                    int d1 = Dust.NewDust(leftHand, 4, 4, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 0.8f);
                    Main.dust[d1].noGravity = true;
                    Main.dust[d1].velocity *= 0.2f;

                    // Right hand glow approx
                    Vector2 rightHand = Player.Center + new Vector2(10f * Player.direction, -2f);
                    int d2 = Dust.NewDust(rightHand, 4, 4, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 0.8f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].velocity *= 0.2f;
                }
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (IsSpellweaverActive && Player.statMana > 0)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
                {
                    var config = ModContent.GetInstance<StatariaConfig>();
                    float aegisPercent = config.roleSettings.SpellweaverManaAegisPercent / 100f;
                    int damageToMana = (int)(info.Damage * aegisPercent);
                    
                    if (damageToMana > Player.statMana)
                    {
                        damageToMana = Player.statMana;
                    }
                    
                    if (damageToMana > 0)
                    {
                        Player.statMana -= damageToMana;
                        info.Damage -= damageToMana;
                        
                        if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
                        {
                            CombatText.NewText(Player.Hitbox, Color.Cyan, $"-{damageToMana} Mana", true);
                            for (int i = 0; i < 8; i++)
                            {
                                int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror, 0, 0, 100, Color.Cyan, 1.2f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].velocity *= 1.2f;
                            }
                        }
                    }
                };
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsSpellweaverActive && item.DamageType == DamageClass.Magic && hit.Crit)
            {
                RestoreManaOnCrit();
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsSpellweaverActive && proj.owner == Player.whoAmI && proj.DamageType == DamageClass.Magic && hit.Crit)
            {
                RestoreManaOnCrit();
            }
        }

        private void RestoreManaOnCrit()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            float restorePercent = config.roleSettings.SpellweaverManaCritRestorePercent / 100f;
            int amount = (int)(Player.statManaMax2 * restorePercent);
            amount = Math.Max(1, amount);
            
            Player.statMana = Math.Min(Player.statManaMax2, Player.statMana + amount);
            
            if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(Player.Hitbox, Color.DeepSkyBlue, $"+{amount} MP", true);
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror, 0, -2f, 100, Color.DeepSkyBlue, 1.1f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.8f;
                }
            }
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (IsSpellweaverActive)
            {
                float oldCharge = ElementalCharge;
                ElementalCharge = Math.Min(MaxElementalCharge, ElementalCharge + manaConsumed);
                
                if (ElementalCharge > oldCharge && Main.netMode != NetmodeID.Server)
                {
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        // Spawn small sparks on charge gain
                        int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Electric, 0, 0, 100, default, 0.7f);
                        Main.dust[d].velocity *= 0.3f;
                        Main.dust[d].noGravity = true;
                    }
                }

                if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.SinglePlayer)
                {
                    SyncSpellweaverState();
                }
            }
        }

        public void ActivateElementalDischarge()
        {
            if (ElementalCharge <= 0f)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // Calculate damage: consumed charge * base multiplier
            float consumedCharge = ElementalCharge;
            float baseMult = config.roleSettings.SpellweaverElementalDischargeBaseMult;
            float damage = consumedCharge * baseMult;

            int finalDamage = (int)damage;
            finalDamage = Math.Max(1, finalDamage);

            // Reset charge (no cooldown)
            ElementalCharge = 0f;

            // Spawn the projectile (only on local player)
            if (Player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<ElementalDischargeProjectile>(),
                    finalDamage,
                    6f, // Knockback
                    Player.whoAmI,
                    ai0: consumedCharge // Pass consumed charge to projectile AI
                );
            }

            // Sync state
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncSpellweaverState();
            }
        }

        public void SyncSpellweaverState(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncSpellweaverState);
            packet.Write(Player.whoAmI);
            packet.Write(ElementalCharge);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncSpellweaverState(toWho, fromWho);
        }
    }
}
