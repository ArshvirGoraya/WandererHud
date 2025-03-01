using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;
using System;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System.Collections.Generic;
using System.Reflection;
using static DaggerfallWorkshop.Game.UserInterface.HUDActiveSpells;
using static ModHelpers;

// * Makes maps fullscreen (with autoscaling and size setting).
// * Disables unnessary ui elements in exterior/interior maps..
// * Disabled inner components of interior map (e.g., beacons)
// Todo: Refactor into different files.
namespace WandererHudMod{
    public class WandererHud : MonoBehaviour{
        public static WandererHud instance;
        public static MapOverwrites mapOverwrites;
        public static HudOverwrites hudOverwrites;
        public static Mod mod;
        public static ModSettings WandererHudSettings;
        public Vector2 LastScreen = new Vector2(0, 0);
        // 
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            Debug.Log($"WandererHud Init");
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            instance = go.AddComponent<WandererHud>();
            // Add other scripts:
            mapOverwrites = go.AddComponent<MapOverwrites>();
            mapOverwrites.Initalize(instance);
            // 
            hudOverwrites = go.AddComponent<HudOverwrites>();
            hudOverwrites.Initalize(instance);
            // 
            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
            mod.IsReady = true;
        }
        private void Start(){
            Debug.Log($"WandererHud start");
            SaveLoadManager.OnLoad += (_) => SaveLoadManager_OnLoad();
            SetLastScreen();
       }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
            MapOverwrites.LoadSettings(modSettings, change);
            HudOverwrites.LoadSettings(modSettings, change);
        }

        public void SaveLoadManager_OnLoad(){
            Debug.Log($"LOADED NEW SAVE");
            mapOverwrites.SaveLoadManager_OnLoad();
            hudOverwrites.SaveLoadManager_OnLoad();
        }

        private void Update(){
            DebugInputs();
            if (Screen.width != LastScreen.x || Screen.height != LastScreen.y){
                ForceResizeAll();
            }
        }

        private void DebugAction(){
            Debug.Log($"Debug Action");
        }
        private void DebugInputs(){
            if (!Input.anyKey){ return; }
            // Keycodes: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/KeyCode.html
            if (Input.GetKeyDown(KeyCode.Slash)){
                DebugAction();
            }
        }

        public void ForceResizeAll(){
            mapOverwrites.ScreenResizeChange();
            hudOverwrites.ScreenResizeChange();
            SetLastScreen();
        }
        
        private void SetLastScreen(){
            LastScreen.x = Screen.width; 
            LastScreen.y = Screen.height;
        }
    }
}
