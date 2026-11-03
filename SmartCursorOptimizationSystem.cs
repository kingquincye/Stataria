using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace Stataria
{
    /// <summary>
    /// Prevents severe frame rate drops (collapsing to 1 FPS) when holding a torch with Smart Cursor active at high TCH stat values.
    /// In vanilla Terraria, SmartCursorHelper.Step_Torch scans a full 2D area proportional to (2 * tileRangeX) * (2 * tileRangeY).
    /// At 500-1000+ TCH, this area explodes to 44,000 - 170,000 tiles per frame with lighting and line-of-sight raycasts.
    /// This system temporarily clamps the reach specifically during SmartCursorLookup for torches, preserving full reach
    /// for manual placement and all other tools without affecting other systems or mods.
    /// </summary>
    public class SmartCursorOptimizationSystem : ModSystem
    {
        public const int MaxTorchSmartCursorReach = 18;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            On_SmartCursorHelper.SmartCursorLookup += ClampTorchSmartCursorReach;
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            On_SmartCursorHelper.SmartCursorLookup -= ClampTorchSmartCursorReach;
        }

        private static void ClampTorchSmartCursorReach(On_SmartCursorHelper.orig_SmartCursorLookup orig, Player player)
        {
            if (player == null || player.HeldItem == null)
            {
                orig(player);
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config != null && !config.statSettings.TCH_OptimizeTorchSmartCursor)
            {
                orig(player);
                return;
            }

            int createTile = player.HeldItem.createTile;
            int itemType = player.HeldItem.type;
            bool isTorch = (createTile >= 0 && (createTile == TileID.Torches || (createTile < TileID.Sets.Torch.Length && TileID.Sets.Torch[createTile])))
                        || (itemType >= 0 && itemType < ItemID.Sets.Torches.Length && ItemID.Sets.Torches[itemType]);

            if (!isTorch || (Player.tileRangeX <= MaxTorchSmartCursorReach && Player.tileRangeY <= MaxTorchSmartCursorReach))
            {
                orig(player);
                return;
            }

            int originalX = Player.tileRangeX;
            int originalY = Player.tileRangeY;

            Player.tileRangeX = Math.Min(originalX, MaxTorchSmartCursorReach);
            Player.tileRangeY = Math.Min(originalY, MaxTorchSmartCursorReach);

            try
            {
                orig(player);
            }
            finally
            {
                Player.tileRangeX = originalX;
                Player.tileRangeY = originalY;
            }
        }
    }
}
