﻿using UnityEngine;
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

// * Makes maps fullscreen (with autoscaling and size setting).
// * Disables unnessary ui elements in exterior/interior maps..
// * Disabled inner components of interior map (e.g., beacons)
// Todo: Refactor into different files.

namespace MapOverwritesMod
{
    public class MapOverwrites : MonoBehaviour
    {
        public static Mod mod;
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
        readonly string TeleporterConnectionColorName = "Door_Inner_Blue";
        Material TeleporterConnectionColor;
        bool ChangedConnectionColor = false;
        readonly Dictionary<string, Transform> exitDoorRotationCorrectHelper = new Dictionary<string, Transform>();
        const int exitStringLength = 4;
        readonly String TeleportExitName = "TeleportExit";
        GameObject TeleportExitPrefab;

        public static Vector2 compassBoxSize = new Vector2(0, 0);
        // 
        int notesCount = 0;
        int teleporterCount = 0;

        const string breathBarFilename = "BreathBar";
        public static HUDCompass wandererCompass;
        public static Vector2 defaultWandererCompassScale;
        public static float wandererCompassScale;
        public static HUDVitals wandererVitals;
        public static WandererHUDBreathBar wandererBreathBar;
        //
        public static Texture2D debugTexture;
        public static Rect debugTextRect = new Rect(0, 0, 1, 1);
        public static Rect debugPosition = new Rect(0, 0, 0, 0);
        public static Color32 debugColor = new Color32(255, 255, 255, 255);
        //
        public static ModSettings WandererHudSettings;
        static HorizontalAlignment wandererCompassHorizontalAlignment;
        static VerticalAlignment wandererCompassVerticalAlignment;
        //
        static HUDInteractionModeIcon interactionModeIcon;
        static bool HUDInteractionModeIconEnabled = true;
        static Vector2 HUDInteractionModeIconPositionOffset = Vector2.zero;
        static Vector2 HUDInteractionModeIconPosition = Vector2.zero;
        static HorizontalAlignment HUDInteractionModeHorizontalAlignment;
        static VerticalAlignment HUDInteractionModeVerticalAlignment;
        // 
        public static EscortingNPCFacePanel facePanelsParent;
        static List<Panel> facePanels;
        // public static float facePanelsScale;
        public static Vector2 facePanelsScale = Vector2.zero;
        const int facePanelsDefaultSize = 48;
        public static bool facePanelsHorizontalOrientation = false;
        static HorizontalAlignment facePanelsHorizontalAlignment;
        static VerticalAlignment facePanelsVerticalAlignment;
        // 
        public static HUDActiveSpells activeSpellsPanel = null;
        public static Vector2 activeSpellsScale = Vector2.zero;
        public static List<ActiveSpellIcon> activeSelfList = new List<ActiveSpellIcon>();
        public static List<ActiveSpellIcon> activeOtherList = new List<ActiveSpellIcon>();
        public static Panel[] iconPool;
        const int maxIconPool = 24;
        public static bool activeSpellsHorizontalOrientation = false;
        static HorizontalAlignment activeSpellsHorizontalAlignment;
        static VerticalAlignment activeSpellsVerticalAlignment;
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
            Texture2D compasImage = mod.GetAsset<Texture2D>("COMPBOX.IMG", false);
            compassBoxSize = new Vector2(compasImage.width, compasImage.height);

            // * HUD:
            wandererCompass = new HUDCompass();
            wandererVitals = new HUDVitals();
            wandererBreathBar = new WandererHUDBreathBar(mod.GetAsset<Texture2D>(breathBarFilename, false));
            // 
            SetLastScreen();
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
            wandererCompassHorizontalAlignment = GetHorizontalAlignmentFromSettings(WandererHudSettings.GetInt("Compass", "HorizontalAlignment"));

            switch (WandererHudSettings.GetInt("Compass", "VerticalAlignment"))
            {
                case 0: { wandererCompassVerticalAlignment = VerticalAlignment.Top; break;}
                case 1: { wandererCompassVerticalAlignment = VerticalAlignment.Bottom; break; }
                // case 2: { wandererCompassVerticalAlignment = VerticalAlignment.Middle; break; }
            }

