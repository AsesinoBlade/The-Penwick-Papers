// Project:      The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  July 2023

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop;
using DaggerfallConnect;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Entity;

namespace ThePenwickPapers
{

    public class LockpickWindow : DaggerfallPopupWindow
    {
        static Transform doorOwner;
        static int lockValue;
        static StaticDoor staticDoor;
        static DaggerfallActionDoor actionDoor;
        static Collider doorCollider;

        readonly Color dimColor = new Color(0, 0, 0, 0.5f);
        readonly Panel mainPanel = new Panel();
        readonly Panel tumblerPlatePanel = new Panel();
        readonly Panel keyholePlatePanel = new Panel();
        readonly Panel keyholePanel = new Panel();
        readonly Panel keyholeProgessPanel = new Panel();
        readonly Panel[] arrowButtonOverlays = new Panel[4];
        readonly Panel[] tumblers = new Panel[4];

        readonly float lockCompletionMax;

        DaggerfallAudioSource dfAudioSource;
        AudioClip lockpickDrop;
        AudioClip lockpickFail;
        AudioClip lockpickUp;
        AudioClip[] lockpickTinker;

        Texture2D tumblerTexture;

        Rect lockpickPlateRect;
        InputManager.Actions lastAction = InputManager.Actions.Unknown;
        float lockCompletionCount;


        /// <summary>
        /// Checks if player clicked on a locked door within range (interior or exterior).
        /// Shows lockpicking window if a pickable door was clicked.
        /// </summary>
        /// <param name="hit">The raycast hit info</param>
        /// <returns>true if player clicked on pickable door lock</returns>
        public static bool CheckLockpickGame(RaycastHit hit)
        {
            doorOwner = null;
            staticDoor = new StaticDoor();
            actionDoor = null;

            // Check if close enough to activate
            if (hit.distance > PlayerActivate.DoorActivationDistance)
                return false;

            actionDoor = hit.collider.gameObject.GetComponent<DaggerfallActionDoor>();
            if (actionDoor)
            {
                if (!actionDoor.IsLocked)
                    return false;
                else
                    lockValue = actionDoor.CurrentLockValue;

                if (actionDoor.FailedSkillLevel < 0)
                {
                    //The door is chocked (see DirtyTricks.cs ChockDoor())
                    Utility.AddHUDText(Text.DoorJammed.Get());
                    return true;
                }
            }
            else if (!HitLockedStaticBuildingDoor(hit))
            {
                return false;
            }

            if (lockValue >= 20)
            {
                Utility.AddHUDText(TextManager.Instance.GetLocalizedText("magicLock"));
                return true;
            }

            doorCollider = hit.collider;

            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
            if (!(uiManager.TopWindow is LockpickWindow))
            {
                LockpickWindow lockpickPopup = new LockpickWindow(uiManager);
                uiManager.PushWindow(lockpickPopup);
            }

            return true;
        }


        /// <summary>
        /// Logic taken from PlayerActivate.
        /// Checks if player is clicking on an exterior locked door.
        /// </summary>
        static bool HitLockedStaticBuildingDoor(RaycastHit hit)
        {
            PlayerActivate playerActivate = GameManager.Instance.PlayerActivate;

            // Check for a static building hit
            DaggerfallStaticBuildings buildings = playerActivate.GetBuildings(hit.transform, out _);
            if (buildings && buildings.HasHit(hit.point, out StaticBuilding building))
            {
                // Get building directory for location
                BuildingDirectory buildingDirectory = GameManager.Instance.StreamingWorld.GetCurrentBuildingDirectory();
                if (!buildingDirectory)
                    return false;

                // Get detailed building data from directory
                if (!buildingDirectory.GetBuildingSummary(building.buildingKey, out BuildingSummary buildingSummary))
                    return false;

                if (playerActivate.BuildingIsUnlocked(buildingSummary))
                    return false;

                //In the base game, lock value is half of building quality.  We will make it harder for the minigame.
                lockValue = buildingSummary.Quality;
            }
            else
            {
                return false;
            }

            // Check for a static door hit
            DaggerfallStaticDoors doors = playerActivate.GetDoors(hit.transform, out doorOwner);
            if (!doors)
                return false;

            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;

            if (CustomDoor.HasHit(hit, out staticDoor) || (doors && doors.HasHit(hit.point, out staticDoor)))
            {
                if (staticDoor.doorType == DoorTypes.Building && !playerEnterExit.IsPlayerInside)
                {
                    // Discover building
                    GameManager.Instance.PlayerGPS.DiscoverBuilding(building.buildingKey);

                    return true;
                }
            }

            return false;
        }



