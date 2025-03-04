using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Linq;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System.Collections.Generic;
using DaggerfallConnect;
using static DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallAutomapWindow;
using WandererHudMod;
using static ModHelpers;
using System.Security.Claims;

public class MapOverwrites : MonoBehaviour
{
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

    public DaggerfallAutomapWindow InteriorMapWindow {get {return DaggerfallUI.Instance.AutomapWindow;}}
    public DaggerfallExteriorAutomapWindow ExteriorMapWindow {get {return DaggerfallUI.Instance.ExteriorAutomapWindow;}}
    // * Prefabs Pointers:
    GameObject PlayerInteriorArrowPrefab;
    GameObject PlayerExteriorArrowPrefab;
    GameObject ExitDoorPrefab;
    GameObject NotePrefab;
    GameObject TeleportEnterPrefab;
    GameObject TeleportExitPrefab;
    // * Object Pointers:
    Material TeleporterConnectionColor;
    GameObject objPointer; // ! If a object is initalized and used only in a SINGLE function/block, it can be initalized in this variable for a negligible performance gain lol. Use a seperate variable for certain prefabs if you need clarity regardless.
    // * Variables
    public bool InteriorMapPanelsDisabled = false; // * Once Per Game. Never reset to false.
    public bool InteriorMapObjectsReplaced = false; // * Reset on interior entrance.
    public bool ExteriorMapComponentsReplacedAndDisabled = false; // * Reset on each load and new exterior location.
    bool ChangedConnectionColor = false; // * Reset on new teleporter connection.
    readonly Dictionary<string, Transform> exitDoorRotationCorrectHelper = new Dictionary<string, Transform>(); // * Reset on Interior Entrance or Interior Load. 
    int notesCount = 0; // * Reset on Interior Entrance or Interior Load.
    int teleporterCount = 0; // * Reset on Interior Entrance or Interior Load.
    Transform BeaconRotationPivot; // * On new dungeon.
    AutomapViewMode automapViewMode; // * On new dungeon & opened map.
    // * Settings
    static bool forceWireFrame = false;
    // 
    static float defaultInteriorZoomOut = 0;
    static float interiorZoomSpeed = 0;
    // 
    static float exteriorZoomSpeed = 0;
    const float maximumExteriorZoom = 25.0f;
    const float minimumExteriorZoom = 250.0f;
    const float defaultExteriorZoomSpeed = 2.0f; // What it is in game.
    // 
    public class ParentDestroyer : MonoBehaviour { void OnDestroy(){Destroy(transform.parent.parent.gameObject); } }
    // 
    public static Mod mod;
    WandererHud wandererHud;

    public void Initalize(WandererHud wandererMod){
        Debug.Log($"MapOverwrites Initalize");
        wandererHud = wandererMod;
        mod = WandererHud.mod;
    }

    public static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
        forceWireFrame = modSettings.GetBool("Maps", "ForceWireMesh");
        defaultInteriorZoomOut = modSettings.GetFloat("Maps", "DefaultMagnificationLevel");
        exteriorZoomSpeed = modSettings.GetFloat("Maps", "ZoomSpeed");
        interiorZoomSpeed = NormalizeValue(exteriorZoomSpeed, 0, 50); // ! 50 = maximum value of zoomspeed setting (no built in wat to get max value?)

