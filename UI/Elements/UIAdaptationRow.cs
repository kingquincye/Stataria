using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using System;
using Stataria.Core;
using Stataria.Players;

namespace Stataria.UI.Elements
{
    public class UIAdaptationRow : UIPanel
    {
        public AdaptationKey Key { get; private set; }
        public Action<AdaptationKey> OnToggleClicked { get; set; }

        private readonly UIText catBadge;
        private readonly UIText typeBadge;
        private readonly UIText nameText;
        private readonly UIText subText;
        private readonly UIText levelText;
        private readonly UIText pctText;
        private readonly UITextPanel<string> toggleBtn;

        private int cachedLevel = -1;
        private float cachedExp = -1f;
        private bool cachedDisabled = false;

        public UIAdaptationRow()
        {
            Width.Set(0, 1f);
            Height.Set(58f, 0f);
            SetPadding(6f);

            // Left: Category tag badge
            catBadge = new UIText("", 0.8f);
            catBadge.Left.Set(4f, 0f);
            catBadge.Top.Set(4f, 0f);
            Append(catBadge);

            // Offensive / Defensive tag badge
            typeBadge = new UIText("", 0.75f);
            typeBadge.Top.Set(4f, 0f);
            Append(typeBadge);

            // Display Name
            nameText = new UIText("", 1f);
            nameText.Left.Set(4f, 0f);
            nameText.Top.Set(24f, 0f);
            Append(nameText);

            // Mod Name subtext
            subText = new UIText("", 0.7f);
            subText.Top.Set(26f, 0f);
            subText.TextColor = new Color(140, 150, 170);
            Append(subText);

            // Right side: Level indicator
            levelText = new UIText("", 0.95f);
            levelText.Left.Set(-250f, 1f);
            levelText.Top.Set(4f, 0f);
            Append(levelText);

            // Progress percentage bar
            pctText = new UIText("", 0.75f);
            pctText.Left.Set(-250f, 1f);
            pctText.Top.Set(26f, 0f);
            Append(pctText);

            // Toggle Button (Enabled vs Disabled)
            toggleBtn = new UITextPanel<string>("ENABLED", 0.8f, false);
            toggleBtn.Width.Set(90f, 0f);
            toggleBtn.Height.Set(32f, 0f);
            toggleBtn.Left.Set(-100f, 1f);
            toggleBtn.Top.Set(8f, 0f);
            toggleBtn.SetPadding(4f);

            toggleBtn.OnLeftClick += (evt, el) =>
            {
                OnToggleClicked?.Invoke(Key);
            };

            Append(toggleBtn);
        }

        public UIAdaptationRow(AdaptationKey key, AdaptationData data, int maxLevel, Action<AdaptationKey> onToggleClicked) : this()
        {
            OnToggleClicked = onToggleClicked;
            Bind(key, data, maxLevel);
        }

        public void Bind(AdaptationKey key, AdaptationData data, int maxLevel)
        {
            if (Key.Equals(key))
            {
                UpdateData(data, maxLevel);
                return;
            }

            Key = key;

            Color catColor = key.Category.GetCategoryColor();

            // Category tag badge
            string categoryTag = $"[{key.Category.ToString().ToUpper()}]";
            catBadge.SetText(categoryTag);
            catBadge.TextColor = catColor;

            float catBadgeWidth = FontAssets.MouseText.Value.MeasureString(categoryTag).X * 0.8f;

            // Offensive / Defensive tag badge
            string typeTag = key.IsOffensive ? "[OFFENSE]" : "[DEFENSE]";
            typeBadge.SetText(typeTag);
            typeBadge.Left.Set(4f + catBadgeWidth + 10f, 0f);
            typeBadge.TextColor = key.IsOffensive ? new Color(255, 215, 100) : new Color(140, 210, 255);

            // Display Name
            string nameStr = key.DisplayName;
            nameText.SetText(nameStr);

            // Mod Name subtext (Only displayed if adaptation originates from an external mod)
            string modSubtext = GetModSubtext(key.TargetId);
            if (!string.IsNullOrEmpty(modSubtext))
            {
                subText.SetText(modSubtext);
                subText.Left.Set(nameText.Left.Pixels + FontAssets.MouseText.Value.MeasureString(nameStr).X * 1f + 8f, 0f);
            }
            else
            {
                subText.SetText("");
            }

            cachedLevel = -1;
            cachedExp = -1f;
            cachedDisabled = !data.Disabled;

            UpdateData(data, maxLevel, force: true);
        }

