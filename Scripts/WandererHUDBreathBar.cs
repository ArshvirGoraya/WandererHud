// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using MapOverwritesMod;

namespace DaggerfallWorkshop.Game.UserInterface
{
    /// <summary>
    /// Player breath bar for HUD.
    /// </summary>
    public class WandererHUDBreathBar : Panel
    {
        public VerticalProgressSmoother breathBar = new VerticalProgressSmoother();
        // VerticalProgressSmoother breathBarLoss = new VerticalProgressSmoother();
        // VerticalProgress breathBarGain = new VerticalProgress();
        // Color breathLossColor = new Color(0, 0.22f, 0);
        // Color breathGainColor = new Color(0.60f, 1f, 0.60f);
        // VitalsChangeDetector vitalsDetector = GameManager.Instance.VitalsChangeDetector;

        // public Color32 normalBreathColor = new Color32(111, 242, 255, 255 / 2); // Blueish
        // public Color32 shortOnBreathColor = new Color32(247, 39, 41, 255 / 2); // Redish

        /// <summary>
        /// Gets or sets current breath as value between 0 and 1.
        /// </summary>
        public float Breath
        {
            get { return breathBar.Amount; }
            set { breathBar.Amount = value; UpdateBreathBar(); }
        }

        public Vector2? CustomBreathBarPosition { get; set; }
        public Vector2? CustomBreathBarSize { get; set; }

        public WandererHUDBreathBar(Texture2D breathBarTexture)
            :base()
        {
            breathBar.ProgressTexture = breathBarTexture;
            BackgroundColor = Color.clear;
            Components.Add(breathBar);
            // Enabled = false;

            // if (DaggerfallUnity.Settings.EnableVitalsIndicators){
            //     breathBarLoss.Color = breathLossColor;
            //     breathBarGain.Color = breathGainColor;
            //     Components.Add(breathBarLoss);
            //     Components.Add(breathBarGain);
            // }
        }

        public override void Update()
        {
            if (Enabled)
            {
                base.Update();
                UpdateBreathBar();
            }
        }

        void UpdateBreathBar()
        {
            float breathBarWidth = 6 * Scale.x;
            float breathBarHeight = GameManager.Instance.PlayerEntity.Stats.LiveEndurance * Scale.y;
            breathBar.Position = (CustomBreathBarPosition != null) ? CustomBreathBarPosition.Value : Position + new Vector2(306 * Scale.x, (-92 * Scale.y) - breathBarHeight);
            breathBar.Size = (CustomBreathBarSize != null) ? CustomBreathBarSize.Value : new Vector2(breathBarWidth, breathBarHeight);
            // 
            breathBar.Amount = GameManager.Instance.PlayerEntity.CurrentBreath / (float)GameManager.Instance.PlayerEntity.MaxBreath;
            // int threshold = ((GameManager.Instance.PlayerEntity.Stats.LiveEndurance) >> 3) + 4;
            // if (threshold > GameManager.Instance.PlayerEntity.CurrentBreath)
            //     breathBar.Color = shortOnBreathColor;
            // else
            //     breathBar.Color = normalBreathColor;
            // 
            // if (DaggerfallUnity.Settings.EnableVitalsIndicators){
            //     breathBarLoss.Position = breathBar.Position;
            //     breathBarLoss.Size = breathBar.Size;
            //     breathBarGain.Position = breathBar.Position;
            //     breathBarGain.Size = breathBar.Size;
            // }
        }
    }
}
