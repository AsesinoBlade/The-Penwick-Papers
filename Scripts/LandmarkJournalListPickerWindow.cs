// Project:     Landmark Journal, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Apr 2021

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Items;
using DaggerfallConnect;

namespace ThePenwickPapers
{
    public class LandmarkJournalListPickerWindow : ListPickerWindow
    {
        readonly List<LandmarkLocation> locations;
        readonly PlayerEntity player;



        public LandmarkJournalListPickerWindow(List<LandmarkLocation> locations)
            : base(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow)
        {
            this.locations = locations;
            player = GameManager.Instance.PlayerEntity;
        }


        protected override void Setup()
        {
            base.Setup();

            OnItemPicked += LocationPicker_OnItemPicked;

            // Populate a list with remembered locations that the player can choose from.
            foreach (LandmarkLocation loc in locations)
            {
                ListBox.AddItem(loc.Name);
            }

            listBox.SelectNone();
        }


        /// <summary>
        /// Triggered when an item is chosen from the landmark location list window.
        /// </summary>
        void LocationPicker_OnItemPicked(int index, string name)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            Vector3 destination = locations[index].Position;

            PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
            float distance = playerMotor.DistanceToPlayer(destination);

            bool isInside = GameManager.Instance.PlayerEnterExit.IsPlayerInside;
            
            bool isRiding = false;

            if (distance > 5)
            {
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();

                playerMotor.transform.position = destination;
                playerMotor.FixStanding(2, 5);

                //reposition any following minions nearby if possible
                PenwickMinion.RepositionFollowers();

                AudioSource audioSource = DaggerfallUI.Instance.AudioSource;

                if (distance > 60 && !isInside && GameManager.Instance.TransportManager.HasHorse())
                {
                    AudioClip clip = DaggerfallUI.Instance.DaggerfallAudioSource.GetAudioClip((int)SoundClips.HorseClop);
                    audioSource.PlayOneShot(clip);
                    Thread.Sleep(1400);
                    audioSource.Stop();

                    isRiding = true;
                }
                else if (distance > 60 && !isInside && GameManager.Instance.TransportManager.HasCart())
                {
                    AudioClip clip = DaggerfallUI.Instance.DaggerfallAudioSource.GetAudioClip((int)SoundClips.HorseAndCart);
                    audioSource.PlayOneShot(clip);
                    Thread.Sleep(1400);
                    audioSource.Stop();

                    isRiding = true;
                }
                else
                {
                    SoundClips clip = GetFootstepSound();
                    Thread.Sleep(200);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                    Thread.Sleep(400);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                    Thread.Sleep(400);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                    Thread.Sleep(400);
                    DaggerfallUI.Instance.PlayOneShot(clip);
                }

                try
                {
                    HandleTravelCost(distance, isRiding);
                }
                catch (System.Exception e)
                {
                    Utility.AddHUDText(Text.DisturbanceInFabricOfReality.Get());
                    Debug.LogException(e);
                }

                DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack(3.0f);
            }
        }



        /// <summary>
        /// Handles fatigue loss, time loss, and checks for any incident that occurs during travel.
        /// </summary>
        bool HandleTravelCost(float distance, bool isRiding)
        {
            bool isInside = GameManager.Instance.PlayerEnterExit.IsPlayerInside;

            if (isInside)
            {
                distance *= 4; //dungeons tend to be tangled
            }
            else
            {
                int streetwise = player.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);
                //lower streetwise results in longer trip
                //at skill:20 distance is 41% longer, at skill:100 37% shorter, at skill:40 no change 
                float streetwiseMod = 1f - (1f / Mathf.Sqrt((float)streetwise / 40f));
                distance -= distance * streetwiseMod;
                float darknessModifier = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight ? 1.3f : 1f;
                distance *= darknessModifier;
            }

            float encumbranceModifier = 1;

            if (isRiding == false)
                encumbranceModifier += player.CarriedWeight / player.MaxEncumbrance;

            //Calculate time loss
            float speedModifier = isRiding ? 1f : 0.6f + 60f / player.Stats.LiveSpeed;
            float travelTime = distance * speedModifier * encumbranceModifier;
            DaggerfallUnity.Instance.WorldTime.RaiseTimeInSeconds += travelTime;


            //Calculate fatigue loss
            float fatigueLoss = travelTime / 6.0f;
            if (isRiding == false)
            {
                fatigueLoss *= encumbranceModifier * 2f;
                if (player.Career.Athleticism)
                    fatigueLoss *= player.ImprovedAthleticism ? 0.6f : 0.8f; //making up my own values
            }

            player.DecreaseFatigue((int)fatigueLoss); //no assignMultiplier argument


