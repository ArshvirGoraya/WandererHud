using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace CameraZoomSpeedMod
{
    public class CameraZoomSpeed : MonoBehaviour
    {
        private static Mod mod;
        static ModSettings WandererHudSettings;

        // public float InteriorZoomSpeed = 0.3f;
        // public float ExteriorZoomSpeed = 20f;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<CameraZoomSpeed>();

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();

            mod.IsReady = true;
        }
        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            WandererHudSettings = modSettings;
        }
        // 
        private void Start(){}
        // 
        public void Update(){
            if (!(DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow || DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow)){
                return;
            }
            float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
            if (mouseScroll > 0f){
                MouseScrollUp();
            }
            else if (mouseScroll < 0f){
                MouseScrollDown();
            }
        }

        public void MouseScrollUp(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
                InteriorZoom(true);
                return;
            }
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                ExteriorZoom(true);
                return;
            }
        }

        public void MouseScrollDown(){
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallAutomapWindow){
                InteriorZoom(false);
                return;
            }
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallExteriorAutomapWindow){
                ExteriorZoom(false);
                return;
            }
        }

        public void ExteriorZoom(bool ZoomIn = true){
            float speed = WandererHudSettings.GetFloat("ExteriorMap", "ZoomSpeed");
            // float speed = ExteriorZoomSpeed;
            if (ZoomIn){
                speed = -speed;
            }
            Camera cameraExteriorAutomap = ExteriorAutomap.instance.CameraExteriorAutomap;
            ExteriorAutomap exteriorAutomap = ExteriorAutomap.instance;
            const float maxZoom = 25.0f;
            const float minZoom = 250.0f;
            // 
            float zoomSpeedCompensated = speed * exteriorAutomap.LayoutMultiplier; 
            cameraExteriorAutomap.orthographicSize += zoomSpeedCompensated;
            
            cameraExteriorAutomap.orthographicSize = Math.Min(minZoom * exteriorAutomap.LayoutMultiplier, (Math.Max(maxZoom * exteriorAutomap.LayoutMultiplier, cameraExteriorAutomap.orthographicSize)));
            
            // DaggerfallExteriorAutomapWindow exteriorAutomapWindow = DaggerfallUI.UIManager.TopWindow as DaggerfallExteriorAutomapWindow;
            // exteriorAutomapWindow.UpdateAutomapView();
        }

        public void InteriorZoom(bool ZoomIn = true){
            float zoomSpeed = WandererHudSettings.GetFloat("InteriorMap", "ZoomSpeed");
            // float zoomSpeed = InteriorZoomSpeed;
            Camera cameraAutomap = Automap.instance.CameraAutomap;
            // 
            float magnitude = Vector3.Magnitude(Camera.main.transform.position - cameraAutomap.transform.position);
            // const float maxMagnitude = 20; // Arbitrary clamp
            // magnitude = Mathf.Min(magnitude, maxMagnitude);

	        float zoomSpeedCompensated = zoomSpeed * magnitude;

            Vector3 translation;
            // 
            if (ZoomIn){
                translation = cameraAutomap.transform.forward * zoomSpeedCompensated;
            }else{
                translation = -cameraAutomap.transform.forward * zoomSpeedCompensated;
            }
            // 
            Vector3 newPosition = cameraAutomap.transform.position;
            newPosition += translation;

            const float max = 1_000; // Arbitrary clamp

            if (
                // Any above maximum
                (newPosition.x > max ||
                newPosition.y > max ||
                newPosition.z > max)
                ||
                // Any below minimum
                (newPosition.x < -max ||
                newPosition.y < -max ||
                newPosition.z < -max)
                ){
                    Debug.Log($"IGNORED interior camera position: {newPosition}");
                    return;
            }
            cameraAutomap.transform.position = newPosition;
            Debug.Log($"interior camera position: {cameraAutomap.transform.position}");

            // DaggerfallAutomapWindow automapWindow = DaggerfallUI.UIManager.TopWindow as DaggerfallAutomapWindow;
            // automapWindow.UpdateAutomapView();
            // UpdateInteriorAutomapView(DaggerfallUI.UIManager.TopWindow as DaggerfallAutomapWindow);
        }

        // * Public version of DaggerfallAutomapWindow.UpdateAutomapView()
        // public void UpdateInteriorAutomapView(DaggerfallAutomapWindow automapWindow){
        //     Automap automap = Automap.instance;
        //     Camera cameraAutomap = Automap.instance.CameraAutomap;

        //     automap.ForceUpdate();
        //     automap.RotationPivotAxisRotation = Quaternion.Euler(0.0f, cameraAutomap.transform.rotation.eulerAngles.y, 0.0f);

        //     // if ((!cameraAutomap) || (!renderTextureAutomap))
        //     //     return;

        //     cameraAutomap.Render();

        //     // RenderTexture.active = renderTextureAutomap;
        //     // textureAutomap.ReadPixels(new Rect(0, 0, renderTextureAutomap.width, renderTextureAutomap.height), 0, 0);
        //     // textureAutomap.Apply(false);
        //     // RenderTexture.active = null;

        //     // panelRenderAutomap.BackgroundTexture = textureAutomap;

        //     // panelRenderOverlay.BackgroundTexture = automap.TextureMicroMap;

        // }




    }
}
