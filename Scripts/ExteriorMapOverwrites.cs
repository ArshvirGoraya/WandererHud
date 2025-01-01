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
        public bool ButtonsDisabled = false;
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
            LastScreen.x = Screen.width;
            LastScreen.y = Screen.height;
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

        private IEnumerator WaitSeconds(float seconds){
            ForceResizeMap();
            LastScreen.x = Screen.width; 
            LastScreen.y = Screen.height;
            ResizeWaiting = true;
            yield return new WaitForSecondsRealtime(seconds);
            ResizeWaiting = false;
        }

        public void ForceResizeMap(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                Debug.Log($"updating map size");
                // ExteriorMapWindow.Update();
                // ExteriorMapWindow.UpdateAutomapView();
                // ExteriorMapWindow.OnPop();
                // ExteriorMapWindow.OnPush();
                // ExteriorMapWindow.Draw(); // ! cant call outside of an GUI function
                // exteriorAutomap.UpdateAutomapStateOnWindowPush()

                ExteriorMapWindow.NativePanel.Size = ExteriorMapWindow.ParentPanel.Rectangle.size;
                // DummyPanelAutomap.Size = ExteriorMapWindow.ParentPanel.Rectangle.size;
                // PanelRenderAutomap.Size = ExteriorMapWindow.ParentPanel.Rectangle.size;
                // Debug.Log($"WandererHud ExteriorMap: ParentPanel.Rectangle: {ExteriorMapWindow.ParentPanel.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!
                // Debug.Log($"WandererHud ExteriorMap: NativePanel.Rectangle: {ExteriorMapWindow.NativePanel.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!
                // Debug.Log($"WandererHud ExteriorMap: DummyPanelAutomap.Rectangle: {DummyPanelAutomap.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!
                // Debug.Log($"WandererHud ExteriorMap: PanelRenderAutomap.Rectangle: {PanelRenderAutomap.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!
            }
        }

        public void DisableComponents(){
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component is Button || component is HUDCompass){ // * Buttons and Compass Texture
                    component.Enabled = false;
                    continue;
                }
                // TODO: find better way to select these panels?
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
            Debug.Log($"WandererHud ExteriorMap Resize: NativePanel.Rectangle: {ExteriorMapWindow.NativePanel.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!

            foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
                if (component.Enabled && component is Panel){
                    // Debug.Log($"component size: {component.Size}");
                    // Debug.Log($"screen size: {Screen.width}, {Screen.height}");
                    if (!$"{component.Size}".Equals($"({Screen.width}, {Screen.height})")){ // * panelRenderAutomap (scales with DummyPanelAutomap so doesn't matter)
                        PanelRenderAutomap = component as Panel;
                    }
                }
            }

            // TODO: find better way to select this panels?
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * is DummyPanelAutomap: buildingNamePlates
                        DummyPanelAutomap = component as Panel;
                        // component.AutoSize = AutoSizeModes.ResizeToFill; // ! Stretches map texture
                        // component.AutoSize = AutoSizeModes.ScaleToFit; // ! Has bars on the sides.
                        // component.AutoSize = AutoSizeModes.Scale; // ! Doesn't scale up
                        // component.AutoSize = AutoSizeModes.None; // ! Doesn't scale up
                        component.AutoSize = AutoSizeModes.ScaleFreely; // * Scales perfectly, without bars on side and without stretching the texture.
                        component.HorizontalAlignment = HorizontalAlignment.Center;
                        component.VerticalAlignment = VerticalAlignment.Middle;
                        Debug.Log($"WandererHud ExteriorMap Resize: DummyPanelAutomap.Rectangle: {component.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!
                        // TODO: fix nameplates
                        // foreach (TextLabel SubComponent in ExteriorMapWindow.PanelRenderAutomap.Components.Cast<TextLabel>()){}
                    }
                }
            }

            ExteriorMapWindow.UpdateAutomapView();
        }

        public void PrintDebug(){
            // * ExteriorMapWindow.ParentPanel
                // * 320.0, 200.0: native Panel
                // * 1144.8, 608.4: panelRenderAutomap (map texture) Scales with DummyPanelAutomap.
            // * ExteriorMapWindow.NativePanel
                // * 318.0, 169.0: DummyPanelAutomap (nameplates)
                // * 320.0, 10.0: panelCaption (map legend)
                // * 76.0, 17.0: dummyPanelCompass (map click function)
            
            foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
                if ($"{component.Size}".Equals("(1144.8, 608.4)")){ // * panelRenderAutomap (scales with DummyPanelAutomap so doesn't matter)
                    component.BackgroundColor = Color.magenta;
                    BaseComponentLog(component, "panelRenderAutomap");
                }
            }
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * DummyPanelAutomap
                        // component.BackgroundColor = Color.green;
                        BaseComponentLog(component, "DummyPanelAutomap");
                    }
                }
            }
        }

        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e){
            // if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){}
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                if (!ButtonsDisabled){ 
                    DisableComponents();
                    ButtonsDisabled = true;
                }
                ResizeMapPanels();
                // PrintDebug();
            }
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
