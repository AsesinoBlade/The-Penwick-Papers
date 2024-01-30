// Project:     Dirty Tricks, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Items;
using System.Reflection;
using DaggerfallWorkshop.Game.Utility.ModSupport;


namespace ThePenwickPapers
{

    public static class DirtyTricks
    {
        const float blindRange = 4.5f;
        const float bootRange = 6f; //max range at which player can *begin* a boot attempt
        const float peepRange = 0.6f;

        const int pebblesOfSkulduggeryItemIndex = 545; //value from thiefoverhaul/skulduggery mod
        const int pebblesConditionReduction = 9;

        static float lastPlayerBlindAttempt = -15f;
        static float lastEnemyBlindAttempt = -290f;
        static float lastRefillAttempt;
        static float lastDiversionTime;
        static bool peeping;

        static readonly HashSet<MobileTypes> sneakyTypes = new HashSet<MobileTypes>() {
            MobileTypes.Bard, MobileTypes.Burglar, MobileTypes.Rogue, MobileTypes.Thief
        };


        /// <summary>
        /// Check if the activation event triggers any dirty tricks.  If so, the dirty deed
        /// will be done.
        /// </summary>
        public static bool CheckAttemptTrick(RaycastHit hitInfo)
        {
            if (!hitInfo.collider)
                return false;

            bool isShowingHandAnimation = Utility.IsShowingHandAnimation();

            PlayerActivateModes mode = GameManager.Instance.PlayerActivate.CurrentMode;

            DaggerfallEntityBehaviour creature = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();

            if (creature)
            {
                var em = creature.GetComponent<EnemyMotor>();
                //If activating a creature, make sure it is a non-allied enemy entity.
                //We don't want to do dirty tricks to allies or civilians.
                if (creature.Entity.Team == MobileTeams.PlayerAlly)
                    return false;
                else if (!(creature.Entity is EnemyEntity))
                    return false;

                if (!em.IsHostile || GameManager.Instance.WeaponManager.Sheathed)
                    return false; //Weapon must be readied
            }

            if (mode == PlayerActivateModes.Steal)
            {
                bool isTerrain = (hitInfo.collider.gameObject.layer == 0);

                DaggerfallActionDoor door = hitInfo.collider.GetComponent<DaggerfallActionDoor>();

                if (Settings.EnableBlinding && creature && hitInfo.distance <= blindRange)
                {
                    bool isFacing = IsFacingTrickster(creature, GameManager.Instance.PlayerEntityBehaviour);
                    if (isFacing)
                    {
                        if (!Utility.HasFreeHand())
                            Utility.AddHUDText(Text.NoFreeHand.Get());
                        else if (!isShowingHandAnimation)
                            AttemptBlindByPlayer(creature);

                        return true;
                    }
                }
                else if (Settings.EnableChock && door && door.IsClosed && hitInfo.distance <= PlayerActivate.DoorActivationDistance)
                {
                    if (!door.IsLocked)
                        return ChockDoor(door);
                    else if (door.FailedSkillLevel < 0) //indicates a chocked door
                        return UnchockDoor(door);
                    else    
                        return false;
                }
                else if (Settings.EnableDiversion && isTerrain && hitInfo.distance > 5)
                {
                    if (!Utility.HasFreeHand())
                        Utility.AddHUDText(Text.NoFreeHand.Get());
                    else if (!isShowingHandAnimation)
                        AttemptDiversion(hitInfo);

                    return true;
                }
            }
            else if (Settings.EnableTheBoot && mode == PlayerActivateModes.Grab)
            {
                if (creature && hitInfo.distance <= bootRange && !GameManager.Instance.PlayerMotor.IsCrouching)
                {
                    var em = creature.GetComponent<EnemyMotor>();
                    if (em.IsHostile && !isShowingHandAnimation)
                        AttemptBoot(creature);

                    return true;
                }
            }
            else if (Settings.EnablePeep && mode == PlayerActivateModes.Info)
            {
                DaggerfallActionDoor door = hitInfo.collider.GetComponent<DaggerfallActionDoor>();

                //Determine XZ distance from player to door, how close is player to door
                Vector3 path = hitInfo.point - GameManager.Instance.PlayerController.transform.position;
                float distanceXZ = Vector3.ProjectOnPlane(path, Vector3.up).magnitude;

                if (door && door.IsClosed && distanceXZ <= peepRange)
                {
                    if (!peeping)
                        ThePenwickPapersMod.Instance.StartCoroutine(PeepDoor(hitInfo, door));

                    return true;
                }

            }

            return false;
        }