            wandererCompassScale = WandererHudSettings.GetFloat("Compass", "Scale");

            if (DaggerfallUI.HasInstance && DaggerfallUI.Instance.DaggerfallHUD != null){
                ForceUpdateHUDElements();
            }
            // 
            if (interactionModeIcon != null){
                SetInteractionModeIconValues();
            }
            // facePanelsScale = WandererHudSettings.GetFloat("FacePanels", "Scale");
            facePanelsScale = new Vector2(
                facePanelsDefaultSize * WandererHudSettings.GetFloat("FacePanels", "Scale"),
                facePanelsDefaultSize * WandererHudSettings.GetFloat("FacePanels", "Scale")
            );
            facePanelsHorizontalOrientation = WandererHudSettings.GetBool("FacePanels", "HorizontalOrientation");
            facePanelsHorizontalAlignment = GetHorizontalAlignmentFromSettings(WandererHudSettings.GetInt("FacePanels", "HorizontalAlignment"));
            facePanelsVerticalAlignment = GetVerticalAlignmentFromSettings(WandererHudSettings.GetInt("FacePanels", "VerticalAlignment"));
            // 
            activeSpellsScale = new Vector2(
                WandererHudSettings.GetFloat("ActiveSpells", "Scale"),
                WandererHudSettings.GetFloat("ActiveSpells", "Scale")
            );
            activeSpellsHorizontalOrientation = WandererHudSettings.GetBool("ActiveSpells", "HorizontalOrientation");
            activeSpellsHorizontalAlignment = GetHorizontalAlignmentFromSettings(WandererHudSettings.GetInt("ActiveSpells", "HorizontalAlignment"));
            activeSpellsVerticalAlignment = GetVerticalAlignmentFromSettings(WandererHudSettings.GetInt("ActiveSpells", "VerticalAlignment"));
            // 
        }

        public static float NormalizeValue(float value, float min, float max){
            return (value - min) / (max - min);
        }
        public static float GetValueFromNormalize(float normalized_value, float min, float max){
            return min + normalized_value * (max - min);
        }
        
        public static void ForceUpdateHUDElements(){
            wandererCompass.Update();
            wandererVitals.Update();
            wandererBreathBar.Update();
            PositionHUDElements();
        }

        public static Vector2 GetStartingPositionFromAlignment(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Vector2 panelSize, float boundingWidth, float boundingHeight, float startingX = 0){
            Vector2 startingPosition = new Vector2(startingX, 0);

            switch(horizontalAlignment){
                case HorizontalAlignment.Center: {
                    startingPosition = new Vector2(
                        boundingWidth / 2,
                        startingPosition.y
                    );
                    break;
                }
                case HorizontalAlignment.Right: {
                    startingPosition = new Vector2(
                        boundingWidth - panelSize.x,
                        startingPosition.y
                    );
                    break;
                }
            }
            switch(verticalAlignment){
                case VerticalAlignment.Middle: {
                    startingPosition = new Vector2(
                        startingPosition.x,
                        boundingHeight / 2
                    );
                    break;
                }
                case VerticalAlignment.Bottom: {
                    startingPosition = new Vector2(
                        startingPosition.x,
                        boundingHeight - panelSize.y
                    );
                    break;
                }
            }
            return startingPosition;
        }

