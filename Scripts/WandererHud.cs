using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
/// <summary>
/// Entry Point for WandererHUD mod.
/// <summary>
namespace WandererHudMod{
    public class WandererHud : MonoBehaviour{
        public static WandererHud instance;
        public static MapOverwrites mapOverwrites;
        public static HudOverwrites hudOverwrites;
        public static Mod mod;
        public static ModSettings WandererHudSettings;
        public bool debugLogging = false;
        public Vector2 LastScreen = new Vector2(0, 0);
        // 
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            instance = go.AddComponent<WandererHud>();
            WandererHud.DebugLog("WandererHud Init");
            // Add other mod components:
            mapOverwrites = go.AddComponent<MapOverwrites>();
            mapOverwrites.Initalize(instance);
            // 
            hudOverwrites = go.AddComponent<HudOverwrites>();
            hudOverwrites.Initalize(instance);
            // 
            mod.LoadSettingsCallback = instance.LoadSettings;
            mod.LoadSettings();
            mod.IsReady = true;
        }
        private void Start(){
            DebugLog("Start");
            SaveLoadManager.OnLoad += (_) => SaveLoadManager_OnLoad();
            SetLastScreen();
       }

        public void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
            instance.debugLogging = WandererHudSettings.GetBool("Debug", "Logging");
            mapOverwrites.LoadSettings(modSettings, change);
            hudOverwrites.LoadSettings(modSettings, change);
        }

        public void SaveLoadManager_OnLoad(){
            DebugLog("LOADED NEW SAVE");
            mapOverwrites.SaveLoadManager_OnLoad();
            hudOverwrites.SaveLoadManager_OnLoad();
        }

        private void Update(){
            DebugInputs();
            if (Screen.width != LastScreen.x || Screen.height != LastScreen.y){
                ForceResizeAll();
            }
        }

        public static void DebugLog(string message){
            if (instance.debugLogging){
                Debug.Log($"WandererHud: {message}");
            }
        }

        private void DebugInputs(){
            if (!Input.anyKey){ return; }
            if (Input.GetKeyDown(KeyCode.Slash)){
                DebugAction();
            }
        }
        private void DebugAction(){
            hudOverwrites.DebugAction();
            mapOverwrites.DebugAction();
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
