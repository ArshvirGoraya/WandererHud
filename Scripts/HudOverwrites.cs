using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System.Collections.Generic;
using static DaggerfallWorkshop.Game.UserInterface.HUDActiveSpells;
using WandererHudMod;
using static ModHelpers;

public class HudOverwrites : MonoBehaviour
{
    // * BreathBar
    const string breathBarFilename = "BreathBar";
    public static WandererHUDBreathBar wandererBreathBar;
    // * Compass
    public static Vector2 compassBoxSize = new Vector2(0, 0);
    public static HUDCompass wandererCompass;
    public static Vector2 defaultWandererCompassScale;
    public static float wandererCompassScale = 1f;
    public static HUDVitals wandererVitals;
    static HorizontalAlignment wandererCompassHorizontalAlignment = HorizontalAlignment.Center;
    static VerticalAlignment wandererCompassVerticalAlignment = VerticalAlignment.Bottom;
    // * InteractionMode
    static HUDInteractionModeIcon interactionModeIcon;
    static bool HUDInteractionModeIconEnabled = false;
    static float HUDInteractionModeIconScale = 0.5f;
    static HorizontalAlignment HUDInteractionModeHorizontalAlignment;
    static VerticalAlignment HUDInteractionModeVerticalAlignment;
    static Vector2 interactionModeIconPosition;
    static Vector2 interactionModeSize = Vector2.zero;
    // * FacePanels
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
    // * SpellEffects
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
    // * Debug Color
    public static Texture2D debugTexture;
    public static Rect debugTextRect = new Rect(0, 0, 1, 1);
    public static Rect debugPosition = new Rect(0, 0, 0, 0);
    public static Color32 debugColor = new Color32(255, 255, 255, 255);
    // 
    public static bool hudElementsInitalized = false;
    static Vector2 edgeMargin = new Vector2(10, 10);
    static float inGameAspectX = 0;
    static bool updateOnUnPause = false;
    public static Mod mod;
    WandererHud wandererHud;

    public void Initalize(WandererHud wandererMod){
        Debug.Log($"HudOverwrites Initalize");
        wandererHud = wandererMod;
        mod = WandererHud.mod;
    }

    public static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
        // * Interaction Mode
        HUDInteractionModeHorizontalAlignment = HorizontalAlignment.Left;
        HUDInteractionModeVerticalAlignment = VerticalAlignment.Bottom;
        if (!hudElementsInitalized){ return; }
        if (change.HasChanged("Hud", "EnableInteractionIcon")){
            Debug.Log($"changed EnableInteractionIcon");
            SetInteractionModeIconValues();
            SetSpellEffectsValues();
        }
    }

    public void SaveLoadManager_OnLoad(){
        // * Order = Draw Order
        hudElementsInitalized = false;
        SetWandererCompass();
        SetVitalsHud();
        SetBreathHud();
        // 
        SetFacePanels();
        SetInteractionModeIcon();
        SetSpellEffects();
        // 
        mod.LoadSettings();
        PositionHUDElements();
        hudElementsInitalized = true;
    }

    public void ScreenResizeChange(){
        // PositionHUDElements();
    }

    // Start is called before the first frame update
    void Start(){
        Debug.Log($"HudOverwrites Start");
        DaggerfallUI.Instance.DaggerfallHUD.ShowInteractionModeIcon = false;
        Texture2D compasImage = mod.GetAsset<Texture2D>("COMPBOX.IMG", false);
        compassBoxSize = new Vector2(compasImage.width, compasImage.height);
        wandererCompass = new HUDCompass();
        wandererVitals = new HUDVitals();
        wandererBreathBar = new WandererHUDBreathBar(mod.GetAsset<Texture2D>(breathBarFilename, false));
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

        if (facePanelsEnable && facePanels != null){
            foreach (Panel facePanel in facePanels){
                if (!facePanel.Enabled) { continue; }
                facePanel.Draw();
            }
        }

        if (activeSpellsPanel != null){
            activeSpellsPanel.Draw();
        }

        // ! Debugging
        // if (debugTexture != null){
        //     DaggerfallUI.DrawTextureWithTexCoords(debugPosition, debugTexture, debugTextRect, false, debugColor);
        // }
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

    public static float GetPanelHorizontalRight(BaseScreenComponent component){
        return component.Position.x + component.Size.x;
    }
    // public static float GetPanelHorizontalMiddle(BaseScreenComponent component){
    //     return component.Position.x + component.Size.x / 2;
    // }
    public static float GetPanelVerticalMiddle(BaseScreenComponent component){
        return component.Position.y + component.Size.y / 2;
    }


    // public static HorizontalAlignment GetHorizontalAlignmentFromSettings(int settingsVal){
    //     switch (settingsVal)
    //     {
    //         case 0: { return HorizontalAlignment.Left;}
    //         case 1: { return HorizontalAlignment.Center; }
    //         case 2: { return HorizontalAlignment.Right; }
    //     }
    //     Debug.Log($"WandererHUD: UNKNOWN HORIZONTAL ALIGNMENT");
    //     return HorizontalAlignment.None;
    // }

    // public static VerticalAlignment GetVerticalAlignmentFromSettings(int settingsVal){
    //     switch (settingsVal)
    //     {
    //         case 0: { return VerticalAlignment.Top;}
    //         case 1: { return VerticalAlignment.Middle; }
    //         case 2: { return VerticalAlignment.Bottom; }
    //     }
    //     Debug.Log($"WandererHUD: UNKNOWN VERTICAL ALIGNMENT");
    //     return VerticalAlignment.None;
    // }

    public static void PositionHUDElements(){
        wandererCompass.Update();
        wandererVitals.Update();
        wandererBreathBar.Update();            
        SetWandererCompassValues();
        // SetFacePanelsValues(); // ! updates each update so no need to update it here.
        SetInteractionModeIconValues();
        SetSpellEffectsValues(); // ! should run after interaction mode and compass.
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

    public Vector2 GetFirstSpellPosition(List<ActiveSpellIcon> activeSpellList){
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

    public static void SetFacePanelsPositions(){
        for (int i = 0; i < facePanels.Count; i++){
            Panel facePanel = facePanels[i];
            if (!facePanel.Enabled){ continue; }
            facePanel.Position = facePanelsPositions[i];
        }
    }


    public static void SetInteractionModeIcon(){
        interactionModeIcon = (HUDInteractionModeIcon)GetNonPublicField(DaggerfallUI.Instance.DaggerfallHUD, "interactionModeIcon");
        interactionModeIcon.SetMargins(Margins.All, 0);
        interactionModeIcon.Enabled = false;
        SetInteractionModeIconValues();
        // interactionModeIcon.HorizontalAlignment = HorizontalAlignment.None;
        // interactionModeIcon.VerticalAlignment = VerticalAlignment.None;
    }

    public static void SetInteractionModeIconValues(){
        // * Call when interaction mode settings changes.
        // * Call when interaction mode icon size changes.

        Debug.Log($"set interaction mode values");
        HUDInteractionModeIconEnabled = WandererHud.WandererHudSettings.GetBool("Hud", "EnableInteractionIcon");
        interactionModeIcon.Enabled = HUDInteractionModeIconEnabled;
        if (!interactionModeIcon.Enabled) { return; }
        Debug.Log($"set interaction mode position");

        SetNonPublicField(interactionModeIcon, "displayScale", HUDInteractionModeIconScale);
        interactionModeIcon.Update(); // Update the size from above scale.

        interactionModeSize = interactionModeIcon.Size;

        Rect boundingBox = DaggerfallUI.Instance.DaggerfallHUD.ParentPanel.Rectangle;

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
}
