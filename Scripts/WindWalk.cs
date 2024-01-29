// Project:     WindWalk, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using UnityEngine;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop;


namespace ThePenwickPapers
{

    public class WindWalk : IncumbentEffect
    {
        public const string effectKey = "Wind-Walk";

        static GameObject windAudioObject;
        static bool resuming;

        float originalLevitateSpeed;
        float spellMagnitude;
        Vector3 lastPlayerPosition;
        Vector3 velocity;
        int exitLocationCountdown = 0;


        public override string GroupName => Text.WindWalkGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();


        /// <summary>
        /// This gets called on certain events, such as game load and player death.
        /// It verifies that any looping audio object is destroyed.
        /// Also verifies the player's levitate move speed is set to the standard value.
        /// </summary>
        public static void Reset()
        {
            if (!resuming && windAudioObject)
                GameObject.Destroy(windAudioObject);

            //Make sure the player levitate move speed is the correct standard value
            LevitateMotor levitator = GameManager.Instance.PlayerObject.GetComponent<LevitateMotor>();
            levitator.LevitateMoveSpeed = 4.0f; //standard levitate move speed defined in LevitateMotor
        }


        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Thaumaturgy;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportDuration = true;
            properties.DurationCosts = MakeEffectCosts(16, 80);
            properties.SupportMagnitude = true;
            properties.MagnitudeCosts = MakeEffectCosts(6, 30);
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null || caster.EntityType != EntityTypes.Player)
            {
                RoundsRemaining = 0;
                ResignAsIncumbent();
                return;
            }

            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                Utility.AddHUDText(Text.MustBeOutside.Get());
                RoundsRemaining = 0;
                ResignAsIncumbent();
                RefundSpellCost();
                return;
            }

            StartFlying();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);
            StartFlying();
            resuming = true;
        }


        public override void ConstantEffect()
        {
            base.ConstantEffect();

            resuming = false;

            if (!windAudioObject)
                return;

            GameManager game = GameManager.Instance;
            DaggerfallEntityBehaviour player = game.PlayerEntityBehaviour;
            LevitateMotor levitateMotor = player.GetComponent<LevitateMotor>();
            PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
            CharacterController controller = player.GetComponent<CharacterController>();
            Camera playerCamera = game.MainCamera;


            //Toggle wind audio depending on current location
            DaggerfallAudioSource dfAudio = windAudioObject.GetComponent<DaggerfallAudioSource>();
            dfAudio.AudioSource.mute = game.PlayerEnterExit.IsPlayerInside || game.PlayerEnterExit.IsPlayerSubmerged;

            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                //There is no wind to walk indoors
                levitateMotor.IsLevitating = false;
                velocity = Vector3.zero;
                exitLocationCountdown = 3;
                return;
            }

            if (levitateMotor.IsSwimming)
                return; //this shouldn't happen but will check anyway


            levitateMotor.IsLevitating = true;

            //When transitioning between regional areas, the position information can go crazy.
            //Can also happen intentially on collisions.
            //We'll ignore it when it does.
            float deltaX = (player.transform.position - lastPlayerPosition).magnitude;
            if (deltaX < 2)
                velocity = player.transform.position - lastPlayerPosition;

            lastPlayerPosition = player.transform.position;


            if (exitLocationCountdown > 0)
            {
                --exitLocationCountdown;
                return;
            }


            //Calculate any acceleration
            float inputX = InputManager.Instance.Horizontal;
            float inputY = InputManager.Instance.Vertical;
            float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f && playerMotor.limitDiagonalSpeed) ? .7071f : 1.0f;

            Vector3 moveXY = new Vector3(inputX * inputModifyFactor, 0, inputY * inputModifyFactor);
            Vector3 acceleration = playerCamera.transform.TransformDirection(moveXY);

            InputManager inputManager = InputManager.Instance;
            if (inputManager.HasAction(InputManager.Actions.Jump) || inputManager.HasAction(InputManager.Actions.FloatUp))
                acceleration += Vector3.up;
            else if (inputManager.HasAction(InputManager.Actions.Crouch) || inputManager.HasAction(InputManager.Actions.FloatDown))
                acceleration += Vector3.down;

            //Altitude limiter
            Physics.Raycast(controller.transform.position, Vector3.down, out RaycastHit hitInfo);
            float altitude = hitInfo.distance;
            if (acceleration.y > 1)
                acceleration.y *= 1 - altitude / 200;

            acceleration *= Time.deltaTime;
            acceleration /= 5f;

            //add some spell magnitude based frictional forces
            acceleration -= velocity / (spellMagnitude * 3);

            velocity += acceleration;


            CheckCollision();


            //Perform the move
            controller.Move(velocity);

        }


        public override void End()
        {
            base.End();

            LevitateMotor levitateMotor = GameManager.Instance.PlayerEntityBehaviour.GetComponent<LevitateMotor>();

            levitateMotor.LevitateMoveSpeed = originalLevitateSpeed;

            levitateMotor.IsLevitating = false;

            GameObject.Destroy(windAudioObject);
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            return (other is WindWalk);
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            if (!(incumbent is WindWalk))
                return;

            WindWalk existing = incumbent as WindWalk;

            float summedMagnitudes = RoundsRemaining * GetMagnitude(caster) + existing.RoundsRemaining * existing.spellMagnitude;

            // Combine the 2 spell effects
            existing.RoundsRemaining += RoundsRemaining;

            //calculate new magnitude
            existing.spellMagnitude = summedMagnitudes / existing.RoundsRemaining;
        }


        /// <summary>
        /// Starts the levitation effect, and creates a looping audio source to handle wind noise.
        /// </summary>
        void StartFlying()
        {
            if (!windAudioObject) //check if WindWalk already active
            {
                spellMagnitude = GetMagnitude(caster);

                DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;
                LevitateMotor levitator = player.GetComponent<LevitateMotor>();
                originalLevitateSpeed = levitator.LevitateMoveSpeed;
                levitator.LevitateMoveSpeed = 0; //prevent levitation motor from moving us, we'll control movement

                lastPlayerPosition = player.transform.position;

                //temporarily adding another audio source for looping sound
                windAudioObject = new GameObject();
                DaggerfallAudioSource dfAudio = windAudioObject.AddComponent<DaggerfallAudioSource>();
                dfAudio.AudioSource.volume = DaggerfallUnity.Settings.SoundVolume;
                dfAudio.AudioSource.loop = true;
                dfAudio.AudioSource.clip = Assets.WindNoise.Get<AudioClip>();
                dfAudio.AudioSource.Play();

                player.GetComponent<LevitateMotor>().IsLevitating = true;

                //start with a little vertical acceleration
                velocity = Vector3.up * 0.02f;
                lastPlayerPosition = Vector3.zero;
            }

        }



        /// <summary>
        /// Check if player collided with any building, people, or creatures while wind-walking.
        /// Apply any resulting damage to the affected parties and contact their insurance agents.
        /// </summary>
        void CheckCollision()
        {
            CharacterController controller = Caster.GetComponent<CharacterController>();

            Vector3 top = controller.transform.position;
            top.y += controller.height / 2f;

            Vector3 bottom = controller.transform.position;
            bottom.y -= controller.height / 2f;

            float distance = velocity.magnitude;

            if (!Physics.CapsuleCast(top, bottom, controller.radius, velocity.normalized, out RaycastHit hitInfo, distance))
                return;

            Vector3 normal = hitInfo.normal;

            //non-elastic collision
            Vector3 absorbedVelocity = Vector3.Project(velocity, normal);

            //the remaining velocity
            velocity = Vector3.ProjectOnPlane(velocity, normal);

            lastPlayerPosition = Vector3.zero; //to prevent ConstantEffect() from calculating velocity

            controller.transform.position += normal * 0.05f; //a little bounce

            float absorbedSpeed = absorbedVelocity.magnitude;
            absorbedSpeed /= Time.deltaTime; //converting to units per second
            absorbedSpeed -= 4;

            if (absorbedSpeed > 0)
            {
                int amount = (int)(absorbedSpeed * absorbedSpeed);
                controller.GetComponent<DaggerfallEntityBehaviour>().Entity.DecreaseHealth(amount);
                if (absorbedSpeed > 2)
                    controller.GetComponent<ShowPlayerDamage>().Flash();

                //see if we collided with another creature
                DaggerfallEntityBehaviour creature = hitInfo.collider.GetComponent<DaggerfallEntityBehaviour>();
                if (creature)
                {
                    creature.Entity.DecreaseHealth(amount);
                    creature.HandleAttackFromSource(GameManager.Instance.PlayerEntityBehaviour);
                }
            }

            //Make appropriate collision sound
            SoundClips clip;
            distance = Vector3.Distance(bottom, hitInfo.point);
            bool collisionNearFeet = distance <= controller.radius + 0.1f;
            if (collisionNearFeet)
                clip = absorbedSpeed < 0 ? SoundClips.PlayerFootstepOutside1 : SoundClips.FallHard;
            else
                clip = SoundClips.BodyFall;

            if (absorbedSpeed > 7)
                clip = SoundClips.ArenaHitSound;

            DaggerfallUI.Instance.PlayOneShot(clip);

            return;
        }


        /// <summary>
        /// Refunds to caster the magicka expended on this effect.
        /// </summary>
        void RefundSpellCost()
        {
            FormulaHelper.SpellCost cost = FormulaHelper.CalculateEffectCosts(this, Settings, Caster.Entity);
            Caster.Entity.IncreaseMagicka(cost.spellPointCost);
        }


        TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                GroupName,
                Text.WindWalkEffectDescription.Get(),
                Text.WindWalkSpellMakerDuration.Get(),
                Text.WindWalkSpellMakerMagnitude.Get());
        }

        TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                GroupName,
                Text.WindWalkSpellBookDuration.Get(),
                Text.WindWalkSpellBookMagnitude.Get(),
                "",
                "\"" + Text.WindWalkEffectDescription.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("thaumaturgy") + "]");
        }



    } //class WindWalk



} //namespace