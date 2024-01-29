// Project:      Penwick Minion, The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  Feb 2022

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallConnect;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;

namespace ThePenwickPapers
{
    public class PenwickMinion : MonoBehaviour
    {
        const string penwickMinionPrefix = "Penwick Minion";
        const string penwickFollowerPrefix = "Penwick Follower";
        const string penwickRenegadePrefix = "Penwick Renegade";
        const float pushActivateDistance = 2.5f;
        const float followTriggerDistance = 2.5f;

        static List<PenwickMinion> minions = new List<PenwickMinion>();

        DaggerfallEntityBehaviour behaviour;
        EnemySenses senses;
        EnemyMotor motor;
        AudioSource audioSource;
        bool isFollower;
        bool isBeingPushed;
        DaggerfallEntityBehaviour proxyTarget; //invisible target used to control minion movement
        Vector3 lastSeenPlayerPosition;
        float lastEquipTime;
        float lastQuestTargetCheckTime;
        IEnumerator pushRoutine;



        /// <summary>
        /// This is called when the game load event occurs.
        /// Searches current location for any minion creatures and initializes their state.
        /// </summary>
        public static void InitializeOnLoad()
        {
            minions = new List<PenwickMinion>();

            DaggerfallEntityBehaviour[] creatures = Object.FindObjectsOfType<DaggerfallEntityBehaviour>();

            foreach (DaggerfallEntityBehaviour creature in creatures)
            {
                if (creature.name.StartsWith(penwickMinionPrefix))
                {
                    PenwickMinion minion = creature.gameObject.AddComponent<PenwickMinion>();
                    minion.Initialize(false);
                    minions.Add(minion);
                }
                else if (creature.name.StartsWith(penwickFollowerPrefix))
                {
                    PenwickMinion minion = creature.gameObject.AddComponent<PenwickMinion>();
                    minion.Initialize(true);
                    minions.Add(minion);
                }
            }
        }


        /// <summary>
        /// Returns a list of all the PC minions in the current location.
        /// </summary>
        public static List<PenwickMinion> GetMinions()
        {
            int removed = minions.RemoveAll(item => item == null);

            return new List<PenwickMinion>(minions);
        }


        /// <summary>
        /// Applies minion status to the specified ally creature.
        /// </summary>
        public static void AddNewMinion(DaggerfallEntityBehaviour creature)
        {
            if (creature.Entity.Team == MobileTeams.PlayerAlly)
            {
                PenwickMinion minion = creature.gameObject.AddComponent<PenwickMinion>();
                minion.Initialize(CanAddFollower());
                minions.Add(minion);
            }
        }


