using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using MapOverwritesMod;
using System.Reflection;
using static DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallAutomapWindow;

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

        public static T GetNonPublicVariable<T>(object instance, string var){
            Type type = instance.GetType();
            FieldInfo fieldInfo = type.GetField(var, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)fieldInfo.GetValue(instance);
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
            float zoomSpeed = MapOverwrites.WandererHudSettings.GetFloat("InteriorMap", "ZoomSpeed"); // must be normalized: 0 - 0.9
            Camera cameraAutomap = Automap.instance.CameraAutomap;
            // float magnitude = Vector3.Magnitude(Camera.main.transform.position - cameraAutomap.transform.position);

            Transform BeaconRotationPivot = GameObject.Find("Automap/InteriorAutomap/Beacons/BeaconRotationPivotAxis").transform;
            float magnitude = Vector3.Magnitude(BeaconRotationPivot.position - cameraAutomap.transform.position);
            
            AutomapViewMode automapViewMode = GetNonPublicVariable<AutomapViewMode>(
                DaggerfallUI.UIManager.TopWindow,
                "automapViewMode"
                );

            // * incremental zoom when zoomed very close
            if (automapViewMode == AutomapViewMode.View2D){
                if (magnitude <= 150){
                    magnitude *= 0.2f;
                }
            }else{
                if (magnitude <= 30){
                    magnitude *= 0.2f;
                }
            }
            
            // Debug.Log($"magnitude: {magnitude}");

            // * Only apply base zoom
            if (magnitude <= 0 || magnitude >= 10_000) { return; }



	        float zoomSpeedCompensated = zoomSpeed * magnitude;

            Vector3 translation;
            // 
            if (ZoomIn){
                translation = cameraAutomap.transform.forward * zoomSpeedCompensated;
            }else{
                translation = -cameraAutomap.transform.forward * zoomSpeedCompensated;
            }
            // 
            // Vector3 newPosition = cameraAutomap.transform.position;
            // newPosition += translation;
            // cameraAutomap.transform.position = newPosition;
            cameraAutomap.transform.position += translation;
        }
    }
}
