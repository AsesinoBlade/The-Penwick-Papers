// Project:     Blind, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.UserInterface;

namespace ThePenwickPapers
{

    public class Blind : IncumbentEffect
    {
        public const string EffectKey = "Blind";

        public static readonly HashSet<MobileTypes> Immune = new HashSet<MobileTypes>() {
            MobileTypes.AncientLich, MobileTypes.Ghost, MobileTypes.GiantBat,
            MobileTypes.Lich, MobileTypes.Mummy, MobileTypes.SkeletalWarrior,
            MobileTypes.Wraith
        };

        public static Panel BlindPanel = new Panel();

        float originalSightRadius = -1f;
        float originalHearingRadius = -1f;

        public override string GroupName => Text.BlindGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();


        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_All;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Illusion;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportDuration = true;
            properties.DurationCosts = MakeEffectCosts(56, 240);
            properties.SupportChance = false;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            ApplyBlindStatus();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            ApplyBlindStatus();
        }


        public override void ConstantEffect()
        {
            base.ConstantEffect();

            //Adjusting the hearing radius of the blinded entity based on noise made by its current target.
            //Note: this is temporary code needed until appropriate code is added to EnemySenses

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);

            //skip adjustment for player, as they have no sense ;)
            if (!entityBehaviour || entityBehaviour.EntityType == EntityTypes.Player)
                return;

            //Keep resetting hud to black to prevent flicker and other unwanted things.
            if (entityBehaviour.EntityType == EntityTypes.Player)
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();

            if (!ThePenwickPapersMod.IsMonsterUniversityInstalled)
            {
                EnemySenses senses = entityBehaviour.GetComponent<EnemySenses>();

                //...if no target then skip
                if (!senses || senses.Target == null)
                    return;

                float movement = senses.Target.GetComponent<CharacterController>().velocity.magnitude;

                float baseLoudness = GetBaseLoudness(senses.Target);

                float noise = movement * baseLoudness / 4;

                //always some effective noise equivalent, i.e. air currents, body heat, etc.
                noise = Mathf.Clamp(noise, 1.3f, originalHearingRadius);

                if (entityBehaviour.Entity.Career.AcuteHearing)
                    noise *= entityBehaviour.Entity.ImprovedAcuteHearing ? 1.5f : 1.3f;

                //Set the entity's hearing radius based on target noise level
                senses.HearingRadius = noise;

                float distance = Vector3.Distance(entityBehaviour.transform.position, senses.Target.transform.position);

                //chance (per frame) of being disoriented and losing target, must reacquire
                if (distance > noise && Random.Range(0f, 2.5f) < Time.smoothDeltaTime)
                {
                    senses.Target = null;
                    senses.DetectedTarget = false;
                    senses.HearingRadius = originalHearingRadius;
                }
            }

        }


        public override void End()
        {
            base.End();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            EnemySenses senses = entityBehaviour.GetComponent<EnemySenses>();

            if (entityBehaviour.EntityType == EntityTypes.Player)
            {
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();
                DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack(3.0f);
            }
            else if (senses && originalSightRadius != -1f)
            {
                senses.SightRadius = originalSightRadius;
                senses.HearingRadius = originalHearingRadius;
            }
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            return (other is Blind);
        }

        protected override void AddState(IncumbentEffect incumbent)
        {
            // Stack my rounds onto incumbent
            incumbent.RoundsRemaining += RoundsRemaining;
        }



        /// <summary>
        /// Calculate how much noise the target is making, based on armor worn, stealth, etc.
        /// </summary>
        float GetBaseLoudness(DaggerfallEntityBehaviour target)
        {
            float loudness = 1f;

            bool grounded;
            if (target.EntityType == EntityTypes.Player)
                grounded = target.GetComponent<PlayerMotor>().IsGrounded;
            else
                grounded = !target.GetComponent<EnemyMotor>().IsLevitating;

            foreach (DaggerfallUnityItem item in target.Entity.ItemEquipTable.EquipTable)
            {
                if (item == null || item.ItemGroup != ItemGroups.Armor)
                    continue;

                float itemLoudness = (item.NativeMaterialValue == (int)ArmorMaterialTypes.Leather) ? 0.3f : 0.9f;
                if (item.EquipSlot == EquipSlots.Feet)
                    itemLoudness *= grounded ? 4 : 1;
                else if (item.EquipSlot == EquipSlots.LegsArmor)
                    itemLoudness *= grounded ? 2 : 1;
                else if (item.EquipSlot == EquipSlots.Head)
                    itemLoudness *= 0.5f;

                loudness += itemLoudness;
            }

            //small luck adjustment
            float luck = target.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Luck);
            loudness -= (luck - 50) / 100;

            float stealth = target.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);
            stealth = Mathf.Clamp(stealth, 0, 100);
            float targetStealthAdjust = (100.0f - stealth) / 100.0f;

            return loudness * targetStealthAdjust;
        }


        /// <summary>
        /// If player, set HUD to black.  If monster, reduce sight radius.
        /// </summary>
        void ApplyBlindStatus()
        {
            if (ParentBundle.icon.index == 0)
            {
                //set icon to either dark cloud or DREAM dark cloud
                Utility.SetIcon(ParentBundle, 37, 126);
            }

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            EnemySenses senses = entityBehaviour.GetComponent<EnemySenses>();

            if (entityBehaviour.EntityType == EntityTypes.Player)
            {
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();
                Utility.AddHUDText(Text.Blinded.Get());
            }
            else if (senses)
            {
                EnemyEntity entity = entityBehaviour.Entity as EnemyEntity;

                if (Immune.Contains((MobileTypes)entity.MobileEnemy.ID))
                {
                    RoundsRemaining = 0;
                }
                else
                {
                    originalSightRadius = senses.SightRadius;
                    originalHearingRadius = senses.HearingRadius;
                    senses.SightRadius = 1.7f; //can only see directly in front of them
                }
            }

        }


        TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                GroupName,
                Text.BlindEffectDescription.Get(),
                Text.BlindSpellMakerDuration.Get());
        }

        TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                GroupName,
                Text.BlindSpellBookDuration.Get(),
                "",
                "\"" + Text.BlindEffectDescription.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("illusion") + "]");
        }

    }



}