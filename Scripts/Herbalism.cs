// Project:     Herbalism, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;


namespace ThePenwickPapers
{

    public static class Herbalism
    {
        public class Remedy
        {
            public Text Name;
            public int RequiredSkill;
            public DFCareer.Stats Stat;
            public Text TreatmentDescription;
            public string ShortIngredientList;
            public SoundClips[] Sounds;
            public int[] Ingredients;

            public Remedy(Text remedyName, int requiredSkill, DFCareer.Stats stat, Text description, string shortIngr, SoundClips[] sounds, int[] ingredients)
            {
                Name = remedyName;
                RequiredSkill = requiredSkill;
                Stat = stat;
                TreatmentDescription = description;
                ShortIngredientList = shortIngr;
                Sounds = sounds;
                Ingredients = ingredients;
            }
        }

        public static readonly List<Remedy> Remedies = new List<Remedy>()
        {
            new Remedy(Text.RecoverFatigue, 16, DFCareer.Stats.None, Text.TreatLethargyViaExhilarantTonic,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Bamboo, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.RecoverFatigue, 30, DFCareer.Stats.None, Text.TreatLethargyViaErgogenicInfusion,
                " (GinkLeaves,PurWater)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Ginkgo_leaves, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RegenerateHealth, 17, DFCareer.Stats.None, Text.TreatSalubriousAccelerantViaBotanicalTincture,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.AmbientWaterBubbles},
                new int[] { (int)PlantIngredients1.Root_bulb, (int)MiscellaneousIngredients1.Elixir_vitae}),

            new Remedy(Text.RegenerateHealth, 33, DFCareer.Stats.None, Text.TreatSalubriousAccelerantViaRemedialPoultice,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients1.Troll_blood, (int)PlantIngredients2.Aloe}),

