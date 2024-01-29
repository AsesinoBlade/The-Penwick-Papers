// Project:     Illusory Decoy, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: June 2021

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Items;

namespace ThePenwickPapers
{
    public class IllusoryDecoy : BaseEntityEffect
    {
        public const string effectKey = "IllusoryDecoy";

        public static string DecoyGameObjectPrefix = "Penwick Decoy";

        DaggerfallEntityBehaviour decoy;
        DaggerfallEntityBehaviour decoyTarget;
        bool atTargetDestination = false;
        bool firstRound = true;
        int magnitude;
        float timeOfLastAgitate;



        public override string GroupName => Text.IllusoryDecoyGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();


        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Illusion;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportDuration = true;
            properties.DurationCosts = MakeEffectCosts(15, 75, 90);
            properties.SupportMagnitude = true;
            properties.MagnitudeCosts = MakeEffectCosts(6, 30);
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null)
            {
                return;
            }

            //Readying other spells disrupts this illusion
            //Remember to unsubscribe
            manager.OnNewReadySpell += OnNewReadySpellEventHandler;

            magnitude = Mathf.Clamp(GetMagnitude(caster), 1, 100);

            bool success = false;

            try
            {
                if (TryGetSpawnLocation(out Vector3 location))
                {
                    Vector3 destination = GetDecoyFinalDestination();
                    Summon(location, destination);
                    timeOfLastAgitate = Time.time - UnityEngine.Random.Range(-0.4f, 0.4f);
                    success = true;
                }
            }
            catch (Exception e)
            {
                Utility.AddHUDText(Text.DisturbanceInFabricOfReality.Get());
                Debug.LogException(e);
            }


