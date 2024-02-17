// Project:     Reanimate, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Player;

namespace ThePenwickPapers
{
    public class Reanimate : BaseEntityEffect
    {
        const string effectKey = "Reanimate";

        static bool alreadyCastingReanimate; //used to prevent multiple reanimates in same spell bundle

        DaggerfallLoot chosenVessel;
        int soulChanceMod = 0;
        ListPickerWindow soulPicker;



        /// <summary>
        /// The soul value is used to determine the type of reanimated entity, its starting health,
        /// and the caster's ability to control it.
        /// </summary>
        public static int GetSoulValue(MobileTypes soulType)
        {
            return (int)soulType < soulStrength.Length ? soulStrength[(int)soulType] : 50;
        }


        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = ElementTypes.Magic;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Mysticism;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportChance = true;
            properties.ChanceFunction = ChanceFunction.Custom;
            properties.ChanceCosts = MakeEffectCosts(10, 40, 300);
        }


        public override string GroupName => Text.ReanimateGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();



        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null)
            {
                return;
            }

            //to prevent casting multiple reanimates in same spell bundle
            if (alreadyCastingReanimate)
            {
                RefundSpellCost();
                return;
            }

            bool success = false;
            
            try
            {
                if (HasRequiredItems())
                {
                    if (TryGetCorpse())
                    {
                        ShowSoulPicker();
                        success = true;
                        alreadyCastingReanimate = true;
                    }
                    else
                    {
                        Utility.AddHUDText(Text.NoViableVesselNearby.Get());
                    }
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


        public override void End()
        {
            base.End();
            alreadyCastingReanimate = false;
        }


        public override bool RollChance()
        {
            //modify the RollChance to include willpower bonus
            int modifiedChance = ChanceValue();
            modifiedChance += Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) - 10;
            modifiedChance -= soulChanceMod; //more powerful souls are harder to control

            modifiedChance = Mathf.Clamp(modifiedChance, 3, 97);

            bool outcome = Dice100.SuccessRoll(modifiedChance);

            return outcome;
        }


        /// <summary>
        /// Checks the caster's inventory for the spell requirements: a ceremonial dagger and the soul
        /// of some poor sap.
        /// </summary>
        bool HasRequiredItems()
        {
            ItemTemplate itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate((int)ReligiousItems.Holy_dagger);
            DaggerfallUnityItem item = Caster.Entity.Items.GetItem(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_dagger, false, false, false);
            if (item == null)
            {
                //Whelp, the player left their ceremonial dagger in the glovebox again...
                string msg = Text.MissingHolyDagger.Get(itemTemplate.name);
                Utility.AddHUDText(msg);
                return false;
            }


            if (GetFilledSoulTraps().Count == 0)
            {
                itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate((int)MiscItems.Soul_trap);
                string msg = Text.MissingSoulTrap.Get(itemTemplate.name);
                Utility.AddHUDText(msg);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Who went shopping for souls today?
        /// </summary>
        void ShowSoulPicker()
        {
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;

            List<DaggerfallUnityItem> soulTraps = GetFilledSoulTraps();
            if (soulTraps.Count == 1)
            {
                //just one soul, selection window not needed
                StartSummoning(soulTraps[0]);
                return;
            }

            soulPicker = new ListPickerWindow(uiManager, uiManager.TopWindow);
            soulPicker.OnItemPicked += SoulPicker_OnItemPicked;
            soulPicker.OnCancel += SoulPicker_OnCancel;

            foreach (DaggerfallUnityItem trap in soulTraps)
            {
                string soulName = TextManager.Instance.GetLocalizedEnemyName((int)trap.TrappedSoulType);
                soulPicker.ListBox.AddItem(soulName, -1, trap);
            }

            uiManager.PushWindow(soulPicker);
        }


        /// <summary>
        /// I'll take the frosted one, with sprinkles
        /// </summary>
        void SoulPicker_OnItemPicked(int index, string soulName)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            DaggerfallUnityItem soulTrap = soulPicker.ListBox.GetItem(index).tag as DaggerfallUnityItem;

            StartSummoning(soulTrap);

            alreadyCastingReanimate = false;
        }


        /// <summary>
        /// If player cancels out of soul selection window, refund spell cost
        /// </summary>
        void SoulPicker_OnCancel(DaggerfallPopupWindow sender)
        {
            RefundSpellCost();

            alreadyCastingReanimate = false;
        }


        /// <summary>
        /// Starts the reanimation process and handles extra costs.
        /// </summary>
        void StartSummoning(DaggerfallUnityItem soulTrap)
        {
            GameObject go = Summon(soulTrap.TrappedSoulType);

            //performing the reanimation ritual
            Transform camera = GameManager.Instance.MainCamera.transform;
            Vector3 position = camera.position + camera.forward;
            go.GetComponent<EnemyBlood>().ShowBloodSplash(0, position);

            DaggerfallUI.Instance.PlayOneShot(SoundClips.EquipShortBlade);

            Caster.Entity.DecreaseHealth(1);
            Caster.Entity.DecreaseFatigue(20, true);

            //empty the soul trap (soul traps aren't stackable)
            soulTrap.TrappedSoulType = MobileTypes.None;
            if (!soulTrap.IsArtifact && !soulTrap.IsEnchanted)
                soulTrap.value = 5000;

            AdjustFactionReps(); //those Divines are loving you now
        }


        /// <summary>
        /// Checks the PC's inventory and returns the list of doomed souls available.
        /// </summary>
        List<DaggerfallUnityItem> GetFilledSoulTraps()
        {
            List<DaggerfallUnityItem> filledTraps = new List<DaggerfallUnityItem>();

            // Count regular filled soul gems
            ItemCollection casterItems = Caster.Entity.Items;
            for (int i = 0; i < casterItems.Count; i++)
            {
                DaggerfallUnityItem item = casterItems.GetItem(i);
                if (!item.IsQuestItem && item.IsOfTemplate(ItemGroups.MiscItems, (int)MiscItems.Soul_trap))
                {
                    if (item.TrappedSoulType != MobileTypes.None)
                        filledTraps.Add(item);
                }
            }

            // Check for filled Azura's Star soul trap
            List<DaggerfallUnityItem> amulets = Caster.Entity.Items.SearchItems(ItemGroups.Jewellery, (int)Jewellery.Amulet);
            foreach (DaggerfallUnityItem amulet in amulets)
            {
                if (amulet.ContainsEnchantment(EnchantmentTypes.SpecialArtifactEffect, (short)ArtifactsSubTypes.Azuras_Star))
                    if (amulet.TrappedSoulType != MobileTypes.None)
                        filledTraps.Add(amulet);
            }

            return filledTraps;
        }



        /// <summary>
        /// Adjusts the player's reputation with various factions
        /// </summary>
        void AdjustFactionReps()
        {
            if (Caster.EntityType != EntityTypes.Player)
            {
                return;
            }

            AdjustFactionRep((int)FactionFile.FactionIDs.Generic_Temple, -1);

            AdjustFactionRep((int)Temple.Divines.Akatosh, -3);
            AdjustFactionRep((int)Temple.Divines.Arkay, -9);
            AdjustFactionRep((int)Temple.Divines.Dibella, -3);
            AdjustFactionRep((int)Temple.Divines.Julianos, -1);
            AdjustFactionRep((int)Temple.Divines.Kynareth, -3);
            AdjustFactionRep((int)Temple.Divines.Mara, -4);
            AdjustFactionRep((int)Temple.Divines.Stendarr, -4);
            AdjustFactionRep((int)Temple.Divines.Zenithar, -3);

            AdjustFactionRep((int)FactionFile.FactionIDs.Meridia, -6);

            //boost rep with a few factions
            AdjustFactionRep((int)FactionFile.FactionIDs.King_of_Worms, 4);
            AdjustFactionRep((int)FactionFile.FactionIDs.Molag_Bal, 5);

        }


        /// <summary>
        /// Adjusts the player's reputation with the specified faction
        /// </summary>
        void AdjustFactionRep(int factionID, int amount)
        {
            PersistentFactionData factions = GameManager.Instance.PlayerEntity.FactionData;
            FactionFile.FactionData factionData = factions.FactionDict[factionID];

            //change rep for this faction as well as all child factions
            factions.PropagateReputationChange(factionData, factionID, amount);
        }


        /// <summary>
        /// Refund magicka cost of this effect to the caster
        /// </summary>
        void RefundSpellCost()
        {
            FormulaHelper.SpellCost cost = FormulaHelper.CalculateEffectCosts(this, Settings, Caster.Entity);
            Caster.Entity.IncreaseMagicka(cost.spellPointCost);
        }


        /// <summary>
        /// Examines the location the player is looking at and checks if there is an appropriate
        /// human-like vessel available in range.
        /// </summary>
        bool TryGetCorpse()
        {
            chosenVessel = null;

            //get all nearby loot, in range of 3 meters
            List<DaggerfallLoot> nearbyLoot = Utility.GetNearbyLoot(caster.transform.position, 3);

            foreach (DaggerfallLoot corpse in nearbyLoot)
            {
                if (CanReanimate(corpse))
                {
                    //must be near enough and looking in the direction of the corpse
                    Vector3 direction = corpse.transform.position - caster.transform.position;
                    Vector3 directionXZ = Vector3.ProjectOnPlane(direction, Vector3.up);
                    if (Vector3.Angle(caster.transform.forward, directionXZ) < 25)
                    {
                        chosenVessel = corpse;
                        return true;
                    }
                }
            }

            return false;
        }


        static readonly MobileTypes[] ViableMonsters =
        {
            MobileTypes.Knight_CityWatch, MobileTypes.Lich, MobileTypes.AncientLich, MobileTypes.Mummy,
            MobileTypes.SkeletalWarrior, MobileTypes.Vampire, MobileTypes.VampireAncient, MobileTypes.Zombie
        };

        /// <summary>
        /// Determines if the specified corpse can be reanimated.
        /// Possible corpses include the human enemy classes as well as skeletons, vampires, zombies, lichen,
        /// mummies, and city watchmen.
        /// </summary>
        bool CanReanimate(DaggerfallLoot corpse)
        {
            if (corpse.isEnemyClass)
            {
                return true;
            }
            else if (corpse.entityName != null)
            {
                foreach (MobileTypes monsterType in ViableMonsters)
                {
                    if (corpse.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)monsterType)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Begins the summoning animation and creates an undead creature corresponding to the selected corpse.
        /// </summary>
        GameObject Summon(MobileTypes soulType)
        {
            string displayName = string.Format("Penwick Summoned[{0}]", chosenVessel.entityName);

            Transform parent = GameObjectHelper.GetBestParent();

            Vector3 location = chosenVessel.transform.position;

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, displayName, parent, location);

            go.SetActive(false);

            MobileTypes minionType;

            if (chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.SkeletalWarrior))
                || chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Lich))
                || chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.AncientLich))
                )
            {
                if (soulType == MobileTypes.Lich)
                    minionType = MobileTypes.Lich;
                else if (soulType == MobileTypes.AncientLich)
                    minionType = MobileTypes.AncientLich;
                else
                    minionType = MobileTypes.SkeletalWarrior;
            }
            else if (chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Mummy)))
            {
                minionType = MobileTypes.Mummy;
            }
            else
            {
                minionType = MobileTypes.Zombie;
            }

            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            // Configure summons
            setupEnemy.ApplyEnemySettings(minionType, MobileReactions.Hostile, MobileGender.Unspecified, 0, false);
            setupEnemy.AlignToGround();

            //additional magnitude-related adjustments
            AdjustUndeadMinion(go, soulType);

            DaggerfallEnemy minion = go.GetComponent<DaggerfallEnemy>();

            //needs a loadID to save/serialize
            minion.LoadID = DaggerfallUnity.NextUID;

            //Have the reanimated entity looking in same direction as caster
            minion.transform.rotation = Caster.transform.rotation;

            //Move any inventory items from the corpse to the reanimated entity and destroy the corpse
            ItemCollection creatureInventory = go.GetComponent<DaggerfallEntityBehaviour>().Entity.Items;
            creatureInventory.Clear();
            creatureInventory.TransferAll(chosenVessel.Items);
            GameObject.Destroy(chosenVessel.gameObject);

            Texture2D eggTexture = ThePenwickPapersMod.Instance.SummoningEggTexture;
            AudioClip sound = Assets.ReanimateWarp.Get<AudioClip>();
            SummoningEgg egg = new SummoningEgg(minion, eggTexture, new Color32(200, 0, 0, 255), sound);

            //start coroutine to animate the 'hatching' process
            IEnumerator coroutine = egg.Hatch();
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);

            return go;
        }


        /// <summary>
        /// Sets the health for the new undead minion.
        /// </summary>
        void AdjustUndeadMinion(GameObject minion, MobileTypes soulType)
        {
            DaggerfallEntityBehaviour behaviour = minion.GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            MobileUnit mobileUnit = minion.GetComponentInChildren<MobileUnit>();

            //Other undead in the game have random health within a certain range.
            //We want ours tied to the soul type used to reanimate
            MobileEnemy mobileEnemy = mobileUnit.Enemy;             //struct copy

            if (mobileEnemy.ID == (int)MobileTypes.Zombie)
            {
                //In vanilla Daggerfall, zombies have a narrow health range (52-66).
                mobileEnemy.MinHealth = 52 + GetSoulValue(soulType) / 4;
            }
            else if (mobileEnemy.ID == (int)MobileTypes.SkeletalWarrior || mobileEnemy.ID == (int)MobileTypes.Mummy)
            {
                //In vanilla Daggerfall, skeletons and mummies have a wide health range (17-66).
                mobileEnemy.MinHealth = 17 + GetSoulValue(soulType);
            }
            else if (mobileEnemy.ID == (int)MobileTypes.Lich)
            {
                //In vanilla Daggerfall, liches have a wide health range (30-170).
                //Health is randomly determined in this case, since the soul type is always the same.
                mobileEnemy.MinHealth = UnityEngine.Random.Range(50, 165);
            }
            else if (mobileEnemy.ID == (int)MobileTypes.AncientLich)
            {
                //In vanilla Daggerfall, ancient liches have a wide health range (30-170).
                //Health is randomly determined in this case, since the soul type is always the same.
                mobileEnemy.MinHealth = UnityEngine.Random.Range(75, 165);
            }
            else
            {
                mobileEnemy.MinHealth += GetSoulValue(soulType);
            }

            int luckMod = Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Luck) / 12;

            mobileEnemy.MaxHealth = mobileEnemy.MinHealth + luckMod;

            if (chosenVessel.entityName.Equals(TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Knight_CityWatch)))
            {
                //with every fiber of their being...
                EnemySounds sounds = minion.GetComponent<EnemySounds>();
                sounds.BarkSound = SoundClips.Halt;
            }

            soulChanceMod = GetSoulValue(soulType); //stronger souls are harder to control

            //Record MobileEnemy changes to the MobileUnit
            mobileUnit.SetEnemy(DaggerfallUnity.Instance, mobileEnemy, MobileReactions.Hostile, 0);

            //Since we made changes to MobileEnemy, we have to reset the enemy career
            entity.SetEnemyCareer(mobileEnemy, behaviour.EntityType);

            if (RollChance()) //have to manually check chance because soulChanceMod was altered
            {
                entity.Team = Caster.EntityType == EntityTypes.Player ? MobileTeams.PlayerAlly : Caster.Entity.Team;
            }

        }


        TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.ReanimateEffectDescription1.Get(),
                Text.ReanimateEffectDescription2.Get(),
                Text.ReanimateDuration.Get(),
                Text.ReanimateSpellMakerChance.Get(),
                Text.ReanimateMagnitude.Get());
        }

        TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.ReanimateDuration.Get(),
                Text.ReanimateSpellBookChance1.Get(),
                Text.ReanimateSpellBookChance2.Get(),
                Text.ReanimateMagnitude.Get(),
                "",
                "\"" + Text.ReanimateEffectDescription1.Get(),
                Text.ReanimateEffectDescription2.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("mysticism") + "]");
        }



        //modifed version classicParamsCosts table found in MagicAndEffects/Effects/Enchanting/SoulBound.cs
        // Matches monster IDs 0-42
        static readonly short[] soulStrength =
        {
            0,  //-0,      //Rat 
            2,  //-10,     //Imp 
            6,  //-20,     //Spriggan 
            0,  //-0,      //GiantBat
            0,  //-0,      //GrizzlyBear
            0,  //-0,      //SabertoothTiger
            0,  //-0,      //Spider
            3,  //-10,     //Orc
            11,  //-30,     //Centaur
            18,  //-90,    //Werewolf
            20,  //-100,   //Nymph
            0,  //-0,      //Slaughterfish
            5,  //-10,     //OrcSergeant
            8,  //-30,     //Harpy
            24,  //-140,   //Wereboar
            0,  //-0,      //SkeletalWarrior
            12,  //-30,     //Giant
            0,  //-0,      //Zombie
            40,  //-300,   //Ghost
            21,  //-100,   //Mummy
            0,  //-0,      //GiantScorpion
            10,  //-30,     //OrcShaman
            11,  //-30,     //Gargoyle
            40,  //-300,   //Wraith
            7,  //-10,     //OrcWarlord
            60,  //-500,   //FrostDaedra
            60,  //-500,   //FireDaedra
            22,  //-100,   //Daedroth
            63,  //-700,   //Vampire
            80,  //-1500,  //DaedraSeducer
            72,  //-1000,  //VampireAncient
            140,  //-8000, //DaedraLord
            72,  //-1000,  //Lich
            110,  //-2500, //AncientLich
            0,  //-0,      //Dragonling (no soul, general spawn)
            38,  //-300,   //FireAtronach
            38,  //-300,   //IronAtronach
            38,  //-300,   //FleshAtronach
            38,  //-300,   //IceAtronach
            0,  //-0,      //Horse_Invalid
            124,  //-5000, //Dragonling_Alternate (has soul, quest spawn only)
            20,  //-100,   //Dreugh
            20,  //-100,   //Lamia
        };


    } //class Reanimate


} //namespace
