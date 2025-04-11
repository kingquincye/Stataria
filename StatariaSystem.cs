using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class StatariaSystem : ModSystem
    {
        public override void PostUpdateInput()
        {
            // Do not process UI input on dedicated servers
            if (Main.dedServ)
                return;
            if (StatariaKeybinds.ToggleStatUI.JustPressed)
            {
                if (StatariaUI.StatUI.CurrentState == null)
                {
                    StatariaUI.StatUI.SetState(StatariaUI.Panel);
                }
                else
                {
                    StatariaUI.StatUI.SetState(null);
                }
            }
        }
    }
}