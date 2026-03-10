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

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ToggleStatariaUI = KeybindLoader.RegisterKeybind(Mod, "Toggle Stataria UI", "K");
            TeleportKey = KeybindLoader.RegisterKeybind(Mod, "AGI Teleport", "Q");
            DivineInterventionKey = KeybindLoader.RegisterKeybind(Mod, "Divine Intervention", "G");
        }

        public override void Unload()
        {
            ToggleStatariaUI = null;
            TeleportKey = null;
            DivineInterventionKey = null;
        }
    }
}