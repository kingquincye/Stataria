using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using System;
using Terraria.Audio;
using Terraria.ID;

namespace Stataria
{
    public class XPVerificationUI : UIState
    {
        private static XPVerificationUI instance;

        private UIPanel panel;
        private UIText titleText;
        private UIText xpAmountText;
        private UIText sourceText;
        private UIText pendingCountText;
        private UIText notificationCounterText;
        private UITextPanel<string> acceptButton;
        private UITextPanel<string> rejectButton;
        private UITextPanel<string> acceptAllButton;
        private UITextPanel<string> rejectAllButton;

        private PendingXPGain currentVerification;
        private Action onAccept;
        private Action onReject;
        private Action onAcceptAll;
        private Action onRejectAll;

        private const float StandardPanelHeight = 200f;
        private const float ExtendedPanelHeight = 250f;

        private int currentIndex = 1;
        private int totalCount = 1;

        private bool dragging = false;
        private Vector2 dragOffset;

        public override void OnInitialize()
        {
            instance = this;

            panel = new UIPanel();
            panel.Width.Set(420, 0f);
            panel.Height.Set(StandardPanelHeight, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.BackgroundColor = new Color(73, 94, 171, 200);
            panel.BorderColor = new Color(150, 50, 50, 255);
            Append(panel);

            panel.OnLeftMouseDown += (evt, element) => {
                if (!acceptButton.ContainsPoint(evt.MousePosition) &&
                    !rejectButton.ContainsPoint(evt.MousePosition) &&
                    !acceptAllButton.ContainsPoint(evt.MousePosition) &&
                    !rejectAllButton.ContainsPoint(evt.MousePosition))
                {
                    dragging = true;
                    dragOffset = new Vector2(evt.MousePosition.X - panel.Left.Pixels, evt.MousePosition.Y - panel.Top.Pixels);
                }
            };
            panel.OnLeftMouseUp += (evt, element) => {
                dragging = false;
            };

            titleText = new UIText("Suspicious XP Gain Detected", 1.3f);
            titleText.HAlign = 0.5f;
            titleText.Top.Set(15, 0f);
            titleText.TextColor = Color.Yellow;
            panel.Append(titleText);

            notificationCounterText = new UIText("", 1f);
            notificationCounterText.Left.Set(380, 0f);
            notificationCounterText.Top.Set(20, 0f);
            notificationCounterText.TextColor = Color.Orange;
            panel.Append(notificationCounterText);

            xpAmountText = new UIText("", 1.1f);
            xpAmountText.HAlign = 0.5f;
            xpAmountText.Top.Set(55, 0f);
            xpAmountText.TextColor = Color.White;
            panel.Append(xpAmountText);

            sourceText = new UIText("", 1f);
            sourceText.HAlign = 0.5f;
            sourceText.Top.Set(85, 0f);
            sourceText.TextColor = Color.LightGray;
            panel.Append(sourceText);

            pendingCountText = new UIText("", 1f);
            pendingCountText.HAlign = 0.5f;
            pendingCountText.Top.Set(110, 0f);
            pendingCountText.TextColor = Color.Orange;
            panel.Append(pendingCountText);

            acceptButton = new UITextPanel<string>("Accept", 1f, false);
            acceptButton.Width.Set(130, 0f);
            acceptButton.Height.Set(40, 0f);
            acceptButton.HAlign = 0.25f;
            acceptButton.Top.Set(140, 0f);
            acceptButton.BackgroundColor = new Color(50, 180, 50, 255);
            acceptButton.BorderColor = new Color(100, 255, 100, 255);
            acceptButton.OnLeftClick += (evt, element) => {
                if (onAccept != null)
                {
                    onAccept.Invoke();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            panel.Append(acceptButton);

            rejectButton = new UITextPanel<string>("Reject", 1f, false);
            rejectButton.Width.Set(130, 0f);
            rejectButton.Height.Set(40, 0f);
            rejectButton.HAlign = 0.75f;
            rejectButton.Top.Set(140, 0f);
            rejectButton.BackgroundColor = new Color(180, 50, 50, 255);
            rejectButton.BorderColor = new Color(255, 100, 100, 255);
            rejectButton.OnLeftClick += (evt, element) => {
                if (onReject != null)
                {
                    onReject.Invoke();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            panel.Append(rejectButton);

            acceptAllButton = new UITextPanel<string>("Accept All", 0.9f, false);
            acceptAllButton.Width.Set(130, 0f);
            acceptAllButton.Height.Set(35, 0f);
            acceptAllButton.HAlign = 0.25f;
            acceptAllButton.Top.Set(190, 0f);
            acceptAllButton.BackgroundColor = new Color(50, 180, 50, 200);
            acceptAllButton.BorderColor = new Color(100, 255, 100, 200);
            acceptAllButton.OnLeftClick += (evt, element) => {
                if (onAcceptAll != null)
                {
                    onAcceptAll.Invoke();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            panel.Append(acceptAllButton);

            rejectAllButton = new UITextPanel<string>("Reject All", 0.9f, false);
            rejectAllButton.Width.Set(130, 0f);
            rejectAllButton.Height.Set(35, 0f);
            rejectAllButton.HAlign = 0.75f;
            rejectAllButton.Top.Set(190, 0f);
            rejectAllButton.BackgroundColor = new Color(180, 50, 50, 200);
            rejectAllButton.BorderColor = new Color(255, 100, 100, 200);
            rejectAllButton.OnLeftClick += (evt, element) => {
                if (onRejectAll != null)
                {
                    onRejectAll.Invoke();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            panel.Append(rejectAllButton);

            SetBatchButtonsVisible(false);
        }

        private void SetBatchButtonsVisible(bool visible)
        {
            if (visible)
            {
                panel.Height.Set(ExtendedPanelHeight, 0f);

                acceptAllButton.BackgroundColor = new Color(50, 180, 50, 200);
                rejectAllButton.BackgroundColor = new Color(180, 50, 50, 200);
                acceptAllButton.BorderColor = new Color(100, 255, 100, 200);
                rejectAllButton.BorderColor = new Color(255, 100, 100, 200);
                acceptAllButton.TextColor = Color.White;
                rejectAllButton.TextColor = Color.White;
            }
            else
            {
                panel.Height.Set(StandardPanelHeight, 0f);

                acceptAllButton.BackgroundColor = new Color(0, 0, 0, 0);
                rejectAllButton.BackgroundColor = new Color(0, 0, 0, 0);
                acceptAllButton.BorderColor = new Color(0, 0, 0, 0);
                rejectAllButton.BorderColor = new Color(0, 0, 0, 0);
                acceptAllButton.TextColor = new Color(0, 0, 0, 0);
                rejectAllButton.TextColor = new Color(0, 0, 0, 0);
            }

            panel.Recalculate();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (dragging)
            {
                Vector2 newPosition = Main.MouseScreen - dragOffset;
                panel.Left.Set(newPosition.X, 0f);
                panel.Top.Set(newPosition.Y, 0f);
                panel.Recalculate();
            }
        }

        public void UpdateVerification(PendingXPGain verification, int currentIdx, int totalNotifications,
                                 Action onAcceptAction, Action onRejectAction,
                                 Action onAcceptAllAction, Action onRejectAllAction)
        {
            currentVerification = verification;
            onAccept = onAcceptAction;
            onReject = onRejectAction;
            onAcceptAll = onAcceptAllAction;
            onRejectAll = onRejectAllAction;

            currentIndex = currentIdx;
            totalCount = totalNotifications;

            xpAmountText.SetText($"XP Amount: {verification.Amount:N0}");
            sourceText.SetText($"Source: {verification.GetFormattedSource()}");

            if (totalCount > 1)
            {
                notificationCounterText.SetText($"{currentIndex}/{totalCount}");
            }
            else
            {
                notificationCounterText.SetText("");
            }

            int pendingCount = totalCount - currentIndex;
            if (pendingCount > 0)
            {
                pendingCountText.SetText($"{pendingCount} suspicious XP gains pending");
                SetBatchButtonsVisible(true);
            }
            else
            {
                pendingCountText.SetText("");
                SetBatchButtonsVisible(false);
            }

            Recalculate();
        }

        public static void ShowVerification(PendingXPGain verification, int currentIdx, int totalCount,
                                        Action onAccept, Action onReject,
                                        Action onAcceptAll, Action onRejectAll)
        {
            if (instance == null)
            {
                instance = new XPVerificationUI();
                instance.Activate();
            }

            instance.UpdateVerification(verification, currentIdx, totalCount, onAccept, onReject, onAcceptAll, onRejectAll);

            StatariaUI.XPVerificationUI?.SetState(instance);
        }

        public static void HideVerification()
        {
            StatariaUI.XPVerificationUI?.SetState(null);
            instance = null;
        }
    }
}