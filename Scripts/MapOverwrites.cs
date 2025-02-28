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
using static DaggerfallWorkshop.Game.UserInterface.HUDActiveSpells;
using static DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallAutomapWindow;

// * Makes maps fullscreen (with autoscaling and size setting).
// * Disables unnessary ui elements in exterior/interior maps..
// * Disabled inner components of interior map (e.g., beacons)
// Todo: Refactor into different files.
namespace MapOverwritesMod{
    public class MapOverwrites : MonoBehaviour{
        public static Mod mod;
        // * Everything stuff
        public static ModSettings WandererHudSettings;
        public Vector2 LastScreen = new Vector2(0, 0);
        // * Map Stuff
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
        // 
        public class ParentDestroyer : MonoBehaviour { void OnDestroy(){Destroy(transform.parent.parent.gameObject); } }

        // * HUD STUFF: 
        public static Texture2D debugTexture;
        public static Rect debugTextRect = new Rect(0, 0, 1, 1);
        public static Rect debugPosition = new Rect(0, 0, 0, 0);
        public static Color32 debugColor = new Color32(255, 255, 255, 255);
        // 
        const string breathBarFilename = "BreathBar";
        public static WandererHUDBreathBar wandererBreathBar;
        // 
        public static Vector2 compassBoxSize = new Vector2(0, 0);
        public static HUDCompass wandererCompass;
        public static Vector2 defaultWandererCompassScale;
        public static float wandererCompassScale = 1f;
        public static HUDVitals wandererVitals;
        static HorizontalAlignment wandererCompassHorizontalAlignment = HorizontalAlignment.Center;
        static VerticalAlignment wandererCompassVerticalAlignment = VerticalAlignment.Bottom;
        //
        static HUDInteractionModeIcon interactionModeIcon;
        static bool HUDInteractionModeIconEnabled = false;
        static float HUDInteractionModeIconScale = 0.5f;
        static HorizontalAlignment HUDInteractionModeHorizontalAlignment;
        static VerticalAlignment HUDInteractionModeVerticalAlignment;
        static Vector2 interactionModeIconPosition;
        static Vector2 interactionModeSize = Vector2.zero;
        // 
        public static EscortingNPCFacePanel facePanelsParent;
        static List<Panel> facePanels;
        public static Vector2 facePanelsScale = Vector2.zero;
        const int facePanelsDefaultSize = 48;
        public static bool facePanelsHorizontalOrientation = false;
        static HorizontalAlignment facePanelsHorizontalAlignment;
        static VerticalAlignment facePanelsVerticalAlignment;
        public static bool facePanelsEnable = false;
        public static Vector2 firstFacePosition = Vector2.zero;
        public static Vector2 firstFaceSize = Vector2.zero;
        public static int enabledFaceCount = 0;
        public static List<Vector2> facePanelsPositions = new List<Vector2>();
        // 
        public static HUDActiveSpells activeSpellsPanel = null;
        public static Vector2 activeSpellsScale = new Vector2(8, 8);
        public static List<ActiveSpellIcon> activeSelfList = new List<ActiveSpellIcon>();
        public static List<ActiveSpellIcon> activeOtherList = new List<ActiveSpellIcon>();
        public static Panel[] iconPool;
        const int maxIconPool = 24;
        public static bool activeSpellsHorizontalOrientation = true;
        static HorizontalAlignment activeSpellsHorizontalAlignment = HorizontalAlignment.Center;
        static VerticalAlignment activeSpellsVerticalAlignment = VerticalAlignment.Bottom;
        public static Vector2 firstBuffPosition = Vector2.zero;
        public static Vector2 firstDeBuffPosition = Vector2.zero;
        public static int enabledDeBuffCount = 0;
        public static int enabledBuffCount = 0;
        public static Rect spellEffectsBuffsRect = Rect.zero;
        public static List<Vector2> activeSelfListPositions = new List<Vector2>();
        public static List<Vector2> activeOtherListPositions = new List<Vector2>();
        // 
        public static bool hudElementsInitalized = false;
        static Vector2 edgeMargin = new Vector2(10, 10);
        // 
        static float inGameAspectX = 0;
        static bool updateOnUnPause = false;
        // 
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
            DaggerfallUI.Instance.DaggerfallHUD.ShowInteractionModeIcon = false;
            // 
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            SaveLoadManager.OnLoad += (_) => SaveLoadManager_OnLoad();
            PlayerEnterExit.OnTransitionInterior += (_) => OnTransitionToAnyInterior();
            PlayerEnterExit.OnTransitionDungeonInterior += (_) => OnTransitionToAnyInterior();
            PlayerGPS.OnEnterLocationRect += (_) => OnNewExteriorLocation();
            //
            // * Names = names of prefab files.
            PlayerInteriorArrowPrefab = mod.GetAsset<GameObject>("InteriorArrow", false);
            PlayerExteriorArrowPrefab = mod.GetAsset<GameObject>("ExteriorArrow", false);
            ExitDoorPrefab = mod.GetAsset<GameObject>("DungeonExit", false);
            NotePrefab = mod.GetAsset<GameObject>("Note", false);
            TeleportEnterPrefab = mod.GetAsset<GameObject>("TeleportEnter", false);
            TeleportExitPrefab = mod.GetAsset<GameObject>("TeleportExit", false);
            TeleporterConnectionColor = mod.GetAsset<Material>("Door_Inner_Blue", false);
            // * HUD:
            Texture2D compasImage = mod.GetAsset<Texture2D>("COMPBOX.IMG", false);
            compassBoxSize = new Vector2(compasImage.width, compasImage.height);
            wandererCompass = new HUDCompass();
            wandererVitals = new HUDVitals();
            wandererBreathBar = new WandererHUDBreathBar(mod.GetAsset<Texture2D>(breathBarFilename, false));
            // 
            SetLastScreen();
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
        public static object CreateNonPublicClassInstance(object targetObject, string className, params object[] args){
            Type classType = targetObject.GetType().GetNestedType(className, BindingFlags.NonPublic);
            ConstructorInfo constructor = classType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, 
                Array.ConvertAll(args, arg => arg.GetType()), null);
            object classInstance = constructor.Invoke(args);
            return classInstance;
        }

