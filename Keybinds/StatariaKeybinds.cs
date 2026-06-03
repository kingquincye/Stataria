using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace Stataria
{
    public class StatariaKeybinds : ModSystem
    {
        public static ModKeybind ToggleStatariaUI;
        public static ModKeybind TeleportKey;
        public static ModKeybind DivineInterventionKey;
        public static ModKeybind DivineResurrectionKey;
        public static ModKeybind SoulRecallKey;
        public static ModKeybind SavageRoarKey;
        public static ModKeybind ElementalDischargeKey;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ToggleStatariaUI = KeybindLoader.RegisterKeybind(Mod, "Toggle Stataria UI", "K");
            TeleportKey = KeybindLoader.RegisterKeybind(Mod, "AGI Teleport", "Q");
            DivineInterventionKey = KeybindLoader.RegisterKeybind(Mod, "Divine Intervention", "G");
            DivineResurrectionKey = KeybindLoader.RegisterKeybind(Mod, "Divine Resurrection", "X");
            SoulRecallKey = KeybindLoader.RegisterKeybind(Mod, "Soul Recall", "C");
            SavageRoarKey = KeybindLoader.RegisterKeybind(Mod, "Savage Roar", "V");
            ElementalDischargeKey = KeybindLoader.RegisterKeybind(Mod, "Elemental Discharge", "Z");
        }

        public override void Unload()
        {
            ToggleStatariaUI = null;
            TeleportKey = null;
            DivineInterventionKey = null;
            DivineResurrectionKey = null;
            SoulRecallKey = null;
            SavageRoarKey = null;
            ElementalDischargeKey = null;
        }
    }
}