        /// <summary>
        /// Used after player uses Landmark Journal, or after a long rest
        /// </summary>
        public static void RepositionFollowers()
        {
            IEnumerator coroutine = RepositionCoroutine();
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        /// <summary>
        /// Restore health and magicka of some minions after long rest (7 hours or so).
        /// Currently only lich minions can restore health by resting.
        /// </summary>
        public static void Rest()
        {
            foreach (PenwickMinion minion in GetMinions())
            {
                EnemyEntity entity = minion.behaviour.Entity as EnemyEntity;
                MobileEnemy mobileEnemy = entity.MobileEnemy;

                entity.CurrentMagicka = entity.MaxMagicka;
                entity.CurrentFatigue = entity.MaxFatigue;

                //Only liches can heal on their own, not other undead or atronachs
                if (mobileEnemy.ID == (int)MobileTypes.Lich || mobileEnemy.ID == (int)MobileTypes.AncientLich)
                {
                    entity.CurrentHealth = entity.MaxHealth;
                }
            }
        }


        /// <summary>
        /// Top level method for controlling minion movement.
        /// This is probably called from an Update() method.
        /// </summary>
        public static void GuideMinions()
        {
            CheckForRenegadeMinions();

            foreach (PenwickMinion minion in GetMinions())
            {
                if (MaintainControl(minion))
                {
                    //perform movement and other minion activities
                    minion.Guide();
                }
            }
        }


        /// <summary>
        /// Check to see if any following minions have gone renegade.
        /// This can happen if the player suffers damage to their Willpower attribute.
        /// </summary>
        static void CheckForRenegadeMinions()
        {
            if (GetFollowerCount() > GetMaxFollowers())
            {
                foreach (PenwickMinion minion in GetMinions())
                {
                    if (minion.isFollower && Dice100.SuccessRoll(50))
                    {
                        minion.motor.MakeEnemyHostileToAttacker(GameManager.Instance.PlayerEntityBehaviour);
                    }
                }
            }
        }


        /// <summary>
        /// Checks if any minions have lost ally status.
        /// If so, provide a Willpower based chance to revert them to ally status.
        /// If that fails, remove their minion status and show the renegade! message.
        /// </summary>
        static bool MaintainControl(PenwickMinion minion)
        {
            if (minion.behaviour.Entity.Team != MobileTeams.PlayerAlly)
            {
                EnemyEntity entity = minion.behaviour.Entity as EnemyEntity;

                int willpower = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower);

                int breakingModifier = entity.MaxHealth / 5; //stronger minions break more readily

                if (Dice100.SuccessRoll(willpower - breakingModifier))
                {
                    //Player maintains control, revert team back to ally
                    entity.Team = MobileTeams.PlayerAlly;
                    minion.senses.Target = null;
                    minion.senses.SecondaryTarget = null;
                }
                else
                {
                    //Remove minion status for any minions that have turned on the player
                    minion.SetMinionObjectName();
                    minion.senses.SightRadius = 50f;
                    minion.senses.HearingRadius = 25f;
                    minion.audioSource.volume = DaggerfallUnity.Settings.SoundVolume;
                    GameObject.Destroy(minion.proxyTarget.gameObject);
                    GameObject.Destroy(minion); //remove minion component of GameObject

                    string entityName = TextManager.Instance.GetLocalizedEnemyName(entity.MobileEnemy.ID);
                    Utility.AddHUDText(Text.MinionGoesRenegade.Get(entityName));

                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Determines if player can add another follower minion.  Determined by the Willpower stat.
        /// </summary>
        static bool CanAddFollower()
        {
            return GetFollowerCount() < GetMaxFollowers();
        }


        /// <summary>
        /// Returns the total number of minions that are also following the player.
        /// </summary>
        static int GetFollowerCount()
        {
            return GetMinions().Count(m => m.isFollower);
        }


        /// <summary>
        /// Calculates the maximum number of minions that can follow the player (based on Willpower)
        /// </summary>
        static int GetMaxFollowers()
        {
            int willpower = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower);

            return willpower / 30;
        }


        /// <summary>
        /// Coroutine to reposition following minions near the player.
        /// </summary>
        static IEnumerator RepositionCoroutine()
        {
            Vector3 playerPos = GameManager.Instance.PlayerController.transform.position;

            foreach (PenwickMinion minion in GetMinions())
            {
                if (!minion.isFollower)
                    continue;

                Vector3 followerPos = minion.transform.position;
                if (Vector3.Distance(playerPos, followerPos) < 6)
                    continue; //close enough

                //yield to prevent position overlap
                yield return null;

                //try to find appropriate nearby spot for minion
                for (int i = 0; i < 20; ++i)
                {
                    float x = playerPos.x + Random.Range(-3.0f, 3.0f);
                    float y = playerPos.y + 0.3f;
                    float z = playerPos.z + Random.Range(-3.0f, 3.0f);
                    Vector3 pos = new Vector3(x, y, z);

                    //need a floor beneath
                    if (!Physics.Raycast(pos, Vector3.down, 4.0f))
                        continue;

                    Vector3 top = pos + Vector3.up * 0.4f;
                    Vector3 bottom = pos - Vector3.up * 0.4f;
                    float radius = 0.4f; //radius*2 included in height
                    if (!Physics.CheckCapsule(top, bottom, radius))
                    {
                        if (HasPath(pos, playerPos))
                        {
                            minion.transform.position = pos;
                            break;
                        }
                    }
                }
                //if new position wasn't set, the follower will be stuck at original location
            }
        }


        /// <summary>
        /// Check if there is a clear path from location1 to location2 (exluding doors)
        /// </summary>
        static bool HasPath(Vector3 location1, Vector3 location2)
        {
            float distance = Vector3.Distance(location1, location2);

            Vector3 direction = (location2 - location1).normalized;

            int layerMask = 1; //just looking for terrain hits

            if (Physics.Raycast(location1, direction, out RaycastHit hit, distance, layerMask))
            {
                //if it's a door, we ignore it
                return (hit.collider.GetComponent<DaggerfallActionDoor>() != null);
            }

            return true;
        }


        /// <summary>
        /// Initializes properties specific to minions.
        /// </summary>
        void Initialize(bool following)
        {
            behaviour = GetComponent<DaggerfallEntityBehaviour>();
            senses = GetComponent<EnemySenses>();
            motor = GetComponent<EnemyMotor>();
            audioSource = GetComponent<DaggerfallAudioSource>().AudioSource;

            isFollower = following;

            SetMinionObjectName();

            //create invisible target for movement control
            proxyTarget = Utility.CreateTarget(Vector3.zero);

            senses.SightRadius = 12f;
            senses.HearingRadius = 4.0f;

            lastSeenPlayerPosition = GameManager.Instance.PlayerController.transform.position;

            lastEquipTime = Time.time + 4f;
        }


        /// <summary>
        /// Handles player activation when targeting minions (follow, stay, push).
        /// Depends on current mode (talk, grab, etc)
        /// </summary>
        public bool Activate(float distance)
        {
            if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Talk)
            {
                if (isFollower)
                {
                    //stop following, stay
                    isFollower = false;
                    senses.Target = null;
                    SetMinionObjectName();
                    Utility.AddHUDText(Text.NotFollowing.Get());
                }
                else if (CanAddFollower())
                {
                    //start following
                    isFollower = true;
                    SetMinionObjectName();
                    Utility.AddHUDText(Text.Following.Get());
                }
                else
                {
                    Utility.AddHUDText(Text.NotEnoughWillpower.Get());
                }
                return true;
            }
            else if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Grab)
            {
                if (distance <= pushActivateDistance)
                {
                    if (pushRoutine != null)
                        StopCoroutine(pushRoutine);

                    pushRoutine = Push();

                    //push minion out of the way
                    StartCoroutine(pushRoutine);

                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Gives the minion GameObject an appropriate name property.
        /// The name is used to identify a creature as a minion, and determine its following/staying status.
        /// </summary>
        void SetMinionObjectName()
        {
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            string entityName = TextManager.Instance.GetLocalizedEnemyName(entity.MobileEnemy.ID);

            string nameFormat;

            if (entity.Team == MobileTeams.PlayerAlly)
            {
                string statusName = isFollower ? penwickFollowerPrefix : penwickMinionPrefix;
                nameFormat = statusName + "[{0}]";
            }
            else
            {
                nameFormat = penwickRenegadePrefix + "[{0}]";
            }

            behaviour.gameObject.name = string.Format(nameFormat, entityName);
        }


        /// <summary>
        /// Handles minion movement when following the PC or being pushed.
        /// Manipulates an invisible target that is placed near the desired destination.
        /// The minion will be made hostile to the target.  The actual movement is performed
        /// by the standard DFU movement code.
        /// </summary>
        void Guide()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            if (CanSee(player.transform.position))
            {
                lastSeenPlayerPosition = player.transform.position;
            }


            if (isBeingPushed)
            {
                //move toward the invisible push target
                senses.Target = null;
                motor.MakeEnemyHostileToAttacker(proxyTarget);
            }
            else if (isFollower)
            {
                DoFollow();
            }

            //set sound volume of minion; gets continuously called in case other parts of the game revert it to default
            SetMinionVolume();

            //try to periodically pick up and/or equip better items
            CheckEquipment();

            //periodically check if quest target enemies are nearby, and allow minions to attack them
            MakeQuestTargetsAttackable();
        }



        /// <summary>
        /// Called by Guide(), controls PC follow logic.
        /// </summary>
        void DoFollow()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            float playerDistance = Vector3.Distance(player.transform.position, transform.position);
            float followDistance = followTriggerDistance + Random.Range(-0.3f, 0.7f);

            senses.SecondaryTarget = null;
            senses.WouldBeSpawnedInClassic = false; //to make passive enemies somewhat less aggressive towards minion

            if (senses.Target == null)
            {
                if (playerDistance > followDistance)
                    StartFollowPlayer();
            }
            else if (senses.Target == proxyTarget)
            {
                if (playerDistance <= followDistance)
                    senses.Target = null;
                else if (Random.Range(0, 100) == 1)
                    StartFollowPlayer(); //refresh proxy target periodically
            }
            else
            {
                //is apparently engaged with an enemy
                EnemyMotor targetMotor = senses.Target.GetComponent<EnemyMotor>();
                EnemySenses targetSenses = senses.Target.GetComponent<EnemySenses>();

                if (!senses.TargetInSight && Vector3.Distance(transform.position, senses.Target.transform.position) > 4)
                    senses.Target = null; //ignore unseen enemy targets
                else if (targetMotor && !targetMotor.IsHostile && targetSenses && targetSenses.Target == null)
                    senses.Target = null; //be less aggressive to passive enemies
                else if (playerDistance > 12)
                    StartFollowPlayer(); //flee current combat and follow player instead

                if (senses.Target != null)
                    senses.WouldBeSpawnedInClassic = true; //allow other AI to aggressively target this minion
            }

            //If player too far away, wait for player to look away from minion, then teleport behind them
            if (playerDistance > 25 && Settings.AutoTeleportMinions)
            {
                Vector3 playerDirection = (player.transform.position - transform.position).normalized;
                float signedAngle = Vector3.SignedAngle(playerDirection, player.transform.forward, Vector3.up);
                if (Mathf.Abs(signedAngle) < 60)
                {
                    //falling too far behind, just teleport
                    if (TeleportBehindPlayer())
                    {
                        senses.Target = null;
                    }
                }
            }

            if (senses.Target == proxyTarget)
            {
                //The proxy is completely invisible.
                //This is a hack to enable minion to be aware of it in EnemySenses logic.
                senses.HearingRadius = 100;
                senses.DetectedTarget = true;
            }
            else
            {
                //Otherwise, keep hearing radius lower so minion isn't constantly
                //chasing enemies through walls
                senses.HearingRadius = 4;
            }

        }

        /// <summary>
        /// Called by DoFollow() to have a minion start following the PC.
        /// </summary>
        void StartFollowPlayer()
        {
            proxyTarget.transform.position = lastSeenPlayerPosition;
            Vector3 direction = (lastSeenPlayerPosition - transform.position).normalized;
            proxyTarget.transform.position += direction; //move a bit beyond last seen position, e.g. through door

            senses.Target = null;
            motor.MakeEnemyHostileToAttacker(proxyTarget);
        }



        /// <summary>
        /// Check if minion can see destination (no terrain blockage, except doors)
        /// </summary>
        bool CanSee(Vector3 destination)
        {
            Vector3 eyePosition = transform.position + Vector3.up * 0.7f;

            return HasPath(eyePosition, destination);
        }


        /// <summary>
        /// Teleports following minions behind the player.
        /// </summary>
        bool TeleportBehindPlayer()
        {
            CharacterController player = GameManager.Instance.PlayerController;

            //check for empty space behind player
            Ray ray = new Ray(player.transform.position, -player.transform.forward);
            if (Physics.Raycast(ray, 3f))
            {
                //need open space behind
                return false;
            }

            Vector3 position = player.transform.position - (player.transform.forward * 2);
            position += Vector3.up;

            Collider[] colliders = Physics.OverlapSphere(position, 0.65f);
            if (colliders.Length > 0)
            {
                return false;
            }


            ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, 3))
            {
                transform.SetPositionAndRotation(position, player.transform.rotation);
                GameObjectHelper.AlignControllerToGround(GetComponent<CharacterController>(), 4);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Called when the player activates a minion while in 'Grab' mode.
        /// The invisible proxy target used for guiding the minion is placed nearby.
        /// </summary>
        IEnumerator Push()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            Vector3 pushDirection = (transform.position - player.transform.position).normalized;

            Ray ray = new Ray(transform.position, pushDirection);

            Physics.Raycast(ray, out RaycastHit hit, 4);

            if (hit.collider && (hit.collider is TerrainCollider || hit.collider is MeshCollider))
            {
                //hitting a wall, set target just in front of
                proxyTarget.transform.position = hit.point - (pushDirection * 0.1f);
            }
            else
            {
                proxyTarget.transform.position = transform.position + (pushDirection * 4);
            }

            isBeingPushed = true;

            yield return new WaitForSeconds(1.5f);

            isBeingPushed = false;
        }