        private void DebugAction(){
            // Debug.Log($"Empty Debug Action");
            // EscortingNPCFacePanel facePanel = DaggerfallUI.Instance.DaggerfallHUD.EscortingFaces;
            // List<FaceDetails> faces = (List<FaceDetails>)GetNonPublicField(facePanel, "faces");
            // faces.RemoveAt(0);
            // facePanel.RefreshFaces();

            // SetFacePanelsValues();
            for (int i = 0; i < facePanels.Count; i++){
                Panel facePanel = facePanels[i];
                if (!facePanel.Enabled){ continue; }
                Debug.Log($"facePanel.Position: {facePanel.Position}");
            }
        }

        private void DebugInputs(){
            if (!Input.anyKey){ return; }

            // Keycodes: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/KeyCode.html
            if (Input.GetKeyDown(KeyCode.Slash)){
                DebugAction();
            }
        }



        public static HorizontalAlignment GetHorizontalAlignmentFromSettings(int settingsVal){
            switch (settingsVal)
            {
                case 0: { return HorizontalAlignment.Left;}
                case 1: { return HorizontalAlignment.Center; }
                case 2: { return HorizontalAlignment.Right; }
            }
            Debug.Log($"WandererHUD: UNKNOWN HORIZONTAL ALIGNMENT");
            return HorizontalAlignment.None;
        }

        public static VerticalAlignment GetVerticalAlignmentFromSettings(int settingsVal){
            switch (settingsVal)
            {
                case 0: { return VerticalAlignment.Top;}
                case 1: { return VerticalAlignment.Middle; }
                case 2: { return VerticalAlignment.Bottom; }
            }
            Debug.Log($"WandererHUD: UNKNOWN VERTICAL ALIGNMENT");
            return VerticalAlignment.None;
        }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
            // * Map:
            forceWireFrame = WandererHudSettings.GetBool("InteriorMap", "ForceWireFrame");
            defaultInteriorZoomOut = WandererHudSettings.GetFloat("InteriorMap", "defaultInteriorZoomOut");
            exteriorZoomSpeed = WandererHudSettings.GetFloat("ExteriorMap", "ZoomSpeed");
            interiorZoomSpeed = WandererHudSettings.GetFloat("InteriorMap", "ZoomSpeed");


            // * Interaction Mode
            HUDInteractionModeHorizontalAlignment = HorizontalAlignment.Left;
            HUDInteractionModeVerticalAlignment = VerticalAlignment.Bottom;

            if (!hudElementsInitalized){ return; }

