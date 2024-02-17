// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022


using System;
using UnityEngine;


namespace ThePenwickPapers
{

    public enum Text
    {
        // General / Mod
        DisturbanceInFabricOfReality,
        Following,
        NotFollowing,
        NotEnoughWillpower,
        NowIsNotTheTime,
        YouAreParalyzed,
        NotWhileSubmerged,
        MinionEquipsItem,
        MinionTakesItem,
        MinionGoesRenegade,

        //Encumbrance details
        WeaponWeight,
        ArmorWeight,
        IngredientWeight,
        OtherWeight,
        GoldWeight,

        //Landmark Journal, LandmarkJournalItem
        LandmarkJournal,
        NotInDungeonOrTown,
        EnemiesNear,
        NotGrounded,
        TooTired,
        YouAreEncumbered,
        TeleportersNearby,

        //Landmark Journal, LandmarkJournalPopupWindow
        TravelButtonText,
        RememberButtonText,
        ForgetButtonText,
        ExitButtonText,
        LandmarkName,

        //Landmark Journal, LandmarkJournalListPickerWindow
        ItemGem,
        ItemJewellery,
        ItemDrug,
        ItemPotion,
        ItemGold,
        ItemBook,
        ItemPocketLint,
        InhaledSpores,
        Thugs,
        AmbushAverted,
        EncounteredPlagueVictim,
        PasserbyAvoidsPlagueVictim,
        CutpurseSucceeds,
        CutpurseFails,
        LostGoldButSnatchedItem,
        NabbedItem,
        SimultaneousPickpocket,
        LostInCityDay,
        LostInCityNight,
        LostInSmallTownDay,
        LostInSmallTownNight,
        LostInDark,

        //Summon Imp
        SummonImpGroupName,
        SummonImpEffectDescription,
        SummonImpSpellMakerChance,
        SummonImpSpellBookChance,

        //Create Atronach
        CreateAtronachGroupName,
        CreateAtronachDisplayName,
        CreateAtronachEffectDescription,
        CreateAtronachDuration,
        CreateAtronachSpellMakerChance,
        CreateAtronachSpellMakerMagnitude,
        CreateAtronachSpellBookChance1,
        CreateAtronachSpellBookChance2,
        CreateAtronachSpellBookMagnitude,
        MissingComponent,
        CantCreateFireAtronachSubmerged,

        //Reanimate
        ReanimateGroupName,
        ReanimateEffectDescription1,
        ReanimateEffectDescription2,
        ReanimateDuration,
        ReanimateSpellMakerChance,
        ReanimateSpellBookChance1,
        ReanimateSpellBookChance2,
        ReanimateMagnitude,
        MissingHolyDagger,
        MissingSoulTrap,
        NoViableVesselNearby,

        //Scour
        ScourGroupName,
        ScourEffectDescription,
        ScourDuration,

        //Illusory Decoy
        IllusoryDecoyGroupName,
        IllusoryDecoyEffectDescription1,
        IllusoryDecoyEffectDescription2,
        IllusoryDecoyEffectDescription3,
        IllusoryDecoyDuration,
        IllusoryDecoySpellMakerChance,
        IllusoryDecoySpellMakerMagnitude,
        IllusoryDecoySpellBookDuration,
        IllusoryDecoySpellBookChance1,
        IllusoryDecoySpellBookChance2,
        IllusoryDecoySpellBookChance3,
        IllusoryDecoySpellBookMagnitude,
        LackSkillToMaintain,
        LostConcentration,

        //Draught Of Seeking
        SeekingPotionName,
        AlreadyHaveItem,
        ItemInWagon,
        AlreadySpokenToPerson,
        AlreadyDealtWithFoe,


        //Blind
        BlindGroupName,
        BlindEffectDescription,
        BlindSpellMakerDuration,
        BlindSpellBookDuration,
        Blinded,


