// Project:     EncumbranceDetails, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Aug 2022

using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace ThePenwickPapers
{

    public static class EncumbranceDetails
    {
        static readonly Dictionary<IUserInterfaceWindow, MultiFormatTextLabel> modifiedWindows;

        static float maxTextWidth = 0;
        static float spaceWidth;

        struct Weight
        {
            public float weapons;
            public float armor;
            public float ingredients;
            public float other;
            public float gold;
        }


        static EncumbranceDetails()
        {
            modifiedWindows = new Dictionary<IUserInterfaceWindow, MultiFormatTextLabel>();
        }

            
        /// <summary>
        /// Adds encumbrance button to Character screen and mouse event to Inventory window backpack if needed.
        /// </summary>
        public static void CheckAddComponents()
        {
            IUserInterfaceWindow window = DaggerfallUI.UIManager.TopWindow;

            if (window is DaggerfallCharacterSheetWindow && !modifiedWindows.ContainsKey(window))
            {
                DaggerfallCharacterSheetWindow charSheetWindow = window as DaggerfallCharacterSheetWindow;
                if (charSheetWindow.IsSetup)
                {
                    modifiedWindows.Add(charSheetWindow, null);
                    AddEncumbranceButton(charSheetWindow);
                }
            }
            else if (window is DaggerfallInventoryWindow && !modifiedWindows.ContainsKey(window))
            {
                DaggerfallInventoryWindow inventoryWindow = window as DaggerfallInventoryWindow;
                if (inventoryWindow.IsSetup)
                {
                    modifiedWindows.Add(inventoryWindow, null);
                    AddEncumbranceBackpackHover(inventoryWindow);
                }
            }
        }


        /// <summary>
        /// Adds encumbrance button to Character screen
        /// </summary>
        static void AddEncumbranceButton(DaggerfallCharacterSheetWindow characterSheetWindow)
        {
            // Adding invisible clickable button over the 'Encumbrance' text label
            Button encumbranceButton = DaggerfallUI.AddButton(new Rect(4, 73, 132, 8), characterSheetWindow.NativePanel);

            encumbranceButton.OnMouseClick += EncumbranceButton_OnMouseClick;
        }


        /// <summary>
        /// Adds OnMouseEnter event handler to backpack panel of Inventory window and gets infoPanel reference
        /// </summary>
        static void AddEncumbranceBackpackHover(DaggerfallInventoryWindow inventoryWindow)
        {
            Texture2D backpack = DaggerfallUnity.Instance.ItemHelper.GetContainerImage(InventoryContainerImages.Backpack).texture;

            MultiFormatTextLabel infoLabel = null;

            foreach (BaseScreenComponent component in inventoryWindow.NativePanel.Components)
            {
                if (component is Panel)
                {
                    Panel panel = component as Panel;
                    if (panel.BackgroundTexture == backpack)
                        panel.OnMouseEnter += Backpack_OnMouseEnter;
                    else if (panel.Components.Count > 1 && panel.Components[1] is MultiFormatTextLabel)
                        infoLabel = panel.Components[1] as MultiFormatTextLabel;
                }
            }

            if (infoLabel != null)
                modifiedWindows[inventoryWindow] = infoLabel;
        }


        /// <summary>
        /// Activated when the Encumbrance button on Character sheet window is clicked
        /// </summary>
        static void EncumbranceButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            DaggerfallMessageBox encumbranceBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            List<TextFile.Token> tokens = CreateEncumbranceText();

            encumbranceBox.SetTextTokens(tokens.ToArray(), null, false);

            encumbranceBox.ClickAnywhereToClose = true;

            encumbranceBox.Show();
        }


        /// <summary>
        /// Activated when mouse cursor moves over Backpack panel on Inventory window
        /// </summary>
        static void Backpack_OnMouseEnter(BaseScreenComponent sender)
        {
            MultiFormatTextLabel infoLabel = modifiedWindows[DaggerfallUI.UIManager.TopWindow];

            if (infoLabel != null)
            {
                List<TextFile.Token> tokens = CreateEncumbranceText();
                infoLabel.SetText(tokens.ToArray());
            }
        }


        /// <summary>
        /// Creates text tokens that list weights in various categories (armor, weapons, ingredients, other, gold)
        /// </summary>
        static List<TextFile.Token> CreateEncumbranceText()
        {
            Weight weight = CollectEncumbranceData();

            CalculateFontMetrics();

            List<TextFile.Token> list = new List<TextFile.Token>
            {
                CreateEntry(Text.WeaponWeight, weight.weapons),
                TextFile.NewLineToken,
                CreateEntry(Text.ArmorWeight, weight.armor),
                TextFile.NewLineToken,
                CreateEntry(Text.IngredientWeight, weight.ingredients),
                TextFile.NewLineToken,
                CreateEntry(Text.OtherWeight, weight.other),
                TextFile.NewLineToken,
                CreateEntry(Text.GoldWeight, weight.gold)
            };

            return list;
        }


        /// <summary>
        /// Create a line of text describing weight entry, with appropriate padding
        /// </summary>
        static TextFile.Token CreateEntry(Text text, float wgt = -1)
        {
            float width = CalculateTextWidth(text.Get());

            StringBuilder entry = new StringBuilder();
            entry.Append(text.Get());

            while (width < maxTextWidth)
            {
                width += spaceWidth;
                entry.Append(' ');
            }

            entry.Append(' ');

            if (wgt >= 0)
                entry.Append(string.Format("{0:F}", wgt));

            //If not using the fixed-width pixelated fonts, tack on the 'kg'
            if (DaggerfallUnity.Settings.SDFFontRendering == true)
                entry.Append("  kg");

            return TextFile.CreateTextToken(entry.ToString());
        }


        /// <summary>
        /// Calculates the width of the various text labels; used for padding purposes.
        /// </summary>
        static void CalculateFontMetrics()
        {
            if (spaceWidth > 0)
                return; //already calculated

            float[] values = {
                CalculateTextWidth(Text.WeaponWeight.Get()),
                CalculateTextWidth(Text.ArmorWeight.Get()),
                CalculateTextWidth(Text.IngredientWeight.Get()),
                CalculateTextWidth(Text.OtherWeight.Get()),
                CalculateTextWidth(Text.GoldWeight.Get())
            };

            maxTextWidth = values.Max();

            spaceWidth = CalculateTextWidth(" ");
        }


        static float CalculateTextWidth(string text)
        {
            return DaggerfallUI.DefaultFont.CalculateTextWidth(text, Vector2.one);
        }


        /// <summary>
        /// Iterates over the player's inventory, summing the weights of items in each category.
        /// </summary>
        static Weight CollectEncumbranceData()
        {
            Weight weight = default;

            weight.gold = GameManager.Instance.PlayerEntity.GoldPieces * DaggerfallBankManager.goldUnitWeightInKg;

            ItemCollection items = GameManager.Instance.PlayerEntity.Items;

            for (int i = 0; i < items.Count; ++i)
            {
                DaggerfallUnityItem item = items.GetItem(i);
                float wgt = item.stackCount * item.EffectiveUnitWeightInKg();

                if (item.IsIngredient && !item.IsEnchanted)
                    weight.ingredients += wgt;
                else if (item.ItemGroup == ItemGroups.Weapons)
                    weight.weapons += wgt;
                else if (item.ItemGroup == ItemGroups.Armor)
                    weight.armor += wgt;
                else
                    weight.other += wgt;
            }

            return weight;
        }



    } //class


} //namespace