            if (change.HasChanged("Hud", "EnableInteractionIcon")){
                Debug.Log($"changed EnableInteractionIcon");
                
                SetInteractionModeIconValues();
                // // 
                // if (HUDInteractionModeIconEnabled){
                //     // Allign to interaction icon (right)
                //     activeSpellsHorizontalAlignment = HorizontalAlignment.Left;
                //     activeSpellsVerticalAlignment = VerticalAlignment.Bottom;                    
                // }else{
                //     // * Allign to compass (top)
                //     activeSpellsHorizontalAlignment = HorizontalAlignment.Center;
                //     activeSpellsVerticalAlignment = VerticalAlignment.Bottom;
                // }
                SetSpellEffectsValues();
            }
        }

        public static float NormalizeValue(float value, float min, float max){
            return (value - min) / (max - min);
        }
        public static float GetValueFromNormalize(float normalized_value, float min, float max){
            return min + normalized_value * (max - min);
        }

        public static Vector2 GetStartingPositionFromAlignment(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Vector2 panelSize, float boundingWidth, float boundingHeight, Vector2 edgeMargins, float xOffset = 0, float yOffset = 0){
            // * Positions from top/left of the panel!!!! Center it later if needed...
            Vector2 startingPosition = new Vector2(xOffset, yOffset);

            switch(horizontalAlignment){
                case HorizontalAlignment.Center: {
                    startingPosition = new Vector2(
                        startingPosition.x + boundingWidth / 2,
                        startingPosition.y
                    );
                    break;
                }
                case HorizontalAlignment.Left: {
                    startingPosition = new Vector2(
                        startingPosition.x + edgeMargins.x,
                        startingPosition.y
                    );
                    break;
                }
                case HorizontalAlignment.Right: {
                    startingPosition = new Vector2(
                        startingPosition.x + boundingWidth - panelSize.x - edgeMargins.x,
                        startingPosition.y
                    );
                    break;
                }
            }
            switch(verticalAlignment){
                case VerticalAlignment.Bottom: {
                    startingPosition = new Vector2(
                        startingPosition.x,
                        startingPosition.y + boundingHeight - panelSize.y - edgeMargins.y
                    );
                    break;
                }
                case VerticalAlignment.Middle: {
                    startingPosition = new Vector2(
                        startingPosition.x,
                        startingPosition.y + boundingHeight / 2
                    );
                    break;
                }
                case VerticalAlignment.Top: {
                    startingPosition = new Vector2(
                        startingPosition.x,
                        startingPosition.y + edgeMargins.y
                    );
                    break;
                }
            }

            return startingPosition;
        }

        public static bool GetEnabledSpellEffectsChanged(int count, List<ActiveSpellIcon> activeSpellList){
            int panelCount = 0;

            foreach (ActiveSpellIcon spell in activeSpellList){
                if (spell.poolIndex >= maxIconPool){ break; }
                Panel panel = iconPool[spell.poolIndex];
                if (!panel.Enabled) { continue; }
                panelCount += 1;
            }
            if (panelCount != count){
                count = panelCount;
                if (activeSpellList == activeOtherList){
                    enabledDeBuffCount = count;
                }else{
                    enabledBuffCount = count;
                }
                return true;
            }
            return false;
        }

        public static void SetSpellEffectsValues(){
            // * When changing aspect ratio correction.
            // * After unpausing: if aspect ratio correction was changed during pause.
            // * When number of enabled buffs/debuffs changes.
            // * When relevant mod settings change (e.g., hus icon enabled/disabled).
            // ! Should be called AFTER SetWandererCompassValues().
            SetSpellEffectsValuesBuffs(activeSelfList);
            SetSpellEffectsValuesBuffs(activeOtherList);
        }
        public static void SetSpellEffectsValuesBuffs(List<ActiveSpellIcon> activeSpellList){
            if (activeSpellList == activeSelfList){
                Debug.Log($"set buffs values + positions");
                // * When number of enabled Buffs changes.
                spellEffectsBuffsRect = PositionSpellEffectIcons(activeSelfList, Rect.zero);
            }else{
                Debug.Log($"set DeBuffs values + positions");
                // * When number of enabled Debuggs changes
                PositionSpellEffectIcons(activeOtherList, spellEffectsBuffsRect);
            }
        }
        public static void SetSpellEffectsValuesPositions(List<ActiveSpellIcon> activeSpellList){
            List<Vector2> referenceList = new List<Vector2>();
            if (activeSpellList == activeSelfList){
                Debug.Log($"set Buffs positions");
                referenceList = activeSelfListPositions;
            }else if (activeSpellList == activeOtherList){
                Debug.Log($"set DeBuffs positions");
                referenceList = activeOtherListPositions;
            }

            for (int i = 0; i < activeSpellList.Count; i++){
                ActiveSpellIcon spell = activeSpellList[i];
                if (spell.poolIndex >= maxIconPool){ break; }
                Panel panel = iconPool[spell.poolIndex];
                if (!panel.Enabled) { continue; }
                panel.Position = referenceList[i];
            }
        }

        public static Vector2 GetMiddleOffsets(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Vector2 totalPanelSize){
            Vector2 offset = Vector2.zero;
            if (horizontalAlignment == HorizontalAlignment.Center || verticalAlignment == VerticalAlignment.Middle){
                if (horizontalAlignment == HorizontalAlignment.Center){
                    offset.x -= totalPanelSize.x / 2;
                }
                if (verticalAlignment == VerticalAlignment.Middle){
                    offset.y -= totalPanelSize.y / 2;
                }
            }
            return offset;
        }

        public static bool GetEnabledFaceCountChanged(){
            int panelCount = 0;
            foreach (Panel facePanel in facePanels){
                if (!facePanel.Enabled){ continue; }
                panelCount += 1;
            }
            if (panelCount != enabledFaceCount){
                enabledFaceCount = panelCount;
                return true;
            }
            return false;
        }
        public static void SetFacePanelsValues(){
            // * Call when number of enabled faces changes
            if (!facePanelsEnable){ return; }
            if (facePanels.Count <= 0){ return; }
            Debug.Log($"set face panels values");
            // 
            int panelCount = enabledFaceCount;
            Rect boundingBox = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;
            bool growLeft = (facePanelsHorizontalAlignment == HorizontalAlignment.Right);
            bool growUp = (facePanelsVerticalAlignment == VerticalAlignment.Bottom);

            Vector2 offset = Vector2.zero;
            if (facePanelsHorizontalAlignment == HorizontalAlignment.Center || facePanelsVerticalAlignment == VerticalAlignment.Middle){
                Vector2 panelsAggregateSize = new Vector2(facePanelsScale.x, panelCount * facePanelsScale.y);
                if (facePanelsHorizontalOrientation){ panelsAggregateSize = new Vector2(panelCount * facePanelsScale.x, facePanelsScale.y); }
                offset = GetMiddleOffsets(facePanelsHorizontalAlignment, facePanelsVerticalAlignment, panelsAggregateSize);
            }

            Vector2 startingPosition = GetStartingPositionFromAlignment(facePanelsHorizontalAlignment, facePanelsVerticalAlignment, facePanelsScale, boundingBox.width, boundingBox.height, edgeMargin, offset.x, offset.y);

            Vector2 panelPosition = startingPosition;
            facePanelsPositions.Clear();
            for (int i = 0; i < facePanels.Count; i++){
                Panel facePanel = facePanels[i];
                if (i >= facePanelsPositions.Count){
                    facePanelsPositions.Add(Vector2.zero);
                }else{
                    facePanelsPositions[i] = Vector2.zero; // just put something empty here to use up the index.
                }
                if (!facePanel.Enabled){ continue; }
                // * Scale
                facePanel.Size = facePanelsScale;
                // * Position
                facePanel.Position = panelPosition;
                facePanelsPositions[i] = panelPosition; // Store at this index.
                if (i + 1 == facePanels.Count){break;}
                // * Next Panel Position:
                panelPosition = GetPositionOfNextPanel(facePanelsHorizontalOrientation, growLeft, growUp, startingPosition, facePanel.Size, panelPosition);
            }
        }

        public static void SetFacePanelsPositions(){
            for (int i = 0; i < facePanels.Count; i++){
                Panel facePanel = facePanels[i];
                if (!facePanel.Enabled){ continue; }
                facePanel.Position = facePanelsPositions[i];
            }
        }

        public static Vector2 GetPositionOfNextPanel(bool horizontalOrientation, bool growLeft, bool growUp, Vector2 startingPosition, Vector2 panelSize, Vector2 previousPanelPosition){
            if (horizontalOrientation){
                if (growLeft){
                    return new Vector2 (
                        previousPanelPosition.x - panelSize.x, 
                        startingPosition.y
                    );
                }else{
                    return new Vector2 (
                        previousPanelPosition.x + panelSize.x, 
                        startingPosition.y
                    );
                }
            } else {
                if (growUp){
                    return new Vector2 (
                        startingPosition.x,
                        previousPanelPosition.y - panelSize.y
                    );
                } else {
                    return new Vector2 (
                        startingPosition.x,
                        previousPanelPosition.y + panelSize.y
                    );
                }
            }
        }

        public static Rect PositionSpellEffectIcons(List<ActiveSpellIcon> activeSpellList, Rect offsetRect){
            if (activeSpellList.Count <= 0){ return Rect.zero; }
            // 
            int panelCount;
            if (activeSpellList == activeSelfList){ panelCount = enabledBuffCount;
            } else { panelCount = enabledDeBuffCount; }
            if (panelCount == 0) {return Rect.zero; }
            // 
            Rect nativeRect = DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle; // In Parent Scale.  // ! MUST BE CALLED FIRST HERE TO UPDATE AUTO SIZING.
            Vector2 nativeScale = DaggerfallUI.Instance.DaggerfallHUD.NativePanel.LocalScale;
            // * Dividing Parent by NativePanel = Native Space
            // * Multiple Native by NativePanel = Parent Space
            // 
            // * Get total size of panels: 
            // * Get Size and Correct of Middle/Center Alignment if needed:
            Vector2 spellsScale = activeSpellsScale * nativeScale; // convert to parentScale.
            Vector2 panelsAggregateSize = new Vector2(spellsScale.x, panelCount * spellsScale.y); // parentScale
            if (activeSpellsHorizontalOrientation){ panelsAggregateSize = new Vector2(panelCount * spellsScale.x, spellsScale.y); } // parentScale

            // * Start from the correct position base on size:
            float xOffset = 0; // nativeScale
            float yOffset = 0; // nativeScale

            // * Offset Rect: for debuffs to be positioned next to buffs instead of on the same x/y alignment.
            if (offsetRect != Rect.zero){
                if (activeSpellsHorizontalOrientation){
                    if (activeSpellsVerticalAlignment == VerticalAlignment.Bottom){
                        yOffset += -offsetRect.height;
                    }else{
                        yOffset += offsetRect.height;
                    }
                } else {
                    if (activeSpellsHorizontalAlignment == HorizontalAlignment.Right){
                        xOffset += offsetRect.width;
                    }else{
                        xOffset += -offsetRect.width;
                    }
                }
            }

            Vector2 startingPosition;
            if (!HUDInteractionModeIconEnabled){
                // * Extra Offsets: Put above the Compass (will also be in this position)
                yOffset -= wandererCompass.Size.y;
                yOffset -= 0.4f; // some padding away from the compass
                xOffset -= panelsAggregateSize.x / 2;
                startingPosition = GetStartingPositionFromAlignment(activeSpellsHorizontalAlignment, activeSpellsVerticalAlignment, spellsScale, nativeRect.width, nativeRect.height, edgeMargin, xOffset, yOffset);
            }else{
                // Position vertically along middle of compass if hud is on.
                startingPosition.y = GetPanelVerticalMiddle(interactionModeIcon) - panelsAggregateSize.y / 2;
                startingPosition.x = GetPanelHorizontalRight(interactionModeIcon);
                startingPosition.x += 0.4f; // padding
                
                // * horiuzontal correction for nativePadding.
                if (DaggerfallUI.Instance.DaggerfallHUD.NativePanel.AutoSize == AutoSizeModes.ScaleToFit){
                    startingPosition.x -= (DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.x - DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle.x);
                }
            }

            // * Correct for aspect ratio mode correction changes during pause.
            if (GameManager.IsGamePaused){
                startingPosition.y += DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.y;

                if (HUDInteractionModeIconEnabled){
                    if (DaggerfallUI.Instance.DaggerfallHUD.NativePanel.AutoSize == AutoSizeModes.ScaleToFit){
                        if (DaggerfallUnity.Settings.RetroModeAspectCorrection == 1){
                            startingPosition.y -= DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.y;
                            startingPosition.y -= (DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.y - DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle.y);
                            startingPosition.x += (DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.x - DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle.x);
                        }else if (DaggerfallUnity.Settings.RetroModeAspectCorrection == 2){
                            startingPosition.x += (DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.x - DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle.x);
                        }
                    }
                }
            }
            
            // DaggerfallUI.Instance.DaggerfallHUD.NativePanel.BackgroundColor = Color.red; // ! debug

            // * Get total size of panels.
            Rect panelsAggregate = new Rect( // ! must be in parent scale for debuffs to position on side of buffs correctly
                startingPosition.x,
                startingPosition.y,
                panelsAggregateSize.x,
                panelsAggregateSize.y
            );

            startingPosition /= nativeScale;
            Vector2 panelPosition = startingPosition;

            List<Vector2> referenceList;
            if (activeSpellList == activeSelfList){
                firstBuffPosition = startingPosition;
                referenceList = activeSelfListPositions;
                Debug.Log($"set firstBuffPosition: {firstBuffPosition}");
            }else{
                firstDeBuffPosition = startingPosition;
                referenceList = activeOtherListPositions;
            }

            // Apply positions:
            bool growLeft = activeSpellsHorizontalAlignment == HorizontalAlignment.Right;
            bool growUp = activeSpellsVerticalAlignment == VerticalAlignment.Bottom;

            referenceList.Clear();
            for (int i = 0; i < activeSpellList.Count; i++){
                ActiveSpellIcon spell = activeSpellList[i];
                if (i >= referenceList.Count){
                    referenceList.Add(Vector2.zero);
                }else{
                    referenceList[i] = Vector2.zero; // just put something empty here to use up the index.
                }
                if (spell.poolIndex >= maxIconPool){ break; }
                Panel panel = iconPool[spell.poolIndex];
                if (!panel.Enabled) { continue; }
                // 
                // * Scale:
                panel.Size = activeSpellsScale;
                // * Position:
                panel.Position = panelPosition;
                referenceList[i] = panelPosition;
                // * Check if there is a next icon or if this is the last one..
                if (i + 1 == activeSpellList.Count){break;}
                // * Determine next icon position: 
                panelPosition = GetPositionOfNextPanel(activeSpellsHorizontalOrientation, growLeft, growUp, startingPosition, panel.Size, panelPosition);
            }
            return panelsAggregate;
        }

        public static float GetPanelHorizontalRight(BaseScreenComponent component){
            return component.Position.x + component.Size.x;
        }
        public static float GetPanelHorizontalMiddle(BaseScreenComponent component){
            return component.Position.x + component.Size.x / 2;
        }
        public static float GetPanelVerticalMiddle(BaseScreenComponent component){
            return component.Position.y + component.Size.y / 2;
        }

        public static void SetInteractionModeIconValues(){
            // * Call when interaction mode settings changes.
            // * Call when interaction mode icon size changes.

            Debug.Log($"set interaction mode values");
            HUDInteractionModeIconEnabled = WandererHudSettings.GetBool("Hud", "EnableInteractionIcon");
            interactionModeIcon.Enabled = HUDInteractionModeIconEnabled;
            if (!interactionModeIcon.Enabled) { return; }
            Debug.Log($"set interaction mode position");

            SetNonPublicField(interactionModeIcon, "displayScale", HUDInteractionModeIconScale);
            interactionModeIcon.Update(); // Update the size from above scale.

            interactionModeSize = interactionModeIcon.Size;

            Rect boundingBox = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;

            // float xOffset = 0;
            // float yOffset = 0;
            // Vector2 middleOffset = GetMiddleOffsets(HUDInteractionModeHorizontalAlignment, HUDInteractionModeVerticalAlignment, interactionModeSize);
            // xOffset += middleOffset.x;
            // yOffset += middleOffset.y;

            interactionModeIconPosition = GetStartingPositionFromAlignment(
                HUDInteractionModeHorizontalAlignment,
                HUDInteractionModeVerticalAlignment,
                interactionModeSize,
                boundingBox.width, 
                boundingBox.height,
                edgeMargin
            );
            
            // * allign with vertical middle of compass
            interactionModeIconPosition.y = GetPanelVerticalMiddle(wandererCompass) - interactionModeIcon.Size.y / 2;
            interactionModeIcon.Position = interactionModeIconPosition;
        }

        public static void PositionHUDElements(){
            wandererCompass.Update();
            wandererVitals.Update();
            wandererBreathBar.Update();            
            SetWandererCompassValues();
            // SetFacePanelsValues(); // ! updates each update so no need to update it here.
            SetInteractionModeIconValues();
            SetSpellEffectsValues(); // ! should run after interaction mode and compass.
        }

        public static void SetInteractionModeIcon(){
            interactionModeIcon = (HUDInteractionModeIcon)GetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD, "interactionModeIcon");
            interactionModeIcon.SetMargins(Margins.All, 0);
            interactionModeIcon.Enabled = false;
            SetInteractionModeIconValues();
            // interactionModeIcon.HorizontalAlignment = HorizontalAlignment.None;
            // interactionModeIcon.VerticalAlignment = VerticalAlignment.None;
        }

        public static void SetSpellEffects(){
            Debug.Log($"SET SPELL EFFECTS");
            
            activeSpellsPanel = DaggerfallUI.Instance.DaggerfallHUD.ActiveSpells;
            activeSpellsPanel.SetMargins(Margins.All, 0);
            activeSelfList = (List<ActiveSpellIcon>)GetNonPublicField(activeSpellsPanel, "activeSelfList");
            activeOtherList = (List<ActiveSpellIcon>)GetNonPublicField(activeSpellsPanel, "activeOtherList");
            iconPool = (Panel[])GetNonPublicField(activeSpellsPanel, "iconPool");
            firstBuffPosition = Vector2.zero;
            firstDeBuffPosition = Vector2.zero;
        }

        public static void SetFacePanels(){
            facePanelsParent = DaggerfallUI.Instance.DaggerfallHUD.EscortingFaces;
            facePanelsParent.SetMargins(Margins.All, 0);
            facePanelsParent.AutoSize = AutoSizeModes.None;
            facePanels = (List<Panel>)GetNonPublicField(facePanelsParent, "facePanels");
            // // ! ↓ debug ↓
            // // * Add faces:
            // List<FaceDetails> faces = new List<FaceDetails>();
            // faces = (List<FaceDetails>)GetNonPublicField(facePanelsParent, "faces");
            // FaceDetails faceRef = faces[0];
            // faceRef.factionFaceIndex = UnityEngine.Random.Range(0, 61);
            // faces.Add(faceRef);
            // faceRef.factionFaceIndex = UnityEngine.Random.Range(0, 61);
            // faces.Add(faceRef);
            // faceRef.factionFaceIndex = UnityEngine.Random.Range(0, 61);
            // faces.Add(faceRef);
            // facePanelsParent.RefreshFaces();
            // // ! ↑ debug ↑
        }

        public static void SetWandererCompassValues(){
            // * On Retro Aspect Ratio Correct Change 
            // * On relevant settings changes

            Debug.Log($"set wanderer compass");
            wandererCompass.Scale = new Vector2(
                defaultWandererCompassScale.x * wandererCompassScale,
                defaultWandererCompassScale.y * wandererCompassScale
            );
            // 
            wandererCompass.Update(); // * Ensure size is updated after setting the scale above.
            // * Compass Position: 
            float compassWidth = wandererCompass.Size.x;
            float compassHeight = wandererCompass.Size.y;
            Rect screenRect = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;

            // Vector2 middleOffset = GetMiddleOffsets(wandererCompassHorizontalAlignment, wandererCompassVerticalAlignment, wandererCompass.Size);

            // Find alignment position + edge margins.
            Vector2 compassPos = GetStartingPositionFromAlignment(wandererCompassHorizontalAlignment, wandererCompassVerticalAlignment, wandererCompass.Size, screenRect.width, screenRect.height, edgeMargin);
            compassPos.x -= compassWidth/2; // horizontal center.
            compassPos.x += screenRect.x; // if using in-game aspect ratio.
            // 
            wandererCompass.Position = new Vector2(
                compassPos.x,
                compassPos.y
            );
            // * Vitals Positions and Size:
            // * Magic numbers: pixels taken up the vitals in the base compass texture image.
            float compassPixelX = wandererCompass.Scale.x; // 1 Compass Pixel Width
            float compassPixelY = wandererCompass.Scale.y; // 1 Compass Pixel Height
            float vitalsHeight = 13 * compassPixelY;
            float vitalsWidth = 2 * compassPixelX;
            float vitalsBreathWidth = 1 * compassPixelX;
            float vitalsYOffset = 2 * compassPixelX; // From bottom of compass.
            float vitalsHealthXOffset = 3 * compassPixelX; // From Left of compass
            float vitalsBreathXOffset = 4 * compassPixelX; // From Right of compass
            float vitalsFatigueXOffset = 5 * compassPixelX; // From Right of compass
            float vitalsMagickaXOffset = 10 * compassPixelX; // From Right of compass
            // 
            wandererVitals.CustomHealthBarSize = new Vector2(vitalsWidth, vitalsHeight);
            wandererVitals.CustomFatigueBarSize = new Vector2(vitalsWidth, vitalsHeight);
            wandererVitals.CustomMagickaBarSize = new Vector2(vitalsWidth, vitalsHeight);
            wandererBreathBar.CustomBreathBarSize = new Vector2 (vitalsBreathWidth, vitalsHeight);
            // 
            float compassLeft = compassPos.x - screenRect.x; 
            float compassRight = compassPos.x + compassWidth - screenRect.x;
            wandererVitals.CustomHealthBarPosition = new Vector2(compassLeft + vitalsHealthXOffset, 0);
            wandererVitals.CustomFatigueBarPosition = new Vector2(compassRight - vitalsFatigueXOffset,0);
            wandererVitals.CustomMagickaBarPosition = new Vector2(compassRight - vitalsMagickaXOffset,0);
            wandererBreathBar.CustomBreathBarPosition = new Vector2(
                compassRight - vitalsBreathXOffset, 
                compassPos.y + vitalsYOffset
            );
            // 
            float vitalsY = compassPos.y + compassHeight - vitalsYOffset;
            wandererVitals.SetMargins(Margins.Bottom, (int)(screenRect.height - vitalsY));
        }

        public static void SetWandererCompass(){
            DaggerfallUI.Instance.DaggerfallHUD.ShowCompass = false; // ? Will this stay false? or will I have to reset it to false anytime it becomes true?
            DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Components.Add(wandererCompass);
            wandererCompass.AutoSize = AutoSizeModes.ScaleFreely;
            wandererCompass.Size = DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Size;
            wandererCompass.SetMargins(Margins.All, 0);
            wandererCompass.Enabled = true;
            SetNonPublicField(wandererCompass, "compassBoxSize", compassBoxSize);

            // * Ceil (or floor) so dont have to deal with decimal imprecision when alligning vitals to compass.
            defaultWandererCompassScale = new Vector2(
                (float)Math.Round(DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Scale.x), 
                (float)Math.Round(DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Scale.y)
            );
        }
        public static void SetVitalsHud(){
            DaggerfallUI.Instance.DaggerfallHUD.ShowVitals = false; // todo: Will this stay false? or will I have to reset it to false anytime it becomes true?
            DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Components.Add(wandererVitals);
            wandererVitals.Enabled = true;
            wandererVitals.AutoSize = DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.AutoSize;
            wandererVitals.HorizontalAlignment = HorizontalAlignment.None;
            wandererVitals.VerticalAlignment = DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.VerticalAlignment;
            wandererVitals.Position = DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Position;
            wandererVitals.Scale = Vector2.one;
            wandererVitals.Size = DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Size;
            wandererVitals.SetMargins(Margins.All, 0);
            // Health: Fatigue is switched
            VerticalProgress healthBar = (VerticalProgress)GetNonPublicField(wandererVitals, "healthBarGain");
            VerticalProgress fatigueBar = (VerticalProgress)GetNonPublicField(wandererVitals, "fatigueBarGain");
            VerticalProgress magickaBar = (VerticalProgress)GetNonPublicField(wandererVitals, "magickaBarGain");

            VerticalProgressSmoother fatigueBarLoss = (VerticalProgressSmoother)GetNonPublicField(wandererVitals, "fatigueBarLoss");
            VerticalProgressSmoother magickaBarLoss = (VerticalProgressSmoother)GetNonPublicField(wandererVitals, "magickaBarLoss");
            VerticalProgressSmoother healthBarLoss = (VerticalProgressSmoother)GetNonPublicField(wandererVitals, "healthBarLoss");
            // Health: switched with fatigue
            fatigueBar.SetColor(new Color32(171, 135, 65, 255));
            fatigueBarLoss.SetColor(new Color32(63, 39, 31, 255));
            // Fatigue: switched with health
            healthBar.SetColor(new Color32(203, 136, 60, 255));
            healthBarLoss.SetColor(new Color32(115, 59, 0, 255));
            //
            magickaBar.SetColor(new Color32(102, 175, 183, 255));
            magickaBarLoss.SetColor(new Color32(25, 51, 38, 255));
        }
        public static void SetBreathHud(){
            DaggerfallUI.Instance.DaggerfallHUD.ShowBreathBar = false; // todo: Will this stay false? or will I have to reset it to false anytime it becomes true?
            DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Components.Add(wandererBreathBar);
            wandererBreathBar.Enabled = true;
            wandererBreathBar.AutoSize = DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.AutoSize;
            wandererBreathBar.HorizontalAlignment = HorizontalAlignment.None;
            wandererBreathBar.VerticalAlignment = VerticalAlignment.None;
            wandererBreathBar.Position = DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.Position;
            wandererBreathBar.Scale = DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.Scale;
            wandererBreathBar.Size = DaggerfallUI.Instance.DaggerfallHUD.HUDBreathBar.Size;
            wandererBreathBar.SetMargins(Margins.All, 0);
        }

        public Vector2 GetFirstFacePosition(){
            Vector2 position = Vector2.zero;
            foreach (Panel facePanel in facePanels){
                if (!facePanel.Enabled){ continue; }
                position = facePanel.Position;
            }
            return position;
        }
        public Vector2 GetFirstFaceSize(){
            Vector2 size = Vector2.zero;
            foreach (Panel facePanel in facePanels){
                if (!facePanel.Enabled){ continue; }
                size = facePanel.Size;
            }
            return size;
        }

        public Vector2 GetFirstSpellPosition(List<ActiveSpellIcon> activeSpellList){
            // Ensure firstBuffPosition is actually accurate.
            // if (activeSpellList == activeSelfList){
            //     if (activeSpellList.Count == 0){
            //         firstBuffPosition = Vector2.zero;
            //     }
            // }else{
            //     if (activeSpellList.Count == 0){
            //         firstDeBuffPosition = Vector2.zero;
            //     }
            // }

            Panel panel = null;
            foreach (ActiveSpellIcon spell in activeSpellList){
                if (spell.poolIndex >= maxIconPool){ break; }
                panel = iconPool[spell.poolIndex];
                break;
            }
            if (panel == null){
                return Vector2.zero;
            }
            return panel.Position;
        }

        private void LateUpdate(){
            if (!hudElementsInitalized){ return; }
            // * Spells and Compass
            if (GameManager.IsGamePaused){
                wandererCompass.Update();
                wandererVitals.Update();
                wandererBreathBar.Update();
                DaggerfallUI.Instance.DaggerfallHUD.ActiveSpells.Enabled = true; // Gets disabled when opening pause dropdown so must re-enable it here.
            }
            // 

            // * Interaction mode:
            interactionModeIcon.Enabled = HUDInteractionModeIconEnabled;
            if (interactionModeSize != interactionModeIcon.Size){
                interactionModeSize = interactionModeIcon.Size;
                SetInteractionModeIconValues();
            }
            interactionModeIcon.Position = interactionModeIconPosition;

            // * Face Panels:
            DaggerfallUI.Instance.DaggerfallHUD.EscortingFaces.Enabled = facePanelsEnable; // Probably best set this each update in case other mods set it to enabled or something?
            // Position faces must occur every update:
            if (facePanelsEnable){
                if (GetEnabledFaceCountChanged()){ // Returns true if face count has changed. Also gets the new face count. 
                    SetFacePanelsValues(); // New values + position
                }else{
                    SetFacePanelsPositions(); // Just positions.
                }
            }

            // * Spells Values & Positioning
            if (firstBuffPosition != GetFirstSpellPosition(activeSelfList)){
                Debug.Log($"firstBuffPosition: {firstBuffPosition} != {GetFirstSpellPosition(activeSelfList)}");
                
                if (GetEnabledSpellEffectsChanged(enabledBuffCount, activeSelfList)){ // Returns true if enabled buffs changed and sets new count.
                    SetSpellEffectsValuesBuffs(activeSelfList); // Get new values + position.
                } else {
                    SetSpellEffectsValuesPositions(activeSelfList); // Just position
                }
            }
            if (firstDeBuffPosition != GetFirstSpellPosition(activeOtherList)){ // Returns true if enabled DeBuffs changed and sets new count.
                if (GetEnabledSpellEffectsChanged(enabledDeBuffCount, activeOtherList)){ // Returns true if enabled buffs changed and sets new count.
                    SetSpellEffectsValuesBuffs(activeOtherList); // Get new values + position.
                } else {
                    SetSpellEffectsValuesPositions(activeOtherList); // Just position.
                }
            }

            // * Compass & Spells: Aspect ratio changes
            if (inGameAspectX != DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle.x){ // Follows `DaggerfallUnity.Settings.RetroModeAspectCorrection`
                // * During pause, position vitals and spell effects correctly on aspect ratio correction setting change.
                inGameAspectX = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle.x;
                updateOnUnPause = true;
                SetWandererCompassValues(); // New values + position
                SetSpellEffectsValues(); // New values + position
            }
            if (updateOnUnPause && !GameManager.IsGamePaused){
                updateOnUnPause = false;
                SetSpellEffectsValues(); // New values + position
            }
        }

        public void ExteriorZoom(bool ZoomIn = true){
            // * Get Data
            Camera cameraExteriorAutomap = ExteriorAutomap.instance.CameraExteriorAutomap;
            ExteriorAutomap exteriorAutomap = ExteriorAutomap.instance;
            float speed = exteriorZoomSpeed;
            if (ZoomIn){ speed = -speed; }
            // * Apply Zoom
            float zoomSpeedCompensated = speed * exteriorAutomap.LayoutMultiplier; 
            cameraExteriorAutomap.orthographicSize += zoomSpeedCompensated;
            // * Clamp Zoom
            cameraExteriorAutomap.orthographicSize = Math.Min(minimumExteriorZoom * exteriorAutomap.LayoutMultiplier, (Math.Max(maximumExteriorZoom * exteriorAutomap.LayoutMultiplier, cameraExteriorAutomap.orthographicSize)));
        }

        public void InteriorZoom(bool ZoomIn = true){
            // * Get Data
            Camera cameraAutomap = Automap.instance.CameraAutomap;
            float magnitude = Vector3.Magnitude(BeaconRotationPivot.position - cameraAutomap.transform.position);
            // * Reduce zoom magnitude if very close
            if (automapViewMode == AutomapViewMode.View2D){
                if (magnitude <= 150){
                    magnitude *= 0.2f;
                }
            }else{
                if (magnitude <= 30){
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

        private void Update(){
            MapZoom();
            DebugInputs();
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
                return; // todo: why is this return here??? does it not resize during automap window? why not?
            }
            if (Screen.width != LastScreen.x || Screen.height != LastScreen.y){
                ForceResizeAll();
            }
        }

        public void ForceResizeAll(){
            ForceResizeMap();
            // PositionHUDElements();
            SetLastScreen();
        }
        
        private void SetLastScreen(){
            LastScreen.x = Screen.width; 
            LastScreen.y = Screen.height;
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
            ExteriorMapComponentsReplacedAndDisabled = false;
        }

        public void OnTransitionToAnyInterior(){
            ResetInteriorMapObjects();
        }

        public void SaveLoadManager_OnLoad(){
            Debug.Log($"LOADED NEW SAVE");
            if (GameManager.Instance.IsPlayerInside || GameManager.Instance.IsPlayerInsideDungeon || GameManager.Instance.IsPlayerInsideCastle){
                ResetInteriorMapObjects();
            }
            // * Order = Draw Order
            hudElementsInitalized = false;
            SetWandererCompass();
            SetVitalsHud();
            SetBreathHud();
            // 
            SetFacePanels();
            SetInteractionModeIcon();
            SetSpellEffects();
            // debugTexture = DaggerfallUI.CreateSolidTexture(UnityEngine.Color.white, 8);
            // 
            mod.LoadSettings();
            PositionHUDElements();
            hudElementsInitalized = true;
        }

        public void ForceWireFrame(){
            if (forceWireFrame){
                Automap.instance.SwitchToAutomapRenderModeWireframe();
                Automap.instance.SlicingBiasY = float.NegativeInfinity;
                Debug.Log($"MapOverwrites: ForceWireFrame");
                
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

        public Vector3 NormalToEuler(Vector3 normal){
            return Quaternion.FromToRotation(Vector3.up, normal).eulerAngles;
        }

        public void ChangeObjectDirectionToNormal(GameObject obj, Vector3 normal){
            // if (normal.x == 1){} // * No rotation needed
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
            Vector3 translation = -cameraAutomap.transform.forward * (int)defaultInteriorZoomOut;
            cameraAutomap.transform.position += translation;
            Debug.Log($"MapOverwrites: SetInitialInteriorCameraZoom");
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
                objPointer.transform.SetParent(child); // ? dont child to portalMaker (for correct rotation).
                objPointer.transform.name = portalMaker.name;
                // * Correct Rotation:
                if (child.name.EndsWith("Exit")){ 
                    // ? Store Exit Doors Names and rotate them seperately.
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
                            actionModel.position.y == objPointer.transform.position.y - 1 && // ? Must subtract 1 for some reason. Is a unit higher than it should be.
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
                    DisableExteriorMapComponents();
                    ExteriorMapComponentsReplacedAndDisabled = true;
                    Debug.Log($"MapOverwrites: ExteriorMapComponentsReplacedAndDisabled");
                }
                ResizeExteriorMapPanels();
            }
        }

        public void OnGUI(){
            if (!DaggerfallUI.HasInstance || DaggerfallUI.Instance.DaggerfallHUD == null){ return; }

            // * Draw even when paused: Does not control draw order!!!
            if (
                GameManager.IsGamePaused
                && GameManager.Instance.StateManager.CurrentState == StateManager.StateTypes.UI
            ){
                wandererCompass.Draw();
                wandererVitals.Draw();
                interactionModeIcon.Draw();
            }
            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged & !GameManager.Instance.PlayerEntity.IsWaterBreathing){
                wandererBreathBar.Draw();
            }

            // if (interactionModeIcon != null && interactionModeIcon.Enabled){
            //     interactionModeIcon.Draw();
            // }

            if (facePanelsEnable && facePanels != null){
                foreach (Panel facePanel in facePanels){
                    if (!facePanel.Enabled) { continue; }
                    facePanel.Draw();
                }
            }

            if (activeSpellsPanel != null){
                activeSpellsPanel.Draw();
            }

            // if (DaggerfallUI.Instance.DaggerfallHUD.NativePanel != null){
            //     DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Draw();
            // }

            // Debugging
            // if (debugTexture != null){
            //     DaggerfallUI.DrawTextureWithTexCoords(debugPosition, debugTexture, debugTextRect, false, debugColor);
            // }
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