        public LockpickWindow(IUserInterfaceManager uiManager) : base(uiManager)
        {
            PauseWhileOpen = false;
            ParentPanel.BackgroundColor = new Color(0, 0, 0, 0.65f);

            lockCompletionMax = 2 + lockValue / 4;
        }


        protected override void Setup()
        {
            base.Setup();

            //Make sure we've stopped swallowing activation actions
            ThePenwickPapersMod.StopSwallowingActions();

            dfAudioSource = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();

            lockpickDrop = Assets.LockpickDrop.Get<AudioClip>();
            lockpickFail = Assets.LockpickFail.Get<AudioClip>();
            lockpickUp = Assets.LockpickUp.Get<AudioClip>();
            lockpickTinker = new AudioClip[4];
            lockpickTinker[0] = Assets.LockpickMetal1.Get<AudioClip>();
            lockpickTinker[1] = Assets.LockpickMetal2.Get<AudioClip>();
            lockpickTinker[2] = Assets.LockpickMetal3.Get<AudioClip>();
            lockpickTinker[3] = Assets.LockpickMetal4.Get<AudioClip>();

            tumblerTexture = Assets.Tumbler.Get<Texture2D>();

            Texture2D texture;

            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            texture = Assets.LockPickPanel.Get<Texture2D>();
            mainPanel.BackgroundTexture = texture;
            mainPanel.Size = new Vector2(texture.width, texture.height);
            
            NativePanel.Components.Add(mainPanel);

            texture = Assets.LockPickPlate.Get<Texture2D>();
            tumblerPlatePanel.BackgroundTexture = texture;
            tumblerPlatePanel.Size = new Vector2(texture.width, texture.height);
            tumblerPlatePanel.Position = new Vector2(5, 4);
            lockpickPlateRect = new Rect(tumblerPlatePanel.Position, tumblerPlatePanel.Size);
            
            mainPanel.Components.Add(tumblerPlatePanel);

            //Add black panel behind keyhole graphic
            keyholePanel.BackgroundColor = Color.black;
            keyholePanel.Size = new Vector2(12, 28);
            keyholePanel.Position = new Vector2(130, 32);

            mainPanel.Components.Add(keyholePanel);

            //Add green progress bar on top of black keyhole panel
            keyholeProgessPanel.Size = new Vector2(keyholePanel.Size.x, 0);
            keyholeProgessPanel.HorizontalAlignment = HorizontalAlignment.Center;
            keyholeProgessPanel.VerticalAlignment = VerticalAlignment.Bottom;
            keyholeProgessPanel.BackgroundColor = new Color(0f, 0.8f, 0f);

            keyholePanel.Components.Add(keyholeProgessPanel);

            //Overlay the keyhole panel and progress bar with the keyhole graphic
            texture = Assets.KeyholePlate.Get<Texture2D>();
            keyholePlatePanel.BackgroundTexture = texture;
            keyholePlatePanel.Size = new Vector2(texture.width, texture.height);
            keyholePlatePanel.Position = new Vector2(114, 21);

            mainPanel.Components.Add(keyholePlatePanel);

            //Add semi-transparent overlay panels for the arrow 'buttons'
            for (int i = 0; i < arrowButtonOverlays.Length; ++i)
            {
                arrowButtonOverlays[i] = new Panel
                {
                    BackgroundColor = dimColor,
                    Size = new Vector2(18, 16),
                    Position = new Vector2(11 + i * 23, 63)
                };

                mainPanel.Components.Add(arrowButtonOverlays[i]);
            }
        }


        public override void Update()
        {
            base.Update();

            if (CancelLockpicking())
            {
                CloseWindow();
                return;
            }

            CheckMovementKeys();

            AdjustTumblers();

            //Update the lock completion progress bar.
            float progress = lockCompletionCount / lockCompletionMax;
            float height = keyholePanel.Size.y * progress;
            keyholeProgessPanel.Size = new Vector2(keyholeProgessPanel.Size.x, height);

            //Play some lockpick tinkering sounds.
            if (Random.Range(0f, 0.3f) < Time.smoothDeltaTime)
            {
                AudioClip clip = lockpickTinker[Random.Range(0, lockpickTinker.Length)];
                dfAudioSource.AudioSource.PlayOneShot(clip);
            }
        }


        /// <summary>
        /// Player turning away from the door signals that lockpicking should be cancelled.
        /// </summary>
        bool CancelLockpicking()
        {
            Camera camera = GameManager.Instance.MainCamera;

            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            int playerMask = ~LayerMask.GetMask("Player");
            bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, PlayerActivate.DoorActivationDistance, playerMask);