        public static void PositionFacePanels(){
            Rect boundingBox = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;
            Vector2  startingPosition = GetStartingPositionFromAlignment(facePanelsHorizontalAlignment, facePanelsVerticalAlignment, facePanelsScale, boundingBox.width, boundingBox.height);

            bool growLeft = (facePanelsHorizontalAlignment == HorizontalAlignment.Right);
            bool growUp = (facePanelsVerticalAlignment == VerticalAlignment.Bottom);

            Vector2 panelPosition = startingPosition;

            int facePanelCount = 0;

            foreach (Panel facePanel in facePanels){
                if (!facePanel.Enabled){ break; }
                facePanelCount += 1 ;
                // * Scale
                facePanel.Size = facePanelsScale;
                
                // * Position
                facePanel.Position = panelPosition;
                if (facePanelsHorizontalOrientation){
                    // 
                    if (growLeft){
                        panelPosition = new Vector2 (
                            panelPosition.x - facePanel.Size.x, 
                            startingPosition.y
                        );
                    }else{
                        panelPosition = new Vector2 (
                            panelPosition.x + facePanel.Size.x, 
                            startingPosition.y
                        );
                    }

                } else {
                    if (growUp){
                        panelPosition = new Vector2 (
                            startingPosition.x,
                            panelPosition.y - facePanel.Size.y
                        );
                    } else {
                        panelPosition = new Vector2 (
                            startingPosition.x,
                            panelPosition.y + facePanel.Size.y
                        );
                    }
                }
            }
            // * Correction for middle alignments:
            if (facePanelsHorizontalAlignment == HorizontalAlignment.Center || facePanelsVerticalAlignment == VerticalAlignment.Middle){
                Vector2 facePanelSize = new Vector2(facePanelsScale.x, facePanelCount * facePanelsScale.y);
                if (facePanelsHorizontalOrientation){
                    facePanelSize = new Vector2(facePanelCount * facePanelsScale.x, facePanelsScale.y);
                }
                Vector2 facePanelPositionOffset = Vector2.zero;
                if (facePanelsHorizontalAlignment == HorizontalAlignment.Center){
                    facePanelPositionOffset = new Vector2(
                        (facePanelSize.x / 2), facePanelPositionOffset.y
                    );
                }
                if (facePanelsVerticalAlignment == VerticalAlignment.Middle){
                    facePanelPositionOffset = new Vector2(
                        facePanelPositionOffset.x, (facePanelSize.y / 2)
                    );
                }
                foreach (Panel facePanel in facePanels){
                    if (!facePanel.Enabled){ break; }
                    facePanel.Position = new Vector2(
                        facePanel.Position.x - facePanelPositionOffset.x,
                        facePanel.Position.y - facePanelPositionOffset.y
                    );
                }
            }
        }

