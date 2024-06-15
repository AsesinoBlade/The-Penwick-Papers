// Project:     Skill Advancement, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Mar 2022

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop;

namespace ThePenwickPapers
{

    public class SkillAdvancement
    {
        static float lastAdvancementTime;
        static int skillIndex;
        static bool levelButtonChanged;
        static bool hasShownWarningMsg;

        static readonly Dictionary<DFCareer.Skills, double> adjustments = new Dictionary<DFCareer.Skills, double>()
        {
            { DFCareer.Skills.Alteration, 0.60 },
            { DFCareer.Skills.Archery, 1.00 },
            { DFCareer.Skills.Axe, 1.15 },
            { DFCareer.Skills.Backstabbing, 0.60 },
            { DFCareer.Skills.BluntWeapon, 1.15 },
            { DFCareer.Skills.Centaurian, 0.50 },
            { DFCareer.Skills.Climbing, 0.90 },
            { DFCareer.Skills.CriticalStrike, 0.90 },
            { DFCareer.Skills.Daedric, 0.50 },
            { DFCareer.Skills.Destruction, 1.10 },
            { DFCareer.Skills.Dodging, 0.85 },
            { DFCareer.Skills.Dragonish, 0.50 },
            { DFCareer.Skills.Etiquette, 0.80 },
            { DFCareer.Skills.Giantish, 0.50 },
            { DFCareer.Skills.HandToHand, 1.15 },
            { DFCareer.Skills.Harpy, 0.50 },
            { DFCareer.Skills.Illusion, 0.70 },
            { DFCareer.Skills.Impish, 0.50 },
            { DFCareer.Skills.Jumping, 0.85 },
            { DFCareer.Skills.Lockpicking, 0.50 },
            { DFCareer.Skills.LongBlade, 1.15 },
            { DFCareer.Skills.Medical, 0.80 },
            { DFCareer.Skills.Mercantile, 1.00 },
            { DFCareer.Skills.Mysticism, 0.60 },
            { DFCareer.Skills.Nymph, 0.50 },
            { DFCareer.Skills.Orcish, 0.50 },
            { DFCareer.Skills.Pickpocket, 0.80 },
            { DFCareer.Skills.Restoration, 0.80 },
            { DFCareer.Skills.Running, 1.00 },
            { DFCareer.Skills.ShortBlade, 1.25 },
            { DFCareer.Skills.Spriggan, 0.50 },
            { DFCareer.Skills.Stealth, 1.00 },
            { DFCareer.Skills.Streetwise, 0.80 },
            { DFCareer.Skills.Swimming, 0.50 },
            { DFCareer.Skills.Thaumaturgy, 0.60 },
        };



        /// <summary>
        /// FormulaHelper override to include governing stat when calculating skill advancement.
        /// </summary>
        public static int CalculateSkillUsesForAdvancement(int skillValue, int skillAdvancementMultiplier, float careerAdvancementMultiplier, int level)
        {
            DaggerfallEntity player = GameManager.Instance.PlayerEntity;

            //Hack: we aren't being passed the skill, so we will infer it based on available information.

            //This function is only occasionally called from PlayerEntity after a long rest or travel.
            //It will be called once for each skill in a loop.
            //All calls should occur in the same frame, so checking frame time.
            if (Time.time != lastAdvancementTime)
            {
                lastAdvancementTime = Time.time;
                skillIndex = 0;
            }
            else
            {
                ++skillIndex;
            }

            if (skillIndex >= (int)DFCareer.Skills.Count)
                skillIndex = 0; //count thrown off, probably a call from another mod


            DFCareer.Skills skill = (DFCareer.Skills)skillIndex;

            int testSkillValue = player.Skills.GetPermanentSkillValue(skill);
            int testAdvancementMultiplier = DaggerfallSkills.GetAdvancementMultiplier(skill);

            //verify values match
            if (testSkillValue != skillValue || testAdvancementMultiplier != skillAdvancementMultiplier)
            {
                if (hasShownWarningMsg == false)
                {
                    //showing the message once is enough
                    hasShownWarningMsg = true;
                    Debug.LogWarning("[The-Penwick-Papers]Warning: Mismatch on call to CalculateSkillUsesForAdvancement() suggests Mod incompatability, some displayed skill-progress values may be incorrect.");
                }

                //try best guess based on available data
                for (int i = 0; i < (int)DFCareer.Skills.Count; ++i)
                {
                    skill = (DFCareer.Skills)i;
                    testSkillValue = player.Skills.GetPermanentSkillValue(skill);
                    testAdvancementMultiplier = DaggerfallSkills.GetAdvancementMultiplier(skill);

                    if (testSkillValue == skillValue && testAdvancementMultiplier == skillAdvancementMultiplier)
                        break; //this is hopefully the right one
                }
            }


            DFCareer.Stats stat = DaggerfallSkills.GetPrimaryStat(skill);
            float statValue = Mathf.Clamp(player.Stats.GetPermanentStatValue(stat), 10, 100);

            //calculate the governing attribute multiplier
            double governingAttributeMultiplier = 1 / Mathf.Sqrt((statValue - 9f) / 50f);

            //check if the governing attribute is being exceeded, adds a soft ceiling
            double exceedingGoverningAttributeMultiplier = 1.0;
            if (skillValue >= statValue)
                exceedingGoverningAttributeMultiplier = 2.0;

            //some skill smoothing
            double skillSmoothingMultiplier = 1.0;
            if (adjustments.TryGetValue(skill, out double dictValue))
                skillSmoothingMultiplier = dictValue;
            else
                Debug.LogWarningFormat("Penwick SkillAdvancement missing entry for skill {0}", skill.ToString());


            //starting with the same basic formula used in FormulaHelper
            double levelMod = Math.Pow(1.04, level);
            double basic = (skillValue * skillAdvancementMultiplier * careerAdvancementMultiplier * levelMod * 2 / 5) + 1;
            basic /= Settings.BonusToGoverningAttributes;
            double modAdjustment = governingAttributeMultiplier * skillSmoothingMultiplier * exceedingGoverningAttributeMultiplier;

            double total = basic * modAdjustment;

            return (int)Math.Floor(total);
        }


