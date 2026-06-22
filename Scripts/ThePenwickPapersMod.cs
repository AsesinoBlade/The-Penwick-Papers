// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System;
using UnityEngine;


namespace ThePenwickPapers
{
    public class ThePenwickPapersMod : MonoBehaviour
    {
        public static Mod Mod;
        static GameObject modObject;
        static string missingTextSubstring;

        public static ThePenwickPapersMod Instance;
        public static SaveData Persistent = new SaveData();
        public static FPSWeapon TheBootAnimator;
        public static FPSGrapplingHook GrapplingHookAnimator;
        public static FPSHandWave HandWaveAnimator;
        public static bool IsMonsterUniversityInstalled;
        public const string PenwickHookName = "Penwick Hook";
        public const string PenwickRopeName = "Penwick Rope";

        Mod firstPersonLightingMod;

        static int potionOfSeekingRecipeKey;
        bool swallowActions = false;
        PlayerActivateModes storedMode;
        int modeSwitchCountdown = 0;

        public Texture2D SummoningEggTexture;


        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            Mod = initParams.Mod;

            modObject = new GameObject(Mod.Title);
            modObject.AddComponent<ThePenwickPapersMod>();

            missingTextSubstring = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.NoTranslationFoundMessage.Substring(0, 14);
            Mod.MessageReceiver = MessageReceiver;

            Mod.IsReady = true;
        }


        /// <summary>
        /// Gets string corresponding to provided key from textdatabase.txt
        /// </summary>
        public static string GetText(string key)
        {
            return Mod.Localize(key);
        }

        public bool DungeonWagonAccessProximityCheck()
        {
            const float proximityWagonAccessDistance = 5f;

            // Get all static doors
            DaggerfallStaticDoors[] allDoors = GameObject.FindObjectsOfType<DaggerfallStaticDoors>();
            if (allDoors != null && allDoors.Length > 0)
            {
                Vector3 playerPos = GameManager.Instance.PlayerObject.transform.position;
                // Find closest door to player
                float closestDoorDistance = float.MaxValue;
                foreach (DaggerfallStaticDoors doors in allDoors)
                {
                    int doorIndex;
                    Vector3 doorPos;
                    if (doors.FindClosestDoorToPlayer(playerPos, -1, out doorPos, out doorIndex, DoorTypes.DungeonExit))
                    {
                        float distance = Vector3.Distance(playerPos, doorPos);
                        if (distance < closestDoorDistance)
                            closestDoorDistance = distance;
                    }
                }

                // Allow wagon access if close enough to any exit door
                if (closestDoorDistance < proximityWagonAccessDistance)
                    return true;
            }
            return false;
        }



