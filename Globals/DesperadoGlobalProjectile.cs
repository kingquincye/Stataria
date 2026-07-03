using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Stataria.Globals
{
    public class DesperadoExtraSource : IEntitySource
    {
        public string Context => "DesperadoExtra";
        public int SourceItemType { get; }
        public DesperadoExtraSource(int sourceItemType)
        {
            SourceItemType = sourceItemType;
        }
    }

    public class DesperadoRicochetSource : IEntitySource
    {
        public string Context => "DesperadoRicochet";
        public int IgnoreNPCIndex { get; }
        public int BounceCount { get; }
        public int CritChance { get; }
        public System.Collections.Generic.List<int> HitHistory { get; }
        public int SourceItemType { get; }

        public DesperadoRicochetSource(int ignoreNPCIndex, int bounceCount, int critChance, System.Collections.Generic.List<int> hitHistory, int sourceItemType)
        {
            IgnoreNPCIndex = ignoreNPCIndex;
            BounceCount = bounceCount;
            CritChance = critChance;
            HitHistory = hitHistory != null ? new System.Collections.Generic.List<int>(hitHistory) : new System.Collections.Generic.List<int>();
            SourceItemType = sourceItemType;
        }
    }

    public class DesperadoGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool isRicochet = false;
        public int ricochetTargetIgnoreIndex = -1;
        public int ricochetBounceCount = 0;
        public System.Collections.Generic.List<int> recentHitNPCs = new System.Collections.Generic.List<int>();
        public int sourceItemType = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            // Propagate source item type
            if (source is EntitySource_ItemUse itemUse)
            {
                sourceItemType = itemUse.Item.type;
            }
            else if (source is EntitySource_ItemUse_WithAmmo itemUseAmmo)
            {
                sourceItemType = itemUseAmmo.Item.type;
            }
            else if (source is EntitySource_Parent parentSource)
            {
                if (parentSource.Entity is Projectile parentProj && parentProj.TryGetGlobalProjectile(out DesperadoGlobalProjectile parentGlobal))
                {
                    sourceItemType = parentGlobal.sourceItemType;
                }
            }
            else if (source is DesperadoExtraSource extraSource)
            {
                sourceItemType = extraSource.SourceItemType;
            }

            if (source is DesperadoRicochetSource ricochetSource)
            {
                isRicochet = true;
                ricochetTargetIgnoreIndex = ricochetSource.IgnoreNPCIndex;
                ricochetBounceCount = ricochetSource.BounceCount;
                projectile.CritChance = ricochetSource.CritChance;
                recentHitNPCs = ricochetSource.HitHistory;
                if (!recentHitNPCs.Contains(ricochetSource.IgnoreNPCIndex))
                {
                    recentHitNPCs.Add(ricochetSource.IgnoreNPCIndex);
                }
                projectile.penetrate = 1;
                sourceItemType = ricochetSource.SourceItemType;
                return;
            }

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player player = Main.player[projectile.owner];
            if (player == null || !player.active || player.dead)
                return;

            var desperadoPlayer = player.GetModPlayer<DesperadoPlayer>();
            if (desperadoPlayer == null || !desperadoPlayer.IsDesperadoActive)
                return;

            if (!projectile.friendly || projectile.hostile || projectile.trap || projectile.minion || projectile.sentry)
                return;

            // Prevent recursion: do not duplicate our own extra projectiles
            if (source is DesperadoExtraSource)
                return;

            // Only duplicate ranged projectiles that deal damage
            if (projectile.damage <= 0 || !projectile.CountsAsClass(DamageClass.Ranged))
                return;

            // Ensure the source is from weapon usage or parent projectile (holdouts/splits)
            bool isValidSource = false;
            if (source is EntitySource_ItemUse || source is EntitySource_ItemUse_WithAmmo)
            {
                isValidSource = true;
            }
            else if (source is EntitySource_Parent parentSource)
            {
                if (parentSource.Entity is Player parentPlayer && parentPlayer.whoAmI == player.whoAmI)
                {
                    isValidSource = true;
                }
                else if (parentSource.Entity is Projectile parentProj && parentProj.owner == player.whoAmI && parentProj.friendly)
                {
                    isValidSource = true;
                }
            }

            if (!isValidSource)
                return;

            if (IsBlacklistedForExtraProjectiles(projectile))
                return;

            int stacks = desperadoPlayer.GetTempoStacks();
            var config = ModContent.GetInstance<StatariaConfig>();
            int extraProjCount = 0;
            if (config.roleSettings.DesperadoStacksPerExtraProjectile > 0)
            {
                extraProjCount = stacks / config.roleSettings.DesperadoStacksPerExtraProjectile;
                extraProjCount = Math.Min(extraProjCount, config.roleSettings.DesperadoMaxExtraProjectiles);
            }

            if (extraProjCount > 0)
            {
                int extraProjDamage = (int)(projectile.damage * config.roleSettings.DesperadoExtraProjectileDamageMultiplier);
                if (extraProjDamage < 1) extraProjDamage = 1;

                float ai0 = projectile.ai[0];
                float ai1 = projectile.ai[1];
                float ai2 = projectile.ai[2];

                if (source is EntitySource_OnHit onHit && onHit.Victim is NPC npc)
                {
                    if (projectile.aiStyle == (int)ProjAIStyleID.MagicMissile)
                    {
                        ai0 = -1f;
                        ai1 = npc.whoAmI;
                    }
                }

                for (int i = 0; i < extraProjCount; i++)
                {
                    Vector2 perturbedSpeed = projectile.velocity.RotatedByRandom(MathHelper.ToRadians(config.roleSettings.DesperadoExtraProjectileSpread));
                    int extraProj = Projectile.NewProjectile(
                        new DesperadoExtraSource(sourceItemType),
                        projectile.Center,
                        perturbedSpeed,
                        projectile.type,
                        extraProjDamage,
                        projectile.knockBack,
                        player.whoAmI,
                        ai0,
                        ai1,
                        ai2
                    );

                    if (extraProj >= 0 && extraProj < Main.maxProjectiles)
                    {
                        Projectile extra = Main.projectile[extraProj];
                        extra.CritChance = projectile.CritChance;

                        // Ensure extra projectiles can hit independently by forcing local immunity
                        if (!extra.usesLocalNPCImmunity)
                        {
                            extra.usesIDStaticNPCImmunity = false;
                            extra.usesLocalNPCImmunity = true;
                            // Set a local hit cooldown. If the original had static cooldown, use it; otherwise default to 10
                            extra.localNPCHitCooldown = projectile.usesIDStaticNPCImmunity ? projectile.idStaticNPCHitCooldown : 10;
                        }
                    }
                }
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player player = Main.player[projectile.owner];
            if (player == null || !player.active || player.dead)
                return;

            var desperadoPlayer = player.GetModPlayer<DesperadoPlayer>();
            if (desperadoPlayer == null || !desperadoPlayer.IsDesperadoActive)
                return;

            if (!projectile.CountsAsClass(DamageClass.Ranged) || !hit.Crit)
                return;

            if (IsBlacklistedForRicochet(projectile))
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = player.GetModPlayer<RPGPlayer>();
            int dex = rpg.GetEffectiveStat("DEX");

            int maxBounces = config.roleSettings.DesperadoBouncesBase;
            if (config.roleSettings.DesperadoBouncesDexScale > 0)
            {
                maxBounces += dex / config.roleSettings.DesperadoBouncesDexScale;
            }
            if (config.roleSettings.DesperadoEnableBounceCap)
            {
                maxBounces = Math.Min(maxBounces, config.roleSettings.DesperadoHardBounceCap);
            }

            if (ricochetBounceCount >= maxBounces)
                return;

            float baseChance = config.roleSettings.DesperadoRicochetBaseChance;
            float dexScale = config.roleSettings.DesperadoRicochetDexScale;
            float maxChance = config.roleSettings.DesperadoRicochetMaxChance;

            float finalChance = Math.Min(baseChance + (dex * dexScale), maxChance) / 100f;

            if (Main.rand.NextFloat() < finalChance)
            {
                NPC targetNPC = FindNearestNPCTarget(target, config.roleSettings.DesperadoRicochetRange);
                if (targetNPC != null)
                {
                    Vector2 direction = targetNPC.Center - target.Center;
                    direction.Normalize();
                    
                    float speed = projectile.velocity.Length();
                    if (speed < 1f) speed = 10f;
                    Vector2 newVelocity = direction * speed;

                    int damage = (int)(projectile.damage * config.roleSettings.DesperadoRicochetDamageMultiplier);
                    if (damage < 1) damage = 1;

                    // Copy the original projectile's AI state
                    float ai0 = projectile.ai[0];
                    float ai1 = projectile.ai[1];
                    float ai2 = projectile.ai[2];

                    // If it is a Magic Missile AI style (standard homing), redirect it to the new target
                    if (projectile.aiStyle == (int)ProjAIStyleID.MagicMissile)
                    {
                        ai0 = -1f;
                        ai1 = targetNPC.whoAmI;
                    }

                    if (player.whoAmI == Main.myPlayer)
                    {
                        var nextHistory = new System.Collections.Generic.List<int>(recentHitNPCs);
                        if (!nextHistory.Contains(target.whoAmI))
                        {
                            nextHistory.Add(target.whoAmI);
                        }

                        Projectile.NewProjectile(
                            new DesperadoRicochetSource(target.whoAmI, ricochetBounceCount + 1, projectile.CritChance, nextHistory, sourceItemType),
                            target.Center,
                            newVelocity,
                            projectile.type,
                            damage,
                            projectile.knockBack,
                            projectile.owner,
                            ai0,
                            ai1,
                            ai2
                        );
                    }
                }
            }
        }

        public override bool? CanHitNPC(Projectile projectile, NPC target)
        {
            if (isRicochet && (target.whoAmI == ricochetTargetIgnoreIndex || recentHitNPCs.Contains(target.whoAmI)))
            {
                return false;
            }
            return null;
        }

        public override void SendExtraAI(Projectile projectile, Terraria.ModLoader.IO.BitWriter bitWriter, System.IO.BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(isRicochet);
            binaryWriter.Write(ricochetTargetIgnoreIndex);
            binaryWriter.Write(ricochetBounceCount);
            binaryWriter.Write(projectile.CritChance);
            binaryWriter.Write(sourceItemType);

            binaryWriter.Write(recentHitNPCs.Count);
            foreach (int index in recentHitNPCs)
            {
                binaryWriter.Write(index);
            }
        }

        public override void ReceiveExtraAI(Projectile projectile, Terraria.ModLoader.IO.BitReader bitReader, System.IO.BinaryReader binaryReader)
        {
            isRicochet = bitReader.ReadBit();
            ricochetTargetIgnoreIndex = binaryReader.ReadInt32();
            ricochetBounceCount = binaryReader.ReadInt32();
            projectile.CritChance = binaryReader.ReadInt32();
            sourceItemType = binaryReader.ReadInt32();

            int count = binaryReader.ReadInt32();
            recentHitNPCs.Clear();
            for (int i = 0; i < count; i++)
            {
                recentHitNPCs.Add(binaryReader.ReadInt32());
            }

            if (isRicochet)
            {
                projectile.penetrate = 1;
            }
        }

        private bool IsBlacklistedForRicochet(Projectile projectile)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config.roleSettings.DesperadoRicochetBlacklist == null)
                return false;

            return IsProjectileInList(projectile, config.roleSettings.DesperadoRicochetBlacklist);
        }

        private bool IsBlacklistedForExtraProjectiles(Projectile projectile)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config.roleSettings.DesperadoExtraProjectileBlacklist == null)
                return false;

            return IsProjectileInList(projectile, config.roleSettings.DesperadoExtraProjectileBlacklist);
        }

        private bool IsProjectileInList(Projectile projectile, System.Collections.Generic.List<string> list)
        {
            if (list == null || list.Count == 0)
                return false;

            // 1. Check Projectile type (numeric ID)
            if (list.Contains(projectile.type.ToString()))
                return true;

            // 2. Check Projectile Name
            if (projectile.ModProjectile != null)
            {
                if (list.Contains(projectile.ModProjectile.Mod.Name + "/" + projectile.ModProjectile.Name) ||
                    list.Contains(projectile.ModProjectile.Name))
                    return true;
            }
            else if (ProjectileID.Search.ContainsId(projectile.type))
            {
                if (list.Contains(ProjectileID.Search.GetName(projectile.type)))
                    return true;
            }

            // 3. Check Source Weapon (numeric ID)
            if (sourceItemType > ItemID.None && ContentSamples.ItemsByType.TryGetValue(sourceItemType, out Item weaponItem))
            {
                if (weaponItem != null)
                {
                    if (list.Contains(sourceItemType.ToString()))
                        return true;

                    if (weaponItem.ModItem != null)
                    {
                        if (list.Contains(weaponItem.ModItem.Mod.Name + "/" + weaponItem.ModItem.Name) ||
                            list.Contains(weaponItem.ModItem.Name))
                            return true;
                    }
                    else if (ItemID.Search.ContainsId(sourceItemType))
                    {
                        if (list.Contains(ItemID.Search.GetName(sourceItemType)))
                            return true;
                    }
                }
            }

            return false;
        }

        private NPC FindNearestNPCTarget(NPC currentTarget, float maxRange)
        {
            NPC nearestNPC = null;
            float nearestDistance = maxRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && npc.whoAmI != currentTarget.whoAmI && !recentHitNPCs.Contains(npc.whoAmI))
                {
                    // Check if it's a target dummy (vanilla or modded)
                    bool isDummy = npc.type == NPCID.TargetDummy || 
                                   (npc.ModNPC != null && npc.ModNPC.GetType().Name.Contains("Dummy", StringComparison.OrdinalIgnoreCase));

                    // Check if target is valid
                    bool isValid;
                    if (StatariaLogger.GlobalDebugMode && isDummy)
                    {
                        isValid = true;
                    }
                    else
                    {
                        isValid = !npc.friendly && npc.lifeMax > 5 && !isDummy;
                    }

                    if (isValid)
                    {
                        float distance = Vector2.Distance(currentTarget.Center, npc.Center);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestNPC = npc;
                        }
                    }
                }
            }

            return nearestNPC;
        }
    }
}
