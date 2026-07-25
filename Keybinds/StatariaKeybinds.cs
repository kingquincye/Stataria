using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace Stataria
{
    public class StatariaKeybinds : ModSystem
    {
        public static ModKeybind ToggleStatariaUI;
        public static ModKeybind ToggleAdaptationUI;
        public static ModKeybind TeleportKey;
        public static ModKeybind DivineInterventionKey;
        public static ModKeybind DivineResurrectionKey;
        public static ModKeybind SoulRecallKey;
        public static ModKeybind SavageRoarKey;
        public static ModKeybind ElementalDischargeKey;
        public static ModKeybind MortalDrawKey;
        public static ModKeybind DesperadoActiveKey;
        public static ModKeybind FleshCloneKey;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ToggleStatariaUI = KeybindLoader.RegisterKeybind(Mod, "Toggle Stataria UI", "K");
            ToggleAdaptationUI = KeybindLoader.RegisterKeybind(Mod, "Toggle Adaptations UI", "O");
            TeleportKey = KeybindLoader.RegisterKeybind(Mod, "AGI Teleport", "Q");
            DivineInterventionKey = KeybindLoader.RegisterKeybind(Mod, "Divine Intervention", "G");
            DivineResurrectionKey = KeybindLoader.RegisterKeybind(Mod, "Divine Resurrection", "X");
            SoulRecallKey = KeybindLoader.RegisterKeybind(Mod, "Soul Recall", "C");
            SavageRoarKey = KeybindLoader.RegisterKeybind(Mod, "Savage Roar", "V");
            ElementalDischargeKey = KeybindLoader.RegisterKeybind(Mod, "Elemental Discharge", "Z");
            MortalDrawKey = KeybindLoader.RegisterKeybind(Mod, "Mortal Draw", "H");
            DesperadoActiveKey = KeybindLoader.RegisterKeybind(Mod, "Desperado Showdown", "F");
            FleshCloneKey = KeybindLoader.RegisterKeybind(Mod, "Flesh Clone", "B");
        }

        public override void Unload()
        {
            ToggleStatariaUI = null;
            ToggleAdaptationUI = null;
            TeleportKey = null;
            DivineInterventionKey = null;
            DivineResurrectionKey = null;
            SoulRecallKey = null;
            SavageRoarKey = null;
            ElementalDischargeKey = null;
            MortalDrawKey = null;
            DesperadoActiveKey = null;
            FleshCloneKey = null;
        }
    }
}