            //check for any unfortunate incident during travel
            return isInside ? CheckForDungeonIncident(distance) : CheckForTownIncident(distance);
        }


        /// <summary>
        /// Checks if PC was exposed to any disease-causing spores while traveling in a dungeon.
        /// </summary>
        bool CheckForDungeonIncident(float distance)
        {
            bool hadIncident = false;

            float sporeChance = (130 - Mathf.Clamp(player.Stats.LiveLuck, 0, 100)) / 50;
            sporeChance *= distance / 100;
            sporeChance = Mathf.Clamp(sporeChance, 0.3f, 10f);

            if (player.Level < 5)
            {
                //go easy on lower level characters
            }
            else if (Random.Range(0.0f, 100.0f) < sporeChance)
            {
                hadIncident = true;

                Diseases[] diseases = new Diseases[] {
                    Diseases.Consumption, Diseases.Cholera, Diseases.TyphoidFever
                };

                FormulaHelper.InflictDisease(player, player, diseases);
                Utility.MessageBox(Text.InhaledSpores.Get());

            }

            return hadIncident;
        }


        /// <summary>
        /// Checks for thugs, pickpockets, and any plague-carriers while PC is traveling in town.
        /// </summary>
        bool CheckForTownIncident(float distance)
        {
            bool hadIncident;

            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            bool[] regionFlags = player.RegionData[regionIndex].Flags;

            bool isNight = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight;

            if (isNight)
                hadIncident = CheckForThugs(distance, regionFlags);
            else
                hadIncident = CheckForPlague(distance, regionFlags) || CheckForCutpurse(distance, regionFlags);

            //Check to show appropriate 'Getting Lost' message if PC has low streetwise skill.
            int streetwise = player.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);
            if (!hadIncident && Dice100.SuccessRoll(70 - streetwise * 7))
            {
                switch (GameManager.Instance.PlayerGPS.CurrentLocationType)
                {
                    case DFRegion.LocationTypes.TownCity:
                    case DFRegion.LocationTypes.TownHamlet:
                        if (isNight)
                            Utility.MessageBox(Text.LostInCityNight.Get());
                        else
                            Utility.MessageBox(Text.LostInCityDay.Get());
                        hadIncident = true;
                        break;
                    case DFRegion.LocationTypes.HomeFarms:
                    case DFRegion.LocationTypes.HomeWealthy:
                    case DFRegion.LocationTypes.Tavern:
                    case DFRegion.LocationTypes.TownVillage:
                    case DFRegion.LocationTypes.ReligionTemple:
                        if (isNight)
                            Utility.MessageBox(Text.LostInSmallTownNight.Get());
                        else
                            Utility.MessageBox(Text.LostInSmallTownDay.Get());
                        hadIncident = true;
                        break;
                    default:
                        break;
                }
            }

            if (!hadIncident && isNight && Dice100.SuccessRoll(30 - streetwise))
            {
                Utility.MessageBox(Text.LostInDark.Get());
                hadIncident = true;
            }

            return hadIncident;
        }


        /// <summary>
        /// Check if PC had an encounter with thugs while traveling in town at night.
        /// </summary>
        bool CheckForThugs(float distance, bool[] regionFlags)
        {
            bool hadIncident = false;

            bool crimeWave = regionFlags[(int)PlayerEntity.RegionDataFlags.CrimeWave];

            float crimeChance = (distance / 100) * (crimeWave ? 8 : 4);
            crimeChance = Mathf.Clamp(crimeChance, 3, 30);

            if (Random.Range(0.0f, 100.0f) < crimeChance)
            {
                hadIncident = true;

                if (player.Career.AcuteHearing && Dice100.SuccessRoll(player.ImprovedAcuteHearing ? 70 : 40))
                {
                    Utility.MessageBox(Text.AmbushAverted.Get());
                }
                else
                {
                    Utility.MessageBox(Text.Thugs.Get());
                    SpawnThugs(player.Level / 8 + 1);
                }
            }

            return hadIncident;
        }


        /// <summary>
        /// Spawns enemies to attack the PC.
        /// </summary>
        void SpawnThugs(int count)
        {
            MobileTypes[] thiefTypes = { MobileTypes.Rogue, MobileTypes.Thief, MobileTypes.Burglar };
            MobileTypes[] fighterTypes = { MobileTypes.Archer, MobileTypes.Barbarian, MobileTypes.Warrior };

            MobileTypes[] thugTypes = Dice100.SuccessRoll(50) ? thiefTypes : fighterTypes;

            while (count-- > 0)
            {
                MobileTypes enemyType = thugTypes[Random.Range(0, thugTypes.Length)];

                GameObjectHelper.CreateFoeSpawner(true, enemyType, 1, 4.0f, 10.0f);
            }
        }


        /// <summary>
        /// Check if PC had an encounter with a plague victim while traveling in town during the day.
        /// This can only occur if there is a plague occuring in the current region.
        /// </summary>
        bool CheckForPlague(float distance, bool[] regionFlags)
        {
            bool hadIncident = false;

            bool plagueOngoing = regionFlags[(int)PlayerEntity.RegionDataFlags.PlagueOngoing];

            if (plagueOngoing)
            {
                float plagueChance = (130f - Mathf.Clamp(player.Stats.LiveLuck, 0, 100)) / 25f;
                plagueChance *= distance / 100f;
                plagueChance = Mathf.Clamp(plagueChance, 1, 15);

                if (Random.Range(0.0f, 100.0f) < plagueChance)
                {
                    hadIncident = true;

                    Diseases[] diseases = new Diseases[] { Diseases.Plague };

                    FormulaHelper.InflictDisease(player, player, diseases);

                    Utility.MessageBoxSequence(Text.EncounteredPlagueVictim.Get(), Text.PasserbyAvoidsPlagueVictim.Get());
                }

            }

            return hadIncident;
        }


        /// <summary>
        /// Check if PC had an encounter with a pickpocket while traveling in town during the day.
        /// There will be a higher incidence if there is a crime wave currently occuring in the region.
        /// </summary>
        bool CheckForCutpurse(float distance, bool[] regionFlags)
        {
            bool hadIncident = false;

            bool crimeWave = regionFlags[(int)PlayerEntity.RegionDataFlags.CrimeWave];
            float crimeChance = (distance / 100) * (crimeWave ? 12 : 6);
            crimeChance = Mathf.Clamp(crimeChance, 3, 35);

            if (Random.Range(0.0f, 100.0f) < crimeChance)
            {
                hadIncident = true;

                player.TallySkill(DFCareer.Skills.Streetwise, 1);

                int streetwise = player.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);
                int pickChance = player.Skills.GetLiveSkillValue(DFCareer.Skills.Pickpocket);

                if (Dice100.SuccessRoll(streetwise))
                {
                    //chance at simultaneously picking the cutpurse's pocket
                    if (Dice100.SuccessRoll(pickChance))
                    {
                        player.TallySkill(DFCareer.Skills.Pickpocket, 2);

                        DaggerfallUnityItem item = SnatchItem(out string itemName);

                        if (item != null)
                            player.Items.AddItem(item);

                        string msg;
                        if (HandleLostGold() > 0)
                            msg = Text.LostGoldButSnatchedItem.Get(itemName); 
                        else
                            msg = Text.NabbedItem.Get(itemName); 

                        //sequence of message boxes
                        Utility.MessageBoxSequence(Text.SimultaneousPickpocket.Get(), msg);
                    }
                    else
                    {
                        Utility.MessageBox(Text.CutpurseFails.Get());
                    }
                }
                else if (HandleLostGold() > 0)
                {
                    Utility.MessageBox(Text.CutpurseSucceeds.Get());
                }
            }

            return hadIncident;
        }


        /// <summary>
        /// Handles loss of gold to a pickpocket.  Returns amount lost.
        /// </summary>
        int HandleLostGold()
        {
            int goldLost = Random.Range(60, 150);

            if (goldLost > player.GoldPieces)
                goldLost = player.GoldPieces;

            player.DeductGoldAmount(goldLost);

            return goldLost;
        }


        /// <summary>
        /// During simultaneous pickpocket events, this determines what item the PC nabs.
        /// </summary>
        DaggerfallUnityItem SnatchItem(out string itemName)
        {
            DaggerfallUnityItem item = null;

            switch (Random.Range(0, 7))
            {
                case 1:
                    item = ItemBuilder.CreateRandomGem();
                    itemName = Text.ItemGem.Get();
                    break;
                case 2:
                    item = ItemBuilder.CreateRandomJewellery();
                    itemName = Text.ItemJewellery.Get();
                    break;
                case 3:
                    item = ItemBuilder.CreateRandomDrug();
                    itemName = Text.ItemDrug.Get();
                    break;
                case 4:
                    item = ItemBuilder.CreateRandomPotion(1);
                    itemName = Text.ItemPotion.Get();
                    break;
                case 5:
                    item = ItemBuilder.CreateGoldPieces(Random.Range(10, 200));
                    itemName = Text.ItemGold.Get();
                    break;
                case 6:
                    item = ItemBuilder.CreateRandomBook();
                    itemName = Text.ItemBook.Get();
                    break;
                default:
                    itemName = Text.ItemPocketLint.Get();
                    break;
            }

            return item;
        }

        /// <summary>
        /// Determines what the appropriate footstep noise is based on the current terrain and season.
        /// </summary>
        SoundClips GetFootstepSound()
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                return SoundClips.PlayerFootstepStone1;

            bool isWinter = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue == DaggerfallDateTime.Seasons.Winter;
            bool canHaveSnow = !WeatherManager.IsSnowFreeClimate(GameManager.Instance.PlayerGPS.CurrentClimateIndex);

            if (GameManager.Instance.PlayerMotor.OnExteriorPath)
                return SoundClips.PlayerFootstepStone2;
            else if (isWinter && canHaveSnow)
                return SoundClips.PlayerFootstepSnow1;
            else
                return SoundClips.PlayerFootstepOutside1;
        }




    } //class LandmarkJournalListPickerWindow


} //namespace
