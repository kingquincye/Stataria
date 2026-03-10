using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Stataria.UI;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.Audio;
using Terraria.ID;
using System;

namespace Stataria
{
    [Autoload(true)]
    public class ConfigUISystem : ModSystem
    {
        public static ConfigUISystem Instance => ModContent.GetInstance<ConfigUISystem>();

        internal CustomConfigUIState CustomConfigUI;
        private static UIState _cachedModConfigListUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                CustomConfigUI = new CustomConfigUIState();
                CustomConfigUI.Activate();

                MethodInfo canPauseGameMethod = typeof(Terraria.Main).GetMethod("CanPauseGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (canPauseGameMethod != null)
                {
                    MonoModHooks.Add(canPauseGameMethod, (Func<Func<bool>, bool>)Main_CanPauseGame);
                }

                // Detour PopulateConfigs instead of OnActivate to catch mod selection changes
                var populateMethod = typeof(Terraria.ModLoader.ModContent).Assembly
                    .GetType("Terraria.ModLoader.Config.UI.UIModConfigList")
                    ?.GetMethod("PopulateConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (populateMethod != null)
                {
                    MonoModHooks.Add(populateMethod, (Action<Action<UIState>, UIState>)On_UIModConfigList_PopulateConfigs);
                }

                // Pre-cache the internal ModConfigList UIState via reflection to avoid
                // costly Garbage Collection micro-stutters during state transitions.
                Type interfaceType = typeof(Terraria.ModLoader.ModContent).Assembly.GetType("Terraria.ModLoader.UI.Interface");
                FieldInfo modConfigListField = interfaceType?.GetField("modConfigList", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                
                if (modConfigListField != null) 
                {
                    _cachedModConfigListUI = modConfigListField.GetValue(null) as UIState;
                }
            }
        }

        public override void Unload()
        {
            CustomConfigUI = null;
            _cachedModConfigListUI = null;
            
            // tModLoader automatically unloads MonoModHooks, so we don't need manual detaching here anymore!
        }

        private bool Main_CanPauseGame(Func<bool> orig)
        {
            // Let the vanilla engine determine if it should already pause based on options menu/etc
            bool shouldPause = orig();

            // The absolute ultimate frame-override. We execute this during the core pause evaluation.
            if (Main.netMode == NetmodeID.SinglePlayer && !Main.gameMenu && CustomConfigUIState.IsUIActive)
            {
                shouldPause = true;
            }

            return shouldPause;
        }

        private void On_UIModConfigList_PopulateConfigs(Action<UIState> orig, UIState self)
        {
            orig(self);

            FieldInfo modField = self.GetType().GetField("selectedMod", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Mod selectedMod = modField?.GetValue(self) as Mod;

            if (selectedMod != null && selectedMod.Name == Mod.Name)
            {
                FieldInfo configListField = self.GetType().GetField("configList", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (configListField != null)
                {
                    UIList configList = configListField.GetValue(self) as UIList;
                    if (configList != null)
                    {
                        InjectConfigCenterButton(configList);
                    }
                }
            }
        }

        private void InjectConfigCenterButton(UIList configList)
        {
            // Create our custom menu button to match the screenshot
            UIPanel button = new UIPanel();
            button.Width.Set(0, 0.95f); // Match standard tML UI button sizing
            button.Height.Set(40, 0f);
            button.HAlign = 0.5f; 
            button.BackgroundColor = new Color(63, 82, 151) * 0.7f; 
            
            UIText buttonText = new UIText("Config Center", 1f, false);
            buttonText.TextColor = Color.Magenta; // Quincy's favorite flavor of purple text 
            buttonText.HAlign = 0.5f;
            buttonText.VAlign = 0.5f;
            button.Append(buttonText);

            button.OnMouseOver += (evt, el) => {
                button.BackgroundColor = new Color(73, 94, 171) * 0.85f;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
            };
            button.OnMouseOut += (evt, el) => {
                button.BackgroundColor = new Color(63, 82, 151) * 0.7f;
            };
            button.OnLeftClick += (evt, el) => {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
                ShowMyUI();
            };

            configList.Add(button);
        }

        public override void PreSaveAndQuit()
        {
            HideMyUI();
        }

        public void ShowMyUI()
        {
            if (CustomConfigUI != null)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
                CustomConfigUI.PopulateConfigsTabs();
                
                if (Main.gameMenu)
                {
                    Main.menuMode = 888;
                    Main.MenuUI.SetState(CustomConfigUI);
                }
                else
                {
                    Terraria.UI.IngameFancyUI.OpenUIState(CustomConfigUI);
                }
            }
        }

        public void HideMyUI()
        {
            if (Main.MenuUI.CurrentState != CustomConfigUI && Main.InGameUI.CurrentState != CustomConfigUI)
                return;

            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);

            if (Main.gameMenu)
            {
                Main.menuMode = 10027; // Vanilla code to return to Mod List
                Main.MenuUI.SetState(null);
            }
            else
            {
                // In-game, grab the cached ModConfigList UIState to return directly into it
                if (_cachedModConfigListUI != null) 
                {
                    // Bridge the delta-tick gap: Ensure the pause is held HIGH for the exact 
                    // frame the engine transitions states, so the world doesn't twitch.
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        Main.gamePaused = true;
                    }
                    
                    Terraria.UI.IngameFancyUI.OpenUIState(_cachedModConfigListUI);
                    return;
                }
                
                // Absolute fallback gracefully closes if caching failed for some reason
                Terraria.UI.IngameFancyUI.Close();
            }
        }
    }
}