            if (!hit)
                return true;

            if (hitInfo.collider.transform.parent.name.Contains("MagicCandle"))
            {
                //A collider of the light spell candle can potentially interfere.
                //This might be due to an issue with the Darker Dungeons mod.
                return false;
            }
            else if (actionDoor)
            {
                //If no longer pointing at interior action door, cancel lockpicking
                return hitInfo.collider != doorCollider || actionDoor.IsOpen || !actionDoor.IsLocked;
            }
            else if (CustomDoor.HasHit(hitInfo, out _))
            {
                return false;
            }
            else
            {
                //If no longer pointing at exterior static door, cancel lockpicking
                DaggerfallStaticDoors doors = GameManager.Instance.PlayerActivate.GetDoors(hitInfo.transform, out _);
                if (doors == null)
                    return true;
                return !doors.HasHit(hitInfo.point, out staticDoor);
            }
        }



        /// <summary>
        /// Check for movement keys to activate tumblers.
        /// </summary>
        void CheckMovementKeys()
        {
            bool movementKeyDown = false;

            foreach (InputManager.Actions action in InputManager.Instance.CurrentActions)
            {
                switch (action)
                {
                    case InputManager.Actions.MoveLeft:
                    case InputManager.Actions.MoveForwards:
                    case InputManager.Actions.MoveBackwards:
                    case InputManager.Actions.MoveRight:
                        movementKeyDown = true;
                        if (action != lastAction)
                        {
                            //The key was just pressed.
                            lastAction = action;
                            EvaluateAction(action);
                        }
                        break;
                    case InputManager.Actions.Escape:
                        CloseWindow();
                        break;
                    case InputManager.Actions.ActivateCursor:
                        InputManager.Instance.ClearAllActions();
                        break;
                }

                if (movementKeyDown)
                    break;
            }

            if (movementKeyDown)
                InputManager.Instance.ClearAllActions(); //To prevent player from actually moving.
            else
                lastAction = InputManager.Actions.Unknown;
        }


        /// <summary>
        /// Activate selected tumbler, determined by which movement key was pressed.
        /// </summary>
        void EvaluateAction(InputManager.Actions action)
        {
            switch (action)
            {
                case InputManager.Actions.MoveLeft:
                    PickTumbler(0);
                    break;
                case InputManager.Actions.MoveForwards:
                    PickTumbler(1);
                    break;
                case InputManager.Actions.MoveBackwards:
                    PickTumbler(2);
                    break;
                case InputManager.Actions.MoveRight:
                    PickTumbler(3);
                    break;
            }
        }


        /// <summary>
        /// Evaluate tumbler/lock picking success for selected tumbler index.
        /// </summary>
        void PickTumbler(int index)
        {
            //Note: an enabled button overlay means the button itself is disabled
            if (arrowButtonOverlays[index].Enabled)
            {
                //Button was inactive, missed tumbler.  Eliminate all progress.
                if (lockCompletionCount > 0)
                    dfAudioSource.AudioSource.PlayOneShot(lockpickFail);

                lockCompletionCount = 0;
            }
            else if (tumblers[index].Tag != null)
            {
                //success
                lockCompletionCount += 1;
                tumblers[index].Tag = null;
                if (lockCompletionCount < lockCompletionMax)
                    dfAudioSource.AudioSource.PlayOneShot(lockpickUp);
            }

            if (lockCompletionCount >= lockCompletionMax)
            {
                CloseWindow();
                UnlockDoor();
            }
        }


        /// <summary>
        /// Add/move tumblers down tumbler plate.
        /// </summary>
        void AdjustTumblers()
        {
            for (int i = 0; i < tumblers.Length; ++i)
                AdjustTumbler(i);
        }


        static readonly int[] tumblerX = { 13, 36, 59, 81 };
        /// <summary>
        /// Moves an existing tumbler down the tumbler plate, or check to add a new one if one doesn't exist.
        /// </summary>
        void AdjustTumbler(int index)
        {
            if (tumblers[index] == null)
            {
                //Check to spawn a new tumbler.
                float max = 2.6f - Mathf.Log10(10 + lockValue);
                if (Random.Range(0f, max) < Time.smoothDeltaTime)
                {
                    int tumblerHeight = (34 - lockValue) / 2;
                    tumblers[index] = new Panel
                    {
                        BackgroundTexture = tumblerTexture,
                        BackgroundTextureLayout = BackgroundLayout.StretchToFill,
                        Position = new Vector2(tumblerX[index], -tumblerHeight),
                        Size = new Vector2(5, tumblerHeight),
                        RestrictedRenderAreaCoordinateType = BaseScreenComponent.RestrictedRenderArea_CoordinateType.ParentCoordinates,
                        RectRestrictedRenderArea = lockpickPlateRect,
                        Tag = "live"
                    };
                    tumblerPlatePanel.Components.Add(tumblers[index]);
                }
            }
            else
            {
                ShiftTumbler(index);
            }
        }


