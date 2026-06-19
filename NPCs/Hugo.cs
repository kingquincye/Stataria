using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Personalities;
using System.Collections.Generic;
using Terraria.GameContent.Events;
using Terraria.Localization;
using Stataria.Items.Cores;
using Terraria.Utilities;

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
            return true;
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