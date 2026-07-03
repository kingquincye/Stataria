using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.Localization;
using Stataria;

namespace Stataria.UI.Elements
{
    public class UIListEditor : UIElement
    {
        private PropertyFieldWrapper _property;
        private object _configInstance;
        private string _label;

        private UIList _itemsList;
        private UIScrollbar _scrollbar;

        private bool _typingNewItem;
        private string _typedNewItem = "";

        private bool _isExpanded = false;
        private bool _wasMouseDown;
        private Terraria.ModLoader.Config.ModConfig _rootConfig;
        private bool _showAddHeld;
        private string _tooltip;
        private bool _reloadRequired;
        private Action<string, bool> _onHover;

        public UIListEditor(string label, PropertyFieldWrapper property, object configInstance, Terraria.ModLoader.Config.ModConfig rootConfig, string tooltip, bool reloadRequired, bool showAddHeld, System.Action<string, bool> onHoverUpdate)
        {
            _label = label;
            _property = property;
            _configInstance = configInstance;
            _rootConfig = rootConfig;
            _tooltip = tooltip;
            _reloadRequired = reloadRequired;
            _showAddHeld = showAddHeld;
            _onHover = onHoverUpdate;

            Width.Set(0, 1f);
            Height.Set(40, 0f); // Collapsed height

            _itemsList = new UIList();
            _itemsList.Top.Set(40, 0f);
            _itemsList.Width.Set(-20, 1f);
            _itemsList.Height.Set(-80, 1f);
            _itemsList.ListPadding = 2f;

            _scrollbar = new UIScrollbar();
            _scrollbar.SetView(100f, 1000f);
            _scrollbar.Height.Set(-80, 1f);
            _scrollbar.Top.Set(40, 0f);
            _scrollbar.HAlign = 1f;
            _itemsList.SetScrollbar(_scrollbar);

            RebuildList();
        }

        private IList GetList()
        {
            return (IList)_property.GetValue(_configInstance);
        }

        private void ToggleExpand()
        {
            _isExpanded = !_isExpanded;
            if (_isExpanded)
            {
                Height.Set(200, 0f);
                Append(_itemsList);
                Append(_scrollbar);
            }
            else
            {
                Height.Set(40, 0f);
                RemoveChild(_itemsList);
                RemoveChild(_scrollbar);
            }
            
            if (Parent != null)
            {
                Parent.Recalculate();
            }
        }

