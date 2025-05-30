using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria
{
    public static class BossHealthHelper
    {
        private static Dictionary<int, HashSet<int>> bossGroups = new Dictionary<int, HashSet<int>>();
        private static Dictionary<int, int> bossMainParts = new Dictionary<int, int>();
        private static Dictionary<int, int> initialMaxHealth = new Dictionary<int, int>();
        private static Dictionary<int, int> highestMaxHealth = new Dictionary<int, int>();
        private static Dictionary<int, bool> activeBossEncounters = new Dictionary<int, bool>();

        static BossHealthHelper()
        {
            InitializeVanillaBosses();
        }

        public static void InitializeVanillaBosses()
        {
            bossGroups.Clear();
            bossMainParts.Clear();
            initialMaxHealth.Clear();
            highestMaxHealth.Clear();
            activeBossEncounters.Clear();

            RegisterBossGroup(NPCID.EaterofWorldsHead, new int[] {
                NPCID.EaterofWorldsHead,
                NPCID.EaterofWorldsBody,
                NPCID.EaterofWorldsTail
            });

            RegisterBossGroup(NPCID.BrainofCthulhu, new int[] {
                NPCID.BrainofCthulhu,
                NPCID.Creeper
            });

            RegisterBossGroup(NPCID.Golem, new int[] {
                NPCID.GolemHead,
                NPCID.Golem
            });

            RegisterBossGroup(NPCID.MoonLordHead, new int[] {
                NPCID.MoonLordCore,
                NPCID.MoonLordHand,
                NPCID.MoonLordHead
            });

            RegisterBossGroup(NPCID.Retinazer, new int[] {
                NPCID.Retinazer,
                NPCID.Spazmatism
            });

            RegisterBossGroup(NPCID.MartianSaucer, new int[] {
                NPCID.MartianSaucerCannon,
                NPCID.MartianSaucerTurret,
                NPCID.MartianSaucerCore
            });

            RegisterBossGroup(NPCID.PirateShip, new int[] {
                NPCID.PirateShipCannon
            });
        }

        public static void RegisterBossGroup(int mainNpcId, int[] groupNpcIds)
        {
            if (!bossGroups.ContainsKey(mainNpcId))
            {
                bossGroups[mainNpcId] = new HashSet<int>();
            }

            foreach (int npcId in groupNpcIds)
            {
                bossGroups[mainNpcId].Add(npcId);
                bossMainParts[npcId] = mainNpcId;
            }
        }

        public static void RegisterModdedBossGroup(string modName, int mainNpcId, int[] groupNpcIds)
        {
            RegisterBossGroup(mainNpcId, groupNpcIds);
            ModContent.GetInstance<Stataria>().Logger.Info($"Registered modded boss group from {modName} with main ID: {mainNpcId}");
        }

        public static bool IsBossPart(int npcId)
        {
            return bossMainParts.ContainsKey(npcId);
        }

        public static bool IsBoss(NPC npc)
        {
            if (npc.type == NPCID.TorchGod)
                return false;

            if (npc.boss)
                return true;

            if (NPCID.Sets.BossHeadTextures[npc.type] >= 0)
                return true;

            switch (npc.type)
            {
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                    return true;
            }

            return IsBossPart(npc.type);
        }

        public static void ResetBossEncounter(int mainPartId)
        {
            initialMaxHealth.Remove(mainPartId);
            highestMaxHealth.Remove(mainPartId);
            activeBossEncounters[mainPartId] = false;
        }

        public static void StartBossEncounter(int mainPartId)
        {
            activeBossEncounters[mainPartId] = true;
        }

        public static int GetMainPartId(int npcId)
        {
            return bossMainParts.TryGetValue(npcId, out int mainId) ? mainId : npcId;
        }

        public static (int current, int max) GetBossGroupHealth(int mainPartId)
        {
            int currentHealth = 0;
            int maxHealth = 0;
            bool foundAnyActive = false;

            if (bossGroups.TryGetValue(mainPartId, out HashSet<int> parts))
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && parts.Contains(npc.type))
                    {
                        currentHealth += npc.life;
                        maxHealth += npc.lifeMax;
                        foundAnyActive = true;
                    }
                }
            }

            if (!foundAnyActive)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.type == mainPartId)
                    {
                        currentHealth += npc.life;
                        maxHealth += npc.lifeMax;
                    }
                }
            }

            if (!initialMaxHealth.ContainsKey(mainPartId) && maxHealth > 0)
            {
                initialMaxHealth[mainPartId] = maxHealth;
                highestMaxHealth[mainPartId] = maxHealth;
                StartBossEncounter(mainPartId);
            }
            else if (maxHealth > highestMaxHealth.GetValueOrDefault(mainPartId, 0))
            {
                highestMaxHealth[mainPartId] = maxHealth;
            }

            int finalMaxHealth = highestMaxHealth.GetValueOrDefault(mainPartId, maxHealth);

            if (finalMaxHealth == 0)
                finalMaxHealth = 1;

            return (currentHealth, finalMaxHealth);
        }

        public static bool IsBossEncounterActive(int mainPartId)
        {
            return activeBossEncounters.GetValueOrDefault(mainPartId, false);
        }

        public static void CleanupInactiveBossEncounters()
        {
            List<int> inactiveBosses = new List<int>();

            foreach (var kvp in activeBossEncounters)
            {
                if (kvp.Value)
                {
                    bool anyPartActive = false;

                    if (bossGroups.TryGetValue(kvp.Key, out HashSet<int> parts))
                    {
                        foreach (int partType in parts)
                        {
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active && Main.npc[i].type == partType)
                                {
                                    anyPartActive = true;
                                    break;
                                }
                            }
                            if (anyPartActive) break;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && Main.npc[i].type == kvp.Key)
                            {
                                anyPartActive = true;
                                break;
                            }
                        }
                    }

                    if (!anyPartActive)
                    {
                        inactiveBosses.Add(kvp.Key);
                    }
                }
            }

            foreach (int bossId in inactiveBosses)
            {
                ResetBossEncounter(bossId);
            }
        }
    }
}