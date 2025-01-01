using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Linq;

namespace ExteriorMapOverwritesMod
{
    public class ExteriorMapOverwrites : MonoBehaviour
    {
        private static Mod mod;

        public DaggerfallExteriorAutomapWindow ExteriorMapWindow;
        public bool ButtonsDisabled = false;

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
        }

        public void DisableComponents(){
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component is Button || component is HUDCompass){ // 
                    component.Enabled = false;
                    continue;
                }
                // TODO: find better way to select these panels?
                if (component is Panel && $"{component.Size}".Equals("(320.0, 10.0)")){ // * panelCaption (map legend)
                    // foreach (BaseScreenComponent SubComponent in (component as Panel).Components){ // * Individual Legend components (temple/shop/tavern)
                    //     SubComponent.Size = new Vector2(0, 0);
                    //     SubComponent.Enabled = false;
                    // }
                    // component.Size = new Vector2(0, 0);
                    component.Enabled = false;
                    continue;
                }
                if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    // component.BackgroundColor = Color.blue;
                    // component.Size = new Vector2(0, 0);
                    component.Enabled = false;
                    continue;
                }
            }
        }

        public void ResizeMapPanels(){
            ExteriorMapWindow.NativePanel.AutoSize = AutoSizeModes.ResizeToFill;
            Debug.Log($"WandererHud ExteriorMap: NativePanel.Rectangle: {ExteriorMapWindow.NativePanel.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!

            // TODO: find better way to select this panels?
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * is dummyPanelAutomap: buildingNamePlates
                        // component.AutoSize = AutoSizeModes.ResizeToFill;
                        component.AutoSize = AutoSizeModes.ScaleToFit;
                        Debug.Log($"WandererHud ExteriorMap: dummyPanelAutomap.Rectangle: {component.Rectangle.width}"); // ! Call component.Rectangle for autosize to apply!
                        // TODO: fix nameplates
                        // foreach (TextLabel SubComponent in ExteriorMapWindow.PanelRenderAutomap.Components.Cast<TextLabel>()){}
                    }
                }
            }

            ExteriorMapWindow.UpdateAutomapView();
        }

        public void PrintDebug(){
            // ExteriorMapWindow.ParentPanel
                // 320.0, 200.0: native Panel
                // 1144.8, 608.4: panelRenderAutomap (map texture)
            // ExteriorMapWindow.NativePanel
                // 318.0, 169.0: dummyPanelAutomap (nameplates)
                // 320.0, 10.0: panelCaption (map legend)
                // 76.0, 17.0: dummyPanelCompass (map click function)
            
            // BaseComponentLog(ExteriorMapWindow.ParentPanel, "The parent component: ");
            // foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){ // * ParentPanel = StretchToFill (to entire screen)
            //     if (component.Enabled && component is Panel){
            //         if ($"{component.Size}".Equals("(320.0, 200.0)")){ // * Is NativePanel (ScaleToFit)
            //             component.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            //             component.AutoSize = AutoSizeModes.ResizeToFill;
            //             Debug.Log($"WandererHud ExteriorMap new NativePanel.Rectangle: {component.Rectangle.width}"); // ! Need to call component.Rectangle for autosize to apply!
            //         }
            //         // if ($"{component.Size}".Equals("(1144.8, 608.4)")){ // * panelRenderAutomap (scales with dummyPanelAutomap so doesn't matter)
            //         //     component.BackgroundColor = Color.magenta;
            //         //     component.AutoSize = AutoSizeModes.ResizeToFill;
            //         //     Debug.Log($"WandererHud ExteriorMap new Map Texture Panel rectangle: {component.Rectangle.width}"); // ! Need to call component.Rectangle for autosize to apply!
            //         // }
            //         // BaseComponentLog(component, "parent component: ");
            //      }
            // }    
            foreach (BaseScreenComponent component in ExteriorMapWindow.NativePanel.Components){
                if (component.Enabled && component is Panel){
                    if ($"{component.Size}".Equals("(318.0, 169.0)")){ // * is dummyPanelAutomap: buildingNamePlates
                        // component.BackgroundColor = Color.green;
                        component.AutoSize = AutoSizeModes.ResizeToFill;
                        Debug.Log($"WandererHud ExteriorMap new dummyPanelAutomap.Rectangle: {component.Rectangle.width}"); // ! Need to call component.Rectangle for autosize to apply!
                        // component.Size = new Vector2(0, 0);

                        foreach (TextLabel SubComponent in ExteriorMapWindow.PanelRenderAutomap.Components.Cast<TextLabel>())
                        {
                            // ! Nothing you do here matters! It gets reset on `UpdateAutomapView()` anyway...
                             // * All NamePlates Labels
                            // SubComponent.RectRestrictedRenderArea = component.Rectangle;
                            // SubComponent.RestrictedRenderAreaCustomParent = component as Panel;
                            // SubComponent.TextScale *= 8;
                            // SubComponent.Scale *= 8;
                            // SubComponent.Position = Vector2.zero;

                            Debug.Log($"TextLabel: {SubComponent}");
                            Debug.Log($"| Position: {SubComponent.Position}");
                            Debug.Log($"| TextScale: {SubComponent.TextScale}");
                            // Debug.Log($"| UseRestrictedRenderArea: {SubComponent.UseRestrictedRenderArea}");
                            // Debug.Log($"| RestrictedRenderAreaCoordinateType: {SubComponent.RestrictedRenderAreaCoordinateType}");
                            // Debug.Log($"| RectRestrictedRenderArea: {SubComponent.RectRestrictedRenderArea}");
                            // Debug.Log($"| RestrictedRenderAreaCustomParent: {SubComponent.RestrictedRenderAreaCustomParent}");
                            // Debug.Log($"| Enabled: {SubComponent.Enabled}");
                            // Debug.Log($"| AutoSize: {SubComponent.AutoSize}");
                            Debug.Log($"| LocalScale: {SubComponent.LocalScale}");
                            Debug.Log($"| Scale: {SubComponent.Scale}");
                            // Debug.Log($"| MinAutoScale: {SubComponent.MinAutoScale}");
                            // Debug.Log( $"| MaxAutoScale: {SubComponent.MaxAutoScale}");
                            Debug.Log($"| WandererHud ExteriorMap new TextLabel.Rectangle: {SubComponent.Rectangle.width}"); // ! Need to call component.Rectangle for autosize to apply!
                            Debug.Log($"===");
                            
                            
                            // BaseComponentLog(SubComponent, "TextLabel?: ");
                        }
                    }
                    // 
                    // BaseComponentLog(component, "native component: ");
                 }
            }

            Debug.Log($"WandererHud ExteriorMap new Map Texture Panel rectangle: {ExteriorMapWindow.PanelRenderAutomap.Rectangle.width}"); // ! Need to call component.Rectangle for autosize to apply!
            ExteriorMapWindow.UpdateAutomapView();
            Debug.Log($"WandererHud ExteriorMap new Map Texture Panel rectangle: {ExteriorMapWindow.PanelRenderAutomap.Rectangle.width}"); // ! Need to call component.Rectangle for autosize to apply!
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
        // public void PrintAllComponents(Panel ParentPanel, string Prefix = ""){
        //     foreach (BaseScreenComponent component in ParentPanel.Components){
        //         if (component.Enabled){
        //             if (component is TextLabel){ continue; }
        //             if (component is Panel){
        //                 BaseComponentLog(component, Prefix);
        //                 Prefix += "\t"; // * translates as 1 character in the string.
        //                 PrintAllComponents(component as Panel, Prefix);
        //                 Debug.Log($"Prefix.Length: {Prefix.Length}");
        //                 Prefix = Prefix.Remove(Prefix.Length - 1, 1);
        //                 continue;
        //             }
        //             BaseComponentLog(component, Prefix);
        //         }
        //     }
        // }
        public void BaseComponentLog(BaseScreenComponent component, string Prefix = ""){
            PrefixLog(Prefix, $"| Component: {component}");
            // PrefixLog(Prefix, $"| Enabled: {component.Enabled}");
            // PrefixLog(Prefix, $"| Size: {component.Size}");
            // PrefixLog(Prefix, $"| AutoSize: {component.AutoSize}");
            // PrefixLog(Prefix, $"| BackgroundColor: {component.BackgroundColor}");
            // PrefixLog(Prefix, $"| BackgroundTexture: {component.BackgroundTexture}");
            // PrefixLog(Prefix, $"| BackgroundTextureLayout: {component.BackgroundTextureLayout}");
            // PrefixLog(Prefix, $"| BackgroundCroppedRect: {component.BackgroundCroppedRect}");
            // PrefixLog(Prefix, $"| HorizontalAlignment: {component.HorizontalAlignment}");
            // PrefixLog(Prefix, $"| VerticalAlignment: {component.VerticalAlignment}");
            // PrefixLog(Prefix, $"| UseRestrictedRenderArea: {component.UseRestrictedRenderArea}");
            // PrefixLog(Prefix, $"| MinAutoScale: {component.MinAutoScale}");
            // PrefixLog(Prefix, $"| MaxAutoScale: {component.MaxAutoScale}");
            // PrefixLog(Prefix, $"| Scale: {component.Scale}");
            // PrefixLog(Prefix, $"| LocalScale: {component.LocalScale}");
            // PrefixLog(Prefix, $"===");
        }

        public void PrefixLog(string Prefix, string LogComponent){
            Debug.Log(Prefix + LogComponent);
        }
    }
}