        private void RebuildList()
        {
            _itemsList.Clear();
            IList list = GetList();
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                int index = i; // capture
                object item = list[i];

                UIPanel itemPanel = new UIPanel();
                itemPanel.Width.Set(0, 1f);
                itemPanel.Height.Set(30, 0f);
                itemPanel.BackgroundColor = new Color(40, 50, 80);

                UIText itemText = new UIText(item.ToString());
                itemText.VAlign = 0.5f;
                itemPanel.Append(itemText);

                UITextPanel<string> deleteBtn = new UITextPanel<string>("X", 0.8f); // Scaled down text
                deleteBtn.PaddingTop = 4f;
                deleteBtn.PaddingBottom = 4f;
                deleteBtn.PaddingLeft = 8f;
                deleteBtn.PaddingRight = 8f;
                deleteBtn.BackgroundColor = new Color(150, 50, 50);
                deleteBtn.Width.Set(26, 0f);
                deleteBtn.Height.Set(26, 0f);
                deleteBtn.HAlign = 1f;
                deleteBtn.VAlign = 0.5f;
                deleteBtn.OnMouseOver += (evt, el) => {
                    if (CustomConfigUIState.DialogOpen) return;
                    deleteBtn.BackgroundColor = new Color(200, 70, 70);
                };
                deleteBtn.OnMouseOut += (evt, el) => {
                    if (CustomConfigUIState.DialogOpen) return;
                    deleteBtn.BackgroundColor = new Color(150, 50, 50);
                };
                deleteBtn.OnLeftClick += (evt, el) =>
                {
                    if (CustomConfigUIState.DialogOpen) return;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    GetList().RemoveAt(index);
                    if (_rootConfig != null) 
                    {
                        var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (saveMethod != null) saveMethod.Invoke(null, new object[] { _rootConfig });
                    }
                    RebuildList();
                };
                itemPanel.Append(deleteBtn);

                _itemsList.Add(itemPanel);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();

            // Draw Background and Label
            Utils.DrawInvBG(spriteBatch, dimensions.ToRectangle(), new Color(30, 15, 45)); // Dark purple bg
            Vector2 textPos = new Vector2(dimensions.X + 40, dimensions.Y + 12); // Moved right to avoid [+], moved down
            Utils.DrawBorderString(spriteBatch, _label, textPos, Color.White, 0.9f); // Scaled down text

            // Draw Toggle button [+] / [-]
            string toggleText = _isExpanded ? "[-]" : "[+]";
            Vector2 togglePos = new Vector2(dimensions.X + 10, dimensions.Y + 12);
            Utils.DrawBorderString(spriteBatch, toggleText, togglePos, Color.White, 0.9f);
            
            Rectangle toggleRect = new Rectangle((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, 40);

            if (!CustomConfigUIState.DialogOpen && IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                _onHover?.Invoke(_tooltip, _reloadRequired);
            }

            bool isMouseDown = Main.mouseLeft;

            if (CustomConfigUIState.DialogOpen)
            {
                if (_typingNewItem)
                {
                    _typingNewItem = false;
                    if (Main.CurrentInputTextTakerOverride == this)
                        Main.CurrentInputTextTakerOverride = null;
                }
            }

            // Expand Toggle Logic
            if (!CustomConfigUIState.DialogOpen && isMouseDown && !_wasMouseDown && toggleRect.Contains(Main.MouseScreen.ToPoint()))
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                ToggleExpand();
            }

            if (_isExpanded)
            {
                // Draw "Add New Item" Textbox area at the bottom
                int buttonY = (int)(dimensions.Y + dimensions.Height - 35);
                Rectangle addInputRect;
                Rectangle addBtnRect;
                Rectangle addHeldBtnRect = Rectangle.Empty;

                if (_showAddHeld)
                {
                    addInputRect = new Rectangle((int)dimensions.X + 10, buttonY, (int)dimensions.Width - 220, 26);
                    addBtnRect = new Rectangle((int)addInputRect.Right + 10, buttonY, 70, 26);
                    addHeldBtnRect = new Rectangle((int)addBtnRect.Right + 10, buttonY, 110, 26);
                }
                else
                {
                    addInputRect = new Rectangle((int)dimensions.X + 10, buttonY, (int)dimensions.Width - 100, 26);
                    addBtnRect = new Rectangle((int)addInputRect.Right + 10, buttonY, 70, 26);
                }

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, addInputRect, (_typingNewItem && !CustomConfigUIState.DialogOpen) ? new Color(80, 40, 100) : new Color(40, 20, 50)); // Purple text box
                
                string displayVal = Language.GetTextValue("Mods.Stataria.UI.TypeListPrompt");
                if (_typingNewItem && !CustomConfigUIState.DialogOpen)
                {
                    displayVal = _typedNewItem;
                    if (Main.GameUpdateCount % 40 < 20)
                    {
                        displayVal += "|";
                    }
                    Utils.DrawBorderString(spriteBatch, displayVal, new Vector2(addInputRect.X + 5, addInputRect.Y + 5), Color.White, 0.9f);
                }
                else
                {
                     Utils.DrawBorderString(spriteBatch, displayVal, new Vector2(addInputRect.X + 5, addInputRect.Y + 5), Color.Gray, 0.9f);
                }

                // Draw Add Button
                bool hoverAdd = !CustomConfigUIState.DialogOpen && addBtnRect.Contains(Main.MouseScreen.ToPoint());
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, addBtnRect, hoverAdd ? new Color(170, 50, 200) : new Color(120, 30, 150)); // Bright purple add button
                Utils.DrawBorderString(spriteBatch, "Add", new Vector2(addBtnRect.X + 20, addBtnRect.Y + 5), Color.White, 0.9f);

                // Draw Add Held Button
                if (_showAddHeld)
                {
                    bool hoverHeld = !CustomConfigUIState.DialogOpen && addHeldBtnRect.Contains(Main.MouseScreen.ToPoint());
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, addHeldBtnRect, hoverHeld ? new Color(170, 50, 200) : new Color(120, 30, 150));
                    Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.Stataria.UI.AddHeldButton"), new Vector2(addHeldBtnRect.X + 15, addHeldBtnRect.Y + 5), Color.White, 0.9f);