            if (!success)
            {
                RefundSpellCost();
                End();
            }
        }


        public override void MagicRound()
        {
            base.MagicRound();

            //have to make a language check every round to maintain illusion
            if (decoy && !firstRound)
            {
                EnemyEntity decoyEntity = decoy.Entity as EnemyEntity;
                DFCareer.Skills skill = decoyEntity.GetLanguageSkill();

                int maintainChance = caster.Entity.Skills.GetLiveSkillValue(skill);

                maintainChance += caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) / 3;
                maintainChance += caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Personality) / 3;

                if (!Dice100.SuccessRoll(maintainChance))
                {
                    string msg = Text.LackSkillToMaintain.Get(skill);
                    Utility.AddHUDText(msg);
                    End();
                }
            }

            firstRound = false;
        }


        public override void ConstantEffect()
        {
            base.ConstantEffect();

            if (decoy)
            {
                ReapplyDecoyConstantEffects();
                VerifyDecoyTarget();
                AgitateNearbyFoes();
                CheckCasterProximity();
                CheckForMissiles();
            }
            else
            {
                End();
            }
        }


        public override void End()
        {
            RoundsRemaining = -1; //in case this method was manually called

            base.End();

            if (decoy)
            {
                //destroy decoy, show sparkles
                EnemyBlood blood = decoy.GetComponent<EnemyBlood>();
                if (blood)
                {
                    blood.ShowMagicSparkles(decoy.transform.position);
                }

                GameObject.Destroy(decoy.gameObject);
            }

            if (decoyTarget)
            {
                GameObject.Destroy(decoyTarget.gameObject);
            }

            //remember to unsubscribe events to prevent resource leaks
            manager.OnNewReadySpell -= OnNewReadySpellEventHandler;

        }



        /// <summary>
        /// Refunds to caster the magicka expended on this effect.
        /// </summary>
        void RefundSpellCost()
        {
            FormulaHelper.SpellCost cost = FormulaHelper.CalculateEffectCosts(this, Settings, Caster.Entity);
            Caster.Entity.IncreaseMagicka(cost.spellPointCost);
        }


        /// <summary>
        /// Reapplies changes to decoy entity that are cleared by DaggerfallEntity.ClearConstantEffects().
        /// </summary>
        void ReapplyDecoyConstantEffects()
        {
            EnemyEntity entity = decoy.Entity as EnemyEntity;

            entity.IsImmuneToDisease = true;
            entity.IsImmuneToParalysis = true;
            entity.IsParalyzed = false;

            //try to prevent the decoy from actually hitting anything
            entity.ChangeChanceToHitModifier(-200);
        }


        /// <summary>
        /// Makes sure the decoy isn't distracted until it reaches its destination.
        /// </summary>
        void VerifyDecoyTarget()
        {
            EnemySenses senses = decoy.GetComponent<EnemySenses>();

            EnemyMotor motor = decoy.GetComponent<EnemyMotor>();
            if (!atTargetDestination)
            {
                if (senses.Target != decoyTarget)
                {
                    //we don't want our decoy to get distracted before reaching its destination
                    senses.Target = null;       //because of logic in MakeEnemyHostileToAttacker()
                    motor.MakeEnemyHostileToAttacker(decoyTarget);
                }
            }

            if (Caster.EntityType == EntityTypes.Player)
            {
                if (decoy.Entity.Team != MobileTeams.PlayerAlly)
                {
                    //prevent illusory decoy from changing allegiance
                    decoy.Entity.Team = MobileTeams.PlayerAlly;
                    senses.Target = null;
                }
            }
            else if (decoy.Entity.Team != Caster.Entity.Team)
            {
                decoy.Entity.Team = Caster.Entity.Team;
                senses.Target = null;
            }

        }


        /// <summary>
        /// Periodically attempts to agitate nearby foes, goading them to attack the decoy
        /// </summary>
        void AgitateNearbyFoes()
        {
            //only attempting agitation every few seconds or so
            if (Time.time - timeOfLastAgitate < 1.5f)
            {
                return;
            }

            timeOfLastAgitate = Time.time;

            int personality = Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Personality);

            if (Dice100.FailedRoll(personality / 2))
                return;

            float agitateRange = personality / 7.0f + 2.0f;

            PlayAttractSound();

            Vector3 location = decoy.transform.position;

            List<DaggerfallEntityBehaviour> entityBehaviours = Utility.GetNearbyEntities(location, agitateRange);

            foreach (DaggerfallEntityBehaviour behaviour in entityBehaviours)
            {
                EnemyMotor motor = behaviour.GetComponent<EnemyMotor>();

                if (motor == null)
                    continue;
                else if (behaviour == decoy || behaviour == Caster)
                    continue;
                else if (Caster.EntityType == EntityTypes.Player && behaviour.Entity.Team == MobileTeams.PlayerAlly)
                    continue;
                else if (behaviour.Entity.Team == Caster.Entity.Team)
                    continue;
                else if (behaviour.Entity.Team == MobileTeams.Undead)
                    continue;
                else if (motor.IsHostile == false)
                    continue;
                else
                {
                    if (Dice100.SuccessRoll(personality))
                    {
                        //potentially pull enemy away from current local target
                        EnemySenses senses = behaviour.GetComponent<EnemySenses>();
                        if (senses.Target == Caster)
                            senses.Target = null;
                    }
                    motor.MakeEnemyHostileToAttacker(decoy);
                }
            }
        }


        /// <summary>
        /// Decoy emits an appropriate attracting sound (bark, laugh, etc.)
        /// </summary>
        void PlayAttractSound()
        {
            if (!decoy.isActiveAndEnabled)
                return;

            DaggerfallAudioSource dfAudio = decoy.GetComponent<DaggerfallAudioSource>();
            dfAudio.AudioSource.spatialize = true;
            dfAudio.AudioSource.spatialBlend = 1.0f;

            EnemySounds sounds = decoy.GetComponent<EnemySounds>();

            if (!dfAudio.IsPlaying())
            {
                if (decoy.EntityType == EntityTypes.EnemyClass)
                {
                    Assets asset = decoy.Entity.Gender == Genders.Male ? Assets.MaleOi : Assets.FemaleLaugh;
                    AudioClip clip = asset.Get<AudioClip>();
                    dfAudio.AudioSource.PlayOneShot(clip);
                }
                else
                {
                    dfAudio.PlayOneShot(sounds.BarkSound);
                }
            }
        }


        /// <summary>
        /// Destroys the decoy if the caster gets too close and isn't magically concealed.
        /// </summary>
        void CheckCasterProximity()
        {
            bool casterIsConcealed = Caster.Entity.IsMagicallyConcealed;

            if (!casterIsConcealed)
            {
                //EntityEffectManager clears & reapplies effects each frame.  This might cause
                //the entity to appear to have no concealment effect as viewed from inside another effect.
                if (manager.FindIncumbentEffect<DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects.ConcealmentEffect>() != null)
                {
                    casterIsConcealed = true;
                }
            }

            if (!casterIsConcealed)
            {
                //manually check proximity to caster, dispel the illusion if unconcealed caster gets too close
                float distance = Vector3.Distance(Caster.transform.position, decoy.transform.position);
                if (distance < 1.0f - magnitude / 250f)
                {
                    End();
                }
            }
        }


        /// <summary>
        /// Checks for missiles (arrows or spells) in the area and potentially modifies mechanics
        /// to account for the decoy illusion.  Also checks if a missile is hitting the decoy.
        /// </summary>
        void CheckForMissiles()
        {
            if (decoy == null)
                return;

            int missileLayerMask = LayerMask.GetMask("SpellMissiles");

            //Looking for any close-by missiles...
            Collider[] colliders = Physics.OverlapSphere(decoy.transform.position, 5, missileLayerMask);

            foreach (Collider collider in colliders)
            {
                DaggerfallMissile missile = collider.GetComponent<DaggerfallMissile>();

                if (missile == null) //arrow collider might be a child object, check parent
                    missile = collider.GetComponentInParent<DaggerfallMissile>();
                
                if (missile)
                {
                    //SetIgnoreMissileCollisions will return false if collisions are already being ignored
                    if (SetIgnoreMissileCollisions(missile))
                        Dodge(missile);

                    float distance = Vector3.Distance(missile.transform.position, decoy.transform.position);

                    //If the missile is close enough to the decoy, count it as a hit and destroy the decoy
                    if (distance < 0.40f)
                    {
                        End(); //'kill' the decoy
                    }
                }
            }

        }



        /// <summary>
        /// Prevent missiles from colliding with the decoy.
        /// Returns false if the collisions are already being ignored.
        /// </summary>
        bool SetIgnoreMissileCollisions(DaggerfallMissile missile)
        {
            Collider collider = missile.GetComponent<Collider>();
            CharacterController decoyController = decoy.GetComponent<CharacterController>();

            //If already ignoring collisions with this missile, just skip it
            if (Physics.GetIgnoreCollision(collider, decoyController))
                return false;

            Collider[] colliders = missile.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; ++i)
                Physics.IgnoreCollision(colliders[i], decoyController);

            return true;
        }


        /// <summary>
        /// Try a small magnitude-based dodge to throw the enemy archer's aim off a little.
        /// </summary>
        void Dodge(DaggerfallMissile missile)
        {
            if (missile.Caster == null)
                return;

            EnemySenses senses = missile.Caster.GetComponent<EnemySenses>();
            if (senses == null || senses.Target != decoy)
                return;

            Vector3 casterPos = missile.Caster.transform.position;
            if (Vector3.Distance(casterPos, decoy.transform.position) < 5)
                return;

            //attempt to dodge a little to the side
            Vector3 targetDirection = decoy.transform.position - casterPos;
            Vector3 dodgeDirection = Vector3.Cross(targetDirection, Vector3.up).normalized;
            dodgeDirection *= Dice100.SuccessRoll(50) ? 1 : -1;
            float dodgeDistance = magnitude * 0.008f;

            CharacterController controller = decoy.GetComponent<CharacterController>();
            controller.enabled = false; //temporarily disabling to prevent collisions on move check

            try //try-block just in case something bad happens
            {
                if (CanMoveTo(decoy.transform.position, dodgeDirection, dodgeDistance, out Vector3 newLocation) ||
                    CanMoveTo(decoy.transform.position, -dodgeDirection, dodgeDistance, out newLocation))
                {
                    decoy.transform.position = newLocation;
                }
            }
            finally
            {
                controller.enabled = true;
            }
        }


        /// <summary>
        /// Event handler to End() this illusion effect if another spell is readied.
        /// </summary>
        void OnNewReadySpellEventHandler(EntityEffectBundle spell)
        {
            if (decoy)
                Utility.AddHUDText(Text.LostConcentration.Get());

            End();
        }



        static readonly float[] scanDistancesNormal = { 1.8f, 2.8f };
        static readonly float[] scanDistancesLookingDown = { 3.0f, 4.0f };
        static readonly float[] scanLefRightRots = { 0, 5, -5, 15, -15, 30, -30, 45, -45 };
        static readonly float[] scanDownUpRots = { 25, 0, -25 };

        /// <summary>
        /// Scans the area in front of the caster and tries to find a location that can fit a medium-sized creature.
        /// </summary>
        bool TryGetSpawnLocation(out Vector3 location)
        {
            Transform casterTransform = Caster.transform;
            float[] scanDistances = scanDistancesNormal;
            if (Caster.EntityType == EntityTypes.Player)
            {
                casterTransform = GameManager.Instance.MainCamera.transform;
                if (casterTransform.rotation.eulerAngles.x > 45 && casterTransform.rotation.eulerAngles.x <= 90)
                {
                    //Looking down from eye-height, possibly a shaft. Provide more distance
                    //between player and decoy to reduce probability of accidental collision
                    scanDistances = scanDistancesLookingDown;
                }
            }

            //Try to find reasonable spawn location in front of the caster
            foreach (float distance in scanDistances)
            {
                foreach (float leftRightRot in scanLefRightRots)
                {
                    foreach (float downUpRot in scanDownUpRots)
                    {
                        Quaternion rotation = Quaternion.Euler(downUpRot, leftRightRot, 0);
                        Vector3 direction = (casterTransform.rotation * rotation) * Vector3.forward;

                        if (CanMoveTo(casterTransform.position, direction, distance, out location))
                        {
                            return true;
                        }
                    }
                }
            }

            location = Vector3.zero;
            return false;
        }


        /// <summary>
        /// Checks for visibility of destination and if there is space available to hold a medium-sized creature.
        /// Note that this method will probably not work for small creatures like rats.
        /// </summary>
        bool CanMoveTo(Vector3 currentPosition, Vector3 direction, float distance, out Vector3 newLocation)
        {
            newLocation = Vector3.zero;

            //shouldn't be anything in the way
            Ray ray = new Ray(currentPosition, direction);

            if (Physics.Raycast(ray, distance))
            {
                return false;
            }

            newLocation = currentPosition + (direction * distance);

            //use a reasonably sized capsule to check if enough space is available
            Vector3 top = newLocation + Vector3.up * 0.3f;
            Vector3 bottom = newLocation - Vector3.up * 0.3f;
            float radius = 0.4f; //radius*2 included in total height

            return !Physics.CheckCapsule(top, bottom, radius);
        }


        /// <summary>
        /// Determines the final destination of the decoy based on the direction the caster is looking.
        /// </summary>
        Vector3 GetDecoyFinalDestination()
        {
            Vector3 position = Caster.transform.position;
            Vector3 forward = Caster.transform.forward;
            if (Caster.EntityType == EntityTypes.Player)
            {
                position = GameManager.Instance.MainCamera.transform.position;
                forward = GameManager.Instance.MainCamera.transform.forward;
            }

            const float maxDistance = 50f;

            if (Physics.Raycast(position, forward, out RaycastHit hit, maxDistance))
            {
                return hit.point - forward * 0.02f;
            }
            else
            {
                return position + forward * maxDistance;
            }
        }


        /// <summary>
        /// Creates a decoy and starts it moving toward a destination.
        /// </summary>
        void Summon(Vector3 location, Vector3 destination)
        {
            MobileTypes decoyType = IllusoryDecoyCatalog.GetDecoyType(Caster, location, destination);

            CreateDecoy(decoyType, location);

            decoyTarget = Utility.CreateTarget(destination);
            decoyTarget.gameObject.name = "George, the decoy target bat";

            IEnumerator coroutine = RunDecoy();
            manager.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Instantiates a decoy unit and modifies its attributes.
        /// </summary>
        void CreateDecoy(MobileTypes decoyType, Vector3 location)
        {
            string displayName = string.Format(DecoyGameObjectPrefix + " [{0}]", decoyType.ToString());
            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject,
                displayName, parent, location);

            go.SetActive(false);

            MobileGender gender = Dice100.SuccessRoll(50) ? MobileGender.Male : MobileGender.Female;
            int dibella = GameManager.Instance.PlayerEntity.FactionData.GetReputation((int)Temple.Divines.Dibella);
            if (dibella > 30)
            {
                //for the hedonists
                gender = (GameManager.Instance.PlayerEntity.Gender == Genders.Male) ?
                    MobileGender.Female : MobileGender.Male;
            }

            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();
            setupEnemy.ApplyEnemySettings(decoyType, MobileReactions.Hostile, gender, 0, false);

            decoy = go.GetComponent<DaggerfallEntityBehaviour>();

            AdjustDecoy();
        }


        /// <summary>
        /// The decoy is significantly different from standard enemy units and requires several adjustments.
        /// </summary>
        void AdjustDecoy()
        {
            MobileUnit mobileUnit = decoy.GetComponentInChildren<MobileUnit>();

            MobileEnemy mobileEnemy = mobileUnit.Enemy; //struct copy
            mobileEnemy.MinDamage = 0;
            mobileEnemy.MaxDamage = 0;
            mobileEnemy.MinHealth = 1;
            mobileEnemy.MaxHealth = 1;
            mobileEnemy.HasRangedAttack1 = false;
            mobileEnemy.HasRangedAttack2 = false;
            mobileEnemy.PrefersRanged = false;
            mobileEnemy.ParrySounds = false;  //bah, parry...
            mobileEnemy.MinMetalToHit = WeaponMaterialTypes.None;
            mobileEnemy.CanOpenDoors = false; //unless metaphorically...
            mobileEnemy.CastsMagic = false;
            mobileEnemy.BloodIndex = 3; //sparkles!
            mobileEnemy.Weight = 0;  //that "South Betony Diet" works wonders
            mobileEnemy.Team = Caster.EntityType == EntityTypes.Player ? MobileTeams.PlayerAlly : Caster.Entity.Team;

            //set new MobileEnemy to the MobileUnit
            mobileUnit.SetEnemy(DaggerfallUnity.Instance, mobileEnemy, MobileReactions.Hostile, 0);

            EnemyEntity entity = decoy.Entity as EnemyEntity;

            //Since we made changes to MobileEnemy, we have to reset the enemy career
            entity.SetEnemyCareer(mobileEnemy, decoy.EntityType);

            //not sure why there are separate gender settings in related classes
            entity.Gender = (mobileEnemy.Gender == MobileGender.Female) ? Genders.Female : Genders.Male;

            //try to prevent 'spell resisted' type messages from appearing
            entity.Resistances.SetPermanentResistanceValue(DFCareer.Elements.Fire, 0);
            entity.Resistances.SetPermanentResistanceValue(DFCareer.Elements.Frost, 0);
            entity.Resistances.SetPermanentResistanceValue(DFCareer.Elements.Magic, 0);
            entity.Resistances.SetPermanentResistanceValue(DFCareer.Elements.Shock, 0);
            entity.Career.SpellAbsorption = DFCareer.SpellAbsorptionFlags.None;
            entity.Career.Fire = DFCareer.Tolerance.CriticalWeakness;
            entity.Career.Frost = DFCareer.Tolerance.CriticalWeakness;
            entity.Career.Magic = DFCareer.Tolerance.CriticalWeakness;
            entity.Career.Poison = DFCareer.Tolerance.CriticalWeakness;
            entity.Career.Shock = DFCareer.Tolerance.CriticalWeakness;

            entity.Stats.SetPermanentStatValue(DFCareer.Stats.Willpower, 10);
            entity.Stats.SetPermanentStatValue(DFCareer.Stats.Strength, 20);
            entity.Stats.SetPermanentStatValue(DFCareer.Stats.Agility, 100);

            short dodging = (short)Mathf.Clamp(magnitude * 4, 1, 1000);
            if (entity.EntityType == EntityTypes.EnemyMonster)
            {
                //by default, monster types are easier to hit than class types, so adjust for that
                dodging += 160;
            }
            entity.Skills.SetPermanentSkillValue(DFCareer.Skills.Dodging, dodging);
            entity.Skills.SetPermanentSkillValue(DFCareer.Skills.Stealth, 0); //it's a DECOY
            entity.Skills.SetPermanentSkillValue(DFCareer.Skills.HandToHand, 1);
            entity.Skills.SetPermanentSkillValue(DFCareer.Skills.Streetwise, 500); //to avoid dirty tricks
            entity.CurrentMagicka = 0;
            entity.MaxMagicka = 0;
            entity.CurrentHealth = 1;

            //The decoy is a complete Bad-Ass and doesn't need equipment
            entity.ItemEquipTable.Clear();
            entity.Items.Clear();

            //effectively unarmored
            sbyte[] armor = entity.ArmorValues;
            for (int i = 0; i < armor.Length; ++i)
            {
                armor[i] = 60;
            }
            entity.ArmorValues = armor;

            entity.Team = Caster.EntityType == EntityTypes.Player ? MobileTeams.PlayerAlly : Caster.Entity.Team;
            mobileEnemy.Team = entity.Team;

            //If decoy 'dies', the OnDeathHandler will destroy it leaving no body.
            //No need to unsubscribe this event since it is attached to the decoy that 'dies'.
            entity.OnDeath += Decoy_OnDeathHandler;

            //An illusion shouldn't be having incumbent effects.
            //No need to unsubscribe this event either.
            EntityEffectManager effectManager = decoy.GetComponent<EntityEffectManager>();
            effectManager.OnAssignBundle += Decoy_OnAssignBundle;


            if (GameManager.Instance.PlayerEnterExit.IsPlayerInDarkness)
            {
                //Need to make sure our illusory decoy has some light to not see by
                GameObject lightGameObject = new GameObject();
                lightGameObject.transform.parent = decoy.transform;
                lightGameObject.transform.localPosition = new Vector3(0, 1.3f, 0.8f);
                Light light = lightGameObject.AddComponent<Light>();
                light.color = Color.yellow;
                light.shadows = DaggerfallUnity.Settings.DungeonLightShadows ? LightShadows.Soft : LightShadows.None;
            }

            //This should hopefully prevent player melee attacks from hitting their own decoy
            //...we're not selling easy backstabs here
            decoy.gameObject.layer = Caster.gameObject.layer;

            CharacterController controller = decoy.GetComponent<CharacterController>();

            //illusion should not stop caster movement
            Physics.IgnoreCollision(Caster.GetComponent<CharacterController>(), controller);

            float altitude = IllusoryDecoyCatalog.GetAltitude(controller.transform.position);
            if (altitude < 2.4f)
            {
                GameObjectHelper.AlignControllerToGround(controller);
                if (mobileEnemy.Behaviour == MobileBehaviour.Flying)
                {
                    decoy.transform.localPosition += Vector3.up * 1.5f;
                }
            }

            //Decoy should face same direction as caster
            decoy.transform.rotation = Caster.transform.rotation;

        }


        /// <summary>
        /// If decoy 'dies', it needs to be immediately destroyed, leaving no body or death message behind.
        /// </summary>
        void Decoy_OnDeathHandler(DaggerfallEntity entity)
        {
            End();
        }



        /// <summary>
        /// Decoy EntityEffectManager event handler, called when a spell bundle gets assigned to the decoy.
        /// </summary>
        void Decoy_OnAssignBundle(LiveEffectBundle bundleAdded)
        {
            //decoy shouldn't have any live spell effects attached to it
            if (decoy)
            {
                EntityEffectManager effectManager = decoy.GetComponent<EntityEffectManager>();
                effectManager.ClearSpellBundles();
            }
        }


        /// <summary>
        /// Coroutine to activate the decoy and start it towards its destination.
        /// The coroutine ends when its destination is reached; it then behaves as a typical allied unit.
        /// </summary>
        IEnumerator RunDecoy()
        {
            EnemyMotor motor = decoy.GetComponent<EnemyMotor>();
            motor.MakeEnemyHostileToAttacker(decoyTarget);

            float blinkDelay = 0.1f;
            bool blink = true;
            while (decoy && blinkDelay > 0.01f)
            {
                decoy.gameObject.SetActive(blink);
                blink = !blink;
                blinkDelay -= 0.01f;
                yield return new WaitForSeconds(blinkDelay);
            }

            if (decoy)
            {
                decoy.gameObject.SetActive(true);
            }

            Vector3 lastPosition = Vector3.zero;

            //loop-wait for decoy to reach target destination or until stopped by something
            while (decoy && !atTargetDestination)
            {
                float distanceToTarget = Vector3.Distance(decoy.transform.position, decoyTarget.transform.position);
                float lastMoveDistance = Vector3.Distance(decoy.transform.position, lastPosition);
                if (distanceToTarget < 1.5f || lastMoveDistance < 0.05f)
                {
                    atTargetDestination = true;
                }
                lastPosition = decoy.transform.position;

                yield return new WaitForSeconds(0.1f);
            }

            //reached our destination
            if (decoy)
            {
                EnemySenses senses = decoy.GetComponent<EnemySenses>();
                senses.Target = null;
                senses.SecondaryTarget = null;

                //try to keep the decoy from chasing after targets
                senses.SightRadius = 2.0f;
                senses.HearingRadius = 2.0f;
            }

        }


        TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                GroupName,
                Text.IllusoryDecoyEffectDescription1.Get(),
                Text.IllusoryDecoyEffectDescription2.Get(),
                Text.IllusoryDecoyEffectDescription3.Get(),
                Text.IllusoryDecoyDuration.Get(),
                Text.IllusoryDecoySpellMakerChance.Get(),
                Text.IllusoryDecoySpellMakerMagnitude.Get());
        }

        TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                GroupName,
                Text.IllusoryDecoySpellBookDuration.Get(),
                Text.IllusoryDecoySpellBookChance1.Get(),
                Text.IllusoryDecoySpellBookChance2.Get(),
                Text.IllusoryDecoySpellBookChance3.Get(),
                Text.IllusoryDecoySpellBookMagnitude.Get(),
                "",
                "\"" + Text.IllusoryDecoyEffectDescription1.Get(),
                Text.IllusoryDecoyEffectDescription2.Get(),
                Text.IllusoryDecoyEffectDescription3.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("illusion") + "]");
        }



    } //class IllusoryDecoy



} //namespace