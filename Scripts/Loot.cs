// Project:      Enemy Loot, The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  June 2022

using System;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallConnect.FallExe;

namespace ThePenwickPapers
{

    public static class Loot
    {
        public static bool InDungeon;

        /// <summary>
        /// Adds appropriate items to the loot piles of certain entities.
        /// </summary>
        public static void AddItems(EnemyEntity entity, EnemyLootSpawnedEventArgs lootArgs)
        {
            if (entity.EntityBehaviour.gameObject.name.Contains("Penwick"))
                return; //summoned creatures don't need loot adjustment

            if (!Enum.IsDefined(typeof(MobileTypes), entity.MobileEnemy.ID))
                return;

            MobileTypes mobileType = (MobileTypes)entity.MobileEnemy.ID;

            switch (mobileType)
            {
                case MobileTypes.Assassin:
                    AddLightingItems(lootArgs.Items);
                    AddPotionOfSeekingItems(12 + entity.Level / 2, lootArgs.Items);
                    break;

                case MobileTypes.Burglar:
                    AddLightingItems(lootArgs.Items);
                    AddPotionOfSeekingItems(12 + entity.Level / 2, lootArgs.Items);
                    AddGrapplingHook(5 + entity.Level / 2, lootArgs.Items);
                    break;

                case MobileTypes.Thief:
                    AddLightingItems(lootArgs.Items);
                    AddPotionOfSeekingItems(entity.Level / 2, lootArgs.Items);
                    AddGrapplingHook(3 + entity.Level / 2, lootArgs.Items);
                    break;

                case MobileTypes.Acrobat:
                    AddLightingItems(lootArgs.Items);
                    AddGrapplingHook(3 + entity.Level / 2, lootArgs.Items);
                    break;

                case MobileTypes.Rogue:
                case MobileTypes.Nightblade:
                    AddLightingItems(lootArgs.Items);
                    break;

                case MobileTypes.Warrior:
                case MobileTypes.Knight:
                case MobileTypes.Archer:
                case MobileTypes.Ranger:
                    AddLightingItems(lootArgs.Items);
                    AddArrows(lootArgs.Items);
                    break;

                case MobileTypes.Healer:
                case MobileTypes.Barbarian:
                case MobileTypes.Monk:
                    AddLightingItems(lootArgs.Items);
                    //Herbalism healing ingredients
                    AddItem(28, lootArgs.Items, ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Elixir_vitae);
                    AddItem(24, lootArgs.Items, ItemGroups.PlantIngredients2, (int)PlantIngredients2.Aloe);
                    AddItem(27, lootArgs.Items, ItemGroups.PlantIngredients1, (int)PlantIngredients1.Root_bulb);
                    AddItem(15 + entity.Level, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Troll_blood);
                    break;

                case MobileTypes.Mage:
                case MobileTypes.Sorcerer:
                    //Herbalism magicka recovery ingredients
                    AddItem(27, lootArgs.Items, ItemGroups.MetalIngredients, (int)MetalIngredients.Silver);
                    AddItem(25, lootArgs.Items, ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Nectar);
                    AddItem(25, lootArgs.Items, ItemGroups.Gems, (int)Gems.Amber);
                    AddItem(entity.Level * 2, lootArgs.Items, ItemGroups.CreatureIngredients2, (int)CreatureIngredients2.Dragons_scales);
                    AddSoulTrap(entity.Level, lootArgs.Items, entity);
                    break;

                case MobileTypes.Bard:
                case MobileTypes.Battlemage:
                case MobileTypes.Spellsword:
                    AddSoulTrap(entity.Level - 8, lootArgs.Items, entity);
                    break;

                case MobileTypes.GiantScorpion:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients2, (int)CreatureIngredients2.Giant_scorpion_stinger);
                    break;

                case MobileTypes.Spider:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Spider_venom);
                    break;

                case MobileTypes.Mummy:
                    AddItem(50, lootArgs.Items, ItemGroups.CreatureIngredients2, (int)CreatureIngredients2.Mummy_wrappings);
                    break;

                case MobileTypes.Orc:
                case MobileTypes.OrcSergeant:
                case MobileTypes.OrcWarlord:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Orcs_blood);
                    break;

