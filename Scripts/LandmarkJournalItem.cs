// Project:     Landmark Journal, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Apr 2021

using System.Collections.Generic;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect;
using DaggerfallWorkshop;

namespace ThePenwickPapers
{
    public class LandmarkJournalItem : DaggerfallUnityItem
    {
        public const int LandmarkJournalTemplateIndex = 1770;

        const int baseValue = 105;    // Base gold value


        public LandmarkJournalItem() : this(baseValue)
        {
        }


        public LandmarkJournalItem(int baseValue) : base(ItemGroups.UselessItems2, LandmarkJournalTemplateIndex)
        {
            value = baseValue;
        }


        public override string ItemName
        {
            get { return Text.LandmarkJournal.Get(); }
        }

        public override string LongName
        {
            get { return ItemName; }
        }

        public override bool IsEnchanted
        {
            get { return false; }
        }


        public override bool UseItem(ItemCollection collection)
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            if (GameManager.Instance.AreEnemiesNearby(true, false))
                Utility.MessageBox(Text.EnemiesNear.Get());
            else if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                Utility.MessageBox(Text.NotWhileSubmerged.Get());
            else if (!GameManager.Instance.PlayerMotor.IsGrounded)
                Utility.MessageBox(Text.NotGrounded.Get());
            else if (playerEntity.CarriedWeight > playerEntity.MaxEncumbrance)
                Utility.MessageBox(Text.YouAreEncumbered.Get());
            else if ((playerEntity.CurrentFatigue / DaggerfallEntity.FatigueMultiplier) < 10)
                Utility.MessageBox(Text.TooTired.Get());
            else if (playerEntity.IsParalyzed)
                Utility.MessageBox(Text.YouAreParalyzed.Get());
            else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                if (AreTeleportersNearby())
                    Utility.MessageBox(Text.TeleportersNearby.Get());
                else
                    ShowLandmarkJournalDialog(ThePenwickPapersMod.Persistent.DungeonLocations);
            }
            else if (GameManager.Instance.PlayerGPS.IsPlayerInTown(true, true))
            {
                string key = GetLocationIdentifier();

                //If an entry for this town doesn't exist yet, then create it
                if (!ThePenwickPapersMod.Persistent.Towns.TryGetValue(key, out List<LandmarkLocation> locations))
                {
                    locations = new List<LandmarkLocation>();
                    ThePenwickPapersMod.Persistent.Towns.Add(key, locations);
                }

                locations.Sort(); //town locations are sorted alphabetically
                ShowLandmarkJournalDialog(locations);
            }
            else
            {
                Utility.MessageBox(Text.NotInDungeonOrTown.Get());
            }


            return true;
        }


        public override bool IsStackable()
        {
            return false;
        }



        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(LandmarkJournalItem).ToString();
            return data;
        }



        /// <summary>
        /// Check if activated dungeon teleporter exits are nearby.
        /// </summary>
        bool AreTeleportersNearby()
        {
            Dictionary<string, Automap.AutomapDungeonState> automapState = Automap.instance.GetState();

            string locationIdentifier = GetLocationIdentifier();

            //get state of this specific dungeon
            if (automapState.TryGetValue(locationIdentifier, out Automap.AutomapDungeonState dungeonState))
            {
                Dictionary<string, Automap.TeleporterConnection> teleporters = dungeonState.dictTeleporterConnections;

                const float maxProximity = 40;

                foreach (KeyValuePair<string, Automap.TeleporterConnection> kvp in teleporters)
                {
                    if (GameManager.Instance.PlayerMotor.DistanceToPlayer(kvp.Value.teleporterExit.position) < maxProximity)
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Closes current window (if any), then creates and shows the dialog window
        /// </summary>
        void ShowLandmarkJournalDialog(List<LandmarkLocation> locations)
        {
            DaggerfallUI.Instance.PopToHUD();

            LandmarkJournalPopupWindow landmarkJournalDialog = new LandmarkJournalPopupWindow(DaggerfallUI.UIManager, locations);
            DaggerfallUI.Instance.UserInterfaceManager.PushWindow(landmarkJournalDialog);

            DaggerfallUI.Instance.PlayOneShot(SoundClips.PageTurn);
        }


        /// <summary>
        /// Creates a string location identifier using current region name and location name
        /// </summary>
        string GetLocationIdentifier()
        {
            DFLocation dfLocation = GameManager.Instance.PlayerGPS.CurrentLocation;
            return string.Format("{0}/{1}", dfLocation.RegionName, dfLocation.Name);
        }


    } //class LandmarkJournalItem


} //namespace

