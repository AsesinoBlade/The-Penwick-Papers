// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022


using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;

namespace ThePenwickPapers
{


    public static class Utility
    {
        static readonly string[] lineSeparators = new string[] { "\\r\\n", "\\n\\r", "\\r", "\\n", "\r", "\n" };


        /// <summary>
        /// Shows HUD text with specified linger time
        /// </summary>
        public static void AddHUDText(string msg)
        {
            //display time depends on length of text
            float delay = 0.8f + msg.Length * 0.03f;
            DaggerfallUI.AddHUDText(msg, delay);
        }


        /// <summary>
        /// Only shows HUD text after the delay time has passed
        /// </summary>
        public static void AddDelayedHUDText(string msg, float delay)
        {
            IEnumerator coroutine = DelayedHUDMsgCoroutine(msg, delay);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }

        static IEnumerator DelayedHUDMsgCoroutine(string msg, float delay)
        {
            yield return new WaitForSeconds(delay);
            AddHUDText(msg);
        }


        /// <summary>
        /// Shows text in a message box.  Text containing newlines will be split into separate lines.
        /// </summary>
        public static void MessageBox(string text, IUserInterfaceWindow previous = null)
        {
            DaggerfallMessageBox msgBox = CreateMessageBox(text, previous);

            msgBox.Show();
        }


        /// <summary>
        /// Creates (but doesn't show) a message box.  Text containing newlines will be split into separate lines.
        /// </summary>
        public static DaggerfallMessageBox CreateMessageBox(string text, IUserInterfaceWindow previous = null)
        {
            if (previous == null)
                previous = DaggerfallUI.UIManager.TopWindow;

            DaggerfallMessageBox msgBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, previous)
            {
                ClickAnywhereToClose = true
            };

            string[] rows = text.Split(lineSeparators, System.StringSplitOptions.None);

            msgBox.SetText(rows);
            
            return msgBox;
        }


        /// <summary>
        /// Shows sequence of multiple message boxes displaying supplied text, one after the other.
        /// </summary>
        public static void MessageBoxSequence(params string[] text)
        {
            MessageBoxSequence(new List<string>(text));
        }


        /// <summary>
        /// Shows sequence of multiple message boxes displaying supplied text, one after the other.
        /// </summary>
        public static void MessageBoxSequence(List<string> text)
        {
            if (text.Count == 0)
                return;

            DaggerfallMessageBox firstMsgBox = CreateMessageBox(text[0]);

            DaggerfallMessageBox previousMsgBox = firstMsgBox;

            for (int i = 1; i < text.Count; ++i)
            {
                DaggerfallMessageBox msgBox = CreateMessageBox(text[i], previousMsgBox);
                previousMsgBox.AddNextMessageBox(msgBox);
                previousMsgBox = msgBox;
            }

            firstMsgBox.Show();
        }


        /// <summary>
        /// Creates a target GameObject that provides an invisible 'enemy' for an entity motor to move towards.
        /// </summary>
        public static DaggerfallEntityBehaviour CreateTarget(Vector3 location, MobileTypes mobileType = MobileTypes.GiantBat)
        {
            string name = "Penwick Target";

            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, name, parent, location);
            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            setupEnemy.ApplyEnemySettings(mobileType, MobileReactions.Hostile, MobileGender.Male, 0, false);

            go.SetActive(false);