        /// <summary>
        /// Called to set or reset the volume of the minion using the MinionVolume mod setting.
        /// </summary>
        void SetMinionVolume()
        {
            if (Settings.MinionVolume == 0)
                audioSource.mute = true; //0 is mute
            else if (Settings.MinionVolume == 1)
                audioSource.volume = DaggerfallUnity.Settings.SoundVolume * 0.2f; //1 is low
            else if (Settings.MinionVolume == 2)
                audioSource.volume = DaggerfallUnity.Settings.SoundVolume * 0.4f; //2 is medium
            else
                audioSource.volume = DaggerfallUnity.Settings.SoundVolume * 1f; //3 is normal
        }


        /// <summary>
        /// Occasionally look for enemy quest targets that are near the minion, and allow them to be attacked
        /// by minions and other AI.
        /// </summary>
        void MakeQuestTargetsAttackable()
        {
            if (Time.time < lastQuestTargetCheckTime + 1f)
                return;

            lastQuestTargetCheckTime = Time.time;

            foreach (DaggerfallEntityBehaviour nearbyBehaviour in Utility.GetNearbyEntities(transform.position, 5))
            {
                EnemyMotor nearbyMotor = nearbyBehaviour.GetComponent<EnemyMotor>();
                EnemySenses nearbySenses = nearbyBehaviour.GetComponent<EnemySenses>();

                if (nearbySenses && nearbySenses.QuestBehaviour && !nearbySenses.QuestBehaviour.IsAttackableByAI)
                    if (nearbyMotor && nearbyMotor.IsHostile)
                        nearbySenses.QuestBehaviour.IsAttackableByAI = true; //allow minions to attack quest targets
            }
        }


