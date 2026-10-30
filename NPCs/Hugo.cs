using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Personalities;
using System.Collections.Generic;
using Terraria.GameContent.Events;
using Terraria.Localization;
using Stataria.Items.Cores;
using Terraria.Utilities;
using Microsoft.Xna.Framework;

namespace Stataria.NPCs
{
    [AutoloadHead]
    public class Hugo : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 25;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.HatOffsetY[Type] = 4;

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f,
                Direction = 1
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            // NPC happiness
            NPC.Happiness
                .SetBiomeAffection<ForestBiome>(AffectionLevel.Love)
                .SetBiomeAffection<SnowBiome>(AffectionLevel.Like)
                .SetBiomeAffection<DesertBiome>(AffectionLevel.Dislike)
                .SetNPCAffection(NPCID.Merchant, AffectionLevel.Like)
                .SetNPCAffection(NPCID.GoblinTinkerer, AffectionLevel.Love)
                .SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Dislike);

            if (WrathOfTheGodsSupportHelper.WotGLoaded)
            {
                WrathOfTheGodsSupportHelper.RegisterHugoReflection(Type);
            }
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 0;
            NPC.defense = 9999;
            NPC.lifeMax = 26000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;

            AnimationType = NPCID.Guide;
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 0.01f;
        }

        public override void OnKill()
        {
            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            if (!config.EnableSocketingSystem || !config.EnableHugoSpawn)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int spawnX = (int)(NPC.position.X);
                int spawnY = (int)(NPC.position.Y);
                
                if (!NPC.homeless)
                {
                    spawnX = NPC.homeTileX * 16;
                    spawnY = NPC.homeTileY * 16;
                }
                
                int npcIndex = NPC.NewNPC(NPC.GetSource_Death(), spawnX, spawnY, Type);
                if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                {
                    Main.npc[npcIndex].homeless = NPC.homeless;
                    Main.npc[npcIndex].homeTileX = NPC.homeTileX;
                    Main.npc[npcIndex].homeTileY = NPC.homeTileY;
                }
            }
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            return config.EnableSocketingSystem && config.EnableHugoSpawn;
        }

        private int sprayReactionCooldown = 0;
        private int sprayStreak = 0;
        private int sprayResetTimer = 0;

        public override void PostAI()
        {
            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            if (!config.EnableSocketingSystem || !config.EnableHugoSpawn)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                    }
                }
                NPC.active = false;
                NPC.netSkip = -1;
                NPC.life = 0;
                return;
            }

            if (sprayReactionCooldown > 0)
                sprayReactionCooldown--;

            if (sprayResetTimer > 0)
            {
                sprayResetTimer--;
                if (sprayResetTimer == 0)
                    sprayStreak = 0;
            }

            if (WrathOfTheGodsSupportHelper.WotGLoaded && sprayReactionCooldown <= 0)
            {
                CheckSprayReflection();
            }
        }

        private void CheckSprayReflection()
        {
            int gasType = WrathOfTheGodsSupportHelper.EmptinessSprayerGasProjType;
            if (gasType <= 0)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == gasType && p.Hitbox.Intersects(NPC.Hitbox))
                {
                    sprayStreak++;
                    sprayResetTimer = 360; // 6 seconds without spray to reset Hugo's patience
                    sprayReactionCooldown = 180; // 3 seconds between speech lines

                    if (p.velocity.LengthSquared() < 0.01f)
                    {
                        p.velocity = (p.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 8f;
                    }

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, NPC.Center);

                    for (int d = 0; d < 25; d++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch, 0f, 0f, 150, default, 1.6f);
                        dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                        dust.noGravity = true;
                    }

                    List<string> quotes = GetEscalatingQuotes(sprayStreak);
                    string quote = quotes[Main.rand.Next(quotes.Count)];

                    Color textColor = sprayStreak switch
                    {
                        <= 1 => new Color(210, 140, 255), // Cosmic Purple
                        <= 3 => new Color(180, 180, 255), // Periwinkle Blue
                        <= 6 => new Color(255, 185, 90),  // Amused Gold
                        _ => new Color(255, 130, 150)     // Exasperated Rose
                    };

                    CombatText.NewText(NPC.Hitbox, textColor, quote, dramatic: true);
                    break;
                }
            }
        }

        private List<string> GetEscalatingQuotes(int streak)
        {
            List<string> quotes = new List<string>();

            if (streak <= 1)
            {
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction1"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction2"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction3"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction4"));
                quotes.RemoveAll(q => string.IsNullOrEmpty(q) || q.StartsWith("Mods.Stataria"));
                if (quotes.Count == 0)
                {
                    quotes.Add("Return to sender!");
                    quotes.Add("Did you really think that would work on me?");
                    quotes.Add("The Cores reject your void.");
                    quotes.Add("You cannot unmake what has already adapted.");
                }
            }
            else if (streak <= 3)
            {
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage2_1"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage2_2"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage2_3"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage2_4"));
                quotes.RemoveAll(q => string.IsNullOrEmpty(q) || q.StartsWith("Mods.Stataria"));
                if (quotes.Count == 0)
                {
                    quotes.Add("You do realize you're just spraying yourself, right?");
                    quotes.Add("Still at it?");
                    quotes.Add("We could do this all day, friend.");
                    quotes.Add("My turn again?");
                }
            }
            else if (streak <= 6)
            {
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage3_1"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage3_2"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage3_3"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage3_4"));
                quotes.RemoveAll(q => string.IsNullOrEmpty(q) || q.StartsWith("Mods.Stataria"));
                if (quotes.Count == 0)
                {
                    quotes.Add("Are you having fun?");
                    quotes.Add("Okay, now you're just wasting void fluid.");
                    quotes.Add("I sell cosmic power, I don't succumb to it.");
                    quotes.Add("Does that even tickle you at this point?");
                }
            }
            else
            {
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage4_1"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage4_2"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage4_3"));
                quotes.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.SprayReaction_Stage4_4"));
                quotes.RemoveAll(q => string.IsNullOrEmpty(q) || q.StartsWith("Mods.Stataria"));
                if (quotes.Count == 0)
                {
                    quotes.Add("Seriously? Still?");
                    quotes.Add("I have all the time in the world.");
                    quotes.Add("Are you waiting for something else to happen?");
                    quotes.Add("*sigh*... Need any Cores while you stand there?");
                }
            }

            return quotes;
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string>() { Language.GetTextValue("Mods.Stataria.NPCs.Hugo.DisplayName") };
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Dialogue.Chat1"));
            chat.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Dialogue.Chat2"));
            chat.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Dialogue.Chat3"));
            chat.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Dialogue.Chat4"));

            if (Main.LocalPlayer.GetModPlayer<RPGPlayer>().RebirthCount > 0)
            {
                chat.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Dialogue.ChatRebirth"));
            }

            if (WrathOfTheGodsSupportHelper.WotGLoaded && Main.LocalPlayer != null)
            {
                bool hasSprayer = false;
                for (int i = 0; i < 58; i++)
                {
                    Item it = Main.LocalPlayer.inventory[i];
                    if (it != null && !it.IsAir && it.ModItem != null && it.ModItem.Mod?.Name == "NoxusBoss" &&
                        (it.ModItem.Name == "EmptinessSprayer" || it.ModItem.Name == "NoxusSprayer"))
                    {
                        hasSprayer = true;
                        break;
                    }
                }

                if (hasSprayer)
                {
                    chat.Add(Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Dialogue.ChatEmptinessSprayer"));
                }
            }

            return chat.Get();
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("Mods.Stataria.NPCs.Hugo.Shop");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (firstButton)
            {
                shop = "Shop";
            }
        }

        public override void AddShops()
        {
            var shop = new NPCShop(Type, "Shop")
                .Add<CoreOfPowerT1>()
                .Add<CoreOfForceT1>()
                .Add<CoreOfPrecisionT1>()
                .Add<CoreOfDefenseT1>()
                .Add<CoreOfVitalityT1>()
                .Add<CoreOfEvasionT1>()
                .Add<CoreOfPowerT2>(Condition.DownedEowOrBoc)
                .Add<CoreOfForceT2>(Condition.DownedEowOrBoc)
                .Add<CoreOfPrecisionT2>(Condition.DownedEowOrBoc)
                .Add<CoreOfDefenseT2>(Condition.DownedEowOrBoc)
                .Add<CoreOfVitalityT2>(Condition.DownedEowOrBoc)
                .Add<CoreOfEvasionT2>(Condition.DownedEowOrBoc)
                .Add<CoreOfPowerT3>(Condition.Hardmode)
                .Add<CoreOfForceT3>(Condition.Hardmode)
                .Add<CoreOfPrecisionT3>(Condition.Hardmode)
                .Add<CoreOfDefenseT3>(Condition.Hardmode)
                .Add<CoreOfVitalityT3>(Condition.Hardmode)
                .Add<CoreOfEvasionT3>(Condition.Hardmode)
                .Add<CoreOfPowerT4>(Condition.DownedGolem)
                .Add<CoreOfForceT4>(Condition.DownedGolem)
                .Add<CoreOfPrecisionT4>(Condition.DownedGolem)
                .Add<CoreOfDefenseT4>(Condition.DownedGolem)
                .Add<CoreOfVitalityT4>(Condition.DownedGolem)
                .Add<CoreOfEvasionT4>(Condition.DownedGolem);

            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            if (config.EnableSocketingSystem)
            {
                shop.Register();
            }
        }
    }
}