        /// <summary>
        /// Moves selected tumbler further down the tumbler plate at a rate primarily determined by lockpick skill.
        /// Enable/disable arrow 'button' overlays based on tumbler position.
        /// Penalize player if tumbler exits plate without being activated.
        /// </summary>
        void ShiftTumbler(int index)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            //The reflex value was chosen by the player during character creation.
            float reflexModifier = 800 - (int)player.Reflexes * 100;
            int skill = player.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);
            float speed = Time.smoothDeltaTime * reflexModifier / Mathf.Sqrt(skill);

            //Positive Y is in the down direction
            tumblers[index].Position += Vector2.up * speed;

            float top = tumblers[index].Position.y;
            float bottom = top + tumblers[index].Size.y;

            if (bottom > lockpickPlateRect.height)
            {
                //Tumbler touching bottom edge of plate, disable overlay to highlight the arrow 'button'.
                arrowButtonOverlays[index].Enabled = false;
            }

            if (top > lockpickPlateRect.height)
            {
                //Tumbler is exiting the screen/window.
                tumblerPlatePanel.Components.Remove(tumblers[index]);
                if (tumblers[index].Tag != null)
                {
                    //tumbler exited the screen without being activated, penalty
                    if (lockCompletionCount > 0)
                    {
                        dfAudioSource.AudioSource.PlayOneShot(lockpickDrop);
                        --lockCompletionCount;
                    }
                }
                tumblers[index] = null;

                //Re-enable button overlay.
                arrowButtonOverlays[index].Enabled = true;
            }
        }


        /// <summary>
        /// Play unlock sound and increment player lockpick skill.
        /// If an interior door, door is unlocked.
        /// If an exterior door, player is transitioned into the interior.
        /// </summary>
        void UnlockDoor()
        {
            // Show success and play unlock sound.
            DaggerfallAudioSource playerAudio = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();
            DaggerfallUI.Instance.PopupMessage(TextManager.Instance.GetLocalizedText("lockpickingSuccess"));
            playerAudio.PlayOneShot(SoundClips.ActivateLockUnlock);

            //Tougher locks net more lockpicking experience.
            int amount = 1 + lockValue / 3;
            GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Lockpicking, (short)amount);

            if (actionDoor)
            {
                //Door was an interior door.
                actionDoor.CurrentLockValue = 0;
            }
            else
            {
                //Door was an exterior building door.
                GameManager.Instance.PlayerEntity.TallyCrimeGuildRequirements(true, 1);
                TransitionInterior(doorOwner, staticDoor, true);
            }
        }



        // Custom transition to store building data before entering building.
        // Note: Logic copied from PlayerActivate.
        void TransitionInterior(Transform doorOwner, StaticDoor door, bool doFade = false)
        {
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;

            // Get building directory for location
            BuildingDirectory buildingDirectory = GameManager.Instance.StreamingWorld.GetCurrentBuildingDirectory();
            if (!buildingDirectory)
            {
                Debug.LogError("LockpickWindow.TransitionInterior() could not retrieve BuildingDirectory.");
                return;
            }

            // Get building discovery data - this is added when player clicks door at exterior
            if (!GameManager.Instance.PlayerGPS.GetDiscoveredBuilding(door.buildingKey, out PlayerGPS.DiscoveredBuilding db))
            {
                Debug.LogErrorFormat("LockpickWindow.TransitionInterior() could not retrieve DiscoveredBuilding for key {0}.", door.buildingKey);
                return;
            }

            // Perform transition
            playerEnterExit.BuildingDiscoveryData = db;
            playerEnterExit.IsPlayerInsideOpenShop = RMBLayout.IsShop(db.buildingType) && PlayerActivate.IsBuildingOpen(db.buildingType);
            playerEnterExit.IsPlayerInsideTavern = RMBLayout.IsTavern(db.buildingType);
            playerEnterExit.IsPlayerInsideResidence = RMBLayout.IsResidence(db.buildingType);
            playerEnterExit.TransitionInterior(doorOwner, door, doFade, false);
        }




    } //class LockPickWindow


} //namespace
