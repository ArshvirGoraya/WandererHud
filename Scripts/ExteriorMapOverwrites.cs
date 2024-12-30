using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace ExteriorMapOverwritesMod
{
    public class ExteriorMapOverwrites : MonoBehaviour
    {
        private static Mod mod;

        public Panel ExteriorMapPanel;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<ExteriorMapOverwrites>();

            mod.IsReady = true;
        }

        private void Start()
        {
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;

            ExteriorMapPanel = DaggerfallUI.Instance.ExteriorAutomapWindow.NativePanel;
        }


        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e)
        {

            // if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){}
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                //// Debug.Log($"NativePanel.EnableBorder: {ExteriorMapPanel.EnableBorder}");
                //// Debug.Log($"NativePanel.Outline: {ExteriorMapPanel.Outline}");
                foreach (BaseScreenComponent component in ExteriorMapPanel.Components){
                    if (component.Enabled){
                        component.Enabled = false;
                        // Debug.Log($"{component}");
                        // component is HUDCompass
                        // component is Button
                        // component is Panel
                    }
                }
            }
        }


        // * Copied from DaggerfallAutoMapWindow.cs
        private HotkeySequence ShortcutOrFallback(DaggerfallShortcut.Buttons button)
        {
            HotkeySequence hotkeySequence = DaggerfallShortcut.GetBinding(button);
            if (hotkeySequence.IsSameKeyCode(KeyCode.None))
                return hotkeySequence.WithKeyCode(KeyCode.Home);
            else
                return hotkeySequence;
        }
    }
}
