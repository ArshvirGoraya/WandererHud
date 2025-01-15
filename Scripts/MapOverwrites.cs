using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Collections;
using System.Linq;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

// * Makes maps fullscreen (with autoscaling and size setting).
// * Disables unnessary ui elements in exterior/interior maps..
// * Disabled inner components of interior map (e.g., beacons)

namespace MapOverwritesMod
{
    public class MapOverwrites : MonoBehaviour
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
        public bool InteriorMapInnerComponentsDisabled = false;
        ///
        public Vector2 LastScreen = new Vector2(0, 0);
        public float ResizeWaitSecs = 0f;
        public bool ResizeWaiting = false;
        //
        Material ExitBoxInteriorMat;
        readonly String ExitBoxInteriorName = "ExitBoxInterior";
        // 
        readonly String PlayerArrowPrefabName = "InteriorArrow";
        GameObject PlayerArrowPrefab;
        GameObject PlayerArrowObj;
        // 
        readonly String ExitDoorPrefabName = "DungeonExit";
        GameObject ExitDoorPrefab;
        GameObject ExitDoorObj;
        // 
        static ModSettings WandererHudSettings;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<MapOverwrites>();
            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
            mod.IsReady = true;
        }

        private void Start(){
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            //
            SaveLoadManager.OnLoad += (_) => SaveLoadManager_OnLoad();
            PlayerEnterExit.OnTransitionInterior += (_) => OnTransitionToAnyInterior();
            PlayerEnterExit.OnTransitionDungeonInterior += (_) => OnTransitionToAnyInterior();
            //
            ExitBoxInteriorMat = mod.GetAsset<Material>(ExitBoxInteriorName, false);
            PlayerArrowPrefab = mod.GetAsset<GameObject>(PlayerArrowPrefabName, false);
            ExitDoorPrefab = mod.GetAsset<GameObject>(ExitDoorPrefabName, false);
            SetLastScreen();
       }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
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

        public void OnTransitionToAnyInterior(){
            ResetInteriorMapInnerComponents();
        }

        public void SaveLoadManager_OnLoad(){
            if (GameManager.Instance.IsPlayerInside || GameManager.Instance.IsPlayerInsideDungeon || GameManager.Instance.IsPlayerInsideCastle){
                ResetInteriorMapInnerComponents();
            }
        }

        public void ForceWireFrame(){
            if (WandererHudSettings.GetBool("InteriorMap", "ForceWireFrame")){
                Automap.instance.SwitchToAutomapRenderModeWireframe();
                Automap.instance.SlicingBiasY = float.NegativeInfinity;
            }            
        }

        public void ResetInteriorMapInnerComponents(){
            InteriorMapInnerComponentsDisabled = false;
            // if (GameManager.Instance.IsPlayerInsideDungeon || GameManager.Instance.IsPlayerInsideCastle){
            //     // ForceWireFrame();
            //     if (WandererHudSettings.GetBool("InteriorMap", "RevealAllOnEnter")){
            //         ConsoleCommandsDatabase.ExecuteCommand("map_revealall");
            //     }
            // }
        }

        public void ChangeObjectLayer(GameObject obj, int layer){
            obj.layer = layer;
            foreach (Transform ObjChild in obj.transform){
                ObjChild.gameObject.layer = obj.layer;
            }
        }

        public void ChangeObjectDirectionToNormal(GameObject obj, Vector3 normal){
            // Debug.Log($"original scale: {obj.transform.localScale}");
            Debug.Log($"normal: {normal}");
            // if (normal.x == 1){} // No rotation needed
            if (normal.x == -1){
                obj.transform.Rotate(0, 180, 0);
            }
            if (normal.z == 1){
                obj.transform.Rotate(0, -90, 0);
            }
            else if (normal.z == -1){
                obj.transform.Rotate(0, 90, 0);
            }
            // if (normal.y == 1){} if (normal.y == -1){}
            // float newX = obj.transform.localScale.x;
            // float newY = obj.transform.localScale.y;
            // float newZ = obj.transform.localScale.z;
            // if ((int)normal.x != 0){ newX = obj.transform.localScale.x * (int)normal.x; }
            // if ((int)normal.y != 0){ newY = obj.transform.localScale.y * (int)normal.y; }
            // if ((int)normal.z != 0){ newZ = obj.transform.localScale.z * -(int)normal.z; }
            // Vector3 newScale = new Vector3( newX, newY, newZ );
            // obj.transform.localScale = newScale;
            // Debug.Log($"newScale: {newScale}");
        }

        public void DisableInnerInteriorMapComponents(){
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside){
                return;
            }
            // * Beacons/Markers:
            foreach (Transform child in GameObject.Find("Automap/InteriorAutomap/Beacons").transform){
                if (child.name == "BeaconEntrancePosition"){
                    // * BeaconEntrancePositionMarker
                    child.gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    // * CubeEntrancePositionMarker
                    // child.gameObject.transform.GetChild(1).GetComponent<MeshRenderer>().material = ExitBoxInteriorMat;
                    child.gameObject.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = false;
                    ExitDoorObj = Instantiate(ExitDoorPrefab);
                    ChangeObjectLayer(ExitDoorObj, child.gameObject.transform.GetChild(1).gameObject.layer);
                    // 
                    // ExitDoorObj.transform.SetPositionAndRotation(child.gameObject.transform.GetChild(1).transform.position, child.gameObject.transform.GetChild(1).transform.rotation);
                    ExitDoorObj.transform.position = child.gameObject.transform.GetChild(1).transform.position;
                    ExitDoorObj.transform.SetParent(child.gameObject.transform.GetChild(1).transform);

                    // * Set Rotation/Direction of door:

                    // foreach (StaticDoor door in GameManager.Instance.PlayerEnterExit.ExteriorDoors){
                    //     Debug.Log($"door: ---");
                    //     Debug.Log($"door position: {DaggerfallStaticDoors.GetDoorPosition(door)}");
                    //     Debug.Log($"door doorType: {door.doorType}");
                    //     Debug.Log($"door doorType: {door.doorType}");
                    // }

                    // StaticDoor closestDoor;
                    // Vector3 closestDoorPosition = DaggerfallStaticDoors.FindClosestDoor(GameManager.Instance.PlayerEnterExit.transform.position, GameManager.Instance.PlayerEnterExit.ExteriorDoors, out closestDoor);
                    // // Debug.Log($"closestDoor: {closestDoor}");
                    // // Debug.Log($"closestDoor position: {closestDoor.ownerPosition}");
                    // Debug.Log($"door: ---");
                    // Debug.Log($"closestDoorPosition: {closestDoorPosition}");
                    
                    

                    // ! If in dungeon:
                    if ((GameManager.Instance.IsPlayerInsideDungeon) || (GameManager.Instance.IsPlayerInsideCastle)){
                        // not sure if we need to do anything with dungeons...
                    }
                    // ! If In building:
                    else{
                        // * Use the direction of the normal to scale the object that way (will face that direction)
                        // Vector3 doorNormal = DaggerfallStaticDoors.GetDoorNormal(GameManager.Instance.PlayerEnterExit.Interior.EntryDoor);
                        ChangeObjectDirectionToNormal(ExitDoorObj, DaggerfallStaticDoors.GetDoorNormal(GameManager.Instance.PlayerEnterExit.Interior.EntryDoor));
                    }
                    continue;
                }
                if (child.name == "PlayerMarkerArrow"){
                    child.GetComponent<MeshRenderer>().enabled = false; // * Make Default player marker invisible.
                    // * Create and Child new Player Marker:
                    PlayerArrowObj = Instantiate(PlayerArrowPrefab);
                    // * Set layer to automap to make it properly visible in the automap.
                    ChangeObjectLayer(PlayerArrowObj, child.gameObject.layer);
                    // * Use new Player Marker:
                    PlayerArrowObj.transform.SetPositionAndRotation(child.transform.position, child.transform.rotation);
                    // * Rotate another -90 degrees to correct the rotation.
                    PlayerArrowObj.transform.Rotate(0, -90, 0);
                    PlayerArrowObj.transform.SetParent(child.transform);
                    PlayerArrowObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    continue;
                }
                child.gameObject.SetActive(false);
            }
        }

        public void DisableInteriorMapComponents(){
            InteriorMapWindow.ParentPanel.Components.OfType<Panel>().LastOrDefault().Enabled = false; // * Last panel = MicroMap/panelRenderOverlay.

            foreach (BaseScreenComponent component in InteriorMapWindow.NativePanel.Components){
                if (component is Outline){ continue; }
                else if (component is Button || component is HUDCompass || component is TextLabel){ // * Buttons and Compass Texture
                    component.Enabled = false; 
                    continue;
                }
                else if (component is Panel && $"{component.Size}".Equals("(76.0, 17.0)")){ // * is dummyPanelCompass (controls click function like a button)
                    component.Enabled = false;
                    continue;
                }
                else if (component is Panel && $"{component.Size}".Equals("(28.0, 28.0)")){ // * dummyPanelOverlay -> for PanelRenderOverlay (micro-map).
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
                    PanelRenderAutomapInterior = component as Panel; // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
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
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
                ForceWireFrame();
                if (!InteriorMapInnerComponentsDisabled){
                    DisableInnerInteriorMapComponents();
                    InteriorMapInnerComponentsDisabled = true;
                }
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
                    // ! Inconsistent Sizing: has a texture2D.
                //  * 85.8, 85.8: panelRenderOverlay (micro-map).
                    // ! Inconsistent Sizing: has a texture2D.
                    // ! Always smaller than panelRenderAutomap.
                    // ! Always last in the components collection.
            // * InteriorMapWindow.NativePanel
                // * 318.0, 169.0: DummyPanelAutomap (nameplates)
                //  * 28.0, 28.0: (dummyPanelOverlay) -> for PanelRenderOverlay (micro-map).
                // * 76.0, 17.0: dummyPanelCompass (map click function)
                // * 0.0, 7.0: TextLabel (labelHoverText)

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