                    if (hoverHeld)
                    {
                        string helpText = Language.GetTextValue("Mods.Stataria.UI.AddHeldTooltip");
                        if (Main.gameMenu)
                        {
                            helpText = Language.GetTextValue("Mods.Stataria.UI.AddHeldTooltipMenu");
                        }
                        else if (Main.LocalPlayer == null || Main.LocalPlayer.HeldItem == null || Main.LocalPlayer.HeldItem.IsAir)
                        {
                            helpText = Language.GetTextValue("Mods.Stataria.UI.AddHeldTooltipEmpty");
                        }
                        else
                        {
                            helpText = Language.GetTextValue("Mods.Stataria.UI.AddHeldTooltipClick", Main.LocalPlayer.HeldItem.Name);
                        }
                        _onHover?.Invoke(helpText, false);
                    }
                }

                // Input handling
                if (!CustomConfigUIState.DialogOpen && isMouseDown && !_wasMouseDown)
                {
                    if (addInputRect.Contains(Main.MouseScreen.ToPoint()))
                    {
                        _typingNewItem = true;
                        Main.CurrentInputTextTakerOverride = this;
                        Main.clrInput();
                    }
                    else if (addBtnRect.Contains(Main.MouseScreen.ToPoint()))
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        AddNewItem();
                    }
                    else if (_showAddHeld && addHeldBtnRect.Contains(Main.MouseScreen.ToPoint()))
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        AddHeldItem();
                    }
                    else if (!toggleRect.Contains(Main.MouseScreen.ToPoint())) // Don't cancel typing if clicking toggle
                    {
                        _typingNewItem = false;
                        if (Main.CurrentInputTextTakerOverride == this)
                            Main.CurrentInputTextTakerOverride = null;
                    }
                }

                if (_typingNewItem && !CustomConfigUIState.DialogOpen)
                {
                    Terraria.GameInput.PlayerInput.WritingText = true;
                    Main.instance.HandleIME();
                    string newText = Main.GetInputText(_typedNewItem);
                    
                    if (Main.inputTextEscape)
                    {
                        _typingNewItem = false;
                        if (Main.CurrentInputTextTakerOverride == this)
                            Main.CurrentInputTextTakerOverride = null;
                    }
                    else if (Main.inputTextEnter)
                    {
                        AddNewItem();
                    }
                    else
                    {
                        _typedNewItem = newText;
                    }
                }
            }

            if (!_typingNewItem && Main.CurrentInputTextTakerOverride == this)
            {
                Main.CurrentInputTextTakerOverride = null;
            }

            _wasMouseDown = isMouseDown;
        }

        private void AddNewItem()
        {
            if (string.IsNullOrWhiteSpace(_typedNewItem)) return;

            IList list = GetList();
            Type elementType = _property.Type.GetGenericArguments()[0];

            try
            {
                if (elementType == typeof(int))
                {
                    if (int.TryParse(_typedNewItem, out int parsed))
                    {
                        list.Add(parsed);
                    }
                }
                else if (elementType == typeof(string))
                {
                    list.Add(_typedNewItem);
                }

                _typedNewItem = "";
                _typingNewItem = false;
                if (Main.CurrentInputTextTakerOverride == this)
                    Main.CurrentInputTextTakerOverride = null;
                if (_rootConfig != null) 
                {
                    var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (saveMethod != null) saveMethod.Invoke(null, new object[] { _rootConfig });
                }
                RebuildList();
            }
            catch { }
        }

        private void AddHeldItem()
        {
            if (Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null || player.HeldItem == null || player.HeldItem.IsAir)
                return;

            _typingNewItem = false;
            if (Main.CurrentInputTextTakerOverride == this)
                Main.CurrentInputTextTakerOverride = null;

            Item item = player.HeldItem;
            string itemName = "";

            if (item.ModItem != null)
            {
                itemName = item.ModItem.Mod.Name + "/" + item.ModItem.Name;
            }
            else
            {
                if (ItemID.Search.ContainsId(item.type))
                {
                    itemName = ItemID.Search.GetName(item.type);
                }
            }

            if (!string.IsNullOrEmpty(itemName))
            {
                IList list = GetList();
                if (list != null && !list.Contains(itemName))
                {
                    list.Add(itemName);
                    if (_rootConfig != null) 
                    {
                        var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (saveMethod != null) saveMethod.Invoke(null, new object[] { _rootConfig });
                    }
                    RebuildList();
                }
            }
        }
    }
}