        //Wind Walk
        WindWalkGroupName,
        WindWalkEffectDescription,
        WindWalkSpellMakerDuration,
        WindWalkSpellMakerMagnitude,
        WindWalkSpellBookDuration,
        WindWalkSpellBookMagnitude,
        MustBeOutside,


        //Trapping
        TrapsUnknown,
        TrappingInterrupted,
        TrapIngredientsNotInInventory,
        Snaring,
        Venomous,
        Paralyzing,
        FlamingBomb,
        LunaStick,

        //LockpickWindow
        DoorJammed,

        //Dirty Tricks
        NotEnoughLockpickingSkill,
        NoFreeHand,
        OutOfPebbles,
        DoorChocked,
        DoorUnchocked,

        //Hook And Rope Item
        HookAndRope,
        HookAndRopeUsage,

        //Herbalism
        MortarAndPestle,
        MortarAndPestleUsage,
        HerbalismUnknown,
        HerbalismInterrupted,
        RemedyIngredientsNotInInventory,
        NotNecessary,
        PatientSuffersFromPoisoning,
        YouSufferFromPoisoning,
        RecoverFatigue,
        RegenerateHealth,
        RecoverMagicka,
        ExpungePoison,
        CleansePoison,
        RecoverStrength,
        RecoverIntelligence,
        RecoverWillpower,
        RecoverAgility,
        RecoverEndurance,
        RecoverPersonality,
        RecoverSpeed,
        ResistParalysis,
        Moonseed,
        Magebane,
        PyrrhicAcid,
        RemedyApplied,
        StuffMakeThingGood,
        TreatPrefix,
        TreatLethargyViaExhilarantTonic,
        TreatLethargyViaErgogenicInfusion,
        TreatSalubriousAccelerantViaBotanicalTincture,
        TreatSalubriousAccelerantViaRemedialPoultice,
        TreatFluxePaucityViaMetallurgicCordial,
        TreatFluxePaucityViaDraconicIncense,
        TreatToxaemiaViaDiaphoreticDepurative,
        TreatToxaemiaViaRenalExpungement,
        TreatAtrophiaViaAnapleroticTincture,
        TreatAtrophiaViaAnapleroticUnction,
        TreatPhrenitisViaAntiphlogisticTonic,
        TreatCephalicPhlegmasiaViaAntiphlogisticInfusion,
        TreatDeliriumViaBotanicalCordial,
        TreatAmentiaViaBotanicalEnema,
        TreatAtaxiaViaAntispasmodicEnema,
        TreatAtaxiaViaAntispasmodicSalve,
        TreatAnaemiaViaHepaticDepurative,
        TreatHepaticPhlegmasiaViaFloralIncense,
        TreatDistemperViaSoothingCordial,
        TreatEffluviaViaRectifyingDecoction,
        TreatKinesiaViaCalomel,
        TreatCatylepsyViaAntiparalyticOintment,
        Envenomed,


        //Enhanced Info
        YouSeeNothing,
        YouSeeACreature,
        YouSeeAbnormalCreature,
        YouSeeVagueThing,
        YouSeeSilhouette,
        YouSenseSomething,
        YouSeeYourDecoy,
        YouSeeDescribedNPC,
        YouSeeMemberNPC,
        YouSeeUndescribedMemberNPC,
        YouWitnessNPC,
        YouSeeFamiliarNPC,
        YouSeeTheNPCGuard,
        Man,
        Woman,
        Boy,
        Girl,
        Foreigner,
        Noble,
        SearchingForSomething,
        NoddingRespectfully,