        /// <summary>
        /// Certain enemy classes in range will periodically attempt to blind the player or others.
        /// </summary>
        public static void CheckEnemyBlindAttempt()
        {
            if (Settings.EnableBlinding == false)
                return;

            //only check for blind attempts once in a while
            if (Dice100.FailedRoll(1))
                return;

            //Don't let enemies attempt blind too often, that can get annoying.
            if (Time.time < lastEnemyBlindAttempt + 300)
                return;

            List<DaggerfallEntityBehaviour> enemyBehaviours = Utility.GetNearbyEntities();

            foreach (DaggerfallEntityBehaviour behaviour in enemyBehaviours)
            {
                EnemySenses senses = behaviour.GetComponent<EnemySenses>();

                if (senses == null || senses.Target == null)
                    continue;
                else if (!senses.TargetInSight)
                    continue;
                else if (senses.SightRadius < blindRange)
                    continue;

                EnemyEntity entity = behaviour.Entity as EnemyEntity;

                if (entity.EntityType != EntityTypes.EnemyClass)
                    continue;

                if (!sneakyTypes.Contains((MobileTypes)entity.MobileEnemy.ID))
                    continue;    //only certain enemy types will attempt blinding
                else if (entity.IsParalyzed)
                    continue;
                else if (behaviour.gameObject.name.StartsWith(IllusoryDecoy.DecoyGameObjectPrefix))
                    continue;
                else if (Vector3.Distance(senses.Target.transform.position, behaviour.transform.position) > blindRange)
                    continue;
                else if (!IsFacingTrickster(senses.Target, behaviour))
                    continue;

                //do raycast to check if anything is in the way (doors, other entities, etc.)
                Vector3 direction = (senses.Target.transform.position - behaviour.transform.position).normalized;
                if (!Physics.Raycast(behaviour.transform.position, direction, out RaycastHit hitInfo, blindRange))
                    continue;

                if (hitInfo.collider.GetComponent<DaggerfallEntityBehaviour>() != senses.Target)
                    continue;

                AttemptBlind(behaviour, senses.Target);

                lastEnemyBlindAttempt = Time.time;

                if (senses.Target == GameManager.Instance.PlayerEntityBehaviour)
                    GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Streetwise, 1);

                break; //if it gets this far, break out of loop
            }


        }

