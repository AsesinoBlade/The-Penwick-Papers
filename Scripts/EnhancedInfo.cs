// Project:     Enhanced Info, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Questing.Actions;
using DaggerfallWorkshop.Utility;

using static DaggerfallConnect.Arena2.FactionFile.SocialGroups;
using static DaggerfallConnect.Arena2.FactionFile.GuildGroups;
using static DaggerfallConnect.DFLocation.BuildingTypes;
using DaggerfallWorkshop.Game.Weather;


namespace ThePenwickPapers
{

    public static class EnhancedInfo
    {
        class NpcData
        {
            public string Name;
            public Genders Gender;
            public Races Race;
            public bool IsRandomRuler;
            public bool IsRandomNoble;
            public bool IsRandomKnight;
            public bool IsChild;
            public bool IsUniqueIndividual;
            public int TextureArchive;
            public int TextureRecord;
            public FactionFile.FactionIDs FactionID;
            public FactionFile.FactionIDs IndividualFactionID;
            public string FactionName;
            public FactionFile.SocialGroups Social;
            public FactionFile.GuildGroups Guild;
            public bool IsLeafFaction;
            public bool IsCowled;
            public bool IsQuestGiver;
            public bool IsQuestNPC;
            public int Reaction;
            public string Descriptor;
            public string Disposition;
            public MobilePersonNPC mobile;
        };


        //for our purposes, shops include banks and taverns, does not include library
        static readonly HashSet<DFLocation.BuildingTypes> shops = new HashSet<DFLocation.BuildingTypes>()
        {
            Alchemist, Armorer, Bank, Bookseller, ClothingStore, FurnitureStore, GemStore,
            GeneralStore, PawnShop, WeaponSmith, Tavern
        };

        /// <summary>
        /// Shows enhanced information about activated enemies or NPCs in HUD text.
        /// </summary>
        public static bool ShowEnhancedEntityInfo(RaycastHit hitInfo)
        {
            EnemySenses senses = hitInfo.transform.GetComponent<EnemySenses>();
            DaggerfallLoot loot = hitInfo.transform.GetComponent<DaggerfallLoot>();
            DaggerfallEntityBehaviour behaviour = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();
            StaticNPC staticNPC = hitInfo.transform.GetComponent<StaticNPC>();
            MobilePersonNPC mobileNPC = hitInfo.transform.GetComponent<MobilePersonNPC>();

            if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Info)
            {
                if (Utility.IsBlind(GameManager.Instance.PlayerEntityBehaviour))
                {
                    Utility.AddHUDText(Text.YouSeeNothing.Get());
                    return true;
                }
                else if (senses)
                {
                    ShowCreatureInfo(behaviour);
                    return true;
                }
                else if (staticNPC)
                {
                    ShowStaticNPCInfo(staticNPC);
                    return true;
                }
                else if (mobileNPC)
                {
                    ShowMobileNPCInfo(mobileNPC);
                    return true;
                }
                else if (loot && loot.ContainerType == LootContainerTypes.ShopShelves)
                {
                    // Extract year and day of year from now
                    int nowYear = DaggerfallUnity.Instance.WorldTime.Now.Year;
                    int nowDayOfYear = DaggerfallUnity.Instance.WorldTime.Now.DayOfYear;

                    // Extract year and day of year from stockedDate
                    int stockDateYear = loot.stockedDate / 1000;
                    int stockDateDayOfYear = loot.stockedDate % 1000;

                    // Convert both dates to total days since year 0
                    int nowTotalDays = nowYear * DaggerfallDateTime.DaysPerYear + nowDayOfYear;
                    int stockDateTotalDays = stockDateYear * DaggerfallDateTime.DaysPerYear + stockDateDayOfYear;

                    // Calculate the difference in days
                    int daysDifference = stockDateTotalDays - nowTotalDays;

                    if (daysDifference < 0 || loot.stockedDate <= 0)
                        DaggerfallUI.AddHUDText($"That is new inventory, have a look.");
                    else if (daysDifference == 0)
                    {
                        var salePercent = ThePenwickPapersMod.GetSalePrice(loot);
                        if (salePercent > 0)
                            DaggerfallUI.AddHUDText($"Everything is marked down {salePercent}%; I'm replacing it all tomorrow.", 2f);
                        else
                            DaggerfallUI.AddHUDText($"Buy up now if you want anything; I'm replacing it tomorrow.", 2f);
                    }
                    else
                    {
                        var salePercent = ThePenwickPapersMod.GetSalePrice(loot);
                        if (salePercent > 0)
                            DaggerfallUI.AddHUDText($"Everything is marked down {salePercent}%; I'm replacing it all in {daysDifference + 1} days.", 2f);
                        else if (Settings.AlwaysDisplayNumberOfDaysToRestocking)
                            DaggerfallUI.AddHUDText(
                                $"Bear in mind, new shipments for this shelf arrive in {daysDifference + 1} days.", 2f);
                    }
                }
            }

            //Input action will not be swallowed, it will be handled by other activation
            //code elsewhere (likely PlayerActivate)
            return false;
        }


        /// <summary>
        /// Called by Herbalism, shows enhanced information about activated patient in HUD text.
        /// </summary>
        public static void ShowPatientInfo(DaggerfallEntityBehaviour patient)
        {
            if (patient.GetComponent<EnemySenses>())
                ShowCreatureInfo(patient);
        }


        static readonly Text[] livingStatus = new Text[] {
            Text.Savaged, Text.Bloody, Text.Wounded, Text.Grazed, Text.Healthy
        };
        static readonly Text[] golemStatus = new Text[] {
            Text.Mangled, Text.Battered, Text.Damaged, Text.Tarnished, Text.Pristine
        };