        // ToDo: this is very unoptimized -> repeated loops that dont need to be repeated.
        public static void PositionSpellEffects(){
            DaggerfallUI.Instance.DaggerfallHUD.ActiveSpells.Enabled = true; // Gets disabled when opening pause dropdown so must re-enable it here.
            bool growLeft = (activeSpellsHorizontalAlignment == HorizontalAlignment.Right);
            bool growUp = (activeSpellsVerticalAlignment == VerticalAlignment.Bottom);

            Rect spellEffectsBuffsRect = PositionSpellEffectIcons(activeSelfList);
            Rect spellEffectsDebuffsRect = PositionSpellEffectIcons(activeOtherList);

            if (spellEffectsDebuffsRect == Rect.zero){ return; }

            // * Fix Debuff Positioning so is next to Buffs instead of in the same position as it.
            Vector2 offsetPosition = Vector2.zero;
            // Determine Offset Position
            if (activeSpellsHorizontalOrientation){
                if (activeSpellsVerticalAlignment == VerticalAlignment.Bottom){
                    offsetPosition = new Vector2(offsetPosition.x, spellEffectsBuffsRect.height);
                }else{
                    offsetPosition = new Vector2(offsetPosition.x, -spellEffectsBuffsRect.height);
                }
            } else {
                if (activeSpellsHorizontalAlignment == HorizontalAlignment.Right){
                    offsetPosition = new Vector2(spellEffectsBuffsRect.width, offsetPosition.y);
                }else{
                    offsetPosition = new Vector2(-spellEffectsBuffsRect.width, offsetPosition.y);
                }
            }
            if (offsetPosition == Vector2.zero){ return; }
            // * Perform offset
            foreach (ActiveSpellIcon spell in activeOtherList){
                if (spell.poolIndex >= maxIconPool){ break; }
                Panel panel = iconPool[spell.poolIndex];
                if (!panel.Enabled) { continue; }
                panel.Position = new Vector2(
                    panel.Position.x - offsetPosition.x,
                    panel.Position.y - offsetPosition.y
                );
            }

        }
        public static Rect PositionSpellEffectIcons(List<ActiveSpellIcon> activeSpellList){
            if (activeSpellList.Count <= 0){ return Rect.zero; }
            Rect boundingBox = new Rect(
                DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.x / DaggerfallUI.Instance.DaggerfallHUD.NativePanel.LocalScale.x,
                DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.y / DaggerfallUI.Instance.DaggerfallHUD.NativePanel.LocalScale.y,
                DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.width / DaggerfallUI.Instance.DaggerfallHUD.NativePanel.LocalScale.x,
                DaggerfallUI.Instance.DaggerfallHUD.NativePanel.Rectangle.height / DaggerfallUI.Instance.DaggerfallHUD.NativePanel.LocalScale.y
            );
            float startingX = 0;
            if (DaggerfallUnity.Settings.RetroRenderingMode == 0 || DaggerfallUnity.Settings.RetroModeAspectCorrection == 0){
                startingX = -boundingBox.x;
            }
            bool growLeft = (activeSpellsHorizontalAlignment == HorizontalAlignment.Right);
            bool growUp = (activeSpellsVerticalAlignment == VerticalAlignment.Bottom);
            int panelCount = 0;

            Vector2 startingPosition = GetStartingPositionFromAlignment(activeSpellsHorizontalAlignment, activeSpellsVerticalAlignment, activeSpellsScale, boundingBox.width, boundingBox.height, startingX);

            Vector2 panelPosition = startingPosition;

            foreach (ActiveSpellIcon spell in activeSpellList){
                if (spell.poolIndex >= maxIconPool){ break; }
                Panel panel = iconPool[spell.poolIndex];
                if (!panel.Enabled) { continue; }
                panelCount += 1;
                // * Scale:
                panel.Size = activeSpellsScale;

                // * Position:
                panel.Position = panelPosition;
                
                // * Determine next icon position: 
                if (activeSpellsHorizontalOrientation){
                    // 
                    if (growLeft){
                        panelPosition = new Vector2 (
                            panelPosition.x - panel.Size.x, 
                            startingPosition.y
                        );
                    }else{
                        panelPosition = new Vector2 (
                            panelPosition.x + panel.Size.x, 
                            startingPosition.y
                        );
                    }

                } else {
                    if (growUp){
                        panelPosition = new Vector2 (
                            startingPosition.x,
                            panelPosition.y - panel.Size.y
                        );
                    } else {
                        panelPosition = new Vector2 (
                            startingPosition.x,
                            panelPosition.y + panel.Size.y
                        );
                    }
                }
            }
            Vector2 panelsAggregateSize = new Vector2(activeSpellsScale.x, panelCount * activeSpellsScale.y);
            if (activeSpellsHorizontalOrientation){
                panelsAggregateSize = new Vector2(panelCount * activeSpellsScale.x, activeSpellsScale.y);
            }
            Rect panelsAggregate = new Rect(
                startingPosition.x,
                startingPosition.y,
                panelsAggregateSize.x,
                panelsAggregateSize.y
            );
            // * Correction for middle alignments:
            Vector2 panelPositionOffset = Vector2.zero;
            if (activeSpellsHorizontalAlignment == HorizontalAlignment.Center || activeSpellsVerticalAlignment == VerticalAlignment.Middle){
                if (activeSpellsHorizontalAlignment == HorizontalAlignment.Center){
                    panelPositionOffset = new Vector2(
                        (panelsAggregate.width / 2), panelPositionOffset.y
                    );
                }
                if (activeSpellsVerticalAlignment == VerticalAlignment.Middle){
                    panelPositionOffset = new Vector2(
                        panelPositionOffset.x, (panelsAggregate.height / 2)
                    );
                }
                foreach (ActiveSpellIcon spell in activeSpellList){
                    if (spell.poolIndex >= maxIconPool){ break; }
                    Panel panel = iconPool[spell.poolIndex];
                    if (!panel.Enabled) { continue; }
                    panel.Position = new Vector2(
                        panel.Position.x - panelPositionOffset.x,
                        panel.Position.y - panelPositionOffset.y
                    );
                }
            }

            panelsAggregate.x = -panelPositionOffset.x;
            panelsAggregate.y = -panelPositionOffset.y;
            return panelsAggregate;
        }

