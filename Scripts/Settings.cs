// Project:      The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  July 2023

using UnityEngine;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace ThePenwickPapers
{


    public static class Settings
    {
        //mod settings
        public static bool EnableEnhancedInfo;
        public static bool EnableHerbalism;
        public static bool EnableTrapping;
        public static bool EnableLockpickGame;
        public static bool EnableBlinding;
        public static bool EnableChock;
        public static bool EnableDiversion;
        public static bool EnableTheBoot;
        public static bool EnablePeep;
        public static int  Mouse3Mode;
        public static int  Mouse4Mode;
        public static bool AddItems;
        public static bool AddSpells;

        public static bool AutoTeleportMinions;
        public static bool MinionsRegenerate;
        public static int MinionVolume = 2;
        public static bool StartGameWithPotionOfSeeking;
        public static bool EnableLootAdjustment;
        public static bool RelaxFreeHandRestriction;

        public static bool EnableGoverningAttributes;
        public static float SkillPerLevel = 15;


        //derived settings
        public static bool UsingHiResSprites;


        public static void Init(Mod mod)
        {
            ModSettings modSettings = mod.GetSettings();

            //Features
            string featuresSection = "Features";

            EnableEnhancedInfo = modSettings.GetBool(featuresSection, "EnhancedInfo");

            EnableHerbalism = modSettings.GetBool(featuresSection, "Herbalism");

            EnableTrapping = modSettings.GetBool(featuresSection, "Trapping");

            EnableLockpickGame = modSettings.GetBool(featuresSection, "LockpickMiniGame");

            EnableBlinding = modSettings.GetBool(featuresSection, "DirtyTricks-Blinding");
            EnableChock = modSettings.GetBool(featuresSection, "DirtyTricks-Chock");
            EnableDiversion = modSettings.GetBool(featuresSection, "DirtyTricks-Diversion");
            EnableTheBoot = modSettings.GetBool(featuresSection, "DirtyTricks-TheBoot");
            EnablePeep = modSettings.GetBool(featuresSection, "DirtyTricks-Peep");

            Mouse3Mode = modSettings.GetInt(featuresSection, "Mouse3");
            Mouse4Mode = modSettings.GetInt(featuresSection, "Mouse4");

            AddItems = modSettings.GetBool(featuresSection, "AddItems");
            AddSpells = modSettings.GetBool(featuresSection, "AddSpells");

            //Options
            string optionsSection = "Options";
            AutoTeleportMinions = modSettings.GetBool(optionsSection, "TeleportMinions");
            MinionsRegenerate = modSettings.GetBool(optionsSection, "MinionsRegenerate");
            MinionVolume = modSettings.GetInt(optionsSection, "MinionSoundVolume");
            StartGameWithPotionOfSeeking = modSettings.GetBool(optionsSection, "StartGameWithPotionOfSeeking");
            EnableLootAdjustment = modSettings.GetBool(optionsSection, "LootAdjustment");
            RelaxFreeHandRestriction = modSettings.GetBool(optionsSection, "RelaxFreeHandRestriction");

            //Advancement
            string advancementSection = "Advancement";
            EnableGoverningAttributes = modSettings.GetBool(advancementSection, "GoverningAttributes");
            SkillPerLevel = modSettings.GetInt(advancementSection, "SkillPerLevel");


            //Other settings derived from elsewhere
            UsingHiResSprites = ModManager.Instance.GetMod("DREAM - SPRITES") != null;

        }



    } //class Settings



} //namespace