        /// <summary>
        /// Shows enhanced information about activated enemies in HUD text.
        /// </summary>
        public static void ShowCreatureInfo(DaggerfallEntityBehaviour creature)
        {
            DaggerfallEntityBehaviour player = GameManager.Instance.PlayerEntityBehaviour;
            bool hasBestiary = GameManager.Instance.PlayerEntity.Items.Contains(ItemGroups.UselessItems2, 900);
            bool enhancedInfo = Input.GetKey(KeyCode.LeftShift);
            EnemyEntity entity = creature.Entity as EnemyEntity;
            MobileEnemy mobileEnemy = entity.MobileEnemy;
            EnemyMotor motor = creature.GetComponent<EnemyMotor>();
            EnemySenses senses = creature.GetComponent<EnemySenses>();

            if (entity.IsInvisible)
                return;

            string creatureName = TextManager.Instance.GetLocalizedEnemyName(mobileEnemy.ID);

            bool golem = mobileEnemy.Affinity == MobileAffinity.Undead || mobileEnemy.Affinity == MobileAffinity.Golem;
            if (mobileEnemy.ID == (int)MobileTypes.Vampire || mobileEnemy.ID == (int)MobileTypes.VampireAncient)
            {
                golem = false;  //Will treat vampires as living for descriptive purposes
            }
            
            int index = Mathf.Clamp((int)(entity.CurrentHealthPercent * 4), 0, 4);
            Text[] statusWords = golem ? golemStatus : livingStatus;
            string status = statusWords[index].Get();

            //check for active effects
            status += GetActiveEffectDescriptions(creature);


            string disposition;
            if (entity.Team == MobileTeams.PlayerAlly)
            {
                disposition = golem ? Text.Obedient.Get() : Text.Friendly.Get();
            }
            else if (motor.IsHostile)
            {
                if (senses.Target == player || senses.SecondaryTarget == player)
                {
                    if (mobileEnemy.Affinity == MobileAffinity.Animal)
                        disposition = Text.Hungry.Get();
                    else
                        disposition = Text.Hostile.Get();
                }
                else if (senses.Target == null)
                    disposition = Text.Unaware.Get();
                else
                    disposition = Text.Distracted.Get();
            }
            else
            {
                disposition = Text.Neutral.Get();
            }

            int minPossibleHealth = GameObjectHelper.EnemyDict[mobileEnemy.ID].MinHealth;
            int maxPossibleHealth = GameObjectHelper.EnemyDict[mobileEnemy.ID].MaxHealth;
            bool hasHighRange = maxPossibleHealth > (minPossibleHealth * 2);
            int healthRangeForCreatureType = maxPossibleHealth - minPossibleHealth;
            bool tough = entity.MaxHealth > maxPossibleHealth - (healthRangeForCreatureType / 3);
            bool weak = entity.MaxHealth <= minPossibleHealth + (healthRangeForCreatureType / 4);
            bool superior = entity.MaxHealth > maxPossibleHealth && entity.EntityType == EntityTypes.EnemyMonster;
            string info;

            if (creature.gameObject.name.Contains("Decoy") && entity.Team == MobileTeams.PlayerAlly)
            {
                info = Text.YouSeeYourDecoy.Get();
            }
            else if (superior)
            {
                //this can occur for player summoned creatures, or if another mod modifies the monster
                info = Text.YouSeeAbnormalCreature.Get(status, Text.Superior.Get(), creatureName, disposition);
            }
            else if (hasHighRange && tough)
            {
                info = Text.YouSeeAbnormalCreature.Get(status, Text.Stout.Get(), creatureName, disposition);
            }
            else if (hasHighRange && weak)
            {
                info = Text.YouSeeAbnormalCreature.Get(status, Text.Frail.Get(), creatureName, disposition);
            }
            else if (entity.IsInvisible)
            {
                return;
            }
            else if (entity.IsMagicallyConcealed)
            {
                info = Text.YouSeeVagueThing.Get();
            }
            else
            {
                info = Text.YouSeeACreature.Get(status, creatureName, disposition);
            }

            var str = string.Empty;
            float percentDamage = entity.CurrentHealthPercent * 100;
            if (percentDamage > 0)
            {

                if (percentDamage > 90)
                    str = "Healthy";
                else if (percentDamage > 80)
                    str = "Lightly Wounded";
                else if (percentDamage > 60)
                    str = "Wounded";
                else if (percentDamage > 40)
                    str = "Severely Wounded";
                else if (percentDamage > 20)
                    str = "Critically Wounded";
                else
                    str = "Nearly Dead";
                info += $"{(hasBestiary ? "\n" : "\r")}{creatureName} appears to be {str}.";
            }

            float lighting = ThePenwickPapersMod.Instance.GetEntityLighting(creature).grayscale;

            if (lighting < 0.03f)
            {
                Vector3 background = GetTargetBackground(creature);
                float bgLighting = ThePenwickPapersMod.Instance.GetLocationLighting(background).grayscale;
                float gropeLightRange = ThePenwickPapersMod.Instance.GetGropeLightRange();

                if (bgLighting > 0.03f)
                    info = Text.YouSeeSilhouette.Get(creatureName);
                else if (senses.DistanceToPlayer < gropeLightRange)
                    info = Text.YouSenseSomething.Get();
                else
                    return;
            }

            if ((hasBestiary || !Settings.RequireBestiaryBook) && enhancedInfo)
            {
                info += GetAbilities(creature);
                DaggerfallUI.Instance.BookReaderWindow.CreateBook(info);
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenBookReaderWindow);
            }
            else
            {
                Utility.AddHUDText(info);
            }
        }

        public static bool CastsMagic(MobileEnemy eMobileEnemy)
        {
            if (eMobileEnemy.CastsMagic)
                return true;
            if (eMobileEnemy.HasSpellAnimation || (eMobileEnemy.SpellAnimFrames != null && eMobileEnemy.SpellAnimFrames.Length > 0))
                return true;
            return false;
        }

        private static string GetAbilities(DaggerfallEntityBehaviour creature)
        {
            DFCareer career;
            var str = "";
            var entity = creature.Entity as EnemyEntity;
            if (entity == null)
                return str;

            var index = entity.CareerIndex;
            career = null;
            career = DaggerfallEntity.GetMonsterCareerTemplate((MonsterCareers)index);
            if (career == null)
                career = DaggerfallEntity.GetCustomCareerTemplate(entity.MobileEnemy.ID);
            if (career == null)
                return str;

            str += "\n\n[/center][/scale=1.5][/color=ff0000]Vitals:\n";
            str += $"[/left][/scale=1][/color=00ff00][/scale=1]Current Health:  {entity.CurrentHealth} out of {entity.MaxHealth}\n";
            str += $"Current Magicka:  {entity.CurrentMagicka} out of {entity.MaxMagicka}\n";
            str += $"Immune to Metals below: {entity.MinMetalToHit.ToString()}\n";
            str += $"Casts Magic: {CastsMagic(entity.MobileEnemy)}\n";

            str += "\n\n[/center][/scale=1.5][/color=ff0000]Stats:\n";
            str += $"[/left][/scale=1][/color=00ff00][/scale=1]Strength: {career.Strength}\n";
            str += $"Intelligence: {career.Intelligence}\n";
            str += $"Willpower: {career.Willpower}\n";
            str += $"Agility: {career.Agility}\n";
            str += $"Endurance: {career.Endurance}\n";
            str += $"Personality: {career.Personality} \n";
            str += $"Speed: {career.Speed}\n";
            str += $"Luck: {career.Luck}\n";
            str += "\n\n[/center][/scale=1.5][/color=ff0000]Tolerances:\n";
            str += $"[/scale=1][/left][/color=00ff00]Paralysis: {career.Paralysis.ToString()}\n";
            str += $"Poison: {career.Poison.ToString()}\n";
            str += $"Shock: {career.Shock.ToString()}\n";
            str += $"Disease: {career.Disease.ToString()}\n";
            str += $"Magic: {career.Magic.ToString()}\n";
            str += $"Fire: {career.Fire.ToString()}\n";
            str += $"Frost: {career.Frost.ToString()}\n";
            str += $"Disease: {career.Disease.ToString()}\n";
            
            return str;
        }


        /// <summary>
        /// Get's location of background terrain in line-of-sight behind target.
        /// </summary>
        static Vector3 GetTargetBackground(DaggerfallEntityBehaviour target)
        {
            // Set origin of ray to approximate player eye position
            Vector3 eyePos = GameManager.Instance.MainCamera.transform.position;

            // Set destination to the target's upper body
            CharacterController controller = target.GetComponent<CharacterController>();
            Vector3 targetTorsoPos = target.transform.position + controller.center;
            targetTorsoPos.y += controller.height / 3f;

            Vector3 eyeDirectionToTarget = (targetTorsoPos - eyePos).normalized;

            int targetMask = ~(1 << target.gameObject.layer);

            //Looking past target toward background.
            Ray ray = new Ray(targetTorsoPos, eyeDirectionToTarget);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, targetMask))
                return hit.point - (eyeDirectionToTarget * 0.01f);
            else
                return Vector3.zero; //This might happen if outside looking at sky.
        }


        /// <summary>
        /// Gets text related to any condition status, i.e. burning, paralyzed, etc.
        /// </summary>
        static string GetActiveEffectDescriptions(DaggerfallEntityBehaviour creature)
        {
            const string comma = ", ";

            StringBuilder effectText = new StringBuilder();

            bool fortified = false;
            bool drained = false;
            bool poisoned = false;

            EntityEffectManager creatureEffects = creature.GetComponent<EntityEffectManager>();
            LiveEffectBundle[] bundles = creatureEffects.EffectBundles;
            foreach (LiveEffectBundle bundle in bundles)
            {
                foreach (IEntityEffect effect in bundle.liveEffects)
                {
                    if (effect is ContinuousDamageHealth)
                    {
                        if (bundle.elementType == ElementTypes.Poison)
                        {
                            poisoned = true;
                            continue;
                        }
                        
                        effectText.Append(comma);

                        switch (bundle.elementType)
                        {
                            case ElementTypes.Cold: effectText.Append(Text.Freezing.Get()); break;
                            case ElementTypes.Fire: effectText.Append(Text.Burning.Get()); break;
                            case ElementTypes.Shock: effectText.Append(Text.Shocked.Get()); break;
                            default: effectText.Append(Text.Suffering.Get()); break;
                        }
                    }
                    else if (effect is ContinuousDamageFatigue || effect is ContinuousDamageSpellPoints)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.Weakening.Get());
                    }
                    else if (effect is PoisonEffect)
                    {
                        poisoned = true;
                    }
                    else if (effect is Paralyze)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.Paralyzed.Get());
                    }
                    else if (effect is Silence)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.Silenced.Get());
                    }
                    else if (effect is SoulTrap)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.SoulTrapped.Get());
                    }
                    else if (effect is FortifyEffect)
                    {
                        fortified = true;
                    }
                    else if (effect is DrainEffect || effect is TransferEffect)
                    {
                        drained = true;
                    }
                    else if (effect is Regenerate)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.Healing.Get());
                    }
                    else if (effect is Shield)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.Shielded.Get());
                    }
                    else if (effect is Blind)
                    {
                        effectText.Append(comma);
                        effectText.Append(Text.Blinded.Get().ToLower());
                    }
                }
            }

            if (fortified)
            {
                effectText.Append(comma);
                effectText.Append(Text.Fortified.Get());
            }

            if (drained)
            {
                effectText.Append(comma);
                effectText.Append(Text.Drained.Get());
            }

            if (poisoned)
            {
                effectText.Append(comma);
                effectText.Append(Text.Poisoned.Get());
            }

            if (creature.Entity.CurrentFatigue < 2)
            {
                effectText.Append(comma);
                effectText.Append(Text.Exhausted.Get());
            }

            return effectText.ToString();
        }


        /// <summary>
        /// Produces HUD text related to static NPC faction and disposition to player.
        /// </summary>
        static void ShowStaticNPCInfo(StaticNPC staticNPC)
        {
            int factionID = staticNPC.Data.factionID;
            if (factionID < 0 || factionID >= 4096)
                factionID = 0; //certain people, like palace guards, might not have a proper faction set

            GetStaticNPCFactionData(factionID, GameManager.Instance.PlayerEnterExit.BuildingType, out FactionFile.FactionData factionData);

            NpcData npc = new NpcData
            {
                Name = staticNPC.DisplayName,
                Gender = staticNPC.Data.gender,
                Race = staticNPC.Data.race,
                IsChild = staticNPC.IsChildNPC,
                TextureArchive = staticNPC.Data.billboardArchiveIndex,
                TextureRecord = staticNPC.Data.billboardRecordIndex,

                IsRandomRuler = factionID == (int)FactionFile.FactionIDs.Random_Ruler,
                IsRandomNoble = factionID == (int)FactionFile.FactionIDs.Random_Noble,
                IsRandomKnight = factionID == (int)FactionFile.FactionIDs.Random_Knight,

                FactionID = (FactionFile.FactionIDs)factionData.id,
                FactionName = factionData.name,
                Social = (FactionFile.SocialGroups)factionData.sgroup,
                Guild = (FactionFile.GuildGroups)factionData.ggroup,
                IsLeafFaction = factionData.children == null || factionData.children.Count == 0
            };

            npc.IsCowled = IsCowledFigure(npc);

            npc.Reaction = GetStaticNPCReaction(factionData);

            npc.IsQuestGiver = IsOfferingWork(staticNPC);
            npc.IsQuestNPC = IsQuestNPC(staticNPC);

            if (factionData.ggroup == (int)FactionFile.GuildGroups.None && factionData.parent != 0)
            {
                //use parent faction data for unique individuals
                npc.IsUniqueIndividual = true;
                npc.IndividualFactionID = npc.FactionID;
                GameManager.Instance.PlayerEntity.FactionData.GetFactionData(factionData.parent, out factionData);
                npc.FactionID = (FactionFile.FactionIDs)factionData.id;
                npc.FactionName = factionData.name;
                npc.Guild = (FactionFile.GuildGroups)factionData.ggroup;
            }


            npc.Descriptor = GetDescriptor(npc);
            if (npc.IsRandomRuler || npc.IsRandomNoble || npc.IsRandomKnight)
            {
                if (npc.Descriptor != null && npc.Descriptor.Length > 0)
                    npc.Descriptor += " ";
                npc.Descriptor += GetTitle(npc);
            }

            npc.Disposition = GetDisposition(npc).Get();

            string info;
            if (ShouldShowMember(npc))
            {
                //Members of Guilds, Temples (Knightly Orders if outside Guild walls)
                string order = npc.FactionName;
                if (npc.Descriptor == null || npc.Descriptor.Length == 0)
                    info = Text.YouSeeUndescribedMemberNPC.Get(npc.Name, order, npc.Disposition);
                else
                    info = Text.YouSeeMemberNPC.Get(npc.Descriptor, npc.Name, order, npc.Disposition);
            }
            else if (npc.Descriptor != null && npc.Descriptor.Length > 0)
            {
                info = Text.YouSeeDescribedNPC.Get(npc.Descriptor, npc.Name, npc.Disposition);
            }
            else if (npc.Social == SupernaturalBeings)
            {
                info = Text.YouWitnessNPC.Get(staticNPC.DisplayName, npc.Disposition);
            }
            else
            {
                //A known or cowled individual (i.e. royalty et.)
                info =Text.YouSeeFamiliarNPC.Get(staticNPC.DisplayName, npc.Disposition);

            }

            Utility.AddHUDText(info);
            if (npc.IsQuestNPC || npc.IsQuestGiver)
            {
                info = npc.Gender == Genders.Male ? "Hmm, Something is on his mind..." : "Hmm, Something is on her mind...";
                Utility.AddHUDText(info);
            }

            if (!Settings.GiveTalkToneTip)
                return;

            Utility.AddHUDText(GetTalkToneTipMessage(npc));
        }


        /// <summary>
        /// Checks if graphic for NPC is of a cloaked/cowled figure.
        /// A cowled npc should likely not show a description or disposition.
        /// </summary>
        static bool IsCowledFigure(NpcData npc)
        {
            if (npc.TextureArchive == 176 && npc.TextureRecord == 5)
            {
                return true;
            }
            else if (npc.TextureArchive == 177 && npc.TextureRecord == 4)
            {
                return true;
            }
            else if (npc.TextureArchive == 178 && npc.TextureRecord == 4)
            {
                //King of Worms
                return true;
            }

            return false;
        }


        /// <summary>
        /// Should member status be shown for NPCs that are guild officers.
        /// </summary>
        static bool ShouldShowMember(NpcData npc)
        {
            if (npc.IsUniqueIndividual)
            {
                return false;
            }
            else if (npc.Guild == KnightlyOrder && npc.FactionID != FactionFile.FactionIDs.The_Blades)
            {
                //Knightly order
                //Don't show title for The Blades as they are covert
                //Don't bother showing full title if inside guild hall
                DFLocation.BuildingTypes buildingType = GameManager.Instance.PlayerEnterExit.BuildingType;
                return npc.IsLeafFaction && buildingType != GuildHall;
            }
            else if (npc.Social == Commoners && npc.Guild == FightersGuild)
            {
                //Fighter's guild
                return npc.IsLeafFaction;
            }
            else if (npc.Social == Scholars && npc.Guild == MagesGuild)
            {
                //Mage's guild
                return npc.IsLeafFaction;
            }
            else if (npc.Social == Underworld && npc.Guild == DarkBrotherHood)
            {
                //Dark Brotherhood guild
                bool isMember = GameManager.Instance.GuildManager.HasJoined(FactionFile.GuildGroups.DarkBrotherHood);
                return npc.IsLeafFaction && isMember;
            }
            else if (npc.Social == Underworld && npc.Guild == GeneralPopulace)
            {
                //Thieves guild
                bool isMember = GameManager.Instance.GuildManager.HasJoined(FactionFile.GuildGroups.GeneralPopulace);
                return npc.IsLeafFaction && isMember;
            }
            else if (npc.Social == GuildMembers && npc.Guild == HolyOrder)
            {
                //Temple
                return npc.IsLeafFaction;
            }

            return false;
        }


        /// <summary>
        /// Produces HUD text related to mobile NPC faction and disposition to player.
        /// </summary>
        static void ShowMobileNPCInfo(MobilePersonNPC mobileNPC)
        {
            if (mobileNPC.IsGuard)
            {
                Utility.AddHUDText(Text.YouSeeTheNPCGuard.Get(mobileNPC.NameNPC));
            }
            else
            {
                // All mobile NPCs use "People of" current region faction
                int factionId = GameManager.Instance.PlayerGPS.GetPeopleOfCurrentRegion();

                GameManager.Instance.PlayerEntity.FactionData.GetFactionData(factionId, out FactionFile.FactionData faction);

                NpcData npc = new NpcData
                {
                    mobile = mobileNPC,

                    Name = mobileNPC.NameNPC,
                    Gender = mobileNPC.Gender,
                    Race = mobileNPC.Race,
                    IsChild = false,
                    TextureArchive = -1,
                    TextureRecord = -1,

                    FactionID = (FactionFile.FactionIDs)factionId,
                    FactionName = faction.name,
                    Social = (FactionFile.SocialGroups)faction.sgroup,
                    Guild = (FactionFile.GuildGroups)faction.ggroup,

                    Reaction = GetReactionToPlayer(faction)
                };

                npc.Descriptor = GetDescriptor(npc);

                npc.Disposition = GetDisposition(npc).Get();

                if (mobileNPC.PickpocketByPlayerAttempted)
                {
                    npc.Disposition = Text.SearchingForSomething.Get();
                }

                Utility.AddHUDText(Text.YouSeeDescribedNPC.Get(npc.Descriptor, npc.Name, npc.Disposition));

                if (!Settings.GiveTalkToneTip)
                    return;
                
                Utility.AddHUDText(GetTalkToneTipMessage(npc));
            }

        }

        static string GetTalkToneTipMessage(NpcData npc)
        {
            var str = string.Empty;
            switch (npc.Social)
            {
                case Commoners:
                    str = "Looks like someone who prefers to speak plainly.";
                    break;
                case Underworld:
                    str = "This one looks shady, best to keep my wits about me.";
                    break;
                case Scholars:
                    str = "This one looks intelligent, I should watch how I speak.";
                    break;
                case Nobility:
                    str = "I sense this one is a person of nobility, I should speak with respect.";
                    break;
                default:
                    str = "I think I can be myself with this person.";
                    break;
            }

            return str;
        }

        static readonly string[] raceKeys = new string[]
        {
            "", "breton", "redguard", "nord", "darkElf", "highElf", "woodElf", "khajiit", "argonian",
            "vampire", "werewolf", "wereboar"
        };
        /// <summary>
        /// Gets race text info description.
        /// </summary>
        static string GetRaceText(Races race)
        {
            int raceIndex = (int)race;
            if (raceIndex > 0 && raceIndex < raceKeys.Length)
            {
                return TextManager.Instance.GetLocalizedText(raceKeys[raceIndex]);
            }
            else
            {
                return Text.Foreigner.Get();
            }
        }

        /// <summary>
        /// Gets Man/Woman or Girl/Boy text.
        /// </summary>
        static string GetGenderText(Genders gender, bool isChild)
        {
            if (gender == Genders.Female)
                return (isChild ? Text.Girl : Text.Woman).Get();
            else
                return (isChild ? Text.Boy : Text.Man).Get();
        }


        /// <summary>
        /// Generates appropriate text to describe the NPCs faction disposition towards player.
        /// </summary>
        static Text GetDisposition(NpcData npc)
        {

            if (npc.IsCowled)
            {
                return Text.Inscrutable;
            }

            int index = Mathf.Clamp(npc.Reaction / 20, -4, 4);

            switch (index)
            {
                case -4: return Text.Baleful;
                case -3: return Text.Scowling;
                case -2: return Text.Icy;
                case -1: return Text.Cool;
                case 1: return Text.Tepid;
                case 2:
                    if (npc.Guild == Vampires || npc.Guild == Necromancers
                        || npc.Guild == DarkBrotherHood || npc.Guild == KnightlyOrder)
                    {
                        return Text.NoddingRespectfully;
                    }
                    else
                    {
                        return Text.Warm;
                    }
                case 3:
                    if (npc.Guild == Vampires || npc.Guild == Necromancers
                        || npc.Guild == DarkBrotherHood)
                    {
                        return Text.Flourishing;
                    }
                    else if (npc.Guild == KnightlyOrder)
                    {
                        return Text.Saluting;
                    }
                    else
                    {
                        return Text.Fond;
                    }
                case 4:
                    if (npc.Guild == Vampires || npc.Guild == Necromancers
                        || npc.Guild == DarkBrotherHood || npc.Guild == KnightlyOrder)
                    {
                        return Text.Bowing;
                    }

                    bool sameGender = GameManager.Instance.PlayerEntity.Gender == npc.Gender;
                    if (!sameGender)
                    {
                        return Text.Winking;
                    }
                    else if (npc.Social == Nobility || npc.Social == SupernaturalBeings)
                    {
                        return Text.Flourishing;
                    }
                    else
                    {
                        return npc.Gender == Genders.Male ? Text.Bowing : Text.Curtseying;
                    }
                default:
                    if (npc.Guild == Vampires || npc.Social == Nobility || npc.Social == SupernaturalBeings)
                    {
                        return Text.Aloof;
                    }
                    return Text.Indifferent;
            }
        }


        /// <summary>
        /// Gets the reaction of the static NPC towards the player.
        /// </summary>
        static int GetStaticNPCReaction(FactionFile.FactionData faction)
        {
            //using some code taken from TalkManager...

            //struct copy
            FactionFile.FactionData factionData = faction;

            // Matched to classic for NPCs that are not of type 2 (Group), 7 (Province) or 9 (Temple): use their first parent if such a parent exists
            // Change from classic: do the same for Courts, People and Individual since they have their own reputation. Moreover
            // classic already uses people and courts factions to get greetings and answers so it add more consistency
            while (factionData.parent != 0 &&
                   factionData.type != (int)FactionFile.FactionTypes.Group && // Avoid using the group leader faction (e.g. Mobar and "The Royal Guard")
                   factionData.type != (int)FactionFile.FactionTypes.Province && // Avoid using "Daggerfall" for "Betony"
                   factionData.type != (int)FactionFile.FactionTypes.Temple && // Avoid using the parent god faction
                   factionData.type != (int)FactionFile.FactionTypes.People && // Avoid using the parent province faction
                   factionData.type != (int)FactionFile.FactionTypes.Courts && // Avoid using the parent province faction
                   factionData.type != (int)FactionFile.FactionTypes.Individual // Always use an individual faction when available
                   )
            {
                GameManager.Instance.PlayerEntity.FactionData.GetFactionData(factionData.parent, out factionData);
            }

            return GetReactionToPlayer(factionData);
        }


        /// <summary>
        /// Get a static NPC faction data from his faction ID. Handles special cases
        /// for NPCs with a faction ID equal to 0 and NPCs from generic nobility.
        /// </summary>
        /// <param name="factionId">The NPC faction ID.</param>
        /// <param name="buildingType">The NPC location building type.</param>
        /// <param name="factionData">The NPC faction data.</param>
        static void GetStaticNPCFactionData(int factionId, DFLocation.BuildingTypes buildingType, out FactionFile.FactionData factionData)
        {
            //Taken from TalkManager...
            if (factionId == 0)
            {
                // Matched to classic: an NPC with a null faction id is assigned to court or people of current region
                if (buildingType == Palace)
                    factionId = GameManager.Instance.PlayerGPS.GetCourtOfCurrentRegion();
                else
                    factionId = GameManager.Instance.PlayerGPS.GetPeopleOfCurrentRegion();
            }
            else if (factionId == (int)FactionFile.FactionIDs.Random_Ruler ||
                     factionId == (int)FactionFile.FactionIDs.Random_Noble ||
                     factionId == (int)FactionFile.FactionIDs.Random_Knight)
            {
                // Change from classic: use "Court of" current region for Random Ruler, Random Noble
                // and Random Knight because these generic factions have no use at all
                factionId = GameManager.Instance.PlayerGPS.GetCourtOfCurrentRegion();
            }
            
            GameManager.Instance.PlayerEntity.FactionData.GetFactionData(factionId, out factionData);
        }


        /// <summary>
        /// Determines reaction to player based on provided faction data.
        /// </summary>
        static int GetReactionToPlayer(FactionFile.FactionData faction)
        {
            //Taken from TalkManager
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            int reaction = faction.rep + player.BiographyReactionMod;

            reaction += player.GetReactionMod((FactionFile.SocialGroups)faction.sgroup);

            if (faction.sgroup >= 0 && faction.sgroup < player.SGroupReputations.Length)
            {
                reaction += player.SGroupReputations[faction.sgroup];
            }

            return reaction;
        }


        /// <summary>
        /// Useful for identifying town folk giving quests (Work).
        /// </summary>
        static bool IsOfferingWork(StaticNPC staticNPC)
        {
            if (!staticNPC.IsChildNPC)
            {
                TalkManager.SaveDataConversation data = TalkManager.Instance.GetConversationSaveData();
                if (data.npcsWithWork.ContainsKey(staticNPC.Data.nameSeed))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Checks if npc is involved with any active quests
        /// </summary>
        static bool IsQuestNPC(StaticNPC staticNPC)
        {
            //check if part of active quest
            ulong[] questIDs = QuestMachine.Instance.GetAllActiveQuests();
            foreach (ulong questID in questIDs)
            {
                Quest quest = QuestMachine.Instance.GetQuest(questID);
                foreach (Person person in quest.GetAllResources(typeof(Person)))
                {
                    if (person.DisplayName.Equals(staticNPC.DisplayName)
                        && person.FactionIndex == staticNPC.Data.factionID)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Gets appropriate title for random rules, nobles, and knights
        /// </summary>
        static string GetTitle(NpcData npc)
        {
            if (npc.IsRandomKnight)
                return TextManager.Instance.GetLocalizedEnemyName((int)MobileTypes.Knight);
            else if (npc.Gender == Genders.Male)
                return TextManager.Instance.GetLocalizedText("Lord");
            else
                return TextManager.Instance.GetLocalizedText("Lady");
        }


        /// <summary>
        /// Generates appropriate text to describe the NPCs mood or behaviour.
        /// </summary>
        static string GetDescriptor(NpcData npc)
        {
            if (npc.IsCowled)
                return "";

            //Indexer value used to manufacture consistent results
            int indexer = npc.Name.GetHashCode();
            indexer += GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            indexer += GameManager.Instance.PlayerGPS.CurrentLocationIndex;
            indexer = Mathf.Abs(indexer);

            if (!npc.IsQuestNPC && npc.mobile == null)
            {
                //Check if of race not belonging to this region and show that.
                //Skipping npcs generated by the quest system because the data may be suspect.
                //Skipping mobile npcs as townspeople race is assumed to be of region.
                //Skipping Nords because that is data default and usually indicates info hasn't been set.

                Races regionRace = GameManager.Instance.PlayerGPS.GetRaceOfCurrentRegion();
                if (regionRace != npc.Race && npc.Race != Races.Nord)
                {
                    string raceText = GetRaceText(npc.Race);
                    string genderText = GetGenderText(npc.Gender, npc.IsChild);

                    return raceText + " " + genderText;
                }

            }

            int buildingQuality = 10;
            DFLocation.BuildingTypes buildingType = DFLocation.BuildingTypes.None;
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                PlayerGPS.DiscoveredBuilding buildingData = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData;
                buildingQuality = buildingData.quality;
                buildingType = buildingData.buildingType;
            }

            //things happening in region can affect mood of population
            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            bool[] regionFlags = GameManager.Instance.PlayerEntity.RegionData[regionIndex].Flags;
            int fearFactor = 0;
            fearFactor += regionFlags[(int)PlayerEntity.RegionDataFlags.BadHarvest] ? 20 : 0;
            fearFactor += regionFlags[(int)PlayerEntity.RegionDataFlags.CrimeWave] ? 20 : 0;
            fearFactor += regionFlags[(int)PlayerEntity.RegionDataFlags.PlagueOngoing] ? 40 : 0;
            fearFactor += regionFlags[(int)PlayerEntity.RegionDataFlags.FamineOngoing] ? 45 : 0;
            fearFactor += regionFlags[(int)PlayerEntity.RegionDataFlags.WarOngoing] ? 30 : 0;
            fearFactor = Mathf.Clamp(fearFactor, 0, 70);

            //...otherwise look up appropriate text based on factors like social group, guild, etc.
            Text[] descriptors = null;

            var texture = (npc.TextureArchive, npc.TextureRecord);

            if (TextureReader.IsChildNPCTexture(npc.TextureArchive, npc.TextureRecord) || children.Contains(texture))
            {
                //putting this check first to prevent undesirable text descriptions from triggering when
                //child textures are shown.
                descriptors = childDescriptors;
            }
            else if (npc.FactionID == FactionFile.FactionIDs.The_Blades)
            {
                descriptors = bladesDescriptors;
            }
            else if (dancers.Contains(texture) && indexer % 3 == 0)
            {
                //most dancers belong to some guild/temple.  At least some should show dancer descriptors
                descriptors = dancerDescriptors;
            }
            else if (npc.Social == Commoners && npc.Guild == GeneralPopulace)
            {
                if (sexyCommoners.Contains(texture))
                {
                    descriptors = npc.Gender == Genders.Female ? prostituteDescriptors : gigoloDescriptors;
                }
                else if (npc.FactionID == FactionFile.FactionIDs.Dancers)
                {
                    descriptors = dancerDescriptors;
                }
                else if (fearFactor > indexer % 100)
                {
                    descriptors = troubledDescriptors;
                }
                else if (aspiringCommoners.Contains(texture))
                {
                    descriptors = buildingQuality > 8 ? aspiringDescriptors : disgustedDescriptors;
                }
                else if (poorCommoners.Contains(texture))
                {
                    descriptors = poorDescriptors;
                }
                else if (bookishCommoners.Contains(texture))
                {
                    descriptors = scholarDescriptors;
                }
                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                {
                    descriptors = npc.Gender == Genders.Male ? maleDescriptors : femaleDescriptors;
                }
                else
                {
                    descriptors = GetWeatherClimateDescriptors(npc);
                }
            }
            else if (npc.Social == Commoners && npc.Guild == Prostitutes)
            {
                descriptors = npc.Gender == Genders.Female ? prostituteDescriptors : gigoloDescriptors;
            }
            else if (npc.Social == Commoners && npc.Guild == FightersGuild)
            {
                descriptors = fighterDescriptors;
            }
            else if (npc.Social == Merchants && npc.Guild == Bards)
            {
                descriptors = quillDescriptors;
            }
            else if (npc.Social == Merchants && npc.Guild == FightersGuild)
            {
                descriptors = merchantDescriptors;
                if (shops.Contains(buildingType))
                {
                    float qualityPerDescriptor = 20.0f / descriptors.Length; //20 is max building quality
                    indexer = (int)(buildingQuality / qualityPerDescriptor);
                    indexer = Mathf.Clamp(indexer, 0, descriptors.Length - 1);
                }
                else if (buildingType == DFLocation.BuildingTypes.Library)
                {
                    descriptors = scholarDescriptors;
                }
            }
            else if (npc.Social == Scholars && npc.Guild == Bards)
            {
                descriptors = bardDescriptors;
            }
            else if (npc.Social == Scholars && npc.Guild == MagesGuild)
            {
                descriptors = scholarDescriptors;
            }
            else if (npc.Social == Underworld && npc.Guild == DarkBrotherHood)
            {
                descriptors = darkBrotherhoodDescriptors;
            }
            else if (npc.Social == Underworld && npc.Guild == GeneralPopulace)
            {
                descriptors = thiefDescriptors;
            }
            else if (npc.Social == GuildMembers && npc.Guild == Necromancers)
            {
                descriptors = necromancerDescriptors;
            }
            else if (npc.Social == GuildMembers && npc.Guild == KnightlyOrder)
            {
                descriptors = knightDescriptors;
            }
            else if (npc.Social == GuildMembers && npc.Guild == HolyOrder)
            {
                descriptors = holyDescriptors;
            }
            else if (npc.Social == GuildMembers && npc.Guild == Witches)
            {
                descriptors = witchDescriptors;
            }
            else if (npc.Social == GuildMembers && npc.Guild == Vampires)
            {
                descriptors = vampireDescriptors;
            }
            else if ((int)npc.FactionID >= 1000 && (int)npc.FactionID <= 1008)
            {
                //Archaeologist Guild
                descriptors = scholarDescriptors;
            }


            //Quest related people tend to be troubled.
            //Members of certain factions aren't 'troubled', just inconvenienced
            if (npc.FactionID != FactionFile.FactionIDs.The_Blades
                && npc.Guild != DarkBrotherHood && npc.Guild != Necromancers && npc.Guild != Vampires)
            {
                if (npc.IsQuestNPC || npc.IsQuestGiver)
                {
                    descriptors = troubledDescriptors;
                    indexer += DaggerfallUnity.Instance.WorldTime.Now.Day;
                }
            }

            if (descriptors != null && descriptors.Length > 0)
            {
                int index = indexer % descriptors.Length;

                return descriptors[index].Get();
            }
            else
            {
                //Probably a well known figure, no additional description needed
                return "";
            }

        }


        /// <summary>
        /// Street commoners can have descriptors related to the current seasonal weather/climate.
        /// </summary>
        static Text[] GetWeatherClimateDescriptors(NpcData npc)
        {
            DFLocation.ClimateBaseType climate = GameManager.Instance.PlayerGPS.ClimateSettings.ClimateType;
            WeatherType weather = GameManager.Instance.WeatherManager.PlayerWeather.WeatherType;
            DaggerfallDateTime.Seasons season = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue;
            bool morning = DaggerfallUnity.Instance.WorldTime.Now.Hour <= 10;

            int indexer = npc.TextureArchive + npc.TextureRecord + DaggerfallUnity.Instance.WorldTime.Now.Hour;
            indexer += npc.mobile != null ? npc.mobile.PersonFaceRecordId : 3;

            //default to normal commoner descriptors
            Text[] descriptors = npc.Gender == Genders.Male ? maleDescriptors : femaleDescriptors;

            if (weather == WeatherType.Rain || weather == WeatherType.Rain_Normal || weather == WeatherType.Thunder)
            {
                if (indexer % (weather == WeatherType.Thunder ? 2 : 4) == 0)
                    descriptors = wetDescriptors;
            }
            else if (season == DaggerfallDateTime.Seasons.Summer)
            {
                if (weather == WeatherType.Sunny || weather == WeatherType.Cloudy)
                {
                    int divisor = 5;
                    if (climate == DFLocation.ClimateBaseType.Swamp)
                        divisor = 2;
                    else if (climate == DFLocation.ClimateBaseType.Temperate)
                        divisor = 3;

                    divisor += morning ? 2 : 0;

                    if (indexer % divisor == 0)
                        descriptors = sweatyDescriptors;
                }
            }
            else if (season == DaggerfallDateTime.Seasons.Winter)
            {
                if (climate == DFLocation.ClimateBaseType.Mountain || climate == DFLocation.ClimateBaseType.Temperate)
                {
                    if (indexer % (morning ? 3 : 6) == 0)
                        descriptors = coldDescriptors;
                }
            }

            return descriptors;
        }


        //-------------Texture Archive,Record sets----------------- 
        static readonly HashSet<(int, int)> aspiringCommoners = new HashSet<(int, int)>()
        {
            (182, 9), (182, 10), (182, 27), (182, 40), (182, 41),
            (184, 0), (184, 4), (184, 5),
            (186, 10), (186, 11), (186, 28), (186, 41), (186, 42),
            (195, 11), (195, 12), (195, 19), (195, 20), (195, 21),
            (195, 22)
        };

        static readonly HashSet<(int, int)> sexyCommoners = new HashSet<(int, int)>()
        {
            //women
            (182, 32), (182, 33), (182, 34), (182, 48),
            (184, 6), (184, 7), (184, 8), (184, 9), (184, 10),
            (184, 11), (184, 12), (184, 13), (184, 14),
            (186, 33), (186, 34), (186, 35), (186, 49),
            (195, 14),
            //men
            (182, 57), (182, 58), (186, 57), (186, 58),
            (357, 10), (357, 11)
        };

        static readonly HashSet<(int, int)> dancers = new HashSet<(int, int)>()
        {
            (176, 2), (176, 3), (176, 4),
            (178, 5), (178, 6), (179, 3),
            (181, 5), (181, 6), (181, 7),
        };

        static readonly HashSet<(int, int)> poorCommoners = new HashSet<(int, int)>()
        {
            (182, 13), (182, 14), (182, 29), (182, 30),
            (182, 31), (184, 27), (186, 14), (186, 15),
            //prisoners
            (184, 31), (186, 30), (186, 31), (186, 32)
        };

        static readonly HashSet<(int, int)> bookishCommoners = new HashSet<(int, int)>()
        {
            (182, 24), (184, 2), (186, 25), (334, 7)
        };

        static readonly HashSet<(int, int)> children = new HashSet<(int, int)>()
        {
            (182, 43), (182, 52), (182, 53),
            (186, 43), (186, 44), (186, 53), (186, 54)
        };



        //------------Below are the text descriptions used for people in various social/guilds--------

        static readonly Text[] bladesDescriptors = new Text[]
        {
            Text.Cryptic, Text.Enigmatic, Text.Mysterious, Text.Impenetrable, Text.Clandestine,
            Text.Covert
        };
        static readonly Text[] maleDescriptors = new Text[]
        {
            Text.Disheveled, Text.Muddled, Text.Eager,
            Text.Coarse, Text.Glum, Text.Weary, Text.Hopeful,
            Text.Brisk, Text.Aspiring, Text.Lively, Text.Spry, Text.Hurried,
            Text.Prudent, Text.Stern, Text.Skittish, Text.Grim, Text.Stubborn
        };
        static readonly Text[] femaleDescriptors = new Text[]
        {
            Text.Frumpy, Text.Glum, Text.Weary, Text.Hopeful, Text.Flirty, Text.Eager,
            Text.Brisk, Text.Aspiring, Text.Lively, Text.Spry, Text.Hurried,
            Text.Prudent, Text.Coy, Text.Skittish, Text.Moody, Text.Stubborn
        };
        static readonly Text[] childDescriptors = new Text[]
        {
            Text.Curious, Text.Bored, Text.Heady, Text.Giddy, Text.Playful, Text.Shy, Text.Stubborn
        };
        static readonly Text[] aspiringDescriptors = new Text[]
        {
            Text.Hopeful, Text.Brisk, Text.Aspiring, Text.Prudent, Text.Curt,
            Text.Refined, Text.Genteel, Text.Urbane, Text.Polished
        };
        static readonly Text[] disgustedDescriptors = new Text[]
        {
            Text.Disgusted, Text.Appalled, Text.Revolted, Text.Bored, Text.Annoyed, Text.Resigned,
            Text.Nauseated, Text.Repulsed, Text.Mortified, Text.Flustered, Text.Miffed
        };
        static readonly Text[] troubledDescriptors = new Text[]
        {
            Text.Troubled, Text.Anxious, Text.Concerned, Text.Tense, Text.Vexed, Text.Exasperated,
            Text.Fretting, Text.Perplexed, Text.Distracted
        };
        static readonly Text[] wetDescriptors = new Text[]
        {
            Text.Drenched, Text.Soaked, Text.Dripping, Text.Muddy, Text.Mucky, Text.Sopping
        };
        static readonly Text[] sweatyDescriptors = new Text[]
        {
            Text.Sweaty, Text.Clammy, Text.Damp, Text.Perspiring, Text.Tired, Text.Weary
        };
        static readonly Text[] coldDescriptors = new Text[]
        {
            Text.Shivering, Text.Numb, Text.Chilly, Text.Invigorated, Text.Quivering, Text.Trembling
        };
        static readonly Text[] prostituteDescriptors = new Text[]
        {
            Text.Lusty, Text.Questionable, Text.Provacative, Text.Comely, Text.Alluring,
            Text.Seductive, Text.Playful, Text.Naughty, Text.Vampish, Text.Amorous, Text.Teasing,
            Text.Scandalous, Text.Impure, Text.Experienced, Text.Sullied
        };
        static readonly Text[] gigoloDescriptors = new Text[]
        {
            Text.Beefcake, Text.Hunky, Text.Virile, Text.Strapping, Text.Buff, Text.Flexing
        };
        static readonly Text[] dancerDescriptors = new Text[]
        {
            Text.Lithe, Text.Sinuous, Text.Provacative, Text.Comely, Text.Alluring, Text.Fetching,
            Text.Seductive, Text.Entrancing, Text.Supple, Text.Limber, Text.Graceful
        };
        static readonly Text[] poorDescriptors = new Text[]
        {
            Text.Crestfallen, Text.Glum, Text.Cynical, Text.Forsaken, Text.Lorn, Text.Leery,
            Text.Bleak, Text.Joyless, Text.Stark, Text.Despondent
        };
        static readonly Text[] fighterDescriptors = new Text[]
        {
            Text.Stoic, Text.Stern, Text.Calloused, Text.Scarred, Text.Hardened, Text.Steadfast,
            Text.Brave, Text.Cocky, Text.Brash, Text.Grim, Text.Stubborn, Text.Violent
        };
        static readonly Text[] quillDescriptors = new Text[]
        {
            //Merchant Bards are 'The Quill Circus'
            Text.Questionable, Text.Nimble, Text.Adroit, Text.Agile, Text.Cryptic, Text.Focused, 
        };
        static readonly Text[] merchantDescriptors = new Text[]
        {
            //in order according to shop quality
            Text.Befuddled, Text.Confused, Text.Mirthless, Text.Harried, Text.Busy, Text.Organized,
            Text.Miserly, Text.Shrewd, Text.Canny, Text.Savvy
        };
        static readonly Text[] bardDescriptors = new Text[]
        {
            Text.Charming, Text.Rousing, Text.Riveting, Text.Poignant, Text.Comical, Text.Jovial,
            Text.Amusing, Text.Inspiring
        };
        static readonly Text[] scholarDescriptors = new Text[]
        {
            Text.Curious, Text.Focused, Text.Bookish, Text.Cerebral, Text.Refined,
            Text.Bumbling, Text.Klutzy, Text.Nervous, Text.Withdrawn, Text.Fascinating,
            Text.Studied, Text.Fascinated, Text.Perplexing
        };
        static readonly Text[] darkBrotherhoodDescriptors = new Text[]
        {
            Text.Focused, Text.Brooding, Text.Baleful, Text.Morbid, Text.Stained, Text.Sullied,
            Text.Bloody, Text.Grim, Text.Violent, Text.Lurid
        };
        static readonly Text[] thiefDescriptors = new Text[]
        {
            Text.Shifty, Text.Sly, Text.Adroit, Text.Shady, Text.Crafty, Text.Cagey, Text.Coy,
            Text.Dubious, Text.Slick, Text.Tricky, Text.Furtive, Text.Wary, Text.Cocky,
            Text.Nimble
        };
        static readonly Text[] necromancerDescriptors = new Text[]
        {
            Text.Obscure, Text.Cryptic, Text.Enigmatic, Text.Dark, Text.Devoted,
            Text.Paranoid, Text.Zealous, Text.Fervid, Text.Restless, Text.Tainted,
            Text.Fervent, Text.Impure, Text.Fetid, Text.Covert
        };
        static readonly Text[] knightDescriptors = new Text[]
        {
            Text.Devout, Text.Pious, Text.Faithful, Text.Wise, Text.Contemplative,
            Text.Enlightened, Text.Solemn, Text.Exemplary, Text.Stern, Text.Stoic,
            Text.Dutiful, Text.Fervent, Text.Doubtful
        };
        static readonly Text[] holyDescriptors = new Text[]
        {
            Text.Devout, Text.Zealous, Text.Pious, Text.Faithful, Text.Virtuous, Text.Wise,
            Text.Contemplative, Text.Enlightened, Text.Solemn, Text.Exemplary, Text.Reverent,
            Text.Dutiful, Text.Fervent, Text.Doubtful
        };
        static readonly Text[] witchDescriptors = new Text[]
        {
            Text.Guarded, Text.Evasive, Text.Leery, Text.Cynical, Text.Sceptical, Text.Suspicious,
            Text.Wry, Text.Bitter, Text.Chaffing, Text.Wary, Text.Solemn, Text.Paranoid
        };
        static readonly Text[] vampireDescriptors = new Text[]
        {
            Text.Hypnotic, Text.Beguiling, Text.Perplexing, Text.Enigmatic, Text.Captivating,
            Text.Enthralling, Text.Fascinating, Text.Charming
        };




    } //class EnhancedInfo



} //namespace