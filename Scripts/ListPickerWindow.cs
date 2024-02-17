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
            NativePanel.OnMouseClick += ParentPanel_OnMouseClick;
            NativePanel.OnRightMouseClick += ParentPanel_OnMouseClick;
            NativePanel.OnMiddleMouseClick += ParentPanel_OnMouseClick;
            pickerPanel.OnMouseLeave += PickerPanel_OnMouseLeave;

            presentationTime = Time.realtimeSinceStartup;

            ListBox.AlwaysAcceptKeyboardInput = true;
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
        /// If mouse cursor leaves the picker window, close the window
        /// </summary>
        void PickerPanel_OnMouseLeave(BaseScreenComponent sender)
        {
            if (uiManager.TopWindow == this)
            {
                CancelWindow();
            }
        }


        /// <summary>
        /// If player clicks outside picker window, then close the window
        /// </summary>
        void ParentPanel_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            //If clicking inside picker window, ignore
            if (pickerPanel.MouseOverComponent)
                return;

            // Must be presented for minimum time before allowing to click through.
            // This prevents capturing parent-level click events and closing immediately.
            if (Time.realtimeSinceStartup - presentationTime < minTimePresented)
                return;

            // Filter out (mouse) fighting activity.
            if (InputManager.Instance.GetKey(InputManager.Instance.GetBinding(InputManager.Actions.SwingWeapon)))
                return;

            if (uiManager.TopWindow == this)
            {
                CancelWindow();
            }
        }


    } //class ListPickerWindow


} //namespace
