// Project:      Potion Of Seeking, The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  February 2022

using System.Text;
using System;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace ThePenwickPapers
{

    public class Seeking : IncumbentEffect
    {
        public const string EffectKey = "Seeking";

        float lastDirectionTime;
        PotionRecipe seekingRecipe;


        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_Self;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.PotionMaker;
            properties.ShowSpellIcon = true;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportDuration = true;
        }


        public override void SetPotionProperties()
        {
            seekingRecipe = new PotionRecipe(
                "quest", //this display key will be ignored, will use CustomDisplayName
                130,
                DefaultEffectSettings(),
                (int)PlantIngredients1.Root_tendrils,
                (int)PlantIngredients2.Black_poppy,
                (int)MetalIngredients.Lodestone,
                (int)CreatureIngredients1.Basilisk_eye);

            AssignPotionRecipes(seekingRecipe);

            seekingRecipe.TextureRecord = 43; //flask image, archive 205
        }


        /// <summary>
        /// Feature added with DFU version 13.5, allows setting custom potion name.
        /// </summary>
        public void SetCustomName(string customName)
        {
            seekingRecipe.CustomDisplayName = customName;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            if (entityBehaviour.EntityType != EntityTypes.Player)
            {
                //If someone other than the player uses the potion, then no effect
                ResignAsIncumbent();
                RoundsRemaining = 0;
                return;
            }

            //set icon to either swirly spiral or DREAM blue eye
            Utility.SetIcon(ParentBundle, 24, 187);

            RoundsRemaining = entityBehaviour.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) / 2;
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            //set icon to either swirly spiral or DREAM blue eye
            Utility.SetIcon(ParentBundle, 24, 187);
        }


        public override void MagicRound()
        {
            base.MagicRound();

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            //need some peace and contemplation
            int chance = player.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) / 2;
            if (Utility.IsPlayerThreatened())
                chance -= 50;

            if (Dice100.FailedRoll(chance))
                return;

            //prevent potentially showing quest target info every round...
            if (Time.time < lastDirectionTime + 6.5f)
                return;

            lastDirectionTime = Time.time;

            if (!player.IsResting && !player.IsLoitering)
            {
                try
                {
                    if (!ShowQuestTargetInfo())
                        RoundsRemaining = 0;
                }
                catch (Exception e)
                {
                    Utility.AddHUDText(Text.DisturbanceInFabricOfReality.Get());
                    Debug.LogException(e);
                }

                PlayBreath();
            }

        }

        protected override bool IsLikeKind(IncumbentEffect other)
        {
            //ChanceBase contains the Remedies index
            return other is Seeking;
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            // Stack my rounds onto incumbent
            incumbent.RoundsRemaining += playerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) / 2;
        }


        /// <summary>
        /// Attempts to find the quest objective in the current location, and shows directional
        /// information as HUD text if found.
        /// Returns true if the objective was found.
        /// </summary>
        bool ShowQuestTargetInfo()
        {
            bool gotTargetInfo = false;

            SiteDetails[] sites = QuestMachine.Instance.GetAllActiveQuestSites();
            foreach (SiteDetails site in sites)
            {
                if (InDaggerfallLocation(site.regionName, site.locationName, site.buildingKey))
                {
                    QuestMarker marker = site.selectedMarker;
                    if (marker.targetResources != null)
                    {
                        Quest quest = QuestMachine.Instance.GetQuest(site.questUID);
                        foreach (Symbol symbol in site.selectedMarker.targetResources)
                        {
                            QuestResource questTargetResource = quest.GetResource(symbol);
                            QuestResourceBehaviour target = questTargetResource.QuestResourceBehaviour;
                            if (target)
                            {
                                EvaluateQuestTargetResource(target);
                                gotTargetInfo = true;
                                break;
                            }
                            else
                            {
                                ShowDefeatedTargetFoe(questTargetResource);
                            }
                        }
                    }
                    else if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    {
                        string direction = TalkManager.Instance.GetBuildingCompassDirection(site.buildingKey);
                        Utility.AddHUDText(direction);
                        gotTargetInfo = true;
                    }
                }
            }

            if (!gotTargetInfo)
            {
                Utility.AddHUDText(TextManager.Instance.GetLocalizedText("Nothing"));
                return false;
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// Evaluates the status of the quest target, whether the player has already achieved it,
        /// or show's directions to it if they haven't.
        /// </summary>
        void EvaluateQuestTargetResource(QuestResourceBehaviour target)
        {
            QuestResourceBehaviour[] questItems = GameObject.FindObjectsOfType<QuestResourceBehaviour>();
            
            if (target.TargetResource is Item)
            {
                Item item = target.TargetResource as Item;
                string itemName = item.DaggerfallUnityItem.ItemName;

                if (GameManager.Instance.PlayerEntity.Items.Contains(item))
                    Utility.AddHUDText(Text.AlreadyHaveItem.Get(itemName));
                else if (GameManager.Instance.PlayerEntity.WagonItems.Contains(item))
                    Utility.AddHUDText(Text.ItemInWagon.Get(itemName));
                else if (questItems.Length == 0)
                    Utility.AddHUDText(TextManager.Instance.GetLocalizedText("Nothing"));
                else
                    ShowDirections(item.QuestResourceBehaviour.transform.position);
            }
            else if (target.TargetResource is Person)
            {
                Person person = target.TargetResource as Person;
                ShowDirections(person.QuestResourceBehaviour.transform.position);
            }
            else if (target.TargetResource is Foe)
            {
                bool exists = false;
                foreach (QuestResourceBehaviour questBehavior in questItems)
                {
                    if (questBehavior == target)
                        exists = true;
                }

                Foe foe = target.TargetResource as Foe;
                if (!exists)
                    ShowDefeatedTargetFoe(target.TargetResource);
                else
                    ShowDirections(foe.QuestResourceBehaviour.transform.position);
            }

            if (questItems.Length == 0)
                RoundsRemaining = 0;

        }


        /// <summary>
        /// Show description of quest foe that has been defeated as HUD text.
        /// </summary>
        void ShowDefeatedTargetFoe(QuestResource questResource)
        {
            if (questResource is Foe)
            {
                Foe foe = questResource as Foe;

                //TODO: Getting the actual name of the foe.  This is likely a generated name and that might
                //conflict with name given by a quest, so not showing for now.
                //foe.ExpandMacro(MacroTypes.DetailsMacro, out string details);

                foe.ExpandMacro(MacroTypes.NameMacro1, out string foeType);

                Utility.AddHUDText(Text.AlreadyDealtWithFoe.Get(foeType));
            }
        }



        static readonly string[] directionKeys = new string[]
        {
            "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest"
        };

        /// <summary>
        /// Shows the directions to the quest target as HUD text.
        /// </summary>
        void ShowDirections(Vector3 targetLocation)
        {
            Vector3 playerPosition = GameManager.Instance.PlayerMotor.transform.position;

            if (Vector3.Distance(playerPosition, targetLocation) < 3)
            {
                Utility.AddHUDText(TextManager.Instance.GetLocalizedText("thisPlace"));
                RoundsRemaining = 0;
                return;
            }

            Vector3 playerXZ = Vector3.ProjectOnPlane(playerPosition, Vector3.up);
            Vector3 targetXZ = Vector3.ProjectOnPlane(targetLocation, Vector3.up);

            Vector3 direction = targetXZ - playerXZ;
            float angleXZ = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
            angleXZ = angleXZ >= 0 ? angleXZ : 360 + angleXZ;
            int ordinalDirection = (int)((angleXZ + 22.5f) / 45.0f);
            ordinalDirection = (ordinalDirection > 7) ? 0 : ordinalDirection;
            string directionKey = directionKeys[ordinalDirection];

            float distanceXZ = Vector3.Distance(playerXZ, targetXZ);

            bool higher = targetLocation.y > playerPosition.y;
            float distanceY = Mathf.Abs(targetLocation.y - playerPosition.y);

            StringBuilder msg = new StringBuilder();

            int ticks = (int)(distanceXZ / 5.0f);
            if (ticks > 0)
            {
                ticks = Mathf.Min(ticks, 20);
                msg.Append(TextManager.Instance.GetLocalizedText(directionKey).ToLower());
                msg.Append('.', ticks);
            }

            ticks = (int)(distanceY / 5.0f);
            if (ticks > 0)
            {
                ticks = Mathf.Min(ticks, 20);
                string highLowKey = higher ? "higher" : "lower";
                msg.Append(TextManager.Instance.GetLocalizedText(highLowKey).ToLower());
                msg.Append('.', ticks);
            }

            if (msg.Length > 0)
                Utility.AddHUDText(msg.ToString());
            else
                Utility.AddHUDText(TextManager.Instance.GetLocalizedText("thisPlace"));

        }


        /// <summary>
        /// Determines if the location the PC is standing in corresponds with the provided location.
        /// </summary>
        bool InDaggerfallLocation(string regionName, string locationName, int buildingKey)
        {
            DFLocation dfLocation = GameManager.Instance.PlayerGPS.CurrentLocation;

            int currentBuildingKey = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.buildingKey;
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
                currentBuildingKey = 0;

            return dfLocation.RegionName.Equals(regionName)
                && dfLocation.Name.Equals(locationName)
                && currentBuildingKey == buildingKey;
        }



        /// <summary>
        /// Plays a male or female breath noise.
        /// </summary>
        void PlayBreath()
        {
            Assets asset = GameManager.Instance.PlayerEntity.Gender == Genders.Male ? Assets.MaleBreath : Assets.FemaleBreath;
            AudioClip clip = asset.Get<AudioClip>();
            DaggerfallUI.Instance.PlayOneShot(clip);
        }



    } //class Seeking



} //namespace