        public void UpdateData(AdaptationData data, int maxLevel, bool force = false)
        {
            if (!force && cachedLevel == data.Level && Math.Abs(cachedExp - data.CurrentExp) < 0.001f && cachedDisabled == data.Disabled)
            {
                return;
            }

            bool isDisabled = data.Disabled;
            Color catColor = Key.Category.GetCategoryColor();

            if (force || cachedDisabled != isDisabled)
            {
                if (isDisabled)
                {
                    BackgroundColor = new Color(45, 25, 30, 220);
                    BorderColor = new Color(180, 70, 70, 230);
                    nameText.TextColor = new Color(170, 140, 140);

                    toggleBtn.SetText("DISABLED");
                    toggleBtn.BackgroundColor = new Color(140, 40, 45, 230);
                    toggleBtn.BorderColor = new Color(220, 80, 80, 255);
                    toggleBtn.TextColor = Color.White;
                }
                else
                {
                    BackgroundColor = new Color(28, 38, 60, 210);
                    BorderColor = catColor * 0.85f;
                    nameText.TextColor = Color.White;

                    toggleBtn.SetText("ENABLED");
                    toggleBtn.BackgroundColor = new Color(40, 120, 50, 230);
                    toggleBtn.BorderColor = new Color(80, 200, 90, 255);
                    toggleBtn.TextColor = Color.White;
                }
            }

            bool isMaxed = data.Level >= maxLevel;
            if (force || cachedLevel != data.Level || cachedDisabled != isDisabled)
            {
                string levelStr = isMaxed ? $"LVL {data.Level} (MAX)" : $"LVL {data.Level}/{maxLevel}";
                levelText.SetText(levelStr);
                levelText.TextColor = isMaxed ? new Color(255, 215, 100) : new Color(200, 220, 255);
            }

            if (force || cachedLevel != data.Level || Math.Abs(cachedExp - data.CurrentExp) >= 0.001f || cachedDisabled != isDisabled)
            {
                float pct = data.GetProgressPercentage(Key.Category, Key.TargetId);
                string pctStr = isMaxed ? "100%" : $"{(pct * 100f):0.#}%";
                pctText.SetText(pctStr);
                pctText.TextColor = isMaxed ? new Color(120, 255, 120) : new Color(180, 200, 230);
            }

            cachedLevel = data.Level;
            cachedExp = data.CurrentExp;
            cachedDisabled = data.Disabled;
        }

        public static string GetModSubtext(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                return "";

            // Modded target IDs contain a slash '/' e.g. "ModName/EntityName" or "Proj_ModName/EntityName"
            int slashIndex = targetId.IndexOf('/');
            if (slashIndex <= 0)
                return "";

            string modPart = targetId.Substring(0, slashIndex);

            // Strip entity type prefixes if present (e.g. Proj_ModName, NPC_ModName, Buff_ModName, Item_ModName)
            string[] prefixes = new[] { "Proj_", "NPC_", "Item_", "Debuff_", "Buff_" };
            foreach (var prefix in prefixes)
            {
                if (modPart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    modPart = modPart.Substring(prefix.Length);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(modPart) || modPart.Equals("Stataria", StringComparison.OrdinalIgnoreCase) || modPart.Equals("Terraria", StringComparison.OrdinalIgnoreCase))
                return "";

            return $"({modPart})";
        }
    }
}
