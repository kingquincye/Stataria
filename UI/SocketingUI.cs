using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stataria.Items.Cores;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Stataria
{
    public class SocketingUI : UIState
    {
        public UIPanel socketingPanel;
        private UIText titleText;
        private UIText rebirthPointsText;

        private ValidatingItemSlot weaponSlot;
        private UIText itemNameText;
        private UIText slotsText;

        private UIPanel compatibleCoresPanel;
        private UIText compatibleCoresTitle;
        private UIList compatibleCoresList;
        private UIScrollbar compatibleCoresScrollbar;

        private UIText attachedCoresTitle;
        private UIList attachedCoresList;

        private UITextPanel<string> expandButton;
        private UITextPanel<string> attachButton;
        private UITextPanel<string> extractButton;
        private UIText expandCostText;
        private UIText extractCostText;

        private SocketedCore? selectedAttachedCore;
        private (CoreType type, int tier)? selectedCompatibleCore;

        private bool dragging = false;
        private Vector2 offset;

        private Item[] socketingItemArray = new Item[1] { new Item() };
        private Item lastItem = new Item();

        public Item SocketingItemSlot => socketingItemArray[0];

        public override void OnInitialize()
        {
            socketingPanel = new UIPanel();
            socketingPanel.Width.Set(800f, 0f);
            socketingPanel.Height.Set(600f, 0f);
            socketingPanel.HAlign = 0.5f;
            socketingPanel.VAlign = 0.5f;
            socketingPanel.SetPadding(15f);
            socketingPanel.BackgroundColor = new Color(25, 35, 60, 240);
            socketingPanel.BorderColor = new Color(100, 120, 180, 255);
            Append(socketingPanel);

            socketingPanel.OnLeftMouseDown += (evt, el) =>
            {
                if (!IsClickingOnInteractiveElement(evt.MousePosition))
                {
                    offset = new Vector2(evt.MousePosition.X - socketingPanel.Left.Pixels, evt.MousePosition.Y - socketingPanel.Top.Pixels);
                    dragging = true;
                }
            };
            socketingPanel.OnLeftMouseUp += (evt, el) => dragging = false;

            InitializeHeader();
            InitializeItemSlot();
            InitializeCompatibleCores();
            InitializeAttachedCores();
            InitializeActionButtons();
        }

        private void InitializeHeader()
        {
            titleText = new UIText("Socketing System", 1.6f);
            titleText.Top.Set(5f, 0f);
            titleText.HAlign = 0.5f;
            titleText.TextColor = new Color(220, 220, 255);
            socketingPanel.Append(titleText);

            rebirthPointsText = new UIText("Your RP: 0", 1f);
            rebirthPointsText.Top.Set(560f, 0f);
            rebirthPointsText.Left.Set(600f, 0f);
            rebirthPointsText.TextColor = new Color(255, 215, 100);
            socketingPanel.Append(rebirthPointsText);
        }

        private void InitializeItemSlot()
        {
            weaponSlot = new ValidatingItemSlot(socketingItemArray, 0);
            weaponSlot.Width.Set(100f, 0f);
            weaponSlot.Height.Set(100f, 0f);
            weaponSlot.Top.Set(50f, 0f);
            weaponSlot.Left.Set(10f, 0f);

            weaponSlot.ValidItemFunc = (item) => SocketingGlobalItem.IsWeapon(item) || SocketingGlobalItem.IsArmor(item);

            socketingPanel.Append(weaponSlot);

            itemNameText = new UIText("Item: ---", 1f);
            itemNameText.Top.Set(60f, 0f);
            itemNameText.Left.Set(130f, 0f);
            itemNameText.TextColor = Color.White;
            socketingPanel.Append(itemNameText);

            slotsText = new UIText("Slots: - / -", 1f);
            slotsText.Top.Set(80f, 0f);
            slotsText.Left.Set(130f, 0f);
            slotsText.TextColor = Color.LightGray;
            socketingPanel.Append(slotsText);
        }

        private void InitializeCompatibleCores()
        {
            compatibleCoresPanel = new UIPanel();
            compatibleCoresPanel.Width.Set(350f, 0f);
            compatibleCoresPanel.Height.Set(300f, 0f);
            compatibleCoresPanel.Top.Set(50f, 0f);
            compatibleCoresPanel.Left.Set(400f, 0f);
            compatibleCoresPanel.BackgroundColor = new Color(40, 50, 80, 200);
            compatibleCoresPanel.BorderColor = new Color(80, 100, 140, 255);
            compatibleCoresPanel.SetPadding(10f);
            socketingPanel.Append(compatibleCoresPanel);

            compatibleCoresTitle = new UIText("Compatible Cores", 1.1f);
            compatibleCoresTitle.Top.Set(5f, 0f);
            compatibleCoresTitle.HAlign = 0.5f;
            compatibleCoresTitle.TextColor = new Color(220, 220, 255);
            compatibleCoresPanel.Append(compatibleCoresTitle);


            compatibleCoresList = new UIList();
            compatibleCoresList.Width.Set(-25f, 1f);
            compatibleCoresList.Height.Set(-40f, 1f);
            compatibleCoresList.Top.Set(30f, 0f);
            compatibleCoresList.ListPadding = 5f;
            compatibleCoresPanel.Append(compatibleCoresList);

            compatibleCoresScrollbar = new UIScrollbar();
            compatibleCoresScrollbar.Height.Set(-40f, 1f);
            compatibleCoresScrollbar.Top.Set(30f, 0f);
            compatibleCoresScrollbar.Left.Set(-20f, 1f);
            compatibleCoresPanel.Append(compatibleCoresScrollbar);
            compatibleCoresList.SetScrollbar(compatibleCoresScrollbar);
        }

        private void InitializeAttachedCores()
        {
            attachedCoresTitle = new UIText("Attached Cores:", 1.1f);
            attachedCoresTitle.Top.Set(200f, 0f);
            attachedCoresTitle.Left.Set(30f, 0f);
            attachedCoresTitle.TextColor = new Color(255, 200, 100);
            socketingPanel.Append(attachedCoresTitle);

            attachedCoresList = new UIList();
            attachedCoresList.Width.Set(350f, 0f);
            attachedCoresList.Height.Set(120f, 0f);
            attachedCoresList.Top.Set(225f, 0f);
            attachedCoresList.Left.Set(30f, 0f);
            attachedCoresList.ListPadding = 3f;
            socketingPanel.Append(attachedCoresList);
        }

        private void InitializeActionButtons()
        {
            expandButton = new UITextPanel<string>("Expand Slot", 1f, false);
            expandButton.Width.Set(150f, 0f);
            expandButton.Height.Set(40f, 0f);
            expandButton.Top.Set(370f, 0f);
            expandButton.Left.Set(30f, 0f);
            expandButton.BackgroundColor = new Color(80, 120, 80, 200);
            expandButton.BorderColor = new Color(120, 180, 120, 255);
            expandButton.OnLeftClick += OnExpandClick;
            socketingPanel.Append(expandButton);

            expandCostText = new UIText("Cost: --- RP", 0.9f);
            expandCostText.Top.Set(415f, 0f);
            expandCostText.Left.Set(30f, 0f);
            expandCostText.TextColor = Color.Yellow;
            socketingPanel.Append(expandCostText);

            attachButton = new UITextPanel<string>("Attach Core", 1f, false);
            attachButton.Width.Set(150f, 0f);
            attachButton.Height.Set(40f, 0f);
            attachButton.Top.Set(500f, 0f);
            attachButton.Left.Set(200f, 0f);
            attachButton.BackgroundColor = new Color(60, 60, 60, 150);
            attachButton.BorderColor = new Color(120, 120, 120, 200);
            attachButton.OnLeftClick += OnAttachClick;
            socketingPanel.Append(attachButton);

            extractButton = new UITextPanel<string>("Extract Core", 1f, false);
            extractButton.Width.Set(150f, 0f);
            extractButton.Height.Set(40f, 0f);
            extractButton.Top.Set(500f, 0f);
            extractButton.Left.Set(400f, 0f);
            extractButton.BackgroundColor = new Color(60, 60, 60, 150);
            extractButton.BorderColor = new Color(120, 120, 120, 200);
            extractButton.OnLeftClick += OnExtractClick;
            socketingPanel.Append(extractButton);

            extractCostText = new UIText("Cost: --- RP", 0.9f);
            extractCostText.Top.Set(545f, 0f);
            extractCostText.Left.Set(400f, 0f);
            extractCostText.TextColor = Color.Yellow;
            socketingPanel.Append(extractCostText);
        }

        private bool IsClickingOnInteractiveElement(Vector2 mousePosition)
        {
            if (weaponSlot?.ContainsPoint(mousePosition) == true) return true;
            if (expandButton?.ContainsPoint(mousePosition) == true) return true;
            if (attachButton?.ContainsPoint(mousePosition) == true) return true;
            if (extractButton?.ContainsPoint(mousePosition) == true) return true;
            if (compatibleCoresScrollbar?.ContainsPoint(mousePosition) == true) return true;

            foreach (var child in compatibleCoresList._items)
            {
                if (child.ContainsPoint(mousePosition)) return true;
            }

            foreach (var child in attachedCoresList._items)
            {
                if (child.ContainsPoint(mousePosition)) return true;
            }

            return false;
        }

        private void OnItemChanged()
        {
            selectedCompatibleCore = null;
            selectedAttachedCore = null;
            RefreshUI();
        }

        private void OnExpandClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            Item item = SocketingItemSlot;

            if (item == null || item.IsAir || !(SocketingGlobalItem.IsWeapon(item) || SocketingGlobalItem.IsArmor(item))) return;

            var socketingData = item.GetGlobalItem<SocketingGlobalItem>();
            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;

            if (socketingData.ExpandedSlots >= config.MaxExpandedSlots) return;

            int cost = socketingData.GetExpandCost();
            if (rpg.RebirthPoints < cost) return;

            rpg.RebirthPoints -= cost;
            socketingData.ExpandSlots();

            SoundEngine.PlaySound(SoundID.Research);

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SocketingGlobalItem.SyncSocketedItem(player, item, -1);
                rpg.SyncPlayer(-1, player.whoAmI, false);
            }

            RefreshUI();
        }

        private void OnAttachClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!selectedCompatibleCore.HasValue) return;

            Player player = Main.LocalPlayer;
            Item item = SocketingItemSlot;

            if (item == null || item.IsAir || !(SocketingGlobalItem.IsWeapon(item) || SocketingGlobalItem.IsArmor(item))) return;

            var socketingData = item.GetGlobalItem<SocketingGlobalItem>();
            var (type, tier) = selectedCompatibleCore.Value;

            if (!socketingData.CanAttachCore(type, item, player)) return;

            int coreItemType = GetCoreItemType(type, tier);
            if (!player.ConsumeItem(coreItemType)) return;

            socketingData.AttachCore(type, tier);

            SoundEngine.PlaySound(SoundID.Grab);

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SocketingGlobalItem.SyncSocketedItem(player, item, -1);
            }

            RefreshUI();
            selectedCompatibleCore = null;
        }

        private void OnExtractClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!selectedAttachedCore.HasValue) return;

            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            Item item = SocketingItemSlot;

            if (item == null || item.IsAir || !(SocketingGlobalItem.IsWeapon(item) || SocketingGlobalItem.IsArmor(item))) return;

            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            if (rpg.RebirthPoints < config.ExtractCost) return;

            var socketingData = item.GetGlobalItem<SocketingGlobalItem>();
            var core = selectedAttachedCore.Value;

            if (!socketingData.ExtractCore(core.Type, core.Tier)) return;

            rpg.RebirthPoints -= config.ExtractCost;

            int coreItemType = GetCoreItemType(core.Type, core.Tier);
            player.QuickSpawnItem(player.GetSource_FromThis(), coreItemType, 1);

            SoundEngine.PlaySound(SoundID.Grab);

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SocketingGlobalItem.SyncSocketedItem(player, item, -1);
                rpg.SyncPlayer(-1, player.whoAmI, false);
            }

            RefreshUI();
            selectedAttachedCore = null;
        }

        private int GetCoreItemType(CoreType type, int tier)
        {
            return type switch
            {
                CoreType.Power => tier switch
                {
                    1 => ModContent.ItemType<Items.Cores.CoreOfPowerT1>(),
                    2 => ModContent.ItemType<Items.Cores.CoreOfPowerT2>(),
                    3 => ModContent.ItemType<Items.Cores.CoreOfPowerT3>(),
                    _ => 0
                },
                CoreType.Force => tier switch
                {
                    1 => ModContent.ItemType<Items.Cores.CoreOfForceT1>(),
                    2 => ModContent.ItemType<Items.Cores.CoreOfForceT2>(),
                    3 => ModContent.ItemType<Items.Cores.CoreOfForceT3>(),
                    _ => 0
                },
                CoreType.Precision => tier switch
                {
                    1 => ModContent.ItemType<Items.Cores.CoreOfPrecisionT1>(),
                    2 => ModContent.ItemType<Items.Cores.CoreOfPrecisionT2>(),
                    3 => ModContent.ItemType<Items.Cores.CoreOfPrecisionT3>(),
                    _ => 0
                },
                CoreType.Defense => tier switch
                {
                    1 => ModContent.ItemType<Items.Cores.CoreOfDefenseT1>(),
                    2 => ModContent.ItemType<Items.Cores.CoreOfDefenseT2>(),
                    3 => ModContent.ItemType<Items.Cores.CoreOfDefenseT3>(),
                    _ => 0
                },
                _ => 0
            };
        }

        private string TruncateItemName(string itemName, float maxWidth)
        {
            if (string.IsNullOrEmpty(itemName))
                return itemName;

            string testText = "Item: " + itemName;
            Vector2 textSize = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(testText);

            if (textSize.X <= maxWidth)
                return itemName;

            int left = 0;
            int right = itemName.Length;
            string result = itemName;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                string truncated = itemName.Substring(0, mid) + "...";
                string testTruncated = "Item: " + truncated;
                Vector2 truncatedSize = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(testTruncated);

                if (truncatedSize.X <= maxWidth)
                {
                    result = truncated;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }

        public void RefreshUI()
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            Item item = SocketingItemSlot;

            rebirthPointsText.SetText($"Your RP: {rpg.RebirthPoints}");

            if (item == null || item.IsAir || !(SocketingGlobalItem.IsWeapon(item) || SocketingGlobalItem.IsArmor(item)))
            {
                itemNameText.SetText("Item: ---");
                slotsText.SetText("Slots: - / -");

                compatibleCoresList.Clear();
                var placeholderText = new UIText("(Place an item to see\n compatible cores)", 0.9f);
                placeholderText.Top.Set(80f, 0f);
                placeholderText.HAlign = 0.5f;
                placeholderText.TextColor = Color.Gray;
                var placeholderPanel = new UIPanel();
                placeholderPanel.Width.Set(0, 1f);
                placeholderPanel.Height.Set(60f, 0f);
                placeholderPanel.BackgroundColor = Color.Transparent;
                placeholderPanel.BorderColor = Color.Transparent;
                placeholderPanel.Append(placeholderText);
                compatibleCoresList.Add(placeholderPanel);

                attachedCoresList.Clear();
                UpdateButtonStates(false, false, false);
                return;
            }

            var socketingData = item.GetGlobalItem<SocketingGlobalItem>();

            float maxNameWidth = 400f - 130f - 20f;
            string truncatedName = TruncateItemName(item.Name, maxNameWidth);
            itemNameText.SetText($"Item: {truncatedName}");
            slotsText.SetText($"Slots: {socketingData.GetUsedSlots()} / {socketingData.MaxSlots}");

            RefreshCompatibleCores(item, player);

            RefreshAttachedCores(socketingData);

            UpdateButtonStates(true, selectedCompatibleCore.HasValue, selectedAttachedCore.HasValue);

            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;
            expandCostText.SetText($"Cost: {socketingData.GetExpandCost()} RP");
            extractCostText.SetText($"Cost: {config.ExtractCost} RP");
        }

        private void RefreshCompatibleCores(Item item, Player player)
        {
            compatibleCoresList.Clear();

            var availableCores = new Dictionary<(CoreType, int), int>();

            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item invItem = player.inventory[i];
                if (invItem.IsAir || invItem.ModItem is not CoreItem core) continue;

                var key = (core.CoreType, core.Tier);
                availableCores[key] = availableCores.GetValueOrDefault(key, 0) + invItem.stack;
            }

            var socketingData = item.GetGlobalItem<SocketingGlobalItem>();

            foreach (var kvp in availableCores)
            {
                var (type, tier) = kvp.Key;
                int count = kvp.Value;

                if (!socketingData.CanAttachCore(type, item, player)) continue;

                var corePanel = CreateCompatibleCorePanel(type, tier, count);
                compatibleCoresList.Add(corePanel);
            }
        }

        private UIPanel CreateCompatibleCorePanel(CoreType type, int tier, int count)
        {
            var panel = new UIPanel();
            panel.Width.Set(0, 1f);
            panel.Height.Set(30f, 0f);
            panel.BackgroundColor = selectedCompatibleCore?.type == type && selectedCompatibleCore?.tier == tier
                ? new Color(80, 120, 80, 200)
                : new Color(60, 60, 60, 150);
            panel.BorderColor = new Color(120, 120, 120, 200);
            panel.SetPadding(5f);

            var core = new SocketedCore(type, tier);
            var text = new UIText($"{core.GetDisplayName()}: x{count}", 0.9f);
            text.VAlign = 0.5f;
            text.TextColor = Color.White;
            panel.Append(text);

            panel.OnLeftClick += (evt, el) =>
            {
                if (selectedCompatibleCore?.type == type && selectedCompatibleCore?.tier == tier)
                {
                    selectedCompatibleCore = null;
                }
                else
                {
                    selectedCompatibleCore = (type, tier);
                    selectedAttachedCore = null;
                }
                RefreshUI();
            };

            return panel;
        }

        private void RefreshAttachedCores(SocketingGlobalItem socketingData)
        {
            attachedCoresList.Clear();

            if (socketingData.SocketedCores.Count == 0)
            {
                var emptyText = new UIText("---", 0.9f);
                emptyText.TextColor = Color.Gray;
                var emptyPanel = new UIPanel();
                emptyPanel.Width.Set(0, 1f);
                emptyPanel.Height.Set(20f, 0f);
                emptyPanel.BackgroundColor = Color.Transparent;
                emptyPanel.BorderColor = Color.Transparent;
                emptyPanel.Append(emptyText);
                attachedCoresList.Add(emptyPanel);
                return;
            }

            foreach (var core in socketingData.SocketedCores)
            {
                var corePanel = CreateAttachedCorePanel(core);
                attachedCoresList.Add(corePanel);
            }
        }

        private UIPanel CreateAttachedCorePanel(SocketedCore core)
        {
            var panel = new UIPanel();
            panel.Width.Set(0, 1f);
            panel.Height.Set(25f, 0f);
            panel.BackgroundColor = selectedAttachedCore?.Type == core.Type && selectedAttachedCore?.Tier == core.Tier
                ? new Color(120, 80, 80, 200)
                : Color.Transparent;
            panel.BorderColor = Color.Transparent;
            panel.SetPadding(3f);

            var text = new UIText($"> {core.GetDisplayName()} (x{core.Count})", 0.85f);
            text.VAlign = 0.5f;
            text.TextColor = Color.LightGray;
            panel.Append(text);

            panel.OnLeftClick += (evt, el) =>
            {
                if (selectedAttachedCore?.Type == core.Type && selectedAttachedCore?.Tier == core.Tier)
                {
                    selectedAttachedCore = null;
                }
                else
                {
                    selectedAttachedCore = core;
                    selectedCompatibleCore = null;
                }
                RefreshUI();
            };

            return panel;
        }

        private void UpdateButtonStates(bool hasItem, bool canAttach, bool canExtract)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>().socketingSystem;

            if (Main.netMode == NetmodeID.MultiplayerClient && hasItem)
            {
                Item item = SocketingItemSlot;
                var socketingData = item.GetGlobalItem<SocketingGlobalItem>();

                if (selectedCompatibleCore.HasValue)
                {
                    var (type, tier) = selectedCompatibleCore.Value;
                    canAttach = socketingData.CanAttachCore(type, item, player) &&
                            player.HasItem(GetCoreItemType(type, tier));
                }

                if (selectedAttachedCore.HasValue)
                {
                    canExtract = socketingData.SocketedCores.Any(c =>
                        c.Type == selectedAttachedCore.Value.Type &&
                        c.Tier == selectedAttachedCore.Value.Tier);
                }
            }

            if (hasItem)
            {
                Item item = SocketingItemSlot;
                var socketingData = item.GetGlobalItem<SocketingGlobalItem>();
                bool canExpand = socketingData.ExpandedSlots < config.MaxExpandedSlots &&
                            rpg.RebirthPoints >= socketingData.GetExpandCost();

                expandButton.BackgroundColor = canExpand
                    ? new Color(80, 120, 80, 200)
                    : new Color(60, 60, 60, 150);
            }
            else
            {
                expandButton.BackgroundColor = new Color(60, 60, 60, 150);
            }

            attachButton.BackgroundColor = canAttach
                ? new Color(80, 120, 80, 200)
                : new Color(60, 60, 60, 150);

            bool canAffordExtract = rpg.RebirthPoints >= config.ExtractCost;
            extractButton.BackgroundColor = canExtract && canAffordExtract
                ? new Color(120, 80, 80, 200)
                : new Color(60, 60, 60, 150);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!SocketingItemSlot.IsAir != !lastItem.IsAir ||
                SocketingItemSlot.type != lastItem.type ||
                SocketingItemSlot.stack != lastItem.stack)
            {
                OnItemChanged();
                lastItem = SocketingItemSlot.Clone();
            }

            if (socketingPanel.ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;

            if (dragging)
            {
                Vector2 mouse = Main.MouseScreen;
                socketingPanel.Left.Set(mouse.X - offset.X, 0f);
                socketingPanel.Top.Set(mouse.Y - offset.Y, 0f);
                socketingPanel.Recalculate();
            }

            RefreshUI();
        }
    }
}