            return go.GetComponent<DaggerfallEntityBehaviour>();
        }


        /// <summary>
        /// Checks if specified item is equipped.
        /// </summary>
        public static bool IsItemEquipped(int itemIndex, DaggerfallEntity onEntity = null)
        {
            return GetEquippedItem(itemIndex, onEntity) != null;
        }


        /// <summary>
        /// Returns specified item if equipped, null otherwise.
        /// If onEntity is not specified, the player entity is assumed.
        /// </summary>
        public static DaggerfallUnityItem GetEquippedItem(int itemIndex, DaggerfallEntity onEntity = null)
        {
            if (onEntity == null)
                onEntity = GameManager.Instance.PlayerEntity;

            foreach (DaggerfallUnityItem item in onEntity.ItemEquipTable.EquipTable)
            {
                if (item != null && item.TemplateIndex == itemIndex)
                    return item;
            }

            return null;
        }


        /// <summary>
        /// Gets list of creatures within specified range of the player.
        /// </summary>
        public static List<DaggerfallEntityBehaviour> GetNearbyEntities(float range = 14)
        {
            List<DaggerfallEntityBehaviour> entities = new List<DaggerfallEntityBehaviour>();

            List<PlayerGPS.NearbyObject> nearby = GameManager.Instance.PlayerGPS.GetNearbyObjects(PlayerGPS.NearbyObjectFlags.Enemy, range);
            foreach (PlayerGPS.NearbyObject no in nearby)
            {
                DaggerfallEntityBehaviour behaviour = no.gameObject.GetComponent<DaggerfallEntityBehaviour>();
                if (behaviour)
                    entities.Add(behaviour);
            }

            return entities;
        }


        /// <summary>
        /// Gets list of creatures within range of specified location. Max 50 meters from player.
        /// </summary>
        public static List<DaggerfallEntityBehaviour> GetNearbyEntities(Vector3 location, float range)
        {
            List<DaggerfallEntityBehaviour> near = new List<DaggerfallEntityBehaviour>();

            foreach (DaggerfallEntityBehaviour behaviour in GetNearbyEntities(50))
            {
                float distance = Vector3.Distance(location, behaviour.transform.position);
                if (distance <= range)
                    near.Add(behaviour);
            }

            return near;
        }


        /// <summary>
        /// Gets list of loot (bodies or treasure piles) within range of specified location. Max 30 meters from player.
        /// </summary>
        public static List<DaggerfallLoot> GetNearbyLoot(Vector3 location, float range)
        {
            List<DaggerfallLoot> nearbyLoot = new List<DaggerfallLoot>();

            List<PlayerGPS.NearbyObject> nearby = GameManager.Instance.PlayerGPS.GetNearbyObjects(PlayerGPS.NearbyObjectFlags.Treasure, 30);
            foreach (PlayerGPS.NearbyObject no in nearby)
            {
                DaggerfallLoot loot = no.gameObject.GetComponent<DaggerfallLoot>();
                float distance = Vector3.Distance(location, loot.transform.position);
                if (distance <= range)
                {
                    nearbyLoot.Add(loot);
                }
            }

            return nearbyLoot;
        }


        /// <summary>
        /// Determines of there are hostile enemies nearby that can see the player.
        /// </summary>
        public static bool IsPlayerThreatened()
        {
            List<DaggerfallEntityBehaviour> creatures = GetNearbyEntities();

            foreach (DaggerfallEntityBehaviour creature in creatures)
            {
                EnemySenses senses = creature.GetComponent<EnemySenses>();
                EnemyMotor motor = creature.GetComponent<EnemyMotor>();

                if (!senses)
                    continue;
                else if (!motor)
                    continue;
                else if (senses.Target != GameManager.Instance.PlayerEntityBehaviour)
                    continue;
                else if (creature.Entity.Team == MobileTeams.PlayerAlly)
                    continue;
                else if (!motor.IsHostile)
                    continue;
                else if (CanSeePlayer(creature))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Check if any terrain is between creature and player
        /// </summary>
        public static bool CanSeePlayer(DaggerfallEntityBehaviour creature)
        {
            Vector3 creaturePos = creature.transform.position;
            Vector3 playerPos = GameManager.Instance.PlayerObject.transform.position;

            float distance = Vector3.Distance(playerPos, creaturePos);
            Vector3 direction = (playerPos - creaturePos).normalized;

            int layerMask = 1; //just looking for terrain hits

            Ray ray = new Ray(creature.transform.position, direction);

            return !Physics.SphereCast(ray, 0.2f, distance, layerMask);
        }


        /// <summary>
        /// Check if specified entity has an active Blind effect
        /// </summary>
        public static bool IsBlind(DaggerfallEntityBehaviour behaviour)
        {
            EntityEffectManager effectManager = behaviour.GetComponent<EntityEffectManager>();
            if (!effectManager)
                return false;

            LiveEffectBundle[] bundles = effectManager.EffectBundles;
            foreach (LiveEffectBundle bundle in bundles)
            {
                foreach (IEntityEffect effect in bundle.liveEffects)
                {
                    if (effect is Blind)
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Check if player is currently attacking, spellcasting, booting, throwing grappling hook, or waving hands
        /// </summary>
        public static bool IsShowingHandAnimation()
        {
            if (GameManager.Instance.PlayerSpellCasting.IsPlayingAnim)
                return true;

            if (GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking())
                return true;

            if (ThePenwickPapersMod.TheBootAnimator.enabled)
                return true;

            if (ThePenwickPapersMod.GrapplingHookAnimator.enabled)
                return true;

            if (ThePenwickPapersMod.HandWaveAnimator.enabled)
                return true;

            return false;
        }


        /// <summary>
        /// Checks if player character has a free hand available.
        /// Returns true if weapons are sheathed or at least one hand is empty (hand slot not equipped).
        /// </summary>
        public static bool HasFreeHand()
        {
            if (GameManager.Instance.WeaponManager.Sheathed)
                return true;

            ItemEquipTable equipTable = GameManager.Instance.PlayerEntity.ItemEquipTable;
            if (equipTable.IsSlotOpen(EquipSlots.LeftHand))
                return true;
            else if (equipTable.IsSlotOpen(EquipSlots.RightHand))
                return true;
            else if (Settings.RelaxFreeHandRestriction && !equipTable.GetItem(EquipSlots.LeftHand).IsShield)
                return true;
            else
                return false;
        }


        /// <summary>
        /// Sets spell icon for bundle, using DREAM icon if 'DREAM Icons' is installed and 'DREAM Sprites' is active
        /// </summary>
        public static void SetIcon(LiveEffectBundle bundle, int defaultIcon, int dreamIcon)
        {
            bundle.icon.index = defaultIcon;

            const string dreamIcons = "D.R.E.A.M. Icons";
            if (Settings.UsingHiResSprites && DaggerfallUI.Instance.SpellIconCollection.HasPack(dreamIcons))
            {
                bundle.icon.key = dreamIcons;
                bundle.icon.index = dreamIcon;
            }

        }



    } //class Utility



} //namespace