        // exteriorZoomSpeed = modSettings.GetFloat("ExteriorMap", "ZoomSpeed");
        // interiorZoomSpeed = modSettings.GetFloat("InteriorMap", "ZoomSpeed");
        Debug.Log($"exteriorZoomSpeed {exteriorZoomSpeed} - interiorZoomSpeed {interiorZoomSpeed}");
    }

    public void SaveLoadManager_OnLoad(){
        if (GameManager.Instance.IsPlayerInside || GameManager.Instance.IsPlayerInsideDungeon || GameManager.Instance.IsPlayerInsideCastle){
            ResetInteriorMapObjects();
        }
    }

    public void ScreenResizeChange(){
        ForceResizeMap();
    }

    // Start is called before the first frame update
    void Start(){
        Debug.Log($"MapOverwrites Start");
        DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
        // SaveLoadManager.OnLoad += (_) => SaveLoadManager_OnLoad();
        PlayerEnterExit.OnTransitionInterior += (_) => OnTransitionToAnyInterior();
        PlayerEnterExit.OnTransitionDungeonInterior += (_) => OnTransitionToAnyInterior();
        PlayerGPS.OnEnterLocationRect += (_) => OnNewExteriorLocation();
        // * Names = names of prefab files.
        PlayerInteriorArrowPrefab = mod.GetAsset<GameObject>("InteriorArrow", false);
        PlayerExteriorArrowPrefab = mod.GetAsset<GameObject>("ExteriorArrow", false);
        ExitDoorPrefab = mod.GetAsset<GameObject>("DungeonExit", false);
        NotePrefab = mod.GetAsset<GameObject>("Note", false);
        TeleportEnterPrefab = mod.GetAsset<GameObject>("TeleportEnter", false);
        TeleportExitPrefab = mod.GetAsset<GameObject>("TeleportExit", false);
        TeleporterConnectionColor = mod.GetAsset<Material>("Door_Inner_Blue", false);
    }

    // Update is called once per frame
    void Update(){
        MapZoom();
        if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
            if (GameObject.Find("Automap/InteriorAutomap/UserMarkerNotes")){
                int newCount = GameObject.Find("Automap/InteriorAutomap/UserMarkerNotes").transform.childCount;
                if (newCount != notesCount){
                    notesCount = newCount;
                    ReplaceNotesMesh();
                }
            }
            if (GameObject.Find("Automap/InteriorAutomap/TeleporterMarkers")){
                int newTeleporterCount = GameObject.Find("Automap/InteriorAutomap/TeleporterMarkers").transform.childCount;
                if (newTeleporterCount != teleporterCount){
                    teleporterCount = newTeleporterCount;
                    ReplaceTeleporters();
                    CorrectExitTeleportersRotation();
                }
            }
            // Change color of teleporter connection
            if (GameObject.Find("Automap/InteriorAutomap/Teleporter Connection") != null && !ChangedConnectionColor){
                GameObject.Find("Automap/InteriorAutomap/Teleporter Connection").GetComponent<MeshRenderer>().material = TeleporterConnectionColor;
                CallNonPublicFunction(DaggerfallUI.UIManager.TopWindow as DaggerfallAutomapWindow, "UpdateAutomapView");
                Debug.Log($"MapOverwrites: overwrite teleporter connection color");
                ChangedConnectionColor = true;
            }else{
                ChangedConnectionColor = false;
            }
        }
    }
    
    public void OnNewExteriorLocation(){
        ExteriorMapComponentsReplacedAndDisabled = false;
    }

    public void OnTransitionToAnyInterior(){
        ResetInteriorMapObjects();
    }

    public void UIManager_OnWindowChangeHandler(object sender, EventArgs e){
        if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
            BeaconRotationPivot = GameObject.Find("Automap/InteriorAutomap/Beacons/BeaconRotationPivotAxis").transform; // TODO: should set if is new dungeon.
            automapViewMode = (AutomapViewMode)GetNonPublicField(DaggerfallUI.UIManager.TopWindow, "automapViewMode"); // TODO: should set if is new dungeon & opened map.
            ForceWireFrame(); // todo: only want to do this first time entering a dungeon and every time until player changes something about this.
            if (!InteriorMapObjectsReplaced){ // if they are not disabled, disable them (needed on each load)
                ReplaceInteriorMapObjects();
                SetInitialInteriorCameraZoom(); // todo: only happens once per save instead of each time entering a interior.
                InteriorMapObjectsReplaced = true;
            }
            if (!InteriorMapPanelsDisabled){ // if they are not disabled, disable them (only needed once per game).
                DisableInteriorMapPanels();
                InteriorMapPanelsDisabled = true;
            }
            ResizeInteriorMapPanels();
        }
        else if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
            if (!ExteriorMapComponentsReplacedAndDisabled){ // if they are not disabled, disable them (on any new exterior location). // todo: should this be set to false on new save load?
                ReplaceExteriorPlayerMarker();
                DisableExteriorMapPanels();
                ExteriorMapComponentsReplacedAndDisabled = true;
                Debug.Log($"MapOverwrites: ExteriorMapComponentsReplacedAndDisabled");
            }
            ResizeExteriorMapPanels();
        }
    }

    public void ResetInteriorMapObjects(){
        Debug.Log($"MapOverwrites: ResetInteriorMap Objects");
        exitDoorRotationCorrectHelper.Clear();
        notesCount = 0;
        teleporterCount = 0;
        InteriorMapObjectsReplaced = false;
    }

    public void ChangeObjectLayer(GameObject obj, int layer){
        obj.layer = layer;
        foreach (Transform ObjChild in obj.transform){
            ObjChild.gameObject.layer = obj.layer;
        }
    }

    public void SlideObjectPosition(GameObject Obj, Vector3 posChange){
        Obj.transform.position = new Vector3(
            Obj.transform.position.x + posChange.x,
            Obj.transform.position.y + posChange.y,
            Obj.transform.position.z + posChange.z
        );
    }

    public void CorrectExitTeleportersRotation(){
        foreach (KeyValuePair<string, Transform> exitDoor in exitDoorRotationCorrectHelper){
            string entranceParentName = exitDoor.Key + "Entrance";
            Transform exitTeleporter = exitDoor.Value;
            foreach (Transform child in GameObject.Find($"Automap/InteriorAutomap/TeleporterMarkers/{entranceParentName}").transform){
                if (child.childCount <= 0){ continue; } // ! Heuristic for if is a entrance door that has been replaced.
                exitTeleporter.eulerAngles = child.eulerAngles;
                exitTeleporter.Rotate(0, 180, 0); // Face opposite direction as entrance teleporter
            }
        }
    }

    public void ReplaceTeleporters(){
        foreach (Transform child in GameObject.Find("Automap/InteriorAutomap/TeleporterMarkers").transform){
            if (child.transform.GetChild(0).transform.childCount > 1){ continue; } // ! Heuristic for already having replaced the teleporter
            // * Disable Existing
            Transform portalMaker = child.transform.GetChild(0).transform;
            portalMaker.GetComponent<MeshRenderer>().enabled = false;
            // * New
            if (child.name.EndsWith("Entrance")){ objPointer = Instantiate(TeleportEnterPrefab); }
            else{ objPointer = Instantiate(TeleportExitPrefab); }
            ChangeObjectLayer(objPointer, portalMaker.gameObject.layer);
            objPointer.transform.position = portalMaker.position;
            objPointer.transform.SetParent(child); // ! dont child to portalMaker (for correct rotation).
            objPointer.transform.name = portalMaker.name;
            // * Correct Rotation:
            if (child.name.EndsWith("Exit")){ 
                // * Store Exit Doors Names and rotate them seperately.
                exitDoorRotationCorrectHelper[child.name.Substring(0, child.name.Length - 4)] = objPointer.transform;
                continue; 
            }
            // * Find matching door:
            Transform matchingActionModel = null;
            string dungeonName = DaggerfallDungeon.GetSceneName(GameManager.Instance.PlayerGPS.CurrentLocation);
            foreach (Transform daggerfallBlock in GameObject.Find($"Dungeon/{dungeonName}").transform){
                if (daggerfallBlock.GetComponent<DaggerfallRDBBlock>() == null){ continue; }
                Transform ActionModels = daggerfallBlock.Find("Action Models");

                foreach (Transform actionModel in ActionModels){
                    if (!actionModel.TryGetComponent<DaggerfallAction>(out DaggerfallAction daggerfallAction)) { continue; }
                    if (daggerfallAction.ActionFlag != DFBlock.RdbActionFlags.Teleport){ continue; }
                    if (daggerfallAction.ModelDescription != "DOR"){ continue; }
                    if (
                        actionModel.position.x == objPointer.transform.position.x &&
                        actionModel.position.y == objPointer.transform.position.y - 1 && // ! Must subtract 1 for some reason. Is a unit higher than it should be.
                        actionModel.position.z == objPointer.transform.position.z
                        ){
                        matchingActionModel = actionModel;
                        break;
                    }
                }
                if (matchingActionModel){ break; }
            }
            if (matchingActionModel){ objPointer.transform.eulerAngles = matchingActionModel.eulerAngles; }
            // * Slide down 1 unit
            SlideObjectPosition(objPointer, new Vector3(0, -0.6f, 0)); 
            Debug.Log($"MapOverwrites: ReplaceTeleporters");
        }
        objPointer = null;
    }
    public void ReplaceNotesMesh(){
        foreach (Transform child in GameObject.Find("Automap/InteriorAutomap/UserMarkerNotes").transform){
            // * Mesh Renderer Disabled: heuristic that it already has a CustomNote.
            if (!child.GetComponent<MeshRenderer>().enabled){ continue; }

            child.GetComponent<MeshRenderer>().enabled = false;

            // * Not CustomNote Found, child one to this:
            objPointer = Instantiate(NotePrefab);
            ChangeObjectLayer(objPointer, child.gameObject.layer);
            objPointer.transform.position = child.transform.position;
            objPointer.transform.SetParent(child.transform);
            SlideObjectPosition(objPointer, new Vector3(0, -0.4f, 0));

            foreach (Transform subChild in objPointer.transform){
                subChild.transform.name = child.name;
                subChild.transform.gameObject.AddComponent<ParentDestroyer>(); // ! Add parent destroyer script to all children: will delete the parent when the child is destroyed, deleting the entire object instead of just the child.
            }
            Debug.Log($"MapOverwrites: ReplaceNotesMesh");
        }
        objPointer = null;
    }

    public void ReplaceInteriorPlayerMarkerArrow(GameObject child){
        // * Disable Existing
        child.GetComponent<MeshRenderer>().enabled = false;
        // * New
        objPointer = Instantiate(PlayerInteriorArrowPrefab);
        ChangeObjectLayer(objPointer, child.layer);
        objPointer.transform.SetPositionAndRotation(child.transform.position, child.transform.rotation);
        // * Correct rotation.
        objPointer.transform.Rotate(0, -90, 0);
        objPointer.transform.SetParent(child.transform);
        objPointer.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        // * Slide Downwards
        SlideObjectPosition(objPointer, new Vector3(0, -0.6f, 0));
        objPointer = null;
    }

    public void ReplaceInteriorExitDoor(GameObject child){
        // * Disable Existing:
        child.transform.GetChild(0).gameObject.SetActive(false);
        child.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = false;
        // * New
        objPointer = Instantiate(ExitDoorPrefab);
        ChangeObjectLayer(objPointer, child.transform.GetChild(1).gameObject.layer);
        objPointer.transform.position = child.transform.GetChild(1).transform.position;
        objPointer.transform.SetParent(child.transform.GetChild(1).transform);
        // * Correct Rotation:
        // * Correct Rotation for Dungeon/Castle:
        if ((GameManager.Instance.IsPlayerInsideDungeon) || (GameManager.Instance.IsPlayerInsideCastle)){
            objPointer.transform.position = GameManager.Instance.PlayerEnterExit.Dungeon.StartMarker.transform.position;
            StaticDoor[] DungeonExitDoors = DaggerfallStaticDoors.FindDoorsInCollections(GameManager.Instance.PlayerEnterExit.Dungeon.StaticDoorCollections, DoorTypes.DungeonExit);
            DaggerfallStaticDoors.FindClosestDoor(
                GameManager.Instance.PlayerEnterExit.Dungeon.StartMarker.transform.position,
                DungeonExitDoors,
                out StaticDoor DungeonExitDoor
            );
            Vector3 doorNormal = DaggerfallStaticDoors.GetDoorNormal(DungeonExitDoor);
            objPointer.transform.rotation = Quaternion.LookRotation(doorNormal, Vector3.up);
            objPointer.transform.Rotate(0, 90, 0);
            // * Slide down if in Castle:
            if (GameManager.Instance.IsPlayerInsideDungeon){
                SlideObjectPosition(objPointer, new Vector3(0, -0.6f, 0));
            }
        }
        // * Rotaion Fix if in Interiors:
        else{
            Vector3 doorNormal = DaggerfallStaticDoors.GetDoorNormal(GameManager.Instance.PlayerEnterExit.Interior.EntryDoor);
            objPointer.transform.rotation = Quaternion.LookRotation(doorNormal, Vector3.up);
            objPointer.transform.Rotate(0, -90, 0);
            // * Slide down:
            SlideObjectPosition(objPointer, new Vector3(0, -1f, 0));
        }
        objPointer = null;
    }

    public void ReplaceInteriorMapObjects(){
        if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside){
            return;
        }
        // * Beacons/Markers:
        foreach (Transform child in GameObject.Find("Automap/InteriorAutomap/Beacons").transform){
            if (child.name == "BeaconEntrancePosition"){
                ReplaceInteriorExitDoor(child.gameObject);
                continue;
            }
            if (child.name == "PlayerMarkerArrow"){
                ReplaceInteriorPlayerMarkerArrow(child.gameObject);
                continue;
            }
            // * Disable any other inner components:
            child.gameObject.SetActive(false);
        }
        Debug.Log($"MapOverwrites: ReplaceInteriorMapObjects");
    }

    public void ReplaceExteriorPlayerMarker(){
        // * Disable existing
        GameObject.Find("Automap/ExteriorAutomap/PlayerMarkerArrowStamp").GetComponent<Transform>().localScale = Vector3.zero;
        GameObject.Find("Automap/ExteriorAutomap/PlayerMarkerCircle").GetComponent<Transform>().localScale = Vector3.zero;
        ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.GetComponent<MeshRenderer>().enabled = false;
        // * Instance New
        objPointer = Instantiate(PlayerExteriorArrowPrefab);
        ChangeObjectLayer(objPointer, ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.layer);
        objPointer.transform.SetPositionAndRotation(ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.transform.position, ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.transform.rotation);
        // * Correct rotation
        objPointer.transform.Rotate(0, -90, 0);
        objPointer.transform.SetParent(ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.transform);
        objPointer = null;
    }

    public void DisableInteriorMapPanels(){
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
        Debug.Log($"MapOverwrites: DisableInteriorMapPanels");
    }

    public void DisableExteriorMapPanels(){
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
        ExteriorMapWindow.UpdateAutomapView();
    }

    public void ResizeInteriorMapPanels(){
        foreach (BaseScreenComponent component in InteriorMapWindow.ParentPanel.Components){
            if (component is Outline){ continue; }
            if (component == InteriorMapWindow.NativePanel){ continue; }
            // if (component.Enabled && component is Panel){} // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
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
        Debug.Log($"MapOverwrites: ResizeInteriorMapPanels");
    }

    public void ResizeExteriorMapPanels(){
        ExteriorMapWindow.NativePanel.AutoSize = AutoSizeModes.ResizeToFill;
        // foreach (BaseScreenComponent component in ExteriorMapWindow.ParentPanel.Components){
        //     if (component.Enabled && component is Panel){
        //         if (!$"{component.Size}".Equals($"({Screen.width}, {Screen.height})")){} // * panelRenderAutomap (map texture) (scales with DummyPanelAutomap)
        //     }
        // }
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
        Debug.Log($"MapOverwrites: ResizeExteriorMapPanels");
    }

    public void ForceResizeMap(){
        if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
            ExteriorMapWindow.NativePanel.Size = ExteriorMapWindow.ParentPanel.Rectangle.size;
        }
        if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
            InteriorMapWindow.NativePanel.Size = InteriorMapWindow.ParentPanel.Rectangle.size;
        }
    }

    public void ForceWireFrame(){
        if (forceWireFrame){
            Automap.instance.SwitchToAutomapRenderModeWireframe();
            Automap.instance.SlicingBiasY = float.NegativeInfinity;
            Debug.Log($"MapOverwrites: ForceWireFrame");
        }
    }

    public void SetInitialInteriorCameraZoom(){
        Camera cameraAutomap = Automap.instance.CameraAutomap;
        Vector3 translation = -cameraAutomap.transform.forward * (int)defaultInteriorZoomOut;
        cameraAutomap.transform.position += translation;
        Debug.Log($"MapOverwrites: SetInitialInteriorCameraZoom");
    }

    public void ExteriorZoom(bool ZoomIn = true){
        if (exteriorZoomSpeed == 0){ return; } // * no need to run extra code just to apply default zoom in.
        // * Get Data
        Camera cameraExteriorAutomap = ExteriorAutomap.instance.CameraExteriorAutomap;
        ExteriorAutomap exteriorAutomap = ExteriorAutomap.instance;
        // * Undo game's default zoom
        float maximumZoom = minimumExteriorZoom * exteriorAutomap.LayoutMultiplier;
        float minimumZoom = maximumExteriorZoom * exteriorAutomap.LayoutMultiplier;
        float defaultZoom = defaultExteriorZoomSpeed;
        if (!ZoomIn){ defaultZoom = -defaultZoom; } // * opposite direction (zoom in should zoom out)
        float zoomSpeedCompensatedDefault = defaultZoom * exteriorAutomap.LayoutMultiplier;
        cameraExteriorAutomap.orthographicSize += zoomSpeedCompensatedDefault;
        cameraExteriorAutomap.orthographicSize = Mathf.Clamp(cameraExteriorAutomap.orthographicSize, minimumZoom, maximumZoom);
        // * Get Wanderer Zoom
        float speed = exteriorZoomSpeed;
        if (ZoomIn){ speed = -speed; }
        float exteriorZoom = speed * exteriorAutomap.LayoutMultiplier; 
        // * Decrease zoom when close to minimum zoom in.
        float currentZoomNormalized = NormalizeValue(cameraExteriorAutomap.orthographicSize, minimumZoom-1 , maximumZoom); // ! -1 is important or may get stuck and not apply ANY zoom after the minimum is reached.
        currentZoomNormalized = CircularEaseOut(currentZoomNormalized);
        exteriorZoom *= currentZoomNormalized;
        // * Apply Zoom
        cameraExteriorAutomap.orthographicSize += exteriorZoom;
        cameraExteriorAutomap.orthographicSize = Mathf.Clamp(cameraExteriorAutomap.orthographicSize, minimumZoom, maximumZoom);
    }

    public void InteriorZoom(bool ZoomIn = true){
        if (interiorZoomSpeed == 0) { return; } // * no need to run extra code just to apply default zoom in.
        // * Get Data
        Camera cameraAutomap = Automap.instance.CameraAutomap;
        float magnitude = Vector3.Magnitude(BeaconRotationPivot.position - cameraAutomap.transform.position);
        // * Reduce zoom magnitude if very close
        if (automapViewMode == AutomapViewMode.View2D){
            if (magnitude <= 150){ // top down view
                magnitude *= 0.2f;
            }
        }else{
            if (magnitude <= 30){ // 3D view.
                magnitude *= 0.2f;
            }
        }
        // * Dont Zoom if too far away (will still apply game's base zoom).
        if (magnitude <= 0 || magnitude >= 10_000) { return; }
        // * Apply Translation
        float zoomSpeedCompensated = interiorZoomSpeed * magnitude;
        Vector3 translation;
        if (ZoomIn){ translation = cameraAutomap.transform.forward * zoomSpeedCompensated;}
        else { translation = -cameraAutomap.transform.forward * zoomSpeedCompensated; }
        cameraAutomap.transform.position += translation;
    }

    private void MapZoom(){
        if (!(DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow || DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow)){
            return;
        }
        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll > 0f){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){ InteriorZoom(); }
            else{ ExteriorZoom(); }
        }
        else if (mouseScroll < 0f){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){ InteriorZoom(false); }
            else{ ExteriorZoom(false); }
        }
    }

}
