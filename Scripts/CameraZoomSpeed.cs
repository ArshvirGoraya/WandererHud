using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using MapOverwritesMod;

namespace CameraZoomSpeedMod
{
    public class CameraZoomSpeed : MonoBehaviour
    {
        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<CameraZoomSpeed>();

            mod.IsReady = true;
        }
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
            float speed = MapOverwrites.WandererHudSettings.GetFloat("ExteriorMap", "ZoomSpeed");
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
        }

        public void InteriorZoom(bool ZoomIn = true){
            float zoomSpeed = MapOverwrites.WandererHudSettings.GetFloat("InteriorMap", "ZoomSpeed");
            Camera cameraAutomap = Automap.instance.CameraAutomap;
            // 
            float magnitude = Vector3.Magnitude(Camera.main.transform.position - cameraAutomap.transform.position);

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
                    return;
            }
            cameraAutomap.transform.position = newPosition;
        }
    }
}
