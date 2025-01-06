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
        // 
        public DaggerfallAutomapWindow InteriorMapWindow {
            get {return DaggerfallUI.Instance.AutomapWindow;}
        }
        public bool InteriorMapComponentsDisabled = false;
        public Panel PanelRenderAutomapInterior;
        // 
        public DaggerfallExteriorAutomapWindow ExteriorMapWindow {
            get {return DaggerfallUI.Instance.ExteriorAutomapWindow;}
        }
        public bool ExteriorMapComponentsDisabled = false;
        public Panel PanelRenderAutomapExterior;
        // 
        public Vector2 LastScreen = new Vector2(0, 0);
        public float ResizeWaitSecs = 0f;
        public bool ResizeWaiting = false;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<ExteriorMapOverwrites>();
            mod.IsReady = true;
        }

        private void Start(){
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            SetLastScreen();
       }

        private void Update(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow || DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
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
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
                InteriorMapWindow.NativePanel.Size = InteriorMapWindow.ParentPanel.Rectangle.size;
            }
        }

        public void DisableInteriorMapComponents(){
            foreach (BaseScreenComponent component in InteriorMapWindow.ParentPanel.Components){
                if (component is Outline){ continue; }
                if (component == InteriorMapWindow.NativePanel){ continue; }
                if (component.Enabled && component is Panel){
                    if (!component.BackgroundTexture){
                        component.Enabled = false; // * Micro-Map
                        continue;
                    }
                }
            }

            foreach (BaseScreenComponent component in InteriorMapWindow.NativePanel.Components){
                if (component is Outline){ continue; }
                // TODO: Find and disable hotkeys to toggle 3D/2D view.
                // TODO: Find and disable hotkeys to toggle wiremesh.
                else if (component is Button || component is HUDCompass || component is TextLabel){ // * Buttons and Compass Texture
                    component.Enabled = false; 
                    continue;
                }
                else if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    component.Enabled = false;
                    continue;
                }
                else if (component is Panel && $"{component.Size}".Equals("(28.0, 28.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    component.Enabled = false;
                    continue;
                }
            }
        }

        public void DisableExteriorMapComponents(){
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component is Outline){ continue; }
                else if (component is Button || component is HUDCompass){ // * Buttons and Compass Texture
                    component.Enabled = false;
                    continue;
                }
                // TODO: find better way to select these panels? 
                    // todo: If this works for different screen sizes than is fine? 
                    // todo: Make a PR to make these public?
                else if (component is Panel && $"{component.Size}".Equals("(320.0, 10.0)")){ // * panelCaption (map legend)
                    component.Enabled = false;
                    continue;
                }
                else if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    component.Enabled = false;
                    continue;
                }
            }
        }

        public void ResizeInteriorMapPanels(){
            foreach (BaseScreenComponent component in InteriorMapWindow.ParentPanel.Components){
                if (component is Outline){ continue; }
                if (component == InteriorMapWindow.NativePanel){ continue; }
                if (component.Enabled && component is Panel){
                    if (component.BackgroundTexture){
                        PanelRenderAutomapInterior = component as Panel; // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
                    }
                }
            }
            // 
            foreach (BaseScreenComponent component in InteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * is DummyPanelAutomap: buildingNamePlates
                        component.AutoSize = AutoSizeModes.ScaleFreely;
                        component.HorizontalAlignment = HorizontalAlignment.Center;
                        component.VerticalAlignment = VerticalAlignment.Middle;
                    }
                }
            }
            ForceResizeMap(); // ! Set size to apply autoscaling!
        }

        public void ResizeExteriorMapPanels(){
            ExteriorMapWindow.NativePanel.AutoSize = AutoSizeModes.ResizeToFill;
            foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
                if (component.Enabled && component is Panel){
                    if (!$"{component.Size}".Equals($"({Screen.width}, {Screen.height})")){ // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
                        PanelRenderAutomapExterior = component as Panel; // ! If equal to screen size = NativePanel. Else, is panelRenderAutoMap.
                    }
                }
            }
            // TODO: find better way to select this panels?
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * is DummyPanelAutomap: buildingNamePlates
                        component.AutoSize = AutoSizeModes.ScaleFreely;
                        component.HorizontalAlignment = HorizontalAlignment.Center;
                        component.VerticalAlignment = VerticalAlignment.Middle;
                    }
                }
            }
            ForceResizeMap(); // ! Set size to apply autoscaling!
        }

        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e){
            // if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow || DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
            //     DebugLog();
            // }
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
                if (!InteriorMapComponentsDisabled){ 
                    DisableInteriorMapComponents();
                    InteriorMapComponentsDisabled = true;
                }
                ResizeInteriorMapPanels();
            }
            else if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                if (!ExteriorMapComponentsDisabled){ 
                    DisableExteriorMapComponents();
                    ExteriorMapComponentsDisabled = true;
                }
                ResizeExteriorMapPanels();
            }
        }

        // * Exterior Map Notes:
            // * ExteriorMapWindow.ParentPanel
                // * 320.0, 200.0: native Panel
                // * 1144.8, 608.4: panelRenderAutomap (map texture) Scales with DummyPanelAutomap.
                    // ! Inconsistent Sizing: has a texture2D
            // * ExteriorMapWindow.NativePanel
                // * 318.0, 169.0: DummyPanelAutomap (nameplates)
                // * 320.0, 10.0: panelCaption (map legend)
                // * 76.0, 17.0: dummyPanelCompass (map click function)
        // * Interior Map Notes:
            // * InteriorMapWindow.ParentPanel
                // * 320.0, 200.0: native Panel (ScaleToFit by default)
                // * 974.7, 518.0: panelRenderAutomap (map texture) Scales with DummyPanelAutomap. Has texture2d
                    // ! Inconsistent Sizing: has a texture2D
                //  * 85.8, 85.8: panelRenderOverlay (micro-map).
                    // ! Inconsistent Sizing
            // * InteriorMapWindow.NativePanel
                // * 318.0, 169.0: DummyPanelAutomap (nameplates)
                //  * 28.0, 28.0: (dummyPanelOverlay) -> for PanelRenderOverlay (micro-map).
                // * 76.0, 17.0: dummyPanelCompass (map click function)
                // * 0.0, 7.0: TextLabel (labelHoverText)
        
        public void DebugLog(){
            Debug.Log($"WandererHud: DEBUG.LOG======");
            Debug.Log($"\nWandererHud: INTERIOR========\n");
            BaseComponentLog(InteriorMapWindow.ParentPanel, "WandererHud: INTERIOR-ParentPanel: ");
            foreach (BaseScreenComponent component in InteriorMapWindow.ParentPanel.Components){
                if (component is Outline){ continue; }
                if (component == InteriorMapWindow.NativePanel){ continue; }
                if (component.Enabled && component is Panel){
                    if (!component.BackgroundTexture){
                        BaseComponentLog(InteriorMapWindow.ParentPanel, "WandererHud: INTERIOR-ParentPanel: MicroMap: ");
                    }else{
                        BaseComponentLog(InteriorMapWindow.ParentPanel, "WandererHud: INTERIOR-ParentPanel: panelRenderAutomap (map texture): ");
                    }
                }
                continue;
            }
            BaseComponentLog(InteriorMapWindow.NativePanel, "WandererHud: INTERIOR-NativePanel: ");
            foreach (BaseScreenComponent component in InteriorMapWindow.NativePanel.Components){
                if (component is Outline){ continue; }
                if (component is Button || component is HUDCompass || component is TextLabel){ // * Buttons and Compass Texture
                    BaseComponentLog(component, "WandererHud: INTERIOR-NativePanel: Button/HudCompass/Text: ");
                    continue;
                }
                if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    BaseComponentLog(component, "WandererHud: INTERIOR-NativePanel: dummyPanelCompass (compass click)): ");
                    continue;
                }
                if (component is Panel && $"{component.Size}".Equals("(28.0, 28.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    BaseComponentLog(component, "WandererHud: INTERIOR-NativePanel: dummyPanelOverlay (micro-map panel)): ");
                    continue;
                }
                if (component is Panel && $"{component.Size}".Equals("(318.0, 169.0)")){ // * is DummyPanelAutomap: buildingNamePlates
                    BaseComponentLog(component, "WandererHud: INTERIOR-NativePanel: DummyPanelAutomap (buildingNamePlates)): ");
                    continue;
                }
            }

            // Debug.Log($"\nWandererHud: EXTERIOR========\n");
            // BaseComponentLog(ExteriorMapWindow.ParentPanel, "WandererHud: EXTERIOR-ParentPanel: ");
            // foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
            //     if (component.Enabled && component is Panel){
            //         if (!$"{component.Size}".Equals($"({Screen.width}, {Screen.height})")){ // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
            //             BaseComponentLog(component, "WandererHud: EXTERIOR-ParentPanel: panelRenderAutomap (map texture)");
            //         }
            //     }
            // }
            // BaseComponentLog(ExteriorMapWindow.NativePanel, "WandererHud: EXTERIOR-NativePanel: ");
            // foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
            //     if (component is Outline){ continue; }
            //     if (component is Button || component is HUDCompass){ // * Buttons and Compass Texture
            //         BaseComponentLog(component, "WandererHud: EXTERIOR-NativePanel: Button/HudCompass: ");
            //         continue;
            //     }
            //     if (component is Panel && $"{component.Size}".Equals("(320.0, 10.0)")){ // * panelCaption (map legend)
            //         BaseComponentLog(component, "WandererHud: EXTERIOR-NativePanel: panelCaption: ");
            //         continue;
            //     }
            //     if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
            //         BaseComponentLog(component, "WandererHud: EXTERIOR-NativePanel: dummyPanelCompass (click): ");
            //         continue;
            //     }
            //     if (component is Panel && $"{component.Size}".Equals("(318.0, 169.0)")){ // * is DummyPanelAutomap: buildingNamePlates
            //         BaseComponentLog(component, "WandererHud: EXTERIOR-NativePanel: DummyPanelAutomap (buildingNamePlates): ");
            //         continue;
            //     }
            // }
        }
        public void PanelChildrenLog(Panel panel, string Prefix = ""){
            foreach (BaseScreenComponent component in panel.Components){
                if (!component.Enabled){
                    Debug.Log($"===");
                    PrefixLog(Prefix, $"Panel Child (disabled): {component}");
                    continue;
                }
                if (component is Outline || component is Button){
                    Debug.Log($"===");
                    PrefixLog(Prefix, $"Panel Child: {component}");
                    continue;
                }
                BaseComponentLog(component, Prefix + "Panel Child: ");
            }
        }

        public void BaseComponentLog(BaseScreenComponent component, string Prefix = ""){
            Debug.Log($"===");
            PrefixLog(Prefix, $"| Component: {component}");
            PrefixLog(Prefix, $"| Enabled: {component.Enabled}");
            PrefixLog(Prefix, $"| Size: {component.Size}");
            PrefixLog(Prefix, $"| AutoSize: {component.AutoSize}");
            // PrefixLog(Prefix, $"| BackgroundColor: {component.BackgroundColor}");
            PrefixLog(Prefix, $"| BackgroundTexture: {component.BackgroundTexture}");
            // PrefixLog(Prefix, $"| BackgroundTextureLayout: {component.BackgroundTextureLayout}");
            // PrefixLog(Prefix, $"| BackgroundCroppedRect: {component.BackgroundCroppedRect}");
            PrefixLog(Prefix, $"| HorizontalAlignment: {component.HorizontalAlignment}");
            PrefixLog(Prefix, $"| VerticalAlignment: {component.VerticalAlignment}");
            // PrefixLog(Prefix, $"| UseRestrictedRenderArea: {component.UseRestrictedRenderArea}");
            // PrefixLog(Prefix, $"| MinAutoScale: {component.MinAutoScale}");
            // PrefixLog(Prefix, $"| MaxAutoScale: {component.MaxAutoScale}");
            // PrefixLog(Prefix, $"| Scale: {component.Scale}");
            // PrefixLog(Prefix, $"| LocalScale: {component.LocalScale}");
            if (component is Panel panel){
                PrefixLog(Prefix, $"| Panel Child Count: {panel.Components.Count}");
                // PanelChildrenLog(panel, "\t");
            }
        }
        public void PrefixLog(string Prefix, string LogComponent){
            Debug.Log(Prefix + LogComponent);
        }
    }
}