        /// <summary>
        /// Called by Guide() to occasionally check and equip items.
        /// </summary>
        void CheckEquipment()
        {
            if (Time.time < lastEquipTime + 2f)
                return;

            lastEquipTime = Time.time;

            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            //see if there is any interesting player-owned loot on the ground
            if (TryGrabLoot())
                return;


            //scan inventory and equip appropriate item if possible
            for (int i = 0; i < entity.Items.Count; ++i)
            {
                DaggerfallUnityItem item = entity.Items.GetItem(i);
                EquipSlots slot = ShouldEquip(item);
                if (slot != EquipSlots.None)
                {
                    entity.ItemEquipTable.UnequipItem(slot);
                    entity.ItemEquipTable.EquipItem(item, false, false);

                    string creatureName = TextManager.Instance.GetLocalizedEnemyName(entity.MobileEnemy.ID);
                    Utility.AddHUDText(Text.MinionEquipsItem.Get(creatureName, item.ItemName));

                    return;
                }
            }

        }


        /// <summary>
        /// Examine nearby player-owned loot to see if there is anything worth taking.
        /// </summary>
        bool TryGrabLoot()
        {
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            List<DaggerfallLoot> nearbyLoot = Utility.GetNearbyLoot(transform.position, 2);

            foreach (DaggerfallLoot loot in nearbyLoot)
            {
                if (!loot.playerOwned)
                    continue;

                for (int i = 0; i < loot.Items.Count; ++i)
                {
                    DaggerfallUnityItem item = loot.Items.GetItem(i);
                    if (ShouldEquip(item) != EquipSlots.None)
                    {
                        entity.Items.Transfer(item, loot.Items);

                        string creatureName = TextManager.Instance.GetLocalizedEnemyName(entity.MobileEnemy.ID);
                        Utility.AddHUDText(Text.MinionTakesItem.Get(creatureName, item.ItemName));

                        // Remove loot container if empty
                        if (loot.Items.Count == 0)
                            GameObjectHelper.RemoveLootContainer(loot);

                        return true;
                    }
                }

            }

            return false;
        }