        /// <summary>
        /// FormulaHelper override to adjust character advancement rate.
        /// </summary>
        public static int CalculatePlayerLevel(int startingLevelUpSkillsSum, int currentLevelUpSkillsSum)
        {
            return (int)Mathf.Floor((currentLevelUpSkillsSum - startingLevelUpSkillsSum + 28) / Settings.SkillPerLevel);
        }



        /// <summary>
        /// Attempts to replace the Level button on the Character sheet window to override the button click method
        /// </summary>
        public static void CheckAddLevelButton()
        {
            IUserInterfaceWindow window = DaggerfallUI.UIManager.TopWindow;
            if (window is DaggerfallCharacterSheetWindow && levelButtonChanged == false)
            {
                levelButtonChanged = true;

                DaggerfallCharacterSheetWindow characterSheetWindow = window as DaggerfallCharacterSheetWindow;

                Vector2 levelButtonPos = new Vector2(4, 33);
                Vector2 levelButtonSize = new Vector2(132, 8);

                bool found = false;

                foreach (BaseScreenComponent component in characterSheetWindow.NativePanel.Components)
                {
                    if (component is Button && component.Position == levelButtonPos)
                    {
                        characterSheetWindow.NativePanel.Components.Remove(component);
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    Button levelButton = DaggerfallUI.AddButton(levelButtonPos, levelButtonSize, characterSheetWindow.NativePanel);

                    levelButton.OnMouseClick += LevelButton_OnMouseClick;
                    levelButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.CharacterSheetLevel);
                }

            }

        }


        /// <summary>
        /// Activated when the Level button on Character sheet window is clicked
        /// </summary>
        static void LevelButton_OnMouseClickOld(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            //same formula as in original character sheet window, but using variable SkillPerLevel
            float currentLevel = (player.CurrentLevelUpSkillSum - player.StartingLevelUpSkillSum + 28f) / Settings.SkillPerLevel;
            int progress = (int)((currentLevel % 1) * 100);

            DaggerfallUI.MessageBox(string.Format(TextManager.Instance.GetLocalizedText("levelProgress"), progress));
        }

        static void LevelButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            float currentLevel = (player.CurrentLevelUpSkillSum - player.StartingLevelUpSkillSum + 28f) / Settings.SkillPerLevel;
            int progress = (int)((currentLevel % 1) * 100);
            float nextLevel =  ((int)currentLevel + 1) * Settings.SkillPerLevel -
                            ((player.CurrentLevelUpSkillSum - player.StartingLevelUpSkillSum + 28));
            var str = $"Progress made to the next level: {progress}%.  {nextLevel:F1} skill advancements needed for promotion.";
            str +=
                $"\r\rCurrentLevel = {currentLevel:F2}  CurrentLevelupSkill ={player.CurrentLevelUpSkillSum}, startingLevelUpSKill = {player.StartingLevelUpSkillSum}";
            str += $"\r\rCurrentLevel=(CurrrentLevelupSkill - startingLevelupSkill + 28)/{Settings.SkillPerLevel}";
            str += "\rcurrentSkill = all primary + all but lowest major + single highest minor";
            DaggerfallUI.MessageBox(str);

        }




    } //class SkillAdvancement



} //namespace
