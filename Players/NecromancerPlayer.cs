using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Stataria.Projectiles;

namespace Stataria
{
    public class NecromancerPlayer : ModPlayer
    {
        public List<float> SoulReserveLifetimes = new List<float>();
        public bool IsNecromancerActive => GetNecromancerRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;

        private Role GetNecromancerRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Necromancer", out Role role) ? role : null;
        }

        public int GetMaxSoulCapacity()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int spr = Player.GetModPlayer<RPGPlayer>().GetEffectiveStat("SPR");
            return config.roleSettings.NecromancerBaseSoulCapacity + (spr / config.roleSettings.NecromancerSPRPerSoul);
        }

        public float GetMaxSoulDuration()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int spr = Player.GetModPlayer<RPGPlayer>().GetEffectiveStat("SPR");
            return config.roleSettings.NecromancerBaseSoulDuration + (spr * config.roleSettings.NecromancerSoulDurationPerSPR);
        }

        public bool IsRecalled { get; set; } = false;

        public override void SaveData(TagCompound tag)
        {
            tag["SoulReserveLifetimes"] = new List<float>(SoulReserveLifetimes);
            tag["IsRecalled"] = IsRecalled;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("SoulReserveLifetimes"))
            {
                SoulReserveLifetimes = tag.Get<List<float>>("SoulReserveLifetimes");
            }
            else
            {
                SoulReserveLifetimes = new List<float>();
            }
            IsRecalled = tag.ContainsKey("IsRecalled") && tag.GetBool("IsRecalled");
        }

        public override void Initialize()
        {
            SoulReserveLifetimes.Clear();
            IsRecalled = false;
        }

        public override void ResetEffects()
        {
            if (!IsNecromancerActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            int thrallCount = GetActiveThrallCount();
            float drPercent = thrallCount * config.roleSettings.NecromancerBoneArmorDRPerThrall / 100f;
            Player.endurance += drPercent;
        }

        public override void PostUpdate()
        {
            if (!IsNecromancerActive)
            {
                KillAllThralls();
                IsRecalled = false;
                return;
            }

            // Auto-summoning logic (only if not recalled, and only on the owner's client)
            if (Player.whoAmI == Main.myPlayer && !IsRecalled)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                int limit = config.roleSettings.NecromancerActiveThrallsLimit;
                int currentActive = GetActiveThrallCount();

                if (currentActive < limit && SoulReserveLifetimes.Count > 0)
                {
                    SpawnThrall();
                }
            }
        }

        public int GetActiveThrallCount()
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<ZombieThrallProjectile>())
                {
                    count++;
                }
            }
            return count;
        }

        public void KillAllThralls()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<ZombieThrallProjectile>())
                {
                    proj.Kill();
                }
            }
        }

        public void HarvestSoul()
        {
            int maxCap = GetMaxSoulCapacity();
            if (SoulReserveLifetimes.Count < maxCap)
            {
                SoulReserveLifetimes.Add(GetMaxSoulDuration());
                if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(Player.Hitbox, Color.LimeGreen, "+1 Soul", true);
                }
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    SyncSouls();
                }
            }
        }

        public void HarvestSoulOnKill()
        {
            HarvestSoul();
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsNecromancerActive && (item.DamageType == DamageClass.Magic || item.DamageType == DamageClass.Summon))
            {
                TryHarvestSoulOnBossHit(target);
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsNecromancerActive && proj.owner == Player.whoAmI && (proj.DamageType == DamageClass.Magic || proj.DamageType == DamageClass.Summon))
            {
                TryHarvestSoulOnBossHit(target);
            }
        }

        private void TryHarvestSoulOnBossHit(NPC target)
        {
            bool isBoss = target.boss || (target.realLife >= 0 && Main.npc[target.realLife].boss);
            if (!isBoss)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (Main.rand.NextFloat() < config.roleSettings.NecromancerBossSoulHarvestChance / 100f)
            {
                HarvestSoul();
            }
        }

        public void PerformSoulRecall()
        {
            if (!IsNecromancerActive)
                return;

            IsRecalled = !IsRecalled;

            if (IsRecalled)
            {
                int maxCap = GetMaxSoulCapacity();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<ZombieThrallProjectile>())
                    {
                        var thrall = proj.ModProjectile as ZombieThrallProjectile;
                        if (thrall != null)
                        {
                            float remainingSeconds = thrall.RemainingLifetimeTicks / 60f;
                            if (remainingSeconds > 0.1f)
                            {
                                if (SoulReserveLifetimes.Count < maxCap)
                                {
                                    SoulReserveLifetimes.Add(remainingSeconds);
                                }
                            }

                            proj.Kill();
                        }
                    }
                }

                if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Player.position);
                    CombatText.NewText(Player.Hitbox, Color.Purple, "Souls Recalled", true);
                }
            }
            else
            {
                if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15, Player.position);
                    CombatText.NewText(Player.Hitbox, Color.LimeGreen, "Summoning Enabled", true);
                }
            }
            
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncSouls();
            }
        }

        private void SpawnThrall()
        {
            if (SoulReserveLifetimes.Count == 0)
                return;

            float lifetime = SoulReserveLifetimes[0];
            SoulReserveLifetimes.RemoveAt(0);

            var config = ModContent.GetInstance<StatariaConfig>();
            int baseDamage = config.roleSettings.NecromancerThrallBaseDamage;
            int intStat = Player.GetModPlayer<RPGPlayer>().GetEffectiveStat("INT");
            int damage = (int)(baseDamage * (1f + intStat * config.roleSettings.NecromancerThrallINTScale / 100f));

            if (Player.whoAmI == Main.myPlayer)
            {
                int projIdx = Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), -4f),
                    ModContent.ProjectileType<ZombieThrallProjectile>(),
                    damage,
                    3f,
                    Player.whoAmI
                );

                if (projIdx >= 0 && projIdx < Main.maxProjectiles)
                {
                    var thrall = Main.projectile[projIdx].ModProjectile as ZombieThrallProjectile;
                    if (thrall != null)
                    {
                        thrall.RemainingLifetimeTicks = (int)(lifetime * 60f);
                    }
                }
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncSouls();
            }
        }

        public void SyncSouls(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncNecromancerSouls);
            packet.Write(Player.whoAmI);
            packet.Write(SoulReserveLifetimes.Count);
            foreach (float lifetime in SoulReserveLifetimes)
            {
                packet.Write(lifetime);
            }
            packet.Write(IsRecalled);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncSouls(toWho, fromWho);
        }
    }
}
