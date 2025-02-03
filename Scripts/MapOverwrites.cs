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
using System.Collections.Generic;
using System.Reflection;
using DaggerfallConnect;
using Boo.Lang;
using JetBrains.Annotations;
using System.Security;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using static DaggerfallConnect.DFCareer;
using System.ComponentModel;

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
        readonly String PlayerArrowPrefabName = "InteriorArrow";
        GameObject PlayerArrowPrefab;
        GameObject PlayerArrowObj;
        // 
        readonly String PlayerArrowPrefabNameExterior = "ExteriorArrow";
        GameObject PlayerArrowPrefabExterior;
        GameObject PlayerArrowObjExterior;
        // 
        readonly String ExitDoorPrefabName = "DungeonExit";
        GameObject ExitDoorPrefab;
        GameObject ExitDoorObj;
        // 
        readonly String NotePrefabName = "Note";
        GameObject NotePrefab;
        GameObject NoteObj;
        // 
        readonly String TeleportEnterName = "TeleportEnter";
        GameObject TeleportEnterPrefab;
        string TeleporterConnectionColorName = "Door_Inner_Blue";
        Material TeleporterConnectionColor;
        bool ChangedConnectionColor = false;
        Dictionary<string, Transform> exitDoorRotationCorrectHelper = new Dictionary<string, Transform>();
        const int exitStringLength = 4;
        readonly String TeleportExitName = "TeleportExit";
        GameObject TeleportExitPrefab;
        // 
        int notesCount = 0;
        int teleporterCount = 0;
        //

        public static HUDCompass wandererCompass = new HUDCompass();
        static bool addedWandererCompass = false;
        public static DaggerfallWorkshop.Utility.Tuple<float, float> wandererCompassLeftBottomPadding;



        public static ModSettings WandererHudSettings;

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
            PlayerGPS.OnEnterLocationRect += (_) => OnNewExteriorLocation();
            //
            PlayerArrowPrefab = mod.GetAsset<GameObject>(PlayerArrowPrefabName, false);
            PlayerArrowPrefabExterior = mod.GetAsset<GameObject>(PlayerArrowPrefabNameExterior, false);
            // * Create and Child new Player Marker:
            ExitDoorPrefab = mod.GetAsset<GameObject>(ExitDoorPrefabName, false);
            NotePrefab = mod.GetAsset<GameObject>(NotePrefabName, false);
            TeleportEnterPrefab = mod.GetAsset<GameObject>(TeleportEnterName, false);
            TeleportExitPrefab = mod.GetAsset<GameObject>(TeleportExitName, false);
            TeleporterConnectionColor = mod.GetAsset<Material>(TeleporterConnectionColorName, false);
            SetLastScreen();
       }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
            if (DaggerfallUI.HasInstance && DaggerfallUI.Instance.DaggerfallHUD != null){
                // SetVitalsHud();
            }
            wandererCompassLeftBottomPadding = WandererHudSettings.GetTupleFloat("Hud", "CompassPaddingLeftBottom");
        }

        public static float NormalizeValue(float value, float min, float max){
            return (value - min) / (max - min);
        }
        public static float GetValueFromNormalize(float normalized_value, float min, float max){
            return min + normalized_value * (max - min);
        }

        public void PositionHUDElements(){
            // * COMPASS:
            Rect screenRect = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;
            float compassX = ((screenRect.x + screenRect.width - (wandererCompass.Size.x)) / 2) + wandererCompassLeftBottomPadding.First; // Allign horizontally + add user padding.
            float compassY = (screenRect.y + screenRect.height - (wandererCompass.Size.y)) - wandererCompassLeftBottomPadding.Second; // Allign to the bottom + add user padding (default = allign to vitals)

            wandererCompass.Position = new Vector2(
                compassX,
                compassY
            );
            // * VITALS:
            // DaggerfallWorkshop.Utility.Tuple<float, float> barSize = WandererHudSettings.GetTupleFloat("Hud", "VirtalsBarSize");
            const int vitalsWidth = 10;
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.CustomHealthBarSize = new Vector2(vitalsWidth, wandererCompass.Size.y);
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.CustomMagickaBarSize = new Vector2(vitalsWidth, wandererCompass.Size.y);
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.CustomFatigueBarSize = new Vector2(vitalsWidth, wandererCompass.Size.y);

            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.CustomHealthBarPosition = new Vector2(compassX - (vitalsWidth * 2), 0);
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.CustomFatigueBarPosition = new Vector2((compassX + wandererCompass.Size.x) - vitalsWidth, 0);
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.CustomMagickaBarPosition = new Vector2(compassX + wandererCompass.Size.x, 0);

            // * BREATH BAR:

            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged & !GameManager.Instance.PlayerEntity.IsWaterBreathing){
                DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.CustomBreathBarSize = new Vector2(vitalsWidth, wandererCompass.Size.y);
                DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.CustomBreathBarPosition = new Vector2(
                    (compassX + wandererCompass.Size.x), 
                    compassY
                );
            }
        }

        public static void SetWandererCompass(){
            // * Enabled: reset to ShowCompass in DaggerfallHUD.
            // * Scale: reset to NativePanel.LocalScale
            // * Position: reset to bottom right of screen size. -> Also relative to compasses size
            // * Size: gets reset on each HUDCompass update relative to compassBoxSize.
            // * AutoSize and Vertical/Horizontal alighments dont seem to matter...

            if (!addedWandererCompass){
                addedWandererCompass = true;
                DaggerfallUI.Instance.DaggerfallHUD.ShowCompass = false; // todo: Will this stay false? or will I have to reset it to false anytime it becomes true?

                DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Components.Add(wandererCompass);
                wandererCompass.Enabled = true;
                // 
                wandererCompass.AutoSize = AutoSizeModes.ScaleFreely;
                wandererCompass.HorizontalAlignment = HorizontalAlignment.None;
                wandererCompass.VerticalAlignment = VerticalAlignment.None;
                // 
                wandererCompass.Position = DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Position;
                wandererCompass.Scale = DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Scale;
                wandererCompass.Size = DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Size;
            }
        }
        public static void SetBreathHud(){
            DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.HorizontalAlignment = HorizontalAlignment.None;
            DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.VerticalAlignment = VerticalAlignment.None;
            DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.SetMargins(Margins.All, 0);
            SetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar, "normalBreathColor", new Color32(111, 242, 255, 255 / 2)); // Blueish
            SetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar, "shortOnBreathColor", new Color32(247, 39, 41, 255 / 2)); // Resdish
        }

        public static object GetNonPublicField(object targetObject, string fieldName){
            FieldInfo fieldInfo = targetObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(targetObject);
        }
        public static void SetNonPublicField(object targetObject, string fieldName, object fieldValue){
            FieldInfo fieldInfo = targetObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(targetObject, fieldValue);
        }

        public static void CallNonPublicFunction(object targetObject, string methodName, object[] parameters = null){
            MethodInfo methodInfo = targetObject.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(targetObject, parameters);
        }

        private void Update(){
            PositionHUDElements();
            
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
                    ChangedConnectionColor = true;
                }else{
                    ChangedConnectionColor = false;
                }
                return;    
            }
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow || DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
                if (Screen.width != LastScreen.x || Screen.height != LastScreen.y){
                    if (!ResizeWaiting){
                        StartCoroutine(WaitSeconds(ResizeWaitSecs));
                    }
                }
                return;
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

        public void OnNewExteriorLocation(){
            ExteriorMapComponentsDisabled = false;
        }

        public void OnTransitionToAnyInterior(){
            ResetInteriorMapInnerComponents();
        }

        public void SaveLoadManager_OnLoad(){
            if (GameManager.Instance.IsPlayerInside || GameManager.Instance.IsPlayerInsideDungeon || GameManager.Instance.IsPlayerInsideCastle){
                ResetInteriorMapInnerComponents();
            }
            SetWandererCompass();
            SetBreathHud();
        }

        public void ForceWireFrame(){
            if (WandererHudSettings.GetBool("InteriorMap", "ForceWireFrame")){
                Automap.instance.SwitchToAutomapRenderModeWireframe();
                Automap.instance.SlicingBiasY = float.NegativeInfinity;
            }            
        }

        public void ResetInteriorMapInnerComponents(){
            InteriorMapInnerComponentsDisabled = false;
            notesCount = 0;
            teleporterCount = 0;
        }

        public void ChangeObjectLayer(GameObject obj, int layer){
            obj.layer = layer;
            foreach (Transform ObjChild in obj.transform){
                ObjChild.gameObject.layer = obj.layer;
            }
        }

        public Vector3 NormalToEuler(Vector3 normal){
            return Quaternion.FromToRotation(Vector3.up, normal).eulerAngles;
        }

        public void ChangeObjectDirectionToNormal(GameObject obj, Vector3 normal){
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
        }

        public void SlideObjectPosition(GameObject Obj, Vector3 posChange){
            Obj.transform.position = new Vector3(
                Obj.transform.position.x + posChange.x,
                Obj.transform.position.y + posChange.y,
                Obj.transform.position.z + posChange.z
            );
        }

        public void SetInitialInteriorCameraZoom(){
            Camera cameraAutomap = Automap.instance.CameraAutomap;
            float zoomOutMagnitude = WandererHudSettings.GetFloat("InteriorMap", "DefaultZoomOut");
            Vector3 translation = -cameraAutomap.transform.forward * (int)zoomOutMagnitude;
            cameraAutomap.transform.position += translation;
        }

        public Vector3 ParseStringToVector3(string stringifiedVector3){
            string[] vectorValue = stringifiedVector3.Split(',');
            return new Vector3(
                float.Parse(vectorValue[0].Trim()),
                float.Parse(vectorValue[1].Trim()),
                float.Parse(vectorValue[2].Trim())
            );
        }

        public void CorrectExitTeleportersRotation(){
            foreach (KeyValuePair<string, Transform> exitDoor in exitDoorRotationCorrectHelper){
                string entranceParentName = exitDoor.Key + "Entrance";
                Transform exitTeleporter = exitDoor.Value;
                foreach (Transform child in GameObject.Find($"Automap/InteriorAutomap/TeleporterMarkers/{entranceParentName}").transform){
                    if (child.childCount <= 0){ continue; }
                    exitTeleporter.eulerAngles = child.eulerAngles;
                    exitTeleporter.Rotate(0, 180, 0); // Face opposite direction as entrance teleporter
                }
            }
        }

        public void ReplaceTeleporters(){
            foreach (Transform child in GameObject.Find("Automap/InteriorAutomap/TeleporterMarkers").transform){
                // * Heuristic for already having replaced the teleporter
                if (child.transform.GetChild(0).transform.childCount > 1){ continue; }
                // * Disable Mesh Renderer
                Transform portalMarker = child.transform.GetChild(0).transform;
                portalMarker.GetComponent<MeshRenderer>().enabled = false;

                // * Replace
                GameObject teleporter;
                if (child.name.EndsWith("Entrance")){
                    teleporter = Instantiate(TeleportEnterPrefab);
                }else{
                    teleporter = Instantiate(TeleportExitPrefab);
                }
                ChangeObjectLayer(teleporter, portalMarker.gameObject.layer);
                teleporter.transform.position = portalMarker.position;
                teleporter.transform.SetParent(child); // * dont child to portalMarker (for correct rotation).
                // * Rename:
                teleporter.transform.name = portalMarker.name;
                // * Rotation:
                if (child.name.EndsWith("Exit")){ 
                    // * Rotate Exit doors seperately.
                    exitDoorRotationCorrectHelper.Add(child.name.Substring(0, child.name.Length - exitStringLength), teleporter.transform);
                    continue; 
                }

                // * Find matching door:
                Transform foundMatchingDoor = null;
                string dungeonName = DaggerfallDungeon.GetSceneName(GameManager.Instance.PlayerGPS.CurrentLocation);
                foreach (Transform daggerfallBlock in GameObject.Find($"Dungeon/{dungeonName}").transform){
                    if (daggerfallBlock.GetComponent<DaggerfallRDBBlock>() == null){ continue; }
                    Transform ActionModels = daggerfallBlock.Find("Action Models");

                    foreach (Transform actionModel in ActionModels){
                        DaggerfallAction daggerfallAction;
                        if (!actionModel.TryGetComponent<DaggerfallAction>(out daggerfallAction)) { continue; }
                        if (daggerfallAction.ActionFlag != DFBlock.RdbActionFlags.Teleport){ continue; }
                        if (daggerfallAction.ModelDescription != "DOR"){ continue; }

                        if (
                            actionModel.position.x == teleporter.transform.position.x &&
                            actionModel.position.y == teleporter.transform.position.y - 1 && // Must subtract 1 for some reason. Is a unit higher than it should be.
                            actionModel.position.z == teleporter.transform.position.z
                            ){
                            foundMatchingDoor = actionModel;
                            break;
                        }
                    }
                    if (foundMatchingDoor){ break; }
                }

                if (foundMatchingDoor){
                    teleporter.transform.eulerAngles = foundMatchingDoor.eulerAngles;
                    // teleporter.transform.Rotate(0, 90, 0);
                }
                // * Slide down 1 unit
                SlideObjectPosition(teleporter, new Vector3(0, -0.6f, 0)); 
            }
        }

        public void ReplaceNotesMesh(){
            foreach (Transform child in GameObject.Find("Automap/InteriorAutomap/UserMarkerNotes").transform){
                // * Mesh Renderer Disabled: heuristic that it already has a CustomNote.
                if (!child.GetComponent<MeshRenderer>().enabled){ continue; }

                child.GetComponent<MeshRenderer>().enabled = false;

                // * Not CustomNote Found, child one to this:
                NoteObj = Instantiate(NotePrefab);
                ChangeObjectLayer(NoteObj, child.gameObject.layer);
                NoteObj.transform.position = child.transform.position;
                NoteObj.transform.SetParent(child.transform);
                SlideObjectPosition(NoteObj, new Vector3(0, -0.4f, 0));

                foreach (Transform subChild in NoteObj.transform){
                    subChild.transform.name = child.name;
                    // subChild.AddComponent<ChildDestroyed>();
                    subChild.transform.gameObject.AddComponent<ChildDestroyed>();
                    // * Add child destoryer script to all children: will delete the parent when the child is destroyed, deleting the entire object instead of just the child.
                }
            }
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
                    child.gameObject.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = false;
                    ExitDoorObj = Instantiate(ExitDoorPrefab);
                    ChangeObjectLayer(ExitDoorObj, child.gameObject.transform.GetChild(1).gameObject.layer);
                    // 
                    ExitDoorObj.transform.position = child.gameObject.transform.GetChild(1).transform.position;
                    ExitDoorObj.transform.SetParent(child.gameObject.transform.GetChild(1).transform);

                    // * Set Rotation/Direction of door:

                    // ! If in dungeon:
                    if ((GameManager.Instance.IsPlayerInsideDungeon) || (GameManager.Instance.IsPlayerInsideCastle)){
                        ExitDoorObj.transform.position = GameManager.Instance.PlayerEnterExit.Dungeon.StartMarker.transform.position;
                        StaticDoor[] DungeonExitDoors = DaggerfallStaticDoors.FindDoorsInCollections(GameManager.Instance.PlayerEnterExit.Dungeon.StaticDoorCollections, DoorTypes.DungeonExit);
                        DaggerfallStaticDoors.FindClosestDoor(
                            GameManager.Instance.PlayerEnterExit.Dungeon.StartMarker.transform.position,
                            DungeonExitDoors,
                            out StaticDoor DungeonExitDoor
                        );

                        // * Dungeon/Castle Rotations:
                        Vector3 doorNormal = DaggerfallStaticDoors.GetDoorNormal(DungeonExitDoor);
                        ExitDoorObj.transform.rotation = Quaternion.LookRotation(doorNormal, Vector3.up);
                        ExitDoorObj.transform.Rotate(0, 90, 0);

                        if (GameManager.Instance.IsPlayerInsideDungeon){
                            SlideObjectPosition(ExitDoorObj, new Vector3(0, -0.6f, 0)); // * Slide down a unit: not in castles?
                        }
                    }
                    // ! If In building:
                    else{
                        // * Use the direction of the normal to scale the object that way (will face that direction)
                        // ChangeObjectDirectionToNormal(ExitDoorObj, DaggerfallStaticDoors.GetDoorNormal(GameManager.Instance.PlayerEnterExit.Interior.EntryDoor));

                        Vector3 doorNormal = DaggerfallStaticDoors.GetDoorNormal(GameManager.Instance.PlayerEnterExit.Interior.EntryDoor);
                        ExitDoorObj.transform.rotation = Quaternion.LookRotation(doorNormal, Vector3.up);
                        ExitDoorObj.transform.Rotate(0, -90, 0);

                        // * Slide Door downwards, just for buildings (not dungeons)
                        SlideObjectPosition(ExitDoorObj, new Vector3(0, -1f, 0));
                    }
                    continue;
                }
                if (child.name == "PlayerMarkerArrow"){
                    child.GetComponent<MeshRenderer>().enabled = false; // * Make Default player marker invisible.
                    PlayerArrowObj = Instantiate(PlayerArrowPrefab);
                    // * Set layer to automap to make it properly visible in the automap.
                    ChangeObjectLayer(PlayerArrowObj, child.gameObject.layer);
                    // * Use new Player Marker:
                    PlayerArrowObj.transform.SetPositionAndRotation(child.transform.position, child.transform.rotation);
                    // * Rotate another -90 degrees to correct the rotation.
                    PlayerArrowObj.transform.Rotate(0, -90, 0);
                    PlayerArrowObj.transform.SetParent(child.transform);
                    PlayerArrowObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                    // * Slide Interior Cursor downwards
                    SlideObjectPosition(PlayerArrowObj, new Vector3(0, -0.6f, 0));
                    continue;
                }
                child.gameObject.SetActive(false);
            }
            SetInitialInteriorCameraZoom();
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


        public void ReplaceExteriorPlayerMarker(){
            // * Simply scales them to 0 so we cannot see them anymore.
            GameObject.Find("Automap/ExteriorAutomap/PlayerMarkerArrowStamp").GetComponent<Transform>().localScale = Vector3.zero;
            GameObject.Find("Automap/ExteriorAutomap/PlayerMarkerCircle").GetComponent<Transform>().localScale = Vector3.zero;
            ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.GetComponent<MeshRenderer>().enabled = false;
            // * Child new 3D marker:
            PlayerArrowObjExterior = Instantiate(PlayerArrowPrefabExterior);
            // * Set layer to automap to make it properly visible in the automap.
            ChangeObjectLayer(PlayerArrowObjExterior, ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.layer);
            // * Use new Player Marker:
            PlayerArrowObjExterior.transform.SetPositionAndRotation(ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.transform.position, ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.transform.rotation);
            // * Rotate another -90 degrees to correct the rotation.
            PlayerArrowObjExterior.transform.Rotate(0, -90, 0);
            PlayerArrowObjExterior.transform.SetParent(ExteriorAutomap.instance.GameobjectPlayerMarkerArrow.transform);
            // PlayerArrowObjExterior.transform.localScale = new Vector3(1, 1, 1);
        }

        public void DisableExteriorMapComponents(){
            ReplaceExteriorPlayerMarker();

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
                // debugPrint();
            }
            else if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                if (!ExteriorMapComponentsDisabled){ 
                    DisableExteriorMapComponents();
                    ExteriorMapComponentsDisabled = true;
                }
                ResizeExteriorMapPanels();
                // debugPrint();
            }
        }

        public void OnGUI(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallPauseOptionsWindow || DaggerfallUI.UIManager.TopWindow is DaggerfallRestWindow){
                // * Draw compass
                wandererCompass.Draw();

                // * Draw BreathBar:
                if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged & !GameManager.Instance.PlayerEntity.IsWaterBreathing){
                    DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.Draw();
                }
                return;
            }
        }

        public void debugPrint(){
            Debug.Log($"Default values:");
            VerticalProgressSmoother healthBar = GetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD.HUDVitals, "healthBar") as VerticalProgressSmoother;
            Debug.Log($"Health: {healthBar.Position} - {healthBar.Size}");

            VerticalProgressSmoother fatigueBar = GetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD.HUDVitals, "fatigueBar") as VerticalProgressSmoother;
            Debug.Log($"Featigue: {fatigueBar.Position} - {fatigueBar.Size}");

            VerticalProgressSmoother magickaBar = GetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD.HUDVitals, "magickaBar") as VerticalProgressSmoother;
            Debug.Log($"Magicka: {magickaBar.Position} - {magickaBar.Size}");
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