        public static void PositionWandererCompass(){
            wandererCompass.Scale = new Vector2(
                defaultWandererCompassScale.x * wandererCompassScale,
                defaultWandererCompassScale.y * wandererCompassScale
            );

            // * COMPASS:
            Rect screenRect = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;
            float compassWidth = wandererCompass.Size.x;
            float compassHeight = wandererCompass.Size.y;
            float xAnchor = 0;
            float yAnchor;
            float compassX = 0;
            float compassY = 0;
            float vitalsY;

            float compassPixelX = wandererCompass.Scale.x; // 1 Compass Pixel Width
            float compassPixelY = wandererCompass.Scale.y; // 1 Compass Pixel Height
            // * Magic numbers: pixels taken up the vitals in the base compass texture image.
            float vitalsHeight = 13 * compassPixelY;
            float vitalsWidth = 2 * compassPixelX;
            float vitalsBreathWidth = 1 * compassPixelX;
            //
            float vitalsYOffset = 2 * compassPixelX; // From bottom of compass.
            float vitalsHealthXOffset = 3 * compassPixelX; // From Left of compass
            float vitalsBreathXOffset = 4 * compassPixelX; // From Right of compass
            float vitalsFatigueXOffset = 5 * compassPixelX; // From Right of compass
            float vitalsMagickaXOffset = 10 * compassPixelX; // From Right of compass

            switch (wandererCompassHorizontalAlignment){
                case HorizontalAlignment.Left: {
                    xAnchor = screenRect.x;
                    compassX = xAnchor;
                    break;
                }
                case HorizontalAlignment.Center: {
                    xAnchor = screenRect.x + (screenRect.width / 2);
                    compassX = xAnchor - (compassWidth / 2);
                    break;
                }
                case HorizontalAlignment.Right: {
                    xAnchor = screenRect.x + screenRect.width;
                    compassX = xAnchor - compassWidth;
                    break;
                }
            }
            switch (wandererCompassVerticalAlignment){
                case VerticalAlignment.Top: {
                    yAnchor = screenRect.y - compassHeight;
                    compassY = yAnchor + compassHeight;
                    break;
                }
                case VerticalAlignment.Bottom: {
                    yAnchor = screenRect.y + screenRect.height;
                    compassY = yAnchor - compassHeight;
                    break;
                }
                // case VerticalAlignment.Middle: {
                //     yAnchor = screenRect.y + (screenRect.height / 2);
                //     compassY = yAnchor - (compassHeight / 2);
                //     break;
                // }
            }            

            wandererCompass.Position = new Vector2(
                compassX,
                compassY
            );
            // 
            vitalsY = compassY + compassHeight - vitalsYOffset;
            wandererVitals.SetMargins(Margins.Bottom, (int)(screenRect.height - vitalsY)); // ! Cant use Margins.Top for some reason.
            // 
            wandererVitals.CustomHealthBarSize = new Vector2(vitalsWidth, vitalsHeight);
            wandererVitals.CustomFatigueBarSize = new Vector2(vitalsWidth, vitalsHeight);
            wandererVitals.CustomMagickaBarSize = new Vector2(vitalsWidth, vitalsHeight);
            wandererBreathBar.CustomBreathBarSize = new Vector2 (vitalsBreathWidth, vitalsHeight);
            // 
            float compassLeft = compassX - screenRect.x; 
            float compassRight = compassX + compassWidth - screenRect.x;
            wandererVitals.CustomHealthBarPosition = new Vector2(compassLeft + vitalsHealthXOffset, 0);
            wandererVitals.CustomFatigueBarPosition = new Vector2(compassRight - vitalsFatigueXOffset,0);
            wandererVitals.CustomMagickaBarPosition = new Vector2(compassRight - vitalsMagickaXOffset,0);
            wandererBreathBar.CustomBreathBarPosition = new Vector2(
                compassRight - vitalsBreathXOffset, 
                compassY + vitalsYOffset
            );
            // debugPosition.x = compassX + vitalsHealthXOffset;
            // debugPosition.y = compassY + vitalsYOffset;
            // debugPosition.width = vitalsWidth;
            // debugPosition.height = vitalsHeight;
            // Debug.Log($"debugPosition: {debugPosition}");
        }