                case MobileTypes.OrcShaman:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Orcs_blood);
                    AddSoulTrap(9, lootArgs.Items, entity);
                    break;

                case MobileTypes.Lich:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Lich_dust);
                    AddSoulTrap(15, lootArgs.Items, entity);
                    break;

                case MobileTypes.AncientLich:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Lich_dust);
                    AddSoulTrap(20, lootArgs.Items, entity);
                    break;

                case MobileTypes.VampireAncient:
                case MobileTypes.DaedraLord:
                    AddSoulTrap(13, lootArgs.Items, entity);
                    break;

                case MobileTypes.SabertoothTiger:
                    AddItem(40, lootArgs.Items, ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Big_tooth);
                    break;

                case MobileTypes.Nymph:
                    AddItem(40, lootArgs.Items, ItemGroups.CreatureIngredients3, (int)CreatureIngredients3.Nymph_hair);
                    break;

                case MobileTypes.Wereboar:
                    AddItem(40, lootArgs.Items, ItemGroups.CreatureIngredients3, (int)CreatureIngredients3.Wereboar_tusk);
                    break;

                case MobileTypes.Werewolf:
                    AddItem(30, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Werewolfs_blood);
                    break;

                case MobileTypes.Ghost:
                    AddItem(35, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Ectoplasm);
                    break;

                case MobileTypes.Wraith:
                    AddItem(35, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Wraith_essence);
                    break;

                case MobileTypes.Giant:
                    AddItem(40, lootArgs.Items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Giant_blood);
                    break;

                default:
                    break;
            }

        }


        /// <summary>
        /// If chance roll succeeds, adds specified item to the item collection.
        /// </summary>
        static bool AddItem(int chance, ItemCollection items, ItemGroups group, int templateIndex)
        {
            if (Dice100.SuccessRoll(chance))
            {
                DaggerfallUnityItem item = ItemBuilder.CreateItem(group, templateIndex);
                items.AddItem(item);
                return true;
            }

            return false;
        }


        /// <summary>
        /// If chance roll succeeds, adds an empty or filled soul trap to the item collection
        /// </summary>
        static void AddSoulTrap(int chance, ItemCollection items, EnemyEntity entity)
        {
            if (Dice100.FailedRoll(chance))
                return;

            DaggerfallUnityItem item = ItemBuilder.CreateRandomlyFilledSoulTrap();
            int soulStrength = Reanimate.GetSoulValue(item.TrappedSoulType);

            if (Dice100.SuccessRoll(70) || soulStrength > 100 || soulStrength > 5 * entity.Level)
            {
                item.TrappedSoulType = MobileTypes.None;
            }

            items.AddItem(item);
        }


        /// <summary>
        /// If chance roll succeeds, adds potion of seeking or its ingredients or recipe to the item collection
        /// </summary>
        static void AddPotionOfSeekingItems(int chance, ItemCollection items)
        {
            int potionOfSeekingKey = ThePenwickPapersMod.Instance.GetPotionOfSeekingRecipeKey();

            if (Dice100.SuccessRoll(chance / 4))
            {
                DaggerfallUnityItem potionRecipe = new DaggerfallUnityItem(ItemGroups.MiscItems, 4) { PotionRecipeKey = potionOfSeekingKey };
                items.AddItem(potionRecipe);
            }

            if (Dice100.SuccessRoll(chance / 2))
            {
                DaggerfallUnityItem potion = ItemBuilder.CreatePotion(potionOfSeekingKey);
                items.AddItem(potion);
            }

            //might carry ingredients to make potion of seeking
            AddItem(chance, items, ItemGroups.PlantIngredients1, (int)PlantIngredients1.Root_tendrils);
            AddItem(chance, items, ItemGroups.PlantIngredients2, (int)PlantIngredients2.Black_poppy);
            AddItem(chance, items, ItemGroups.MetalIngredients, (int)MetalIngredients.Lodestone);
            AddItem(chance, items, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Basilisk_eye);
        }


        /// <summary>
        /// Chance of adding some lighting items (torches, lantern, oil) if in a dungeon
        /// </summary>
        static void AddLightingItems(ItemCollection items)
        {
            if (!InDungeon)
                return;

            if (Dice100.SuccessRoll(40))
            {
                DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Lantern);
                item.currentCondition = UnityEngine.Random.Range(0, 90);
                items.AddItem(item);

                while (Dice100.SuccessRoll(60))
                    AddItem(100, items, ItemGroups.UselessItems2, (int)UselessItems2.Oil);
            }
            else
            {
                while (Dice100.SuccessRoll(50))
                    AddItem(100, items, ItemGroups.UselessItems2, (int)UselessItems2.Torch);
            }
        }


        /// <summary>
        /// Chance of adding some arrows to loot ItemCollection
        /// </summary>
        static void AddArrows(ItemCollection items)
        {
            if (Dice100.SuccessRoll(60))
            {
                DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.Weapons, (int)Weapons.Arrow);
                item.stackCount = UnityEngine.Random.Range(2, 20);
                items.AddItem(item);
            }
        }


        /// <summary>
        /// Chance of adding grappling hook to loot ItemCollection
        /// </summary>
        static void AddGrapplingHook(int chance, ItemCollection items)
        {
            //First check if the HookAndRope item is registered
            ItemTemplate template = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(GrapplingHook.HookAndRopeItemIndex);
            if (template.name != null)
            {
                if (Dice100.SuccessRoll(chance))
                {
                    DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.MiscItems, GrapplingHook.HookAndRopeItemIndex);
                    items.AddItem(item);
                }
            }
        }



    } //class Loot



} //namespace
