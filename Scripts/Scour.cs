// Project:     Scour, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: July 2022

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Utility;


namespace ThePenwickPapers
{

    public class Scour : BaseEntityEffect
    {
        public const string effectKey = "Scour";

        DaggerfallLoot chosenVessel;


        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.ShowSpellIcon = false;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = ElementTypes.Magic;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Alteration;
            properties.DisableReflectiveEnumeration = true;
        }


        public override string GroupName => Text.ScourGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();



        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null)
                return;

            bool success = false;

            try
            {
                if (TryGetCorpse())
                {
                    success = true;
                    ThePenwickPapersMod.Instance.StartCoroutine(ScourCorpse());
                }
                else
                {
                    Utility.AddHUDText(Text.NoViableVesselNearby.Get());
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
        /// human-like corpse available in range.
        /// </summary>
        bool TryGetCorpse()
        {
            chosenVessel = null;

            //get all nearby loot, in range of 3 meters
            List<DaggerfallLoot> nearbyLoot = Utility.GetNearbyLoot(caster.transform.position, 3);

            foreach (DaggerfallLoot corpse in nearbyLoot)
            {
                if (CanScour(corpse))
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
            MobileTypes.Knight_CityWatch, MobileTypes.Mummy, MobileTypes.Vampire, MobileTypes.VampireAncient,
            MobileTypes.Zombie
        };

        /// <summary>
        /// Determines if the specified corpse can be scoured.
        /// Possible corpses include the human enemy classes as well as vampires, zombies, mummies,
        /// and city watchmen.
        /// </summary>
        bool CanScour(DaggerfallLoot corpse)
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
        /// Replaces corpse with skeleton corpse.
        /// </summary>
        IEnumerator ScourCorpse()
        {
            //Create an invisible skeleton enemy to use for creating a new corpse
            DaggerfallEntityBehaviour skelly = Utility.CreateTarget(chosenVessel.transform.position + Vector3.up, MobileTypes.SkeletalWarrior);
            skelly.gameObject.SetActive(false);

            MobileUnit mobile = skelly.GetComponent<DaggerfallEnemy>().MobileUnit;

            // Generate lootable corpse marker
            DaggerfallLoot newCorpse = GameObjectHelper.CreateLootableCorpseMarker(
                GameManager.Instance.PlayerObject,
                skelly.gameObject,
                skelly.Entity as EnemyEntity,
                mobile.Enemy.CorpseTexture,
                DaggerfallUnity.NextUID);

            newCorpse.gameObject.SetActive(false);

            skelly.CorpseLootContainer = newCorpse;

            //transfer loot from old corpse to new corpse
            newCorpse.Items.TransferAll(chosenVessel.Items);

            //create scouring spell animated billboard effect
            GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(377, 1, chosenVessel.transform.parent);
            go.SetActive(true);
            go.transform.position = chosenVessel.transform.position;
            go.transform.LookAt(GameManager.Instance.PlayerObject.transform);
            go.transform.position += go.transform.forward * 0.2f; //move slightly toward the player
            go.transform.position += Vector3.up * 0.3f;
            Billboard billboard = go.GetComponent<Billboard>();
            billboard.FramesPerSecond = 15;
            billboard.FaceY = true;
            billboard.OneShot = true;
            billboard.GetComponent<MeshRenderer>().receiveShadows = false;

            DaggerfallAudioSource audioSource = go.AddComponent<DaggerfallAudioSource>();
            audioSource.PlayOneShot(SoundClips.SpellImpactPoison, 1);

            yield return new WaitForSeconds(0.15f);

            GameObject.Destroy(chosenVessel.gameObject);

            newCorpse.gameObject.SetActive(true);
        }



        TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.ScourEffectDescription.Get(),
                Text.ScourDuration.Get());
        }

        TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.ScourDuration.Get(),
                "",
                "\"" + Text.ScourEffectDescription.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("alteration") + "]");
        }




    } //class Scour


} //namespace