        // TODO: optimize this so isnt calculated on each update lol.
        
        public static void PositionInteractionModeIcon(){
            interactionModeIcon.Enabled = HUDInteractionModeIconEnabled;
            if (!interactionModeIcon.Enabled) { return; }

            Vector2 interactionModeSize = interactionModeIcon.Size;

            Rect boundingBox = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;

            Vector2 startingPosition = GetStartingPositionFromAlignment(
                HUDInteractionModeHorizontalAlignment,
                HUDInteractionModeVerticalAlignment,
                interactionModeSize,
                boundingBox.width, 
                boundingBox.height
            );

            if (HUDInteractionModeHorizontalAlignment == HorizontalAlignment.Center){
                startingPosition = new Vector2(
                    startingPosition.x - interactionModeSize.x / 2,
                    startingPosition.y
                );
            }
            if (HUDInteractionModeVerticalAlignment == VerticalAlignment.Middle){
                startingPosition = new Vector2(
                    startingPosition.x,
                    startingPosition.y - interactionModeSize.y / 2
                );
            }

            interactionModeIcon.Position = startingPosition + HUDInteractionModeIconPositionOffset;
        }

        public static void PositionHUDElements(){
            if (wandererCompass.Parent == null){ return; } // ! Heuristic for checking if in game
            PositionFacePanels();
            PositionSpellEffects();
            PositionWandererCompass();
            PositionInteractionModeIcon();
        }

        public static void SetInteractionModeIconValues(){
            HUDInteractionModeIconEnabled = !WandererHudSettings.GetBool("InteractionMode", "Disable");
            HUDInteractionModeHorizontalAlignment = GetHorizontalAlignmentFromSettings(WandererHudSettings.GetInt("InteractionMode", "HorizontalAlignment"));
            HUDInteractionModeVerticalAlignment = GetVerticalAlignmentFromSettings(WandererHudSettings.GetInt("InteractionMode", "VerticalAlignment"));
            HUDInteractionModeIconPositionOffset = new Vector2(
                WandererHudSettings.GetTupleFloat("InteractionMode", "PositionOffset").First,
                WandererHudSettings.GetTupleFloat("InteractionMode", "PositionOffset").Second
            );
            SetNonPublicField(interactionModeIcon, "displayScale", WandererHudSettings.GetFloat("InteractionMode", "Scale"));
        }