        //EnhancedInfo Descriptive words
        Adroit, Agile, Alluring, Aloof, Amorous, Amusing, Annoyed, Anxious,
        Appalled, Aspiring,
        Baleful, Battered, Beefcake, Befuddled, Beguiling, Bitter, Bleak, Bloody, Bookish, Bored, Bowing, Brash,
        Brave, Brisk, Brooding, Buff, Bumbling, Burning, Busy,
        Cagey, Calloused, Canny, Captivating, Cerebral, Chaffing, Charming, Chilly, Clammy, Clandestine,
        Coarse, Cocky, Comely, Comical, Concerned, Confused, Contemplative, Cool, Coy, Covert, Crafty,
        Crestfallen, Cryptic, Curious, Curt, Curtseying, Cynical,
        Damaged, Damp, Dark, Despondent, Devoted, Devout, Disgusted, Disheveled, Distracted, Doubtful,
        Drained, Drenched, Dripping, Dubious, Dutiful,
        Eager, Enigmatic, Enlightened, Enthralling, Entrancing, Evasive, Exasperated, Exemplary, Exhausted,
        Experienced,
        Faithful, Fascinated, Fascinating, Fervent, Fervid, Fetching, Fetid, Flexing, Flirty, Flourishing,
        Flustered, Focused, Fond, Forsaken, Fortified, Frail, Freezing, Fretting, Friendly, Frugal, Frumpy,
        Furtive,
        Genteel, Giddy, Glum, Graceful, Grazed, Grim, Guarded,
        Hardened, Harried, Heady, Healing, Healthy, Hopeful, Hostile, Hungry, Hunky, Hurried, Hypnotic,
        Icy, Impenetrable, Impure, Indifferent, Inscrutable, Inspiring, Invigorated,
        Jovial, Joyless,
        Klutzy,
        Leery, Limber, Lithe, Lively, Lorn, Lurid, Lusty,
        Mangled, Miffed, Mirthless, Miserly, Morbid, Moody, Mortified, Mucky, Muddled, Muddy, Mysterious,
        Naughty, Nauseated, Nervous, Neutral, Nimble, Numb,
        Obedient, Obscure, Organized,
        Paralyzed, Paranoid, Perplexed, Perplexing, Perspiring, Pious, Playful, Poignant, Poisoned, Polished,
        Pristine, Provacative, Prudent,
        Questionable, Quivering,
        Refined, Repulsed, Resigned, Restless, Reverent, Revolted, Riveting, Rousing,
        Saluting, Savaged, Savvy, Scandalous, Scarred, Sceptical, Scowling, Seductive, Shifty, Shivering, Shrewd,
        Shy, Shady, Shielded, Shocked, Silenced, Sinuous, Skittish, Slick, Sly, Soaked, Solemn, Sopping, SoulTrapped, Spry,
        Stained, Stark, Stern, Steadfast, Stoic, Stout, Strapping, Stubborn, Studied, Suffering,
        Sullied, Superior, Supple, Suspicious, Sweaty,
        Tainted, Tarnished, Teasing, Tense, Tepid, Tired, Trembling, Tricky, Troubled,
        Unaware, Urbane,
        Vampish, Vexed, Violent, Virile, Virtuous,
        Warm, Wary, Weakening, Weary, Winking, Wise, Withdrawn, Wounded, Wry,
        Zealous,

    }


    public static class TextExtension
    {
        /// <summary>
        /// Gets the localized text corresponding to the provided key from the mod's '??textdatabase.txt' file.
        /// Optional args are used for string formatting.
        /// </summary>
        public static string Get(this Text key, params System.Object[] args)
        {
            string format = ThePenwickPapersMod.GetText(key.ToString());
            return string.Format(format, args);
        }

        /// <summary>
        /// Loops through all entries in the Text enum and verifies that corresponding keys exist
        /// in the mod's '??textdatabase.txt' file.
        /// Returns true if all entries exist in the file.
        /// </summary>
        public static bool CheckTextKeysValid()
        {
            bool areAllKeysValid = true;

            string[] keys = Enum.GetNames(typeof(Text));
            foreach (string key in keys)
            {
                if (!ThePenwickPapersMod.TextExists(key))
                {
                    Debug.LogWarningFormat("The mod ??textdatabase.txt file is missing an entry for key '{0}'", key);
                    areAllKeysValid = false;
                }
            }

            return areAllKeysValid;
        }


    } //class TextExtension



} //namespace

