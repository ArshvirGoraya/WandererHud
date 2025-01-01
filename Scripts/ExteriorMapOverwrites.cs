using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;

namespace ExteriorMapOverwritesMod
{
    public class ExteriorMapOverwrites : MonoBehaviour
    {
        private static Mod mod;

        public DaggerfallExteriorAutomapWindow ExteriorMapWindow;
        public bool ComponentsDisabled = false;
        public Vector2 LastScreen = new Vector2(0, 0);
        public float ResizeWaitSecs = 0f;
        public bool ResizeWaiting = false;
        public Panel DummyPanelAutomap;
        public Panel PanelRenderAutomap;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<ExteriorMapOverwrites>();
            mod.IsReady = true;
        }

        private void Start(){
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            ExteriorMapWindow = DaggerfallUI.Instance.ExteriorAutomapWindow;
            SetLastScreen();
       }

        private void Update(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                if (Screen.width != LastScreen.x || Screen.height != LastScreen.y){
                    if (!ResizeWaiting){
                        StartCoroutine(WaitSeconds(ResizeWaitSecs));
                    }
                }
            }
        }

        private void SetLastScreen(){
            LastScreen.x = Screen.width; 
            LastScreen.y = Screen.height;
        }

        private IEnumerator WaitSeconds(float seconds){
            ForceResizeMap();
            SetLastScreen();
            ResizeWaiting = true;
            yield return new WaitForSecondsRealtime(seconds);
            ResizeWaiting = false;
        }

        public void ForceResizeMap(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                ExteriorMapWindow.NativePanel.Size = ExteriorMapWindow.ParentPanel.Rectangle.size;
            }
        }

        public void DisableComponents(){
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component is Button || component is HUDCompass){ // * Buttons and Compass Texture
                    component.Enabled = false;
                    continue;
                }
                // TODO: find better way to select these panels? 
                    // todo: If this works for different screen sizes than is fine? 
                    // todo: Make a PR to make these public?
                if (component is Panel && $"{component.Size}".Equals("(320.0, 10.0)")){ // * panelCaption (map legend)
                    component.Enabled = false;
                    continue;
                }
                if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    component.Enabled = false;
                    continue;
                }
            }
        }

        public void ResizeMapPanels(){
            ExteriorMapWindow.NativePanel.AutoSize = AutoSizeModes.ResizeToFill;

            foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
                if (component.Enabled && component is Panel){
                    if (!$"{component.Size}".Equals($"({Screen.width}, {Screen.height})")){ // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
                        PanelRenderAutomap = component as Panel;
                    }
                }
            }

            // TODO: find better way to select this panels?
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * is DummyPanelAutomap: buildingNamePlates
                        DummyPanelAutomap = component as Panel;
                        component.AutoSize = AutoSizeModes.ScaleFreely;
                        component.HorizontalAlignment = HorizontalAlignment.Center;
                        component.VerticalAlignment = VerticalAlignment.Middle;
                    }
                }
            }

            ForceResizeMap(); // ! Set size to apply autoscaling!
        }

        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e){
            // if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){}
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                if (!ComponentsDisabled){ 
                    DisableComponents();
                    ComponentsDisabled = true;
                }
                ResizeMapPanels();
                // PrintDebug();
            }
        }

        // DEBUGGING:
        public void PrintDebug(){
            // * ExteriorMapWindow.ParentPanel
                // * 320.0, 200.0: native Panel
                // * 1144.8, 608.4: panelRenderAutomap (map texture) Scales with DummyPanelAutomap.
            // * ExteriorMapWindow.NativePanel
                // * 318.0, 169.0: DummyPanelAutomap (nameplates)
                // * 320.0, 10.0: panelCaption (map legend)
                // * 76.0, 17.0: dummyPanelCompass (map click function)
            
            // foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
            //     if ($"{component.Size}".Equals("(1144.8, 608.4)")){ // * panelRenderAutomap (scales with DummyPanelAutomap so doesn't matter)
            //         component.BackgroundColor = Color.magenta;
            //         BaseComponentLog(component, "panelRenderAutomap");
            //     }
            // }
            // foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
            //     if (component.Enabled && component is Panel){
            //         if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * DummyPanelAutomap
            //             // component.BackgroundColor = Color.green;
            //             BaseComponentLog(component, "DummyPanelAutomap");
            //         }
            //     }
            // }
        }
        public void BaseComponentLog(BaseScreenComponent component, string Prefix = ""){
            PrefixLog(Prefix, $"| Component: {component}");
            PrefixLog(Prefix, $"| Enabled: {component.Enabled}");
            PrefixLog(Prefix, $"| Size: {component.Size}");
            PrefixLog(Prefix, $"| AutoSize: {component.AutoSize}");
            PrefixLog(Prefix, $"| BackgroundColor: {component.BackgroundColor}");
            PrefixLog(Prefix, $"| BackgroundTexture: {component.BackgroundTexture}");
            PrefixLog(Prefix, $"| BackgroundTextureLayout: {component.BackgroundTextureLayout}");
            PrefixLog(Prefix, $"| BackgroundCroppedRect: {component.BackgroundCroppedRect}");
            PrefixLog(Prefix, $"| HorizontalAlignment: {component.HorizontalAlignment}");
            PrefixLog(Prefix, $"| VerticalAlignment: {component.VerticalAlignment}");
            PrefixLog(Prefix, $"| UseRestrictedRenderArea: {component.UseRestrictedRenderArea}");
            PrefixLog(Prefix, $"| MinAutoScale: {component.MinAutoScale}");
            PrefixLog(Prefix, $"| MaxAutoScale: {component.MaxAutoScale}");
            PrefixLog(Prefix, $"| Scale: {component.Scale}");
            PrefixLog(Prefix, $"| LocalScale: {component.LocalScale}");
            PrefixLog(Prefix, $"===");
        }
        public void PrefixLog(string Prefix, string LogComponent){
            Debug.Log(Prefix + LogComponent);
        }
    }
}
