// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Aug 2022

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;



namespace ThePenwickPapers
{

    public class ListPickerWindow : DaggerfallListPickerWindow
    {
        const float minTimePresented = 0.0833f;

        float presentationTime = 0;



        public ListPickerWindow(IUserInterfaceManager uiManager, IUserInterfaceWindow previous = null, DaggerfallFont font = null, int rowsDisplayed = 0)
        : base(uiManager, previous, font, rowsDisplayed)
        {
        }


        public override void OnPush()
        {
            base.OnPush();

            //close window if player clicks outside of window
            parentPanel.OnMouseClick += ParentPanel_OnMouseClick;
            parentPanel.OnRightMouseClick += ParentPanel_OnMouseClick;
            parentPanel.OnMiddleMouseClick += ParentPanel_OnMouseClick;

            presentationTime = Time.realtimeSinceStartup;
        }

        public override void OnPop()
        {
            base.OnPop();

            parentPanel.OnMouseClick -= ParentPanel_OnMouseClick;
            parentPanel.OnRightMouseClick -= ParentPanel_OnMouseClick;
            parentPanel.OnMiddleMouseClick -= ParentPanel_OnMouseClick;

            //Make sure we've stopped swallowing activation actions
            ThePenwickPapersMod.StopSwallowingActions();
        }



        /// <summary>
        /// If player clicks outside picker window, then close the window
        /// </summary>
        void ParentPanel_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            //If clicking inside picker window, ignore
            if (pickerPanel.Rectangle.Contains(position))
                return;

            // Must be presented for minimum time before allowing to click through
            // This prevents capturing parent-level click events and closing immediately
            if (Time.realtimeSinceStartup - presentationTime < minTimePresented)
                return;

            // Filter out (mouse) fighting activity
            if (InputManager.Instance.GetKey(InputManager.Instance.GetBinding(InputManager.Actions.SwingWeapon)))
                return;

            if (uiManager.TopWindow == this)
            {
                CancelWindow();
            }
        }


    } //class ListPickerWindow


} //namespace
