// Project:     FPSHandWave, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: July 2022

using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;


namespace ThePenwickPapers
{

    public class FPSHandWave : MonoBehaviour
    {
        bool rightToLeft;
        float location; //current X draw location from 0 to 1
        Rect screenRect;
        float offsetHeightForLargeHUD;
        float scaleX;
        float scaleY;
        Texture2D texture;


        /// <summary>
        /// Starts the hand-wave animation using specified hand.
        /// </summary>
        public bool DoHandWave(bool rightToLeft)
        {
            if (Utility.IsShowingHandAnimation())
                return false; //currently attacking, casting spell, etc.

            this.enabled = true;
            this.rightToLeft = rightToLeft;
            location = 0f;

            return true;
        }


        /// <summary>
        /// Starts the hand-wave animation using the appropriate hand.
        /// </summary>
        public bool DoHandWave()
        {
            WeaponManager weapons = GameManager.Instance.WeaponManager;

            bool rightToLeft = false;

            if (weapons.Sheathed)
                rightToLeft = true;
            else if (weapons.ScreenWeapon.FlipHorizontal ^ weapons.ScreenWeapon.WeaponType == WeaponTypes.Bow)
                rightToLeft = true;

            return DoHandWave(rightToLeft);
        }


        void Start()
        {
            texture = Assets.GrapplingHookHand.Get<Texture2D>();
        }



        void OnGUI()
        {
            //only handling repaint events
            if (!Event.current.type.Equals(EventType.Repaint))
                return;

            if (GameManager.IsGamePaused)
                return;

            if (location >= 1f)
            {
                //finished with animation
                this.enabled = false;
                return;
            }

            GUI.depth = 1;

            UpdateScreenProperties();
                
            //reverse image for other hand
            Rect animRect = rightToLeft ? new Rect(0, 0, 1, 1) : new Rect(1, 0, -1, 1);

            float imageWidth = scaleX * texture.width;
            float imageHeight = scaleY * texture.height;

            location = Mathf.Clamp(location + Time.smoothDeltaTime * 5f, 0f, 1f);

            float xOffset = location * screenRect.width / 2;
            float x = screenRect.x - imageWidth / 2;
            x += rightToLeft ? screenRect.width - xOffset : xOffset;

            //In Unity GUI, the y-axis increases downward (y=0 is top of screen)
            float y = screenRect.y + screenRect.height - imageHeight - offsetHeightForLargeHUD;

            Rect position = new Rect(
                x,
                y,
                imageWidth,
                imageHeight);

            Color playerTint = ThePenwickPapersMod.Instance.GetPlayerTint();

            DaggerfallUI.DrawTextureWithTexCoords(position, texture, animRect, true, playerTint);
        }


        void UpdateScreenProperties()
        {
            if (DaggerfallUI.Instance.CustomScreenRect != null)
                screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
            else
                screenRect = new Rect(0, 0, Screen.width, Screen.height);

            // Offset animation by large HUD height when both large HUD and undocked weapon offset enabled
            // Animation is forced to offset when using docked HUD else it would appear underneath HUD
            // This helps user avoid such misconfiguration or it might be interpreted as a bug
            // (Same logic as in FPSWeapon)
            offsetHeightForLargeHUD = 0;
            if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                DaggerfallUnity.Settings.LargeHUD &&
                (DaggerfallUnity.Settings.LargeHUDUndockedOffsetWeapon || DaggerfallUnity.Settings.LargeHUDDocked))
            {
                offsetHeightForLargeHUD = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
            }


            const float nativeScreenWidth = 300f;
            const float nativeScreenHeight = 200f;
            scaleX = screenRect.width / nativeScreenWidth;
            scaleY = screenRect.height / nativeScreenHeight;

            // Adjust scale to be slightly larger when not using point filtering
            // This reduces the effect of filter shrink at edge of display
            if (DaggerfallUnity.Instance.MaterialReader.MainFilterMode != FilterMode.Point)
            {
                scaleX *= 1.01f;
                scaleY *= 1.01f;
            }

        }

    } //class FPSHandWave


} //namespace