        /// <summary>
        /// Examines item to see if this minion should try to equip it.
        /// Returns desired equip slot if so.
        /// </summary>
        EquipSlots ShouldEquip(DaggerfallUnityItem item)
        {
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            if (item.IsEquipped || item.currentCondition == 0)
                return EquipSlots.None;

            MobileTypes mobileType = (MobileTypes)entity.MobileEnemy.ID;
            DaggerfallUnityItem equippedItem;

            switch (entity.ItemEquipTable.GetEquipSlot(item))
            {
                case EquipSlots.Amulet0:
                case EquipSlots.Amulet1:
                    equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.Amulet0);
                    if (equippedItem == null || item.value > equippedItem.value)
                        return EquipSlots.Amulet0;
                    equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.Amulet1);
                    if (equippedItem == null || item.value > equippedItem.value)
                        return EquipSlots.Amulet1;
                    break;

                case EquipSlots.Cloak1:
                case EquipSlots.Cloak2:
                    if (mobileType == MobileTypes.Lich || mobileType == MobileTypes.AncientLich)
                    {
                        equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
                        if (equippedItem == null || item.value > equippedItem.value)
                            return EquipSlots.Cloak1;
                        equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
                        if (equippedItem == null || item.value > equippedItem.value)
                            return EquipSlots.Cloak2;
                    }
                    break;

                case EquipSlots.RightHand:
                case EquipSlots.LeftHand:
                    if (mobileType == MobileTypes.SkeletalWarrior)
                    {
                        if (item.IsShield)
                        {
                            equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.LeftHand);
                            if (equippedItem == null || item.value > equippedItem.value)
                                return EquipSlots.LeftHand;
                        }
                        else if (ItemEquipTable.GetItemHands(item) == ItemHands.Either)
                        {
                            //one-handed weapon
                            equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.RightHand);
                            if (equippedItem == null || item.value > equippedItem.value)
                                return EquipSlots.RightHand;
                        }
                    }
                    else if (mobileType == MobileTypes.Lich || mobileType == MobileTypes.AncientLich)
                    {
                        WeaponTypes weaponType = item.GetWeaponType();
                        if (weaponType == WeaponTypes.Staff || weaponType == WeaponTypes.Staff_Magic)
                        {
                            equippedItem = entity.ItemEquipTable.GetItem(EquipSlots.RightHand);
                            if (equippedItem == null || item.value > equippedItem.value)
                                return EquipSlots.RightHand;
                        }
                    }
                    break;

                default:
                    break;
            }

            return EquipSlots.None;
        }



    } //class PenwickMinion



} //namespace