        static void MessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            bool val;
            switch (message)
            {
                case "PotionOfSeekingRecipe":
                    callBack?.Invoke("PotionOfSeekingRecipe", potionOfSeekingRecipeKey);
                    break;

                case "ClimbingBonus":
                    callBack?.Invoke("ClimbingBonus", GrapplingHook.ClimbingBonus);
                    break;


                default:
                    Debug.LogErrorFormat("{0}: unknown message received ({1}).", Mod, message);
                    break;
            }

        }

        public static int GetSalePrice(DaggerfallLoot loot)
        {
            var SalePrice = 0;
            ModManager.Instance.SendModMessage("Change Formulas", "SalePrice", loot, (string message, object data) =>
            {
                if (data != null && data is int)
                {
                    SalePrice = (int)data;
                }
            });

            return SalePrice;

        }

        /// <summary>
        /// Checks if specified key actually exists in textdatabase.txt
        /// </summary>
        public static bool TextExists(string key)
        {
            string result = GetText(key);

            return result.Length > 0 && !result.StartsWith(missingTextSubstring);
        }


        /// <summary>
        /// This is called after list picker windows are shown to manually stop swalling activate actions.
        /// </summary>
        public static void StopSwallowingActions()
        {
            Instance.swallowActions = false;
        }


        /// <summary>
        /// Returns the recipe key for the potion of seeking.
        /// </summary>
        public int GetPotionOfSeekingRecipeKey()
        {
            return potionOfSeekingRecipeKey;
        }

        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            Settings.Init(Mod);
        }


        /// <summary>
        /// Returns true if the First-Person-Lighting mod is installed and ready.
        /// </summary>
        public bool UsingFirstPersonLighting()
        {
            return firstPersonLightingMod != null && firstPersonLightingMod.IsReady;
        }


        /// <summary>
        /// Gets the player tint of the player-character if the First-Person-Lighting mod is installed.
        /// Otherwise returns White.
        /// </summary>
        public Color GetPlayerTint()
        {
            Color tint = Color.white;

            if (firstPersonLightingMod == null || !firstPersonLightingMod.IsReady)
                return tint;

            firstPersonLightingMod.MessageReceiver("playerTint", null, (string message, object data) =>
            {
                tint = (Color)data;
            });

            return tint;
        }


        /// <summary>
        /// Gets the lighting around the specified creature if the First-Person-Lighting mod is installed.
        /// Otherwise returns White.
        /// </summary>
        public Color GetEntityLighting(DaggerfallEntityBehaviour creature)
        {
            Color tint = Color.white;

            if (firstPersonLightingMod == null || !firstPersonLightingMod.IsReady)
                return tint;

            firstPersonLightingMod.MessageReceiver("entityLighting", creature, (string message, object data) =>
            {
                tint = (Color)data;
            });

            return tint;
        }


        /// <summary>
        /// Gets the lighting at a specific location if the First-Person-Lighting mod is installed.
        /// Otherwise returns White.
        /// </summary>
        public Color GetLocationLighting(Vector3 location)
        {
            Color tint = Color.white;

            if (firstPersonLightingMod == null || !firstPersonLightingMod.IsReady)
                return tint;

            firstPersonLightingMod.MessageReceiver("locationLighting", location, (string message, object data) =>
            {
                tint = (Color)data;
            });

            return tint;
        }


        /// <summary>
        /// Gets the max range of the player's grope light if the First-Person-Lighting mod is installed.
        /// Otherwise returns 0.
        /// </summary>
        public float GetGropeLightRange()
        {
            float range = 0;

            if (firstPersonLightingMod == null || !firstPersonLightingMod.IsReady)
                return range;

            firstPersonLightingMod.MessageReceiver("gropeLightRange", null, (string message, object data) =>
            {
                range = (float)data;
            });

            return range;
        }

        void Start()
        {
            Debug.Log("Start(): The-Penwick-Papers");
            PlayerActivate.RegisterCustomActivation(Mod, PenwickHookName, OnGrapplingRopeActivated);
            PlayerActivate.RegisterCustomActivation(Mod, PenwickRopeName, OnGrapplingRopeActivated);

            Instance = this;
            FormulaHelper.RegisterOverride(Mod, "WorldDataObjectIsClimbable", (Func<string, int, bool>)WorldDataObjectIsClimbable);
            Mod.SaveDataInterface = Persistent;


            //Settings.Init(Mod);

            firstPersonLightingMod = ModManager.Instance.GetMod("First-Person-Lighting");

            IsMonsterUniversityInstalled = (ModManager.Instance.GetMod("Monster-University") != null);

            Mod.LoadSettingsCallback = LoadSettings;
            Mod.LoadSettings();

            CreateSummoningEggTexture();

            //Make sure all localization text keys have entries in textdatabase.txt
            if (TextExtension.CheckTextKeysValid())
                Debug.Log("All localization text keys are valid");
            else
                Debug.LogWarning("Some localization text keys were not valid");


            //Attach accessory mod components
            GrapplingHookAnimator = modObject.AddComponent<FPSGrapplingHook>();
            GrapplingHookAnimator.enabled = false;

            HandWaveAnimator = modObject.AddComponent<FPSHandWave>();
            HandWaveAnimator.enabled = false;

            //add FPSWeapon component for TheBoot
            TheBootAnimator = modObject.AddComponent<FPSWeapon>();
            TheBootAnimator.WeaponType = WeaponTypes.Melee;
            TheBootAnimator.enabled = false;


            //event handler registration
            StartGameBehaviour.OnStartGame += StartGameBehaviour_OnStartGameHandler;
            SaveLoadManager.OnStartLoad += SaveLoadManager_OnStartLoadHandler;
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoadHandler;
            PlayerDeath.OnPlayerDeath += PlayerDeath_OnPlayerDeathHandler;
            DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallRestWindow.OnSleepEnd += DaggerfallRestWindow_OnSleepEndHandler;
            EnemyEntity.OnLootSpawned += EnemyEntity_OnLootSpawned;
            PlayerEnterExit.OnPreTransition += PlayerEnterExit_HandleOnPreTransition;


            //Register the FormulaHelper overrides that are used for skill advancement if enabled
            if (Settings.EnableGoverningAttributes)
            {
                FormulaHelper.RegisterOverride(Mod, "CalculateSkillUsesForAdvancement",
                    (Func<int, int, float, int, int>)SkillAdvancement.CalculateSkillUsesForAdvancement);

                FormulaHelper.RegisterOverride(Mod, "CalculatePlayerLevel",
                    (Func<int, int, int>)SkillAdvancement.CalculatePlayerLevel);
            }

            RegisterSpellsAndItems();

            Debug.Log("Finished Start(): The-Penwick-Papers");
        }

        static void OnGrapplingRopeActivated(RaycastHit hit)
        {
            GameObject clickedObject = hit.transform.gameObject;
            GameObject ropeObject = ResolveRopeObject(clickedObject);

            if (ropeObject == null)
            {
                Debug.LogWarning("[Penwick] OnGrapplingRopeActivated: could not resolve rope from clicked object: " + clickedObject.name);
                return;
            }

            bool clickedHook = (ropeObject != clickedObject); // came via hook path
            TeleportToRopeEnd(ropeObject, clickedHook);
        }

        /// <summary>
        /// Given either the hook or the rope GameObject, always returns the rope.
        /// Returns null if neither can be identified.
        /// </summary>
        static GameObject ResolveRopeObject(GameObject clicked)
        {
            // Clicked directly on the rope
            if (clicked.name == PenwickRopeName)
                return clicked;

            // Clicked on the hook — rope is a child
            if (clicked.name == PenwickHookName)
            {
                Transform ropeChild = clicked.transform.Find(PenwickRopeName);
                return ropeChild != null ? ropeChild.gameObject : null;
            }

            // Clicked on something else entirely
            return null;
        }

        /// <summary>
        /// Teleports the player to the bottom of the rope when the hook was clicked,
        /// or to the top when the rope itself was clicked.
        /// </summary>
        static void TeleportToRopeEnd(GameObject ropeObject, bool toBottom)
        {
            PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
            Bounds bounds = GetWorldBounds(ropeObject.transform);

            float destinationY = toBottom ? bounds.min.y : bounds.max.y + 3;

            // Get the hook (parent of rope) center for the reflection pivot
            Vector3 hookPos = ropeObject.transform.parent != null
                ? ropeObject.transform.parent.position
                : bounds.center;

            Vector3 playerPos = playerMotor.transform.position;

            // XZ offset from hook to player
            float offsetX = playerPos.x - hookPos.x;
            float offsetZ = playerPos.z - hookPos.z;

            // Reflect through the hook: place player on the opposite side
            float destinationX = (hookPos.x - offsetX);
            float destinationZ = hookPos.z - offsetZ;

            Vector3 destination = new Vector3(destinationX, destinationY, destinationZ);

            playerMotor.transform.position = destination;
            playerMotor.FixStanding();
        }

        /// <summary>
        /// Returns a world-space Bounds that encapsulates the object and all its children,
        /// preferring Collider bounds (physics shape) and falling back to Renderer bounds.
        /// </summary>
        static Bounds GetWorldBounds(Transform root)
        {
            // Try colliders first - more reliable for non-rendered physics objects
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                    b.Encapsulate(colliders[i].bounds);
                return b;
            }

            // Fall back to renderers
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                return b;
            }

            // Last resort: a small bounds at the object's own position
            return new Bounds(root.position, Vector3.one);
        }

        private bool WorldDataObjectIsClimbable(string dataID, int ladder)
        {
            if (ladder == 41409)
                return true;

            if (dataID == "Penwick Rope")
                return true;

            return false;
        }


        void Update()
        {
            if (GameManager.IsGamePaused)
            {
                //check if showing character sheet window or inventory window and add encumbrance detail information
                EncumbranceDetails.CheckAddComponents();
                SkillAdvancement.CheckAddLevelButton();

                return; //Let's not hasten the heat-death of the universe
            }

            CheckForActivationAction();

            CheckUndeadInTown();

            DirtyTricks.CheckEnemyBlindAttempt();

            DirtyTricks.RefillPebblesOfSkulduggery();

            //control behavior of any summoned atronach/undead minions
            PenwickMinion.GuideMinions();

        }


        /// <summary>
        /// Checks for player activation actions and passes control to appropriate modules.
        /// If no module can use the activation action, it is ignored and left to the system.
        /// </summary>
        void CheckForActivationAction()
        {
            //Check if user-configured alternate mouse buttons have been used
            CheckMouseButtonOneShotActions();


            //swallow activate actions until mouse button is released
            if (swallowActions)
            {
                //ActionComplete is true after mouse button is released
                if (InputManager.Instance.ActionComplete(InputManager.Actions.ActivateCenterObject))
                {
                    swallowActions = false;
                }

                //This will prevent PlayerActivate from handling completed ActivateCenterObject
                //actions for a fraction of a second.
                GameManager.Instance.PlayerActivate.SetClickDelay(0.1f);

                return;
            }


            //This ActionStarted() check will trigger before the PlayerActivate ActionComplete() check
            //which gives us an opportunity to test and swallow events if needed.
            if (InputManager.Instance.ActionStarted(InputManager.Actions.ActivateCenterObject))
            {
                if (GameManager.Instance.PlayerEffectManager.HasReadySpell)
                    return;
                else if (GameManager.Instance.PlayerMouseLook.cursorActive)
                    return;

                Camera camera = GameManager.Instance.MainCamera;

                Collider playerCollider = GameManager.Instance.PlayerController;
                playerCollider.enabled = false;

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                float maxDistance = GrapplingHook.MaxRopeLength;
                bool hitSomething = Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, ~0);

                playerCollider.enabled = true;

                if (hitSomething)
                {
                    DaggerfallEntityBehaviour creature = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();

                    bool actionHandled = false;

                    PlayerActivateModes mode = GameManager.Instance.PlayerActivate.CurrentMode;

                    if (Settings.EnableEnhancedInfo && mode == PlayerActivateModes.Info)
                    {
                        actionHandled = EnhancedInfo.ShowEnhancedEntityInfo(hitInfo);
                    }

                    if (!actionHandled && Settings.EnableHerbalism && mode == PlayerActivateModes.Grab)
                    {
                        //must be crouched and activating ground or ally/neutral
                        actionHandled = Herbalism.AttemptRemedy(hitInfo);
                    }

                    if (!actionHandled && Settings.EnableTrapping && mode == PlayerActivateModes.Steal)
                    {
                        //must be crouched and activating ground
                        actionHandled = Trapping.AttemptLayTrap(hitInfo);
                    }

                    if (!actionHandled)
                    {
                        actionHandled = DirtyTricks.CheckAttemptTrick(hitInfo);
                    }

                    if (!actionHandled && mode == PlayerActivateModes.Grab)
                    {
                        actionHandled = GrapplingHook.AttemptHook(hitInfo);
                    }

                    if (!actionHandled && creature)
                    {
                        PenwickMinion minion = creature.GetComponent<PenwickMinion>();
                        actionHandled = minion && minion.Activate(hitInfo.distance);
                    }

                    if (!actionHandled && Settings.EnableLockpickGame && mode == PlayerActivateModes.Steal)
                    {
                        actionHandled = LockpickWindow.CheckLockpickGame(hitInfo);
                    }

                    if (actionHandled)
                    {
                        //We successfully handled this action, need to prevent PlayerActivate from seeing anything
                        swallowActions = true;
                    }
                }
            }
        }


        /// <summary>
        /// Checks if alternate mouse buttons have been clicked (mouse buttons 3 and 4)
        /// and handles the action by briefly switching to alternate interaction mode,
        /// performing the activation, then switching back, over a span of multiple frames.
        /// </summary>
        void CheckMouseButtonOneShotActions()
        {
            if (modeSwitchCountdown > 0)
                --modeSwitchCountdown;

            if (modeSwitchCountdown == 1)
            {
                //activation finished, switch back to previous mode
                GameManager.Instance.PlayerActivate.ChangeInteractionMode(storedMode, false);
            }

            //check to see if mouse3 or mouse4 buttons have been used
            if (InputManager.Instance.GetKeyUp(KeyCode.Mouse3))
                StartOneShotMouseAction(Settings.Mouse3Mode);
            else if (InputManager.Instance.GetKeyUp(KeyCode.Mouse4))
                StartOneShotMouseAction(Settings.Mouse4Mode);

        }



        /// <summary>
        /// Switches activation mode if necessary and adds the ActivateCenterObject action to InputManager.
        /// </summary>
        void StartOneShotMouseAction(int modeNum)
        {
            PlayerActivateModes mode;

            switch (modeNum)
            {
                case 1: mode = PlayerActivateModes.Steal; break;
                case 2: mode = PlayerActivateModes.Grab; break;
                case 3: mode = PlayerActivateModes.Info; break;
                case 4: mode = PlayerActivateModes.Talk; break;
                default: return;
            }

            if (mode != GameManager.Instance.PlayerActivate.CurrentMode)
            {
                storedMode = GameManager.Instance.PlayerActivate.CurrentMode;
                modeSwitchCountdown = 3;
                GameManager.Instance.PlayerActivate.ChangeInteractionMode(mode, false);
            }

            InputManager.Instance.AddAction(InputManager.Actions.ActivateCenterObject);
        }



        /// <summary>
        /// Check if player is walking around town with reanimated undead, which is a felony is these parts.
        /// </summary>
        void CheckUndeadInTown()
        {
            if (!GameManager.Instance.PlayerGPS.IsPlayerInTown(true, true))
                return;  //either not in a town or inside a buliding

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
                return;

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            //if player already wanted for a crime, skip
            if (player.CrimeCommitted != 0)
                return;

            if (Dice100.FailedRoll(1))
                return;  //only checking occasionally


            //Player is in a town, outside, during the daytime.
            //Check if they have any undead minions.
            // "Oi! You gotta loicence for dat zombie?"
            foreach (PenwickMinion minion in PenwickMinion.GetMinions())
            {
                EnemyEntity entity = minion.GetComponent<DaggerfallEntityBehaviour>().Entity as EnemyEntity;
                if (entity.MobileEnemy.Affinity == MobileAffinity.Undead)
                {
                    //there officially is no 'Unlawful_Use_Of_Necromancy' crime in the books
                    player.CrimeCommitted = PlayerEntity.Crimes.Criminal_Conspiracy;
                    player.SpawnCityGuards(true);
                    break;
                }
            }

        }


        /// <summary>
        /// Loads the texture assets needed by this mod.
        /// </summary>
        void CreateSummoningEggTexture()
        {
            //using native game texture for summoning egg
            GetTextureResults results = GetBuiltinTexture(157, 1);
            SummoningEggTexture = results.albedoMap;
            //modify the alpha transparency
            Color32[] pixels = SummoningEggTexture.GetPixels32();
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].a = (byte)(255 - pixels[i].r);
            }
            SummoningEggTexture.SetPixels32(pixels);
            SummoningEggTexture.Apply(false);
        }


        /// <summary>
        /// Gets game texture as GetTextureResults given the archive and record.
        /// </summary>
        GetTextureResults GetBuiltinTexture(int archive, int record)
        {
            GetTextureSettings settings = new GetTextureSettings
            {
                archive = archive,
                record = record,
                frame = 0,
                stayReadable = true
            };
            return DaggerfallUnity.Instance.MaterialReader.TextureReader.GetTexture2D(settings);
        }


        /// <summary>
        /// Registers magic effect templates and item templates.
        /// </summary>
        void RegisterSpellsAndItems()
        {
            EntityEffectBroker effectBroker = GameManager.Instance.EntityEffectBroker;

            if (Settings.EnableHerbalism)
            {
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(
                    MortarAndPestle.MortarAndPestleTemplateIndex,
                    ItemGroups.MiscellaneousIngredients2,
                    typeof(MortarAndPestle));

                HerbalRemedy herbalRemedyTemplateEffect = new HerbalRemedy();
                effectBroker.RegisterEffectTemplate(herbalRemedyTemplateEffect);

                Envenomed envenomedTemplateEffect = new Envenomed();
                effectBroker.RegisterEffectTemplate(envenomedTemplateEffect);
            }

            if (Settings.EnableTrapping)
            {
                LunaStick lunaStickTemplateEffect = new LunaStick();
                effectBroker.RegisterEffectTemplate(lunaStickTemplateEffect);
            }

            if (Settings.AddItems)
            {
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(
                    LandmarkJournalItem.LandmarkJournalTemplateIndex,
                    ItemGroups.UselessItems2,
                    typeof(LandmarkJournalItem));

                //Only add our HookAndRope item if Skulduggery isn't installed.
                Mod skulduggery = ModManager.Instance.GetMod("Skulduggery - A Thief Overhaul");
                if (skulduggery == null)
                {
                    DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(
                        GrapplingHookAndRope.HookAndRopeTemplateIndex,
                        ItemGroups.UselessItems2,
                        typeof(GrapplingHookAndRope));
                }

                Seeking effect = new Seeking();
                effect.SetCustomName(Text.SeekingPotionName.Get());
                effectBroker.RegisterEffectTemplate(effect, true);
                PotionRecipe recipe = effectBroker.GetEffectPotionRecipe(effect);
                potionOfSeekingRecipeKey = recipe.GetHashCode();
            }

            if (Settings.AddSpells)
            {
                SummonImp summonImpTemplateEffect = new SummonImp();
                effectBroker.RegisterEffectTemplate(summonImpTemplateEffect);

                CreateAtronach createAtronachTemplateEffect = new CreateAtronach();
                effectBroker.RegisterEffectTemplate(createAtronachTemplateEffect);

                Reanimate reanimateTemplateEffect = new Reanimate();
                effectBroker.RegisterEffectTemplate(reanimateTemplateEffect);

                Scour scourTemplateEffect = new Scour();
                effectBroker.RegisterEffectTemplate(scourTemplateEffect);

                IllusoryDecoy illusoryDecoyTemplateEffect = new IllusoryDecoy();
                effectBroker.RegisterEffectTemplate(illusoryDecoyTemplateEffect);

                //The 'Blind' spell effect is needed for dirty tricks to work
                Blind blindTemplateEffect = new Blind();
                effectBroker.RegisterEffectTemplate(blindTemplateEffect);

                WindWalk windWalkTemplateEffect = new WindWalk();
                effectBroker.RegisterEffectTemplate(windWalkTemplateEffect);
            }

        }


        /// <summary>
        /// Event handler triggered when creating a new character.
        /// </summary>
        void StartGameBehaviour_OnStartGameHandler(object sender, EventArgs e)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            if (Settings.StartGameWithPotionOfSeeking && Settings.AddItems)
            {
                //when new character is created, add Potion Of Seeking recipe to their inventory
                DaggerfallUnityItem potionRecipe = new DaggerfallUnityItem(ItemGroups.MiscItems, 4) { PotionRecipeKey = potionOfSeekingRecipeKey };
                player.Items.AddItem(potionRecipe);

                //player must have nabbed a potion from a kiosk
                DaggerfallUnityItem item = ItemBuilder.CreatePotion(potionOfSeekingRecipeKey);
                player.Items.AddItem(item);
            }

            if (Settings.EnableHerbalism)
            {
                //give the player starting herbalism equipment if high enough medical skill
                int medical = player.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
                if (medical >= Settings.MedicalNeededSkillForStartingHerbalism)
                {
                    DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.MiscellaneousIngredients2, MortarAndPestle.MortarAndPestleTemplateIndex);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.PlantIngredients2, (int)PlantIngredients2.Bamboo);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Rain_water);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.PlantIngredients1, (int)PlantIngredients1.Root_bulb);
                    player.Items.AddItem(item);
                    item = ItemBuilder.CreateItem(ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Elixir_vitae);
                    player.Items.AddItem(item);
                }
            }
        }


        /// <summary>
        /// Event handler triggered when a game has started loading.
        /// </summary>
        void SaveLoadManager_OnStartLoadHandler(SaveData_v1 saveData)
        {
            WindWalk.Reset();
        }


        /// <summary>
        /// Event handler triggered when player dies
        /// </summary>
        void PlayerDeath_OnPlayerDeathHandler(object sender, EventArgs e)
        {
            WindWalk.Reset();
        }


        /// <summary>
        /// Event handler triggered when a game has finished loading.
        /// </summary>
        void SaveLoadManager_OnLoadHandler(SaveData_v1 saveData)
        {
            PenwickMinion.InitializeOnLoad();

            if (Settings.EnableTrapping)
                Trapping.OnLoadEvent();

            if (Settings.EnableHerbalism)
                Herbalism.OnLoadEvent();
        }


        /// <summary>
        /// Event handler triggered when player enters/exits a dungeon or building.
        /// </summary>
        void PlayerEnterExit_HandleOnPreTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            //Clear landmark journal dungeon locations when entering/exiting a dungeon
            Persistent.DungeonLocations.Clear();

            //Enemies in dungeons might have slightly different loot (like torches)
            Loot.InDungeon = (args.TransitionType == PlayerEnterExit.TransitionType.ToDungeonInterior);
        }


        /// <summary>
        /// Event handler triggered when player takes a long rest (6 hours or more?)
        /// Heal/recharge minions after lengthy rest, if possible.
        /// Distant following minions catch up to player.
        /// </summary>
        void DaggerfallRestWindow_OnSleepEndHandler()
        {
            PenwickMinion.RepositionFollowers();
            PenwickMinion.Rest();
        }


        /// <summary>
        /// Event handler triggered when enemy loot spawns, which usually happens when creature is created
        /// </summary>
        void EnemyEntity_OnLootSpawned(System.Object sender, EnemyLootSpawnedEventArgs lootArgs)
        {
            if (Settings.EnableLootAdjustment && sender is EnemyEntity)
            {
                Loot.AddItems(sender as EnemyEntity, lootArgs);
            }
        }


    } //class ThePenwickPapersMod



} //namespace