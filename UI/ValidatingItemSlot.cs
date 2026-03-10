using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;
using Terraria.Audio;
using Terraria.ID;

namespace Stataria
{
    public class ValidatingItemSlot : UIElement
    {
        private Item[] _itemArray;
        private int _itemIndex;
        private int _itemSlotContext;
        public Func<Item, bool> ValidItemFunc { get; set; }

        private bool _isMouseDown = false;
        private bool _wasMouseDown = false;

        public ValidatingItemSlot(Item[] itemArray, int itemIndex, int itemSlotContext = 0)
        {
            _itemArray = itemArray;
            _itemIndex = itemIndex;
            _itemSlotContext = itemSlotContext;
            Width = new StyleDimension(52f, 0f);
            Height = new StyleDimension(52f, 0f);
        }

        public Item Item => _itemArray[_itemIndex];

        private void HandleItemSlotLogic()
        {
            if (!IsMouseHovering)
            {
                return;
            }

            Main.LocalPlayer.mouseInterface = true;
            Item currentItem = _itemArray[_itemIndex];

            if (currentItem.type > ItemID.None && currentItem.stack > 0)
            {
                Main.hoverItemName = currentItem.Name;
                if (currentItem.stack > 1)
                    Main.hoverItemName = Main.hoverItemName + " (" + currentItem.stack + ")";
                Main.HoverItem = currentItem.Clone();
            }

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                HandleLeftClick();
            }

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                HandleRightClick();
            }
        }

        private void HandleLeftClick()
        {
            Item currentItem = _itemArray[_itemIndex];
            Item mouseItem = Main.mouseItem;
            bool itemChanged = false;

            if ((currentItem.IsAir || currentItem.stack <= 0) && !mouseItem.IsAir)
            {
                if (ValidItemFunc == null || ValidItemFunc(mouseItem))
                {
                    _itemArray[_itemIndex] = mouseItem.Clone();
                    Main.mouseItem = new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                    itemChanged = true;
                }
            }
            else if (!currentItem.IsAir && mouseItem.IsAir)
            {
                Main.mouseItem = currentItem.Clone();
                _itemArray[_itemIndex] = new Item();
                SoundEngine.PlaySound(SoundID.Grab);
                itemChanged = true;
            }
            else if (!currentItem.IsAir && !mouseItem.IsAir)
            {
                if (currentItem.type == mouseItem.type && currentItem.stack < currentItem.maxStack && mouseItem.stack < mouseItem.maxStack)
                {
                    int transferAmount = Math.Min(currentItem.maxStack - currentItem.stack, mouseItem.stack);
                    currentItem.stack += transferAmount;
                    mouseItem.stack -= transferAmount;

                    if (mouseItem.stack <= 0)
                        Main.mouseItem = new Item();

                    _itemArray[_itemIndex] = currentItem;
                    SoundEngine.PlaySound(SoundID.Grab);
                    itemChanged = true;
                }
                else
                {
                    if (ValidItemFunc == null || ValidItemFunc(mouseItem))
                    {
                        _itemArray[_itemIndex] = mouseItem.Clone();
                        Main.mouseItem = currentItem.Clone();
                        SoundEngine.PlaySound(SoundID.Grab);
                        itemChanged = true;
                    }
                }
            }

            if (itemChanged && Main.netMode != NetmodeID.SinglePlayer)
            {
                Item newItem = _itemArray[_itemIndex];
                if (!newItem.IsAir && SocketingGlobalItem.IsWeapon(newItem))
                {
                    SocketingGlobalItem.SyncSocketedItem(Main.LocalPlayer, newItem, -1);
                }
            }
        }

        private void HandleRightClick()
        {
            Item currentItem = _itemArray[_itemIndex];
            Item mouseItem = Main.mouseItem;

            if (!currentItem.IsAir && currentItem.stack > 0)
            {
                if (mouseItem.IsAir)
                {
                    Main.mouseItem = currentItem.Clone();
                    Main.mouseItem.stack = 1;
                    currentItem.stack--;

                    if (currentItem.stack <= 0)
                        _itemArray[_itemIndex] = new Item();
                    else
                        _itemArray[_itemIndex] = currentItem;

                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                else if (mouseItem.type == currentItem.type && mouseItem.stack < mouseItem.maxStack)
                {
                    mouseItem.stack++;
                    currentItem.stack--;

                    if (currentItem.stack <= 0)
                        _itemArray[_itemIndex] = new Item();
                    else
                        _itemArray[_itemIndex] = currentItem;

                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            else if (currentItem.IsAir && !mouseItem.IsAir && mouseItem.stack > 1)
            {
                if (ValidItemFunc == null || ValidItemFunc(mouseItem))
                {
                    Item newItem = mouseItem.Clone();
                    newItem.stack = 1;
                    _itemArray[_itemIndex] = newItem;
                    mouseItem.stack--;

                    if (mouseItem.stack <= 0)
                        Main.mouseItem = new Item();

                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            HandleItemSlotLogic();

            Item currentItem = _itemArray[_itemIndex];
            CalculatedStyle dimensions = GetDimensions();

            Vector2 position = dimensions.Position();
            Vector2 size = new Vector2(dimensions.Width, dimensions.Height);

            float targetSize = Math.Min(size.X, size.Y);
            float scale = targetSize / 52f;

            Texture2D backgroundTexture = Terraria.GameContent.TextureAssets.InventoryBack4.Value;
            Color backgroundColor = Color.White;

            if (IsMouseHovering)
            {
                backgroundColor = Color.LightBlue * 0.8f;
            }

            spriteBatch.Draw(backgroundTexture,
                position + size * 0.5f,
                null,
                backgroundColor,
                0f,
                backgroundTexture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

            if (!currentItem.IsAir && currentItem.stack > 0)
            {
                Vector2 itemPosition = position + size * 0.5f;

                float maxItemSize = targetSize * 0.75f;
                Texture2D itemTexture = Terraria.GameContent.TextureAssets.Item[currentItem.type].Value;
                float itemScale = Math.Min(maxItemSize / itemTexture.Width, maxItemSize / itemTexture.Height);
                itemScale = Math.Min(itemScale, scale * 0.9f);

                float finalItemScale = ItemSlot.DrawItemIcon(currentItem, _itemSlotContext, spriteBatch,
                    itemPosition, itemScale, maxItemSize, Color.White);

                if (currentItem.stack > 1)
                {
                    Vector2 stackPosition = position + new Vector2(
                        size.X - 20f * scale,
                        size.Y - 14f * scale
                    );

                    stackPosition.X = Math.Max(stackPosition.X, position.X + 2f * scale);
                    stackPosition.Y = Math.Max(stackPosition.Y, position.Y + size.Y - 16f * scale);

                    Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch,
                        Terraria.GameContent.FontAssets.ItemStack.Value,
                        currentItem.stack.ToString(),
                        stackPosition,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        new Vector2(scale * 0.7f),
                        -1f,
                        scale);
                }
            }
        }
    }
}