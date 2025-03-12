using UnityEngine;
namespace DaggerfallWorkshop.Game.UserInterface
{
    /// <summary>
    /// Modified version of Daggerfall Unity's HUDVitals class.
    /// Player breath bar for WandererHUD. 
    /// </summary>
    public class WandererHUDBreathBar : Panel
    {
        public VerticalProgressSmoother breathBar = new VerticalProgressSmoother();
        public Vector2? CustomBreathBarPosition { get; set; }
        public Vector2? CustomBreathBarSize { get; set; }

        public float Breath{
            get { return breathBar.Amount; }
            set { 
                breathBar.Amount = value; 
                UpdateBreathBar(); 
            }
        }

        public WandererHUDBreathBar(Texture2D breathBarTexture)
            :base()
        {
            breathBar.ProgressTexture = breathBarTexture;
            BackgroundColor = Color.clear;
            Components.Add(breathBar);
        }

        public override void Update(){
            if (Enabled){
                base.Update();
                UpdateBreathBar();
            }
        }

        void UpdateBreathBar(){
            float breathBarWidth = 6 * Scale.x;
            float breathBarHeight = GameManager.Instance.PlayerEntity.Stats.LiveEndurance * Scale.y;
            breathBar.Position = (CustomBreathBarPosition != null) ? CustomBreathBarPosition.Value : Position + new Vector2(306 * Scale.x, (-92 * Scale.y) - breathBarHeight);
            breathBar.Size = (CustomBreathBarSize != null) ? CustomBreathBarSize.Value : new Vector2(breathBarWidth, breathBarHeight);
            breathBar.Amount = GameManager.Instance.PlayerEntity.CurrentBreath / (float)GameManager.Instance.PlayerEntity.MaxBreath;
        }
    }
}
