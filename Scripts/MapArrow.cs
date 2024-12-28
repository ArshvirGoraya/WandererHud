using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace MapArrowMod
{
    public class MapArrow : MonoBehaviour
    {
        public Mesh Cube;
        public Material ArrowMaterial;

        private string ArrowMaterialName = "Player Arrow Material";

        public static GameObject GameobjectPlayerMarkerArrow;
        public GameObject GameobjectPlayerMarkerArrowStamp;
        public GameObject GameobjectPlayerMarkerCircle;

        public static float ARROW_ASPECT_RATIO;

        static ModSettings WandererHudSettings;

        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<MapArrow>();
            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
            mod.IsReady = true;
        }
        
        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
            ChangePlayerMarkerTextureSize();
        }

        private void Start()
        {
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
            //
            GameObject CubeOb = GameObject.CreatePrimitive(PrimitiveType.Cube); 
            Cube = CubeOb.GetComponent<MeshFilter>().mesh; 
            Destroy (CubeOb);
            ArrowMaterial = mod.GetAsset<Material>(ArrowMaterialName, false);
            ARROW_ASPECT_RATIO = GetAspectRatio(ArrowMaterial.mainTexture as Texture2D);
        }

        private void SaveLoadManager_OnLoad(SaveData_v1 saveData){
            ImplementArrowElement();
        }

        public void ImplementArrowElement(){
            RemoveUnneededPlayerMarkerArrowElements();
            ChangePlayerMarkerTexture();            
        }

        public void RemoveUnneededPlayerMarkerArrowElements(){
            GameobjectPlayerMarkerArrowStamp = GameObject.Find("Automap/ExteriorAutomap/PlayerMarkerArrowStamp");
            GameobjectPlayerMarkerCircle = GameObject.Find("Automap/ExteriorAutomap/PlayerMarkerCircle");
            // * Simply scales them to 0 so we cannot see them anymore.
            GameobjectPlayerMarkerArrowStamp.GetComponent<Transform>().localScale = new Vector3 (0, 0, 0);
            GameobjectPlayerMarkerCircle.GetComponent<Transform>().localScale = new Vector3 (0, 0, 0);
        }
        public void ChangePlayerMarkerTexture(){
            GameobjectPlayerMarkerArrow = ExteriorAutomap.instance.GameobjectPlayerMarkerArrow;
            GameobjectPlayerMarkerArrow.GetComponent<MeshRenderer>().material = ArrowMaterial;
            GameobjectPlayerMarkerArrow.GetComponent<MeshFilter>().mesh = Cube;
            ChangePlayerMarkerTextureSize();
        }

         public static void ChangePlayerMarkerTextureSize(){
            GameobjectPlayerMarkerArrow = ExteriorAutomap.instance.GameobjectPlayerMarkerArrow;
            if (GameobjectPlayerMarkerArrow == null){ return; } // * For when called when game is just starting up, not when loaded into game.
            // 
            float fixedWidth = WandererHudSettings.GetInt("ExteriorMap", "MapArrowSize");
            Vector2 newSize = GetFixedWidthSize(ARROW_ASPECT_RATIO, fixedWidth);
            GameobjectPlayerMarkerArrow.GetComponent<Transform>().localScale = new Vector3 (newSize.x, 0, newSize.y);
        }

        public float GetAspectRatio(Texture2D t){
            float width = t.width;
            float height = t.height;
            float aspect = width / height;
            return aspect;
        }

        public static Vector2 GetFixedWidthSize(float AspectRatio, float FixedWidth){
            return new Vector2(
                FixedWidth,
                FixedWidth / AspectRatio
            );
        }
        public Vector2 GetFixedHeightSize(float AspectRatio, float FixedHeight){
            return new Vector2(
                FixedHeight * AspectRatio,
                FixedHeight
            );
        }
    }
}