        public static void SetInteractionModeIcon(){
            interactionModeIcon = (HUDInteractionModeIcon)GetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD, "interactionModeIcon");
            interactionModeIcon.HorizontalAlignment = HorizontalAlignment.None;
            interactionModeIcon.VerticalAlignment = VerticalAlignment.None;
            interactionModeIcon.SetMargins(Margins.All, 0);
            SetInteractionModeIconValues();
        }

        public static void SetActiveSpells(){
            activeSpellsPanel = DaggerfallUI.Instance.DaggerfallHUD.ActiveSpells;
            activeSpellsPanel.SetMargins(Margins.All, 0);
            activeSpellsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            activeSpellsPanel.VerticalAlignment = VerticalAlignment.Top;
            activeSelfList = (List<ActiveSpellIcon>)GetNonPublicField(activeSpellsPanel, "activeSelfList");
            activeOtherList = (List<ActiveSpellIcon>)GetNonPublicField(activeSpellsPanel, "activeOtherList");
            iconPool = (Panel[])GetNonPublicField(activeSpellsPanel, "iconPool");
        }

        public static void SetFacePanels(){
            facePanelsParent = DaggerfallUI.Instance.DaggerfallHUD.EscortingFaces;
            facePanelsParent.SetMargins(Margins.All, 0);
            facePanelsParent.AutoSize = AutoSizeModes.None;
            facePanels = (List<Panel>)GetNonPublicField(facePanelsParent, "facePanels");
        }

        public static void SetWandererCompass(){
            // * Enabled: reset to ShowCompass in DaggerfallHUD.
            // * Scale: reset to NativePanel.LocalScale
            // * Position: reset to bottom right of screen size. -> Also relative to compasses size
            // * Size: gets reset on each HUDCompass update relative to compassBoxSize.
            // * AutoSize and Vertical/Horizontal alighments dont seem to matter...

            DaggerfallUI.Instance.DaggerfallHUD.ShowCompass = false; // todo: Will this stay false? or will I have to reset it to false anytime it becomes true?
            DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Components.Add(wandererCompass);
            wandererCompass.Enabled = true;
            // 
            wandererCompass.AutoSize = AutoSizeModes.ScaleFreely;
            wandererCompass.HorizontalAlignment = HorizontalAlignment.None;
            wandererCompass.VerticalAlignment = VerticalAlignment.None;
            // 
            wandererCompass.Position = DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Position;

            // * Ceil (or floor) so dont have to deal with decimal imprecision when alligning things to compass.
            wandererCompass.Scale = new Vector2(
                (float)Math.Round(DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Scale.x), 
                (float)Math.Round(DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Scale.y)
            );
            defaultWandererCompassScale = new Vector2(
                wandererCompass.Scale.x,
                wandererCompass.Scale.y
            );
            wandererCompass.Size = DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Size;
            wandererCompass.SetMargins(Margins.All, 0);

            SetNonPublicField(wandererCompass, "compassBoxSize", compassBoxSize);
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

            VerticalProgress fatigueBar = (VerticalProgress)GetNonPublicField(wandererVitals, "fatigueBarGain");
            VerticalProgress magickaBar = (VerticalProgress)GetNonPublicField(wandererVitals, "magickaBarGain");
            VerticalProgress healthBar = (VerticalProgress)GetNonPublicField(wandererVitals, "healthBarGain");

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
            // if (constructor == null){
            //     Debug.Log($"UNABLE TO FIND CONSTRUCTOR WITH ARGS: {args}");
            //     foreach (var arg in args){
            //         Debug.Log($"arg: {arg.GetType()}");
            //     }
            //     return null;
            // }
            object classInstance = constructor.Invoke(args);
            return classInstance;
        }

        private void DebugAction(){
            Debug.Log($"Empty Debug Action");
            
        }

        private void DebugInputs(){
            if (!Input.anyKey){ return; }

            // Keycodes: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/KeyCode.html
            if (Input.GetKeyDown(KeyCode.Slash)){
                DebugAction();
            }
        }

        private void LateUpdate(){
            if (GameManager.IsGamePaused){
                ForceUpdateHUDElements();
            } else{
                PositionHUDElements();
            }
        }

        private void Update(){
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
                    ChangedConnectionColor = true;
                }else{
                    ChangedConnectionColor = false;
                }
                return;    
            }
            if (Screen.width != LastScreen.x || Screen.height != LastScreen.y){
                if (!ResizeWaiting){
                    StartCoroutine(WaitSeconds(ResizeWaitSecs));
                }
            }
        }

        private void SetLastScreen(){
            LastScreen.x = Screen.width; 
            LastScreen.y = Screen.height;
        }

        private IEnumerator WaitSeconds(float seconds){
            ForceResizeMap();
            ForceUpdateHUDElements();
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
            // * Order = Draw Order
            SetWandererCompass();
            SetVitalsHud();
            SetBreathHud();
            debugTexture = DaggerfallUI.CreateSolidTexture(UnityEngine.Color.white, 8);
            SetInteractionModeIcon();
            SetFacePanels();
            SetActiveSpells();
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
                    // string doorName = child.name.Substring(0, child.name.Length - exitStringLength);
                    // * Rotate Exit doors seperately.
                    exitDoorRotationCorrectHelper[child.name.Substring(0, child.name.Length - exitStringLength)] = teleporter.transform;
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

            // if (interactionModeIcon != null){
            //     interactionModeIcon.Position = HUDInteractionModeIconPositionOffset;
            //     interactionModeIcon.Draw();
            // }

            if (facePanels != null){
                foreach (Panel facePanel in facePanels){
                    if (!facePanel.Enabled) { break; }
                    facePanel.Draw();
                }
            }
            if (activeSpellsPanel != null){
                activeSpellsPanel.Draw();
            }

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
