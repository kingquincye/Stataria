using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace Stataria
{
    public class StatariaKeybinds : ModSystem
    {
        public static ModKeybind ToggleStatUI;
        public static ModKeybind TeleportKey;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ToggleStatUI = KeybindLoader.RegisterKeybind(Mod, "Toggle Stat Panel", "K");
            TeleportKey = KeybindLoader.RegisterKeybind(Mod, "AGI Teleport", "Q");
        }

        public override void Unload()
        {
            ToggleStatUI = null;
            TeleportKey = null;
        }
    }
}