using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using System;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;

namespace Stataria
{
    public class BulkAllocationManager
    {
        public static readonly int[] BulkOptions = { 1, 5, 10, 50, 100, 1000 };

        private int selectedOption = 0;

        private UITextPanel<string>[] bulkButtons;

        private readonly Color StandardBgColor = new Color(150, 150, 150, 20);
        private readonly Color StandardBorderColor = new Color(200, 200, 200, 20);

        private readonly Color SelectedBgColor = new Color(120, 190, 120, 200);
        private readonly Color SelectedBorderColor = new Color(150, 255, 150, 200);

        public BulkAllocationManager()
        {
            bulkButtons = new UITextPanel<string>[BulkOptions.Length];
        }

        public void Initialize(UIPanel panel, float baseY)
        {
            var bulkHeader = new UIText("Allocation Amount:", 1f);
            baseY += 10f;
            bulkHeader.Top.Set(baseY, 0f);
            bulkHeader.Left.Set(10f, 0f);
            panel.Append(bulkHeader);

            float buttonWidth = 45f;
            float buttonSpacing = 10f;
            float buttonY = baseY + 25f;

            float totalButtonWidth = (buttonWidth + buttonSpacing) * BulkOptions.Length - buttonSpacing;
            float startX = (panel.Width.Pixels - totalButtonWidth) / 2;

            for (int i = 0; i < BulkOptions.Length; i++)
            {
                int optionIndex = i;

                bulkButtons[i] = new UITextPanel<string>(BulkOptions[i].ToString(), textScale: 1f, large: false)
                {
                    Top = { Pixels = buttonY },
                    Left = { Pixels = startX + (buttonWidth + buttonSpacing) * i },
                    Width = { Pixels = buttonWidth },
                    Height = { Pixels = 25f },
                    BackgroundColor = (i == selectedOption) ? SelectedBgColor : StandardBgColor,
                    BorderColor = (i == selectedOption) ? SelectedBorderColor : StandardBorderColor
                };

                bulkButtons[i].SetPadding(0f);

                bulkButtons[i].OnLeftClick += (evt, el) =>
                {
                    SelectBulkOption(optionIndex);
                };

                panel.Append(bulkButtons[i]);
            }
        }

        public void SelectBulkOption(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= BulkOptions.Length)
                return;

            if (selectedOption < bulkButtons.Length && bulkButtons[selectedOption] != null)
            {
                bulkButtons[selectedOption].BackgroundColor = StandardBgColor;
                bulkButtons[selectedOption].BorderColor = StandardBorderColor;
            }

            selectedOption = optionIndex;

            if (selectedOption < bulkButtons.Length && bulkButtons[selectedOption] != null)
            {
                bulkButtons[selectedOption].BackgroundColor = SelectedBgColor;
                bulkButtons[selectedOption].BorderColor = SelectedBorderColor;
            }

            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public int GetCurrentAmount() => BulkOptions[selectedOption];
    }
}