            new Remedy(Text.RecoverMagicka, 18, DFCareer.Stats.None, Text.TreatFluxePaucityViaMetallurgicCordial,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)MetalIngredients.Silver, (int)MiscellaneousIngredients1.Nectar}),

            new Remedy(Text.RecoverMagicka, 35, DFCareer.Stats.None, Text.TreatFluxePaucityViaDraconicIncense,
                " (DrgnScale,Amber)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.Burning},
                new int[] {(int)CreatureIngredients2.Dragons_scales, (int)Gems.Amber}),

            new Remedy(Text.ExpungePoison, 19, DFCareer.Stats.None, Text.TreatToxaemiaViaRenalExpungement,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)MetalIngredients.Copper, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.CleansePoison, 29, DFCareer.Stats.None, Text.TreatToxaemiaViaDiaphoreticDepurative,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients1.Clover, (int)MiscellaneousIngredients1.Elixir_vitae}),

            new Remedy(Text.RecoverStrength, 24, DFCareer.Stats.Strength, Text.TreatAtrophiaViaAnapleroticTincture,
                " (OrcBld,ElxrVit)",
                new SoundClips[] {SoundClips.AmbientWaterBubbles, SoundClips.MakePotion},
                new int[] {(int)CreatureIngredients1.Orcs_blood, (int)MiscellaneousIngredients1.Elixir_vitae}),

            new Remedy(Text.RecoverStrength, 36, DFCareer.Stats.Strength, Text.TreatAtrophiaViaAnapleroticUnction,
                "(WereBrTusk,Ichor)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients3.Wereboar_tusk, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.RecoverIntelligence, 22, DFCareer.Stats.Intelligence, Text.TreatPhrenitisViaAntiphlogisticTonic,
                " (WhtRose,RainWtr)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.White_rose, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.RecoverIntelligence, 37, DFCareer.Stats.Intelligence, Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion,
                " (LchDst,PureWtr)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)CreatureIngredients1.Lich_dust, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RecoverWillpower, 21, DFCareer.Stats.Willpower, Text.TreatDeliriumViaBotanicalCordial,
                " (GrnBerries,Nectar)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] { (int)PlantIngredients1.Green_berries, (int)MiscellaneousIngredients1.Nectar}),

            new Remedy(Text.RecoverWillpower, 36, DFCareer.Stats.Willpower, Text.TreatAmentiaViaBotanicalEnema,
                " (GrnBerries,PureWtr)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients1.Green_berries, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RecoverAgility, 23, DFCareer.Stats.Agility, Text.TreatAtaxiaViaAntispasmodicSalve,
                " (GrnLeaves,Aloe)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)PlantIngredients1.Green_leaves, (int)PlantIngredients2.Aloe}),

            new Remedy(Text.RecoverAgility, 38, DFCareer.Stats.Agility,Text.TreatAtaxiaViaAntispasmodicEnema,
                "(GrnLeavs,PureWtr)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients1.Green_leaves, (int)MiscellaneousIngredients1.Pure_water}),

            new Remedy(Text.RecoverEndurance, 25, DFCareer.Stats.Endurance, Text.TreatAnaemiaViaHepaticDepurative,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Bamboo, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.RecoverEndurance, 39, DFCareer.Stats.Endurance, Text.TreatHepaticPhlegmasiaViaFloralIncense,
                " (YlwRose,Amber)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.Burning},
                new int[] {(int)PlantIngredients1.Yellow_rose, (int)Gems.Amber}),

            new Remedy(Text.RecoverPersonality, 28, DFCareer.Stats.Personality, Text.TreatDistemperViaSoothingCordial,
                " (GldPoppy,Nectar)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients1.Golden_poppy, (int)MiscellaneousIngredients1.Nectar}),

            new Remedy(Text.RecoverPersonality, 40, DFCareer.Stats.Personality, Text.TreatEffluviaViaRectifyingDecoction,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.SplashSmall, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients2.Palm, (int)MiscellaneousIngredients1.Rain_water}),

            new Remedy(Text.RecoverSpeed, 27, DFCareer.Stats.Speed, Text.TreatKinesiaViaCalomel,
                "(YlwBerries,Mercury)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.MakePotion},
                new int[] {(int)PlantIngredients1.Yellow_berries, (int)MetalIngredients.Mercury}),

            new Remedy(Text.ResistParalysis, 20, DFCareer.Stats.None, Text.TreatCatylepsyViaAntiparalyticOintment,
                " (BslkEye,Ichor)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles, SoundClips.EquipLeather},
                new int[] {(int)CreatureIngredients1.Basilisk_eye, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.Moonseed, 26, DFCareer.Stats.None, Text.Envenomed,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients1.Red_berries, (int)MiscellaneousIngredients1.Ichor}),

            new Remedy(Text.Magebane, 31, DFCareer.Stats.None, Text.Envenomed,
                " (BlkPoppy,WrthEss)",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients2.Black_poppy, (int)CreatureIngredients1.Wraith_essence}),

            new Remedy(Text.PyrrhicAcid, 44, DFCareer.Stats.None, Text.Envenomed,
                "",
                new SoundClips[] {SoundClips.ActivateGears, SoundClips.AmbientWaterBubbles},
                new int[] {(int)PlantIngredients2.Black_rose, (int)PlantIngredients2.Cactus}),

        };

        public const int PoisonsOffset = 128;
        public static readonly HashSet<Poisons> Expungable = new HashSet<Poisons>() {
            Poisons.Nux_Vomica, Poisons.Arsenic, Poisons.Moonseed, Poisons.Pyrrhic_Acid
        };

        static DaggerfallEntityBehaviour patient;
        static ListPickerWindow remedyPicker;
        static bool creatingRemedy;



        /// <summary>
        /// Checks activate details and character info to see if conditions are met to trigger
        /// herbalism remedies selection window.
        /// Shows remedy selection screen if so.
        /// </summary>
        public static bool AttemptRemedy(RaycastHit hitInfo)
        {
            if (!hitInfo.collider)
                return false;
            else if (hitInfo.distance > 2.5f)
                return false;
            else if (!GameManager.Instance.PlayerMotor.IsCrouching)
                return false;
            else if (!Utility.IsItemEquipped(MortarAndPestle.MortarAndPestleTemplateIndex))
                return false;

            bool hitTerrain = (hitInfo.collider.gameObject.layer == 0);

            bool hitFriendly = false;
            DaggerfallEntityBehaviour creature = hitInfo.collider.GetComponent<DaggerfallEntityBehaviour>();
            if (creature && (creature.EntityType == EntityTypes.EnemyClass || creature.EntityType == EntityTypes.EnemyMonster))
            {
                EnemyMotor motor = creature.GetComponent<EnemyMotor>();
                EnemyEntity entity = creature.Entity as EnemyEntity;
                MobileEnemy mobileEnemy = entity.MobileEnemy;
                if (creature.EntityType != EntityTypes.EnemyClass &&
                    mobileEnemy.Affinity != MobileAffinity.Animal &&
                    mobileEnemy.Team != MobileTeams.Orcs)
                    return false; //can only treat humans, orcs, and animals

                hitFriendly = entity.MobileEnemy.Team == MobileTeams.PlayerAlly || !motor.IsHostile;
            }

            if (!hitTerrain && !hitFriendly)
                return false;
            else if (hitTerrain && Vector3.Angle(hitInfo.normal, Vector3.up) > 25)
                return false; //if treating self, must activate floor or table

            if (creatingRemedy)
                return true; //already working on something

            if (Utility.IsPlayerThreatened())
            {
                Utility.AddHUDText(Text.NowIsNotTheTime.Get());
                return true;
            }

            if (GameManager.Instance.PlayerEntity.IsParalyzed)
            {
                Utility.AddHUDText(Text.YouAreParalyzed.Get());
                return true;
            }

            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
            {
                Utility.AddHUDText(Text.NotWhileSubmerged.Get());
                return true;
            }

            if (hitFriendly)
                EnhancedInfo.ShowPatientInfo(creature);

            patient = hitFriendly ? creature : GameManager.Instance.PlayerEntityBehaviour;
            ShowRemedyPicker();

            return true;
        }


        /// <summary>
        /// Called when a game is loaded.
        /// </summary>
        public static void OnLoadEvent()
        {
            //make sure static state variables are properly set so we don't get stuck
            creatingRemedy = false;
        }


        /// <summary>
        /// Show color coded available remedy selections in list window.
        /// </summary>
        static void ShowRemedyPicker()
        {
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;

            int medical = GameManager.Instance.PlayerEntity.Skills.GetPermanentSkillValue(DFCareer.Skills.Medical);

            remedyPicker = new ListPickerWindow(uiManager, uiManager.TopWindow);
            remedyPicker.OnItemPicked += RemedyPicker_OnItemPicked;

            ListBox listBox = remedyPicker.ListBox;
            listBox.SelectNone();

            foreach (Remedy remedy in Remedies)
            {
                if (remedy.RequiredSkill > medical)
                    continue;

                //Player can only envenom themselves
                if (patient != GameManager.Instance.PlayerEntityBehaviour)
                    if (remedy.Name == Text.Moonseed || remedy.Name == Text.Magebane || remedy.Name == Text.PyrrhicAcid)
                        continue;

                StringBuilder sbuff = new StringBuilder();
                sbuff.Append(remedy.Name.Get());
                if (remedy.ShortIngredientList.Length == 0 || DaggerfallUnity.Settings.SDFFontRendering == true)
                {
                    sbuff.Append(" (");
                    for (int i = 0; i < remedy.Ingredients.Length; ++i)
                    {
                        if (i > 0)
                            sbuff.Append(", ");

                        ItemTemplate template = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(remedy.Ingredients[i]);
                        sbuff.Append(template.name);
                    }
                    sbuff.Append(")");
                }
                else
                {
                    //use alternate shortened ingredient list when using the pixelated font
                    sbuff.Append(remedy.ShortIngredientList);
                }

                listBox.AddItem(sbuff.ToString(), out ListBox.ListItem item, -1, remedy);

                if (!IsRemedyNeeded(remedy))
                {
                    item.textColor = new Color(0.4f, 0.8f, 0.4f); ;
                    item.highlightedTextColor = item.textColor;
                }

                if (!HasAllIngredients(remedy))
                {
                    item.textColor = new Color(0.8f, 0.8f, 0.8f);
                    item.highlightedTextColor = item.textColor;
                }
            }

            if (listBox.Count == 0)
            {
                string skillName = DaggerfallUnity.Instance.TextProvider.GetSkillName(DFCareer.Skills.Medical);
                Utility.AddHUDText(Text.HerbalismUnknown.Get(skillName));
                return;
            }

            uiManager.PushWindow(remedyPicker);
        }


        /// <summary>
        /// Checks if PC has all ingredients necessary to create remedy.
        /// </summary>
        static bool HasAllIngredients(Remedy remedy)
        {
            bool hasAll = true;

            foreach (int ingredient in remedy.Ingredients)
                hasAll &= HasItem(ingredient);

            return hasAll;
        }


        /// <summary>
        /// Checks if PC has specified item in their inventory.
        /// Ignore quest items.
        /// </summary>
        static bool HasItem(int itemTemplateIndex)
        {
            ItemCollection items = GameManager.Instance.PlayerEntity.Items;

            for (int i = 0; i < items.Count; i++)
            {
                DaggerfallUnityItem item = items.GetItem(i);
                if (!item.IsQuestItem && item.IsOfTemplate(itemTemplateIndex))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Triggered when a remedy is selected from the list-picker window.
        /// Starts the remedy production coroutine.
        /// </summary>
        static void RemedyPicker_OnItemPicked(int index, string itemText)
        {
            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();

            ListBox.ListItem item = remedyPicker.ListBox.GetItem(index);
            Remedy remedy = (Remedy)item.tag;

            if (!HasAllIngredients(remedy))
            {
                Utility.AddHUDText(Text.RemedyIngredientsNotInInventory.Get());
                return;
            }

            if (IsRemedyNeeded(remedy))
            {
                IEnumerator coroutine = CreateRemedy(remedy);
                creatingRemedy = true;
                ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
            }
            else
            {
                Utility.AddHUDText(Text.NotNecessary.Get());
            }

        }



        /// <summary>
        /// Coroutine produces sound effects when creating remedies, then applies the remedy when complete.
        /// </summary>
        static IEnumerator CreateRemedy(Remedy remedy)
        {
            Camera camera = GameManager.Instance.MainCamera;

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;

            DaggerfallAudioSource dfAudioSource = GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>();

            foreach (SoundClips sound in remedy.Sounds)
            {
                dfAudioSource.PlayOneShot(sound, 1, 0.5f);

                AudioClip audioClip = dfAudioSource.GetAudioClip((int)sound);

                const float tick = 0.1f;

                float length = Mathf.Min(audioClip.length, 2f);

                for (float time = 0; time < length; time += tick)
                {
                    float distance = Vector3.Distance(camera.transform.position, originalPosition);
                    float angle = Quaternion.Angle(camera.transform.rotation, originalRotation);
                    if (distance > 0.4f || angle > 20)
                    {
                        Utility.AddHUDText(Text.HerbalismInterrupted.Get());
                        dfAudioSource.AudioSource.Stop();
                        creatingRemedy = false;
                        yield break;
                    }
                    yield return new WaitForSeconds(tick);
                }
            }

            ApplyRemedy(remedy);

            creatingRemedy = false;
        }


        /// <summary>
        /// Applies the appropriate HerbalRemedy incumbent effect status for the selected remedy.
        /// Removes the required remedy ingredients from PC inventory.
        /// </summary>
        static void ApplyRemedy(Remedy remedy)
        {
            //Using herbalism causes any existing envenomed state to be removed.
            ExitEnvenomedState();

            switch (remedy.Name)
            {
                case Text.Moonseed:
                case Text.Magebane:
                case Text.PyrrhicAcid:
                    EnterEnvenomedState(remedy);
                    Utility.AddHUDText(Text.Envenomed.Get());
                    break;
                default:
                    ApplyRemedyEffect(remedy);
                    ShowRemedyDescriptiveText(remedy);
                    break;
            }

            ApplySideEffects(remedy);

            RemoveIngredients(remedy);

            //medical skill has a large advancement multiplier (12)
            switch (remedy.TreatmentDescription)
            {
                //The second version of various remedies grant more experience
                case Text.TreatLethargyViaErgogenicInfusion:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                case Text.TreatFluxePaucityViaDraconicIncense:
                case Text.TreatAtrophiaViaAnapleroticUnction:
                case Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion:
                case Text.TreatAmentiaViaBotanicalEnema:
                case Text.TreatAtaxiaViaAntispasmodicSalve:
                case Text.TreatHepaticPhlegmasiaViaFloralIncense:
                case Text.TreatEffluviaViaRectifyingDecoction:
                    GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Medical, 60);
                    break;
                default:
                    GameManager.Instance.PlayerEntity.TallySkill(DFCareer.Skills.Medical, 40);
                    break;
            }

        }


        /// <summary>
        /// Shows appropriate flavor text related to the applied remedy as HUD text.
        /// </summary>
        static void ShowRemedyDescriptiveText(Remedy remedy)
        {
            int intelligence = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Intelligence);
            if (intelligence > 50)
            {
                string prefix = Text.TreatPrefix.Get();
                string description = string.Format("\"{0} {1}\"", prefix, remedy.TreatmentDescription.Get());
                Utility.AddHUDText(description);
            }
            else if (intelligence < 35)
            {
                Utility.AddHUDText("\"" + Text.StuffMakeThingGood.Get() + "\"");
            }
            else
            {
                Utility.AddHUDText(Text.RemedyApplied.Get());
            }
        }


        /// <summary>
        /// Removes all the ingredients needed for the remedy from PC inventory.
        /// </summary>
        static void RemoveIngredients(Remedy remedy)
        {
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;

            foreach (int templateIndex in remedy.Ingredients)
            {
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (!item.IsQuestItem && item.IsOfTemplate(templateIndex))
                        playerItems.RemoveOne(item);
                }
            }
        }


        /// <summary>
        /// Attached remedy incumbent effect to the patient.
        /// </summary>
        static void ApplyRemedyEffect(Remedy remedy)
        {
            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();

            int remedyIndex = Remedies.LastIndexOf(remedy);
            settings.ChanceBase = remedyIndex;

            EntityEffectManager manager = patient.GetComponent<EntityEffectManager>();

            EntityEffectBundle bundle = CreateBundle(HerbalRemedy.HerbalEffectKey, remedy, settings);

            AssignBundleFlags flags = AssignBundleFlags.BypassChance | AssignBundleFlags.BypassSavingThrows;

            manager.AssignBundle(bundle, flags);
        }



        /// <summary>
        /// Enters player into envenomed state incubent effect.
        /// </summary>
        static void EnterEnvenomedState(Remedy remedy)
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;

            EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();

            int remedyIndex = Remedies.LastIndexOf(remedy);
            settings.ChanceBase = remedyIndex;
            
            EntityEffectManager manager = player.GetComponent<EntityEffectManager>();

            EntityEffectBundle bundle = CreateBundle(Envenomed.EnvenomedEffectKey, remedy, settings);
            
            AssignBundleFlags flags = AssignBundleFlags.BypassChance | AssignBundleFlags.BypassSavingThrows;

            manager.AssignBundle(bundle, flags);
        }


        /// <summary>
        /// Removes envenomed incumbent effect from player.
        /// This is needed in cases where the player attempts another herbal remedy while
        /// envenomed status is active.
        /// </summary>
        static void ExitEnvenomedState()
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;
            EntityEffectManager manager = player.GetComponent<EntityEffectManager>();

            foreach (LiveEffectBundle bundle in manager.EffectBundles)
            {
                foreach (IEntityEffect effect in bundle.liveEffects)
                {
                    if (effect.Key.Equals(Envenomed.EnvenomedEffectKey))
                    {
                        Envenomed envenomed = effect as Envenomed;
                        envenomed.ExitState();
                        return;
                    }
                }
            }
        }


        /// <summary>
        /// Checks if a remedy is actually needed by the patient.
        /// </summary>
        static bool IsRemedyNeeded(Remedy remedy)
        {
            EntityEffectManager manager = patient.GetComponent<EntityEffectManager>();

            switch (remedy.Name)
            {
                case Text.RecoverFatigue:
                    return patient.Entity.CurrentFatigue < patient.Entity.MaxFatigue;
                case Text.ResistParalysis:
                    return true;
                case Text.RegenerateHealth:
                    return patient.Entity.CurrentHealth < patient.Entity.MaxHealth;
                case Text.RecoverMagicka:
                    return patient.Entity.CurrentMagicka < patient.Entity.MaxMagicka;
                case Text.ExpungePoison:
                case Text.CleansePoison:
                    bool cleansing = remedy.Name.Equals(Text.CleansePoison);

                    foreach (LiveEffectBundle bundle in manager.EffectBundles)
                    {
                        foreach (IEntityEffect effect in bundle.liveEffects)
                        {
                            if (!(effect is PoisonEffect))
                                continue;

                            PoisonEffect poisonEffect = effect as PoisonEffect;

                            Poisons variant = (Poisons)(PoisonsOffset + poisonEffect.CurrentVariant);

                            //'Expunge' is for expungable poisons and 'Cleanse' is for the others.
                            bool treatable = cleansing ^ Expungable.Contains(variant);
                            if (!treatable)
                                continue;

                            if (poisonEffect.CurrentState == PoisonEffect.PoisonStates.Active)
                            {
                                string poisonName = variant.ToString().Replace('_', ' ');
                                string diagnosis = Text.PatientSuffersFromPoisoning.Get(poisonName);
                                if (patient.EntityType == EntityTypes.Player)
                                    diagnosis = Text.YouSufferFromPoisoning.Get(poisonName);

                                Utility.AddHUDText(diagnosis);
                                return true;
                            }

                        }
                    }
                    return false;
                case Text.Moonseed:
                case Text.Magebane:
                case Text.PyrrhicAcid:
                    return true;
                default:
                    if (remedy.Stat != DFCareer.Stats.None)
                    {
                        int currentStatValue = patient.Entity.Stats.GetLiveStatValue(remedy.Stat);
                        int normalStatValue = patient.Entity.Stats.GetPermanentStatValue(remedy.Stat);
                        return currentStatValue < normalStatValue;
                    }
                    else
                    {
                        return false;
                    }
            }

        }

        /// <summary>
        /// Creates a non-spell bundle.
        /// </summary>
        static EntityEffectBundle CreateBundle(string effectKey, Remedy remedy, EffectSettings? effectSettings = null)
        {
            EffectBundleSettings settings = new EffectBundleSettings()
            {
                Version = EntityEffectBroker.CurrentSpellVersion,
                BundleType = BundleTypes.None,
                ElementType = ElementTypes.None,
                Effects = new EffectEntry[] { new EffectEntry(effectKey, effectSettings.Value) },
                Name = remedy.Name.Get(),
            };

            return new EntityEffectBundle(settings, GameManager.Instance.PlayerEntityBehaviour);
        }



        /// <summary>
        /// Applies appropriate side effect for the remedy used.
        /// </summary>
        static void ApplySideEffects(Remedy remedy)
        {
            int value = Random.Range(8, 28);

            switch (remedy.TreatmentDescription)
            {
                case Text.TreatLethargyViaExhilarantTonic:
                case Text.TreatLethargyViaErgogenicInfusion:
                case Text.Envenomed:
                    //no side effects
                    break;
                case Text.TreatToxaemiaViaRenalExpungement:
                case Text.TreatKinesiaViaCalomel:
                    patient.Entity.DecreaseMagicka(value);
                    break;
                case Text.TreatDeliriumViaBotanicalCordial:
                case Text.TreatAmentiaViaBotanicalEnema:
                    patient.Entity.DecreaseHealth(value); //ouchy
                    break;
                case Text.TreatSalubriousAccelerantViaBotanicalTincture:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                case Text.TreatFluxePaucityViaMetallurgicCordial:
                case Text.TreatFluxePaucityViaDraconicIncense:
                case Text.TreatToxaemiaViaDiaphoreticDepurative:
                case Text.TreatAtrophiaViaAnapleroticTincture:
                case Text.TreatAtrophiaViaAnapleroticUnction:
                case Text.TreatPhrenitisViaAntiphlogisticTonic:
                case Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion:
                case Text.TreatAtaxiaViaAntispasmodicEnema:
                case Text.TreatAtaxiaViaAntispasmodicSalve:
                case Text.TreatAnaemiaViaHepaticDepurative:
                case Text.TreatHepaticPhlegmasiaViaFloralIncense:
                case Text.TreatDistemperViaSoothingCordial:
                case Text.TreatEffluviaViaRectifyingDecoction:
                case Text.TreatCatylepsyViaAntiparalyticOintment:
                default:
                    patient.Entity.DecreaseFatigue(value, true);
                    break;
            }

        }



    } //class Herbalism




    public class MortarAndPestle : DaggerfallUnityItem
    {
        public const int MortarAndPestleTemplateIndex = 1772;

        const int baseValue = 75;    // Base gold value


        public MortarAndPestle() : this(baseValue)
        {
        }


        public MortarAndPestle(int baseValue) : base(ItemGroups.MiscellaneousIngredients2, MortarAndPestleTemplateIndex)
        {
            value = baseValue;
        }


        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(MortarAndPestle).ToString();
            return data;
        }

        public override string ItemName
        {
            get { return Text.MortarAndPestle.Get(); }
        }


        public override string LongName
        {
            get { return ItemName; }
        }


        public override bool UseItem(ItemCollection collection)
        {
            Utility.MessageBox(Text.MortarAndPestleUsage.Get());

            return true;
        }


        public override bool IsStackable()
        {
            return false;
        }

        public override EquipSlots GetEquipSlot()
        {
            return GameManager.Instance.PlayerEntity.ItemEquipTable.GetFirstSlot(EquipSlots.Mark0, EquipSlots.Mark1);
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipBow;
        }


    } //class MortarAndPestle



    public class HerbalRemedy : IncumbentEffect
    {

        public const string HerbalEffectKey = "Herbal-Remedy";

        Herbalism.Remedy remedy;

        public override string GroupName => remedy?.Name.Get();


        public override void SetProperties()
        {
            properties.Key = HerbalEffectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_All;
            properties.AllowedElements = ElementTypes.None;
            properties.AllowedCraftingStations = MagicCraftingStations.None;
            properties.SupportDuration = true;
            properties.DisableReflectiveEnumeration = true;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            InitRemedy();

            RoundsRemaining = CalculateDuration();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            InitRemedy();
        }


        public override void MagicRound()
        {
            base.MagicRound();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            DaggerfallEntity entity = entityBehaviour.Entity;

            float medical = Caster == null ? 30 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);

            float potency = 0.1f + medical / 100.0f;

            switch (remedy.TreatmentDescription)
            {
                case Text.TreatLethargyViaExhilarantTonic:
                case Text.TreatLethargyViaErgogenicInfusion:
                    entity.IncreaseFatigue((int)(potency * 16), true);
                    break;
                case Text.TreatSalubriousAccelerantViaBotanicalTincture:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                    entity.IncreaseHealth((int)(potency * 14));
                    break;
                case Text.TreatFluxePaucityViaMetallurgicCordial:
                case Text.TreatFluxePaucityViaDraconicIncense:
                    entity.IncreaseMagicka((int)(potency * 18));
                    break;
                case Text.TreatToxaemiaViaDiaphoreticDepurative:
                    TreatPoison(potency, true);
                    break;
                case Text.TreatToxaemiaViaRenalExpungement:
                    TreatPoison(potency, false);
                    break;
                default:
                    manager.HealAttribute(remedy.Stat, 1 + (int)(potency * 3));
                    break;
            }

        }

        public override void ConstantEffect()
        {
            base.ConstantEffect();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            //paralysis immunity is reset each frame, and must be reapplied
            if (remedy.Name == Text.ResistParalysis)
                entityBehaviour.Entity.IsImmuneToParalysis = true;
        }


        public override void End()
        {
            base.End();

            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            //ChanceBase contains the Remedies index
            return other is HerbalRemedy && other.Settings.ChanceBase == settings.ChanceBase;
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            //The remedy index is being stored in the ChanceBase effect value
            remedy = Herbalism.Remedies[settings.ChanceBase];

            // Stack my rounds onto incumbent
            incumbent.RoundsRemaining += CalculateDuration();
        }


        /// <summary>
        /// Sets the spell icon and gets remedy record
        /// </summary>
        void InitRemedy()
        {
            //set icon to either blue-white flame or DREAM green leaves
            Utility.SetIcon(ParentBundle, 56, 220);

            //The remedy index is being stored in the ChanceBase effect value
            remedy = Herbalism.Remedies[settings.ChanceBase];
        }


        /// <summary>
        /// Reduces poison based on herbalist medical skill.
        /// </summary>
        void TreatPoison(float potency, bool cleansing)
        {
            foreach (LiveEffectBundle bundle in manager.EffectBundles)
            {
                foreach (IEntityEffect effect in bundle.liveEffects)
                {
                    if (!(effect is PoisonEffect))
                        continue;

                    PoisonEffect poisonEffect = effect as PoisonEffect;

                    Poisons variant = (Poisons)(Herbalism.PoisonsOffset + poisonEffect.CurrentVariant);

                    bool treatable = cleansing ^ Herbalism.Expungable.Contains(variant);
                    if (!treatable)
                        continue;

                    //Using the SaveData mechanism because PoisonEffect doesn't expose the info by default.
                    PoisonEffect.SaveData_v1 data = (PoisonEffect.SaveData_v1)poisonEffect.GetSaveData();

                    if (data.minutesRemaining > 1)
                    {
                        //Reduce poison time.
                        float reduction = 1 + (data.minutesRemaining * potency);
                        if (reduction > data.minutesRemaining)
                            reduction = data.minutesRemaining;

                        data.minutesRemaining -= (int)reduction;
                        if (data.minutesRemaining < 3)
                            data.minutesRemaining = 1;

                        poisonEffect.RestoreSaveData(data);
                    }
                    else
                    {
                        //Attempt to heal some of any stat damage that occurred.
                        for (int i = 0; i < poisonEffect.StatMods.Length; ++i)
                        {
                            float reduction = (float)poisonEffect.StatMods[i] * potency / 1.5f;
                            poisonEffect.StatMods[i] -= (int)reduction;
                            if (poisonEffect.StatMods[i] > -2 && poisonEffect.StatMods[i] < 2)
                                poisonEffect.StatMods[i] = 0;
                        }
                    }
                }

            }

        }


        /// <summary>
        /// Determines effect duration based on medical skill and remedy type
        /// </summary>
        int CalculateDuration()
        {
            int medical = Caster == null ? 25 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);

            switch (remedy.TreatmentDescription)
            {
                //The second version of various remedies are longer lasting
                case Text.TreatLethargyViaErgogenicInfusion:
                case Text.TreatSalubriousAccelerantViaRemedialPoultice:
                case Text.TreatFluxePaucityViaDraconicIncense:
                case Text.TreatAtrophiaViaAnapleroticUnction:
                case Text.TreatCephalicPhlegmasiaViaAntiphlogisticInfusion:
                case Text.TreatAmentiaViaBotanicalEnema:
                case Text.TreatAtaxiaViaAntispasmodicSalve:
                case Text.TreatHepaticPhlegmasiaViaFloralIncense:
                case Text.TreatEffluviaViaRectifyingDecoction:
                    return medical / 2;
                case Text.TreatCatylepsyViaAntiparalyticOintment:
                    return medical * 2;
                default:
                    return medical / 3;
            }
        }


    } //class HerbalRemedy



    public class Envenomed : IncumbentEffect
    {
        public const string EnvenomedEffectKey = "Envenomed";

        Herbalism.Remedy remedy;

        public override string GroupName => remedy?.Name.Get();


        public override void SetProperties()
        {
            properties.Key = EnvenomedEffectKey;
            properties.ShowSpellIcon = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_Self;
            properties.AllowedElements = ElementTypes.None;
            properties.AllowedCraftingStations = MagicCraftingStations.None;
            properties.DisableReflectiveEnumeration = true;
        }


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            //Duration
            int medical = Caster == null ? 30 : Caster.Entity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
            RoundsRemaining = medical;

            EnvenomWeapons();
        }


        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);

            EnvenomWeapons();
        }


        public override void MagicRound()
        {
            base.MagicRound();

            //The poison is applied/reapplied each round
            EnvenomWeapons();
        }


        protected override bool IsLikeKind(IncumbentEffect other)
        {
            //the incumbent effect will resign and be replaced by this effect
            return false; 
        }


        protected override void AddState(IncumbentEffect incumbent)
        {
            //the incumbent effect will resign and be replaced by this effect
        }


        /// <summary>
        /// Resigns as incumbent and sets RoundsRemaining to zero
        /// </summary>
        public void ExitState()
        {
            ResignAsIncumbent();
            RoundsRemaining = 0;
        }


        /// <summary>
        /// Sets the poisonType value for all shortblade and missile items in PC inventory.
        /// </summary>
        void EnvenomWeapons()
        {
            //set icon to either greenish thing or DREAM green dagger
            Utility.SetIcon(ParentBundle, 52, 100);

            //The remedy index is being stored in the ChanceBase effect value
            remedy = Herbalism.Remedies[settings.ChanceBase];

            Poisons poisonType;
            if (remedy.Name.Equals(Text.Magebane))
                poisonType = Poisons.Magebane;
            else if (remedy.Name.Equals(Text.PyrrhicAcid))
                poisonType = Poisons.Pyrrhic_Acid;
            else
                poisonType = Poisons.Moonseed;


            //Apply poison to short blades and bows
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
            for (int i = 0; i < playerItems.Count; ++i)
            {
                DaggerfallUnityItem item = playerItems.GetItem(i);
                int skill = item.GetWeaponSkillUsed();
                if (skill == (int)DFCareer.ProficiencyFlags.ShortBlades || skill == (int)DFCareer.ProficiencyFlags.MissileWeapons)
                {
                    item.poisonType = poisonType;
                }
            }
        }


    } //class Envenomed



} //namespace