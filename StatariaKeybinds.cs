using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace Stataria
{
    public class StatariaKeybinds : ModSystem
    {
        public static ModKeybind ToggleStatUI;

        public override void Load()
        {
            // Only register keybinds on non-dedicated servers
            if (Main.dedServ)
                return;
            ToggleStatUI = KeybindLoader.RegisterKeybind(Mod, "Toggle Stat Panel", "K");
        }

        public override void Unload()
        {
            ToggleStatUI = null;
        }
    }
}