        /// <summary>
        /// Called on update to attempt to refill depleted Pebbles of Skulduggery
        /// </summary>
        public static void RefillPebblesOfSkulduggery()
        {
            if (Settings.EnableDiversion == false)
                return;

            if (Time.time < lastRefillAttempt + 20f)
                return;
            
            lastRefillAttempt = Time.time;

            DaggerfallUnityItem pebbles = Utility.GetEquippedItem(pebblesOfSkulduggeryItemIndex);
            if (pebbles != null && pebbles.currentCondition > 0 && pebbles.currentCondition <= pebbles.maxCondition)
            {
                int luck = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Luck);
                if (Dice100.SuccessRoll(luck))
                {
                    pebbles.currentCondition += pebblesConditionReduction;
                    if (pebbles.currentCondition > pebbles.maxCondition)
                        pebbles.currentCondition = pebbles.maxCondition;
                }
            }

        }


        /// <summary>
        /// Checks if the target (victim) is facing towards the trickster.
        /// </summary>
        static bool IsFacingTrickster(DaggerfallEntityBehaviour victim, DaggerfallEntityBehaviour trickster)
        {
            float angle = Vector3.SignedAngle(victim.transform.forward, trickster.transform.forward, Vector3.up);

            angle = 180 - Mathf.Abs(angle);

            return angle < 45;
        }


        /// <summary>
        /// Player attempts to blind target creature
        /// </summary>
        static void AttemptBlindByPlayer(DaggerfallEntityBehaviour victim)
        {
            DaggerfallEntityBehaviour playerBehaviour = GameManager.Instance.PlayerEntityBehaviour;
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                return;
            else if (playerEntity.IsParalyzed)
                return;
            else if (Utility.IsBlind(playerBehaviour))
                return;

            float rechargeTime = (140 - playerEntity.Stats.LiveLuck) / 6.0f;

            if (Time.time < lastPlayerBlindAttempt + rechargeTime)
                return;

            lastPlayerBlindAttempt = Time.time;

            //always at least some skill increment
            playerEntity.TallySkill(DFCareer.Skills.Pickpocket, 1);

            //show hand wave/swipe animation
            ThePenwickPapersMod.HandWaveAnimator.DoHandWave();

            CommitHostileAct(victim);

            if (AttemptBlind(playerBehaviour, victim))
            {
                //another tally on success
                playerEntity.TallySkill(DFCareer.Skills.Pickpocket, 1);
            }

        }


        static readonly HashSet<MobileTypes> resistant = new HashSet<MobileTypes>() {
            MobileTypes.FireAtronach, MobileTypes.FleshAtronach, MobileTypes.IceAtronach, MobileTypes.IronAtronach,
            MobileTypes.Spider, MobileTypes.GiantScorpion, MobileTypes.Spriggan
        };
        /// <summary>
        /// Performs skill checks to see if a blinding attempt succeeds.
        /// Applies Blind incumbent effect on success.
        /// </summary>
        static bool AttemptBlind(DaggerfallEntityBehaviour trickster, DaggerfallEntityBehaviour victim)
        {
            //attempting blinding maneuver, play sound
            DaggerfallAudioSource dfAudioSource = trickster.GetComponent<DaggerfallAudioSource>();
            dfAudioSource.PlayOneShot(SoundClips.SwingHighPitch, 1, 0.5f);

            //Check chance of success by comparing trickster pickpocket&agility versus victim streetwise&agility
            int pickpocket = trickster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Pickpocket);
            int streetwise = victim.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);

            if (victim != GameManager.Instance.PlayerEntityBehaviour)
            {
                EnemyEntity enemy = victim.Entity as EnemyEntity;
                if (Blind.Immune.Contains((MobileTypes)enemy.MobileEnemy.ID))
                    return false;
                else if (resistant.Contains((MobileTypes)enemy.MobileEnemy.ID) && Dice100.SuccessRoll(50))
                    return false;

                if (!ThePenwickPapersMod.IsMonsterUniversityInstalled || enemy.EntityType != EntityTypes.EnemyClass)
                {
                    //adjusting enemy skills.  In the base game enemy skills go up 5pts per level by default
                    bool isSneaky = sneakyTypes.Contains((MobileTypes)enemy.MobileEnemy.ID);
                    streetwise -= enemy.Level * (isSneaky ? 2 : 3);
                }
            }

            if (trickster != GameManager.Instance.PlayerEntityBehaviour && !ThePenwickPapersMod.IsMonsterUniversityInstalled)
            {
                //adjusting enemy skills.  In the base game enemy skills go up 5pts per level by default
                pickpocket -= trickster.Entity.Level * 2;
            }

            int offense = pickpocket;
            offense += trickster.Entity.Stats.LiveAgility / 2;

            int defense = streetwise;
            defense += victim.Entity.Stats.LiveAgility / 2;

            int chance = Mathf.Clamp(40 + offense - defense, 5, 95);

            if (Dice100.SuccessRoll(chance))
            {
                BlindVictim(trickster, victim);
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Performs animation and knockback, then applies Blind incumbent effect on victim.
        /// </summary>
        static void BlindVictim(DaggerfallEntityBehaviour trickster, DaggerfallEntityBehaviour victim)
        {
            //Do knockback and dirt animation on non-player victims
            if (victim.EntityType != EntityTypes.Player)
            {
                EnemyMotor motor = victim.GetComponent<EnemyMotor>();
                EnemyEntity enemy = victim.Entity as EnemyEntity;

                bool knockable = victim.EntityType == EntityTypes.EnemyClass || enemy.MobileEnemy.Weight > 0;
                if (knockable && motor.KnockbackSpeed < 5)
                {
                    motor.KnockbackSpeed = 15 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10);
                    motor.KnockbackDirection = -victim.transform.forward;
                }

                //show graphic dirt-throw
                Vector3 pos = victim.transform.position;
                MobileUnit unit = victim.GetComponentInChildren<MobileUnit>();
                float eyeHeight = pos.y + unit.GetSize().y / 2.0f - 0.3f;
                Vector3 impactPosition = new Vector3(pos.x, eyeHeight, pos.z);
                impactPosition += victim.transform.forward * 0.2f;

                EnemyBlood blood = victim.GetComponent<EnemyBlood>();
                if (blood)
                    blood.ShowBloodSplash(2, impactPosition);
            }

            //Finally, induce an incumbent blinding effect
            EntityEffectManager manager = victim.GetComponent<EntityEffectManager>();

            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();
            settings.DurationBase = 2; //should be 5-10 seconds
            settings.DurationPlus = 0;
            settings.DurationPerLevel = 1;

            EntityEffectBundle bundle = CreateBundle(Blind.EffectKey, trickster, ElementTypes.None, settings);
            AssignBundleFlags flags = AssignBundleFlags.BypassChance | AssignBundleFlags.BypassSavingThrows;
            manager.AssignBundle(bundle, flags);
        }


        /// <summary>
        /// Creates a non-spell bundle.
        /// </summary>
        static EntityEffectBundle CreateBundle(string effectKey, DaggerfallEntityBehaviour trickster, ElementTypes element, EffectSettings? effectSettings = null)
        {
            EffectBundleSettings settings = new EffectBundleSettings()
            {
                Version = EntityEffectBroker.CurrentSpellVersion,
                BundleType = BundleTypes.None,
                ElementType = element,
                Effects = new EffectEntry[] { new EffectEntry(effectKey, effectSettings.Value) },
                Name = effectKey,
            };

            EntityEffectBundle bundle = new EntityEffectBundle(settings);

            bundle.CasterEntityBehaviour = trickster;

            return bundle;
        }


        /// <summary>
        /// Effectively 'locks' a closed, unlocked door with a makeshift chock.
        /// Must be an inward opening door.
        /// </summary>
        static bool ChockDoor(DaggerfallActionDoor door)
        {
            if (!GameManager.Instance.PlayerMotor.IsCrouching)
                return false; //Must be crouching to chock doors

            Vector3 playerForward = GameManager.Instance.PlayerObject.transform.forward;
            if (Vector3.Angle(playerForward, door.transform.forward) <= 90)
                return false; //doors opening outward can't be chocked

            int lockpicking = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);
            if (lockpicking < 20)
            {
                string skillName = DaggerfallUnity.Instance.TextProvider.GetSkillName(DFCareer.Skills.Lockpicking);
                Utility.AddHUDText(Text.NotEnoughLockpickingSkill.Get(skillName));
                return true;
            }

            int lockLevel = lockpicking / 5 - 1;
            door.CurrentLockValue = Mathf.Min(lockLevel, 19);
            door.FailedSkillLevel = -1; //flag to signify a chocked door

            DaggerfallAudioSource dfAudioSource = door.GetComponent<DaggerfallAudioSource>();
            if (dfAudioSource != null)
                dfAudioSource.PlayOneShot(door.PickedLockSound);

            Utility.AddHUDText(Text.DoorChocked.Get());

            return true;
        }


        /// <summary>
        /// Unchocks a chocked door.
        /// Must be an inward opening door.
        /// </summary>
        static bool UnchockDoor(DaggerfallActionDoor door)
        {
            if (!GameManager.Instance.PlayerMotor.IsCrouching)
                return false; //Must be crouching to unchock doors

            Vector3 playerForward = GameManager.Instance.PlayerObject.transform.forward;
            if (Vector3.Angle(playerForward, door.transform.forward) <= 90)
                return false; //doors opening outward can't be unchocked

            door.StartingLockValue = 0;
            door.FailedSkillLevel = 0;

            DaggerfallAudioSource dfAudioSource = door.GetComponent<DaggerfallAudioSource>();
            if (dfAudioSource != null)
                dfAudioSource.PlayOneShot(door.PickedLockSound);

            Utility.AddHUDText(Text.DoorUnchocked.Get());

            return true;
        }


        /// <summary>
        /// Tosses a pebble to distract unaware/unengaged opponents.
        /// </summary>
        static void AttemptDiversion(RaycastHit hitInfo)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                return;

            float diversionRecoveryTime = 20f - (GameManager.Instance.PlayerEntity.Stats.LiveLuck / 8);

            DaggerfallUnityItem pebbles = Utility.GetEquippedItem(pebblesOfSkulduggeryItemIndex);
            if (pebbles == null && (lastDiversionTime > Time.time - diversionRecoveryTime))
                return;

            //Try to prevent accidental diversion when really attempting blind
            Transform cameraTransform = GameManager.Instance.MainCamera.transform;
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            LayerMask layerMask = LayerMask.GetMask("Enemies");
            if (Physics.SphereCast(ray, 0.25f, out RaycastHit checkHit, blindRange, layerMask))
            {
                //If an unblinded enemy is in front of us, abort diversion
                DaggerfallEntityBehaviour behaviour = checkHit.collider.GetComponent<DaggerfallEntityBehaviour>();
                if (behaviour && !Utility.IsBlind(behaviour))
                    return;
            }

            if (pebbles != null)
            {
                if (pebbles.currentCondition - pebblesConditionReduction < 1)
                {
                    Utility.AddHUDText(Text.OutOfPebbles.Get());
                    return;
                }

                pebbles.LowerCondition(pebblesConditionReduction);
            }
            else
            {
                lastDiversionTime = Time.time;
            }

            Vector3 location = hitInfo.point + (hitInfo.normal * 0.1f);

            //show hand wave/swipe animation
            ThePenwickPapersMod.HandWaveAnimator.DoHandWave();

            DaggerfallAudioSource audio = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();
            audio.PlayOneShot(SoundClips.SwingHighPitch, 1, 0.3f);

            DaggerfallEntityBehaviour diversion = Utility.CreateTarget(location);

            IEnumerator coroutine = DoDiversion(hitInfo.distance, diversion);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);

            return;
        }


        /// <summary>
        /// A coroutine that handles the diversion mechanism on opponents in range.
        /// </summary>
        static IEnumerator DoDiversion(float distance, DaggerfallEntityBehaviour diversion)
        {
            yield return new WaitForSeconds(distance * 0.1f);

            Vector3 location = diversion.transform.position;

            DaggerfallAudioSource dfAudioSource = DaggerfallUI.Instance.GetComponent<DaggerfallAudioSource>();
            dfAudioSource.PlayClipAtPoint(SoundClips.DiceRoll2, location, 1.0f);

            List<DaggerfallEntityBehaviour> enemyBehaviours = Utility.GetNearbyEntities(location, 14);

            foreach (DaggerfallEntityBehaviour behaviour in enemyBehaviours)
            {
                if (!(behaviour.Entity is EnemyEntity))
                    continue;

                EnemyEntity entity = behaviour.Entity as EnemyEntity;
                EnemySenses senses = behaviour.GetComponent<EnemySenses>();

                if (senses == null)
                    continue;
                else if (senses.Target != null && senses.TargetInSight)
                    continue;    //if target is engaged with visible target, ignore diversions
                else if (behaviour.gameObject.name.StartsWith(IllusoryDecoy.DecoyGameObjectPrefix))
                    continue;
                else if (entity.Team == MobileTeams.PlayerAlly)
                    continue;
                else if ((location - behaviour.transform.position).magnitude > senses.HearingRadius)
                    continue; //the diversion is out of hearing range

                EnemyMotor motor = behaviour.GetComponent<EnemyMotor>();
                motor.MakeEnemyHostileToAttacker(diversion);
            }

            yield return new WaitForSeconds(4);

            //finally, destroy the temporary diversion object
            if (diversion)
                GameObject.Destroy(diversion.gameObject);
        }


        /// <summary>
        /// Attempts to apply a boot to the face.
        /// </summary>
        static void AttemptBoot(DaggerfallEntityBehaviour victim)
        {
            if (ThePenwickPapersMod.TheBootAnimator.enabled)
                return; //already booting

            DaggerfallUI.Instance.PlayOneShot(SoundClips.SwingMediumPitch);

            ThePenwickPapersMod.TheBootAnimator.enabled = true;
            ThePenwickPapersMod.TheBootAnimator.OnAttackDirection(WeaponManager.MouseDirections.Up);

            IEnumerator coroutine = BootCoroutine(victim);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Coroutine that handles TheBoot animation and triggers a knockback attempt.
        /// </summary>
        static IEnumerator BootCoroutine(DaggerfallEntityBehaviour victim)
        {
            FPSWeapon TheBoot = ThePenwickPapersMod.TheBootAnimator;

            int hitFrame = TheBoot.GetHitFrame();

            while (TheBoot.IsAttacking() && TheBoot.GetCurrentFrame() < hitFrame)
            {
                SetBootTint(TheBoot);
                yield return null;
            }

            CheckKnockAttempt(victim);

            while (TheBoot.IsAttacking() && TheBoot.GetCurrentFrame() < 4)
            {
                SetBootTint(TheBoot);
                yield return null;
            }

            TheBoot.ChangeWeaponState(WeaponStates.Idle);
            TheBoot.enabled = false;
        }


        static void SetBootTint(FPSWeapon TheBoot)
        {
            if (!ThePenwickPapersMod.Instance.UsingFirstPersonLighting())
                return;

            //DFU 1.0.0 and prior doesn't have a Tint property on FPSWeapon, so skip.
            PropertyInfo property = TheBoot.GetType().GetProperty("Tint");
            if (property == null)
                return;

            Color tint = ThePenwickPapersMod.Instance.GetPlayerTint();
            property.SetValue(TheBoot, tint);
        }


        /// <summary>
        /// Handles contested attempt to knockback the victim; performs knockback on success.
        /// </summary>
        static void CheckKnockAttempt(DaggerfallEntityBehaviour victim)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
            CharacterController playerController = GameManager.Instance.PlayerController;

            float fatigueLossMultiplier = 1.0f;
            if (player.Career.Athleticism)
                fatigueLossMultiplier = (player.ImprovedAthleticism) ? 0.6f : 0.8f;

            player.DecreaseFatigue((int)(22 * fatigueLossMultiplier));

            if (!(victim.Entity is EnemyEntity))
                return;

            EnemyEntity enemy = victim.Entity as EnemyEntity;

            CommitHostileAct(victim);

            //Check if victim is at the contact point
            if (!Physics.Raycast(playerMotor.transform.position, playerMotor.transform.forward, out RaycastHit hit, 2.3f))
                return;

            if (!hit.collider || hit.collider.GetComponent<DaggerfallEntityBehaviour>() != victim)
                return;

            int offense = player.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike);
            offense += player.Stats.GetLiveStatValue(DFCareer.Stats.Agility) / 2;

            int defense = enemy.Skills.GetLiveSkillValue(DFCareer.Skills.Dodging);

            if (!ThePenwickPapersMod.IsMonsterUniversityInstalled || enemy.EntityType != EntityTypes.EnemyClass)
            {
                //Enemies have skill of Base + level * 5 skills, which is a bit high.  Adjusting...
                defense -= (int)((float)enemy.Level * 2.5f);
            }

            defense += enemy.Stats.GetLiveStatValue(DFCareer.Stats.Agility) / 2;

            if (victim.GetComponent<DaggerfallEnemy>().MobileUnit.IsAttacking())
                defense -= 20;

            if (!IsFacingTrickster(victim, GameManager.Instance.PlayerEntityBehaviour))
                defense -= 30;

            if (Utility.IsBlind(victim))
                defense -= 20;

            if (victim.Entity.IsParalyzed)
                defense -= 40;

            int calculatedHitChance = 50 + offense - defense;

            int hitChance = Mathf.Clamp(calculatedHitChance, 10, 90);

            if (Dice100.FailedRoll(hitChance))
                return;

            if (Settings.KickBackCausesDamage)
                GameManager.Instance.WeaponManager.WeaponDamage(null, false, false, hit.transform, hit.point, GameManager.Instance.MainCamera.transform.forward);


            Vector3 playerVelocity = playerController.velocity;
            Vector3 direction = (victim.transform.position - playerMotor.transform.position).normalized;
            float power = Vector3.Project(playerVelocity, direction).magnitude;

            float playerStrength = player.Stats.GetLiveStatValue(DFCareer.Stats.Strength);
            power += playerStrength / 10;

            if (calculatedHitChance > 100)
                power *= 1.3f;

            int creatureWeight = enemy.GetWeightInClassicUnits();
            if (creatureWeight == 0)
                return; //ghosts etc.

            power *= 700.0f / creatureWeight;

            power = Mathf.Clamp(power, 0, 30);

            if (playerMotor.IsSwimming)
                power /= 3;

            DaggerfallAudioSource dfAudio = victim.GetComponent<DaggerfallAudioSource>();
            dfAudio.PlayOneShot(SoundClips.Hit2);

            EnemyMotor motor = victim.GetComponent<EnemyMotor>();
            motor.KnockbackSpeed += power;
            motor.KnockbackDirection = direction;

            //critical strike has a large advancement multiplier (8)
            GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.CriticalStrike, 3);

            // Break "normal power" concealment effects on source
            if (player.IsMagicallyConcealedNormalPower)
                EntityEffectManager.BreakNormalPowerConcealmentEffects(GameManager.Instance.PlayerEntityBehaviour);

        }


        /// <summary>
        /// Peeps through door hole, or under door if crouching.
        /// Stops peeping when a key is pressed.
        /// </summary>
        static IEnumerator PeepDoor(RaycastHit hitInfo, DaggerfallActionDoor actionDoor)
        {
            peeping = true;

            ModManager.Instance.SendModMessage("ToolTips", "peeping", true);
            DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = false;

            Collider door = actionDoor.GetComponent<BoxCollider>();

            bool isCrouching = GameManager.Instance.PlayerMotor.IsCrouching;

            Vector3 pos;
            if (isCrouching)
            {
                //Looking under door, it's possible the raycast might not find the ground correctly in a few cases
                if (!Physics.Raycast(door.bounds.center, Vector3.down, out RaycastHit floorHit))
                    yield break;

                if (floorHit.distance > 5)
                    yield break;

                pos = floorHit.point;
                pos.y += 0.1f;
            }
            else
            {
                pos = door.bounds.center;

                //Peephole can be located at various positions on different doors.
                //Using a consistently repeatable algorithm based on door position.
                float hOffset = (Mathf.Abs(door.bounds.center.x % 10) - 5) / 15f; //-0.33 to 0.33
                float vOffset = Mathf.Abs(door.bounds.center.z % 10) / 12f - 0.15f; //-0.15 to 0.68
                pos.y = GameManager.Instance.PlayerMotor.transform.position.y;
                pos.y += vOffset; //waist-height plus offset
                bool northSouthDoor = Vector3.Angle(hitInfo.normal, Vector3.forward) != 90;
                if (northSouthDoor)
                    pos.x += hOffset;
                else
                    pos.z += hOffset;
            }

            GameObject peeper = new GameObject("Penwick Peeper");

            //camera will be placed inside of door, which should allow player to see through the door
            peeper.transform.position = pos;

            Camera mainCamera = GameManager.Instance.MainCamera;

            Camera camera = peeper.AddComponent<Camera>();

            camera.transform.LookAt(pos - hitInfo.normal);
            camera.depth = mainCamera.depth + 1;
            camera.allowHDR = mainCamera.allowHDR;
            camera.allowMSAA = mainCamera.allowMSAA;
            camera.targetTexture = mainCamera.targetTexture; //for retro mode rendering

            if (isCrouching)
                camera.transform.Rotate(Vector3.forward, -90);

            Texture2D peepTexture;
            if (isCrouching)
                peepTexture = Assets.PeepSlit.Get<Texture2D>();
            else
                peepTexture = Assets.PeepHole.Get<Texture2D>();

            DaggerfallHUD hud = DaggerfallUI.Instance.DaggerfallHUD;
            Texture2D originalBackgroundTexture = hud.ParentPanel.BackgroundTexture;
            BackgroundLayout originalBackgroundLayout = hud.ParentPanel.BackgroundTextureLayout;
            hud.ParentPanel.BackgroundTexture = peepTexture;
            hud.ParentPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

            yield return new WaitForSeconds(0.3f); //time to process initial mouse click

            //Cancel peek if the door is opened or a key is pressed
            InputManager input = InputManager.Instance;
            while (actionDoor.IsClosed && (!input.AnyKeyDown || input.HasAction(InputManager.Actions.PrintScreen)))
            {
                //Allow print screen action to be processed, swallow other actions
                if (!input.HasAction(InputManager.Actions.PrintScreen))
                {
                    InputManager.Instance.ClearAllActions();
                }
                yield return null;
            }

            GameObject.Destroy(peeper);

            hud.ParentPanel.BackgroundTexture = originalBackgroundTexture;
            hud.ParentPanel.BackgroundTextureLayout = originalBackgroundLayout;
            ModManager.Instance.SendModMessage("ToolTips", "peeping", false);
            DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = DaggerfallUnity.Settings.Crosshair;


            peeping = false;
        }


        /// <summary>
        /// Makes the victim hostile to the player, if not already.
        /// Any neutral nearby entities on the same team as the victim will also become hostile.
        /// </summary>
        static void CommitHostileAct(DaggerfallEntityBehaviour victim)
        {
            EnemyMotor victimMotor = victim.GetComponent<EnemyMotor>();
            if (victimMotor == null)
                return;

            if (victimMotor.IsHostile == false)
            {
                foreach (DaggerfallEntityBehaviour creature in Utility.GetNearbyEntities(25))
                {
                    if (creature.Entity.Team != MobileTeams.PlayerAlly && creature.Entity.Team == victim.Entity.Team)
                    {
                        EnemyMotor motor = creature.GetComponent<EnemyMotor>();
                        motor.IsHostile = true;
                    }
                }

                victimMotor.MakeEnemyHostileToAttacker(GameManager.Instance.PlayerEntityBehaviour);
            }
        }




    } //class DirtyTricks


} //namespace