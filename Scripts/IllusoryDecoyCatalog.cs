// Project:     Illusory Decoy, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: June 2021

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using static DaggerfallConnect.DFCareer;
using static DaggerfallWorkshop.MobileTypes;

namespace ThePenwickPapers
{
    public static class IllusoryDecoyCatalog
    {
        static readonly Dictionary<Skills, MobileTypes[]> DecoyTypes = new Dictionary<Skills, MobileTypes[]>()
        {
            { Skills.Centaurian, new MobileTypes[] {Centaur} },
            { Skills.Daedric, new MobileTypes[] {Daedroth, FireDaedra, DaedraLord, DaedraSeducer, FrostDaedra} },
            { Skills.Dragonish, new MobileTypes[] {Dragonling} },
            { Skills.Etiquette, new MobileTypes[] {Bard, Archer, Battlemage, Knight, Mage, Spellsword, Sorcerer} },
            { Skills.Giantish, new MobileTypes[] {Giant, Gargoyle} },
            { Skills.Harpy, new MobileTypes[] {Harpy} },
            { Skills.Impish, new MobileTypes[] {Imp} },
            { Skills.Nymph, new MobileTypes[] {Nymph} },
            { Skills.Orcish, new MobileTypes[] {Orc, OrcSergeant, OrcShaman, OrcWarlord} },
            { Skills.Spriggan, new MobileTypes[] {Spriggan} },
            { Skills.Streetwise, new MobileTypes[] {Thief, Assassin, Rogue, Burglar} },
        };

        static readonly Skills[] Flyers = { Skills.Dragonish, Skills.Impish, Skills.Harpy };


        /// <summary>
        /// Determines decoy type to get based on caster language skills and positioning.
        /// </summary>
        /// <returns>MobileType of chosen unit</returns>
        public static MobileTypes GetDecoyType(DaggerfallWorkshop.Game.Entity.DaggerfallEntityBehaviour caster, Vector3 location, Vector3 destination)
        {
            //check if we need flying decoy
            if (NeedsFlyingDecoy(location, destination))
            {
                return GetFlyingDecoyType(caster);
            }

            MobileTypes[] availableTypes = GetDecoyTypes(caster);
            if (availableTypes.Length == 0)
            {
                return MobileTypes.None;
            }

            MobileTypes decoyType = availableTypes[Random.Range(0, availableTypes.Length)];

            if (caster.EntityType == EntityTypes.Player && GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
            {
                //if player underwater, limited options
                int nymphSkill = caster.Entity.Skills.GetLiveSkillValue(Skills.Nymph);
                int impSkill = caster.Entity.Skills.GetLiveSkillValue(Skills.Impish);

                decoyType = nymphSkill >= impSkill ? Lamia : Dreugh;
            }

            return decoyType;
        }


        /// <summary>
        /// Returns distance above ground of provided position, maximum of 20.
        /// </summary>
        /// <returns>Altitude, maximum of 20</returns>
        public static float GetAltitude(Vector3 position)
        {
            const float maxDistance = 20f;

            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, maxDistance))
                return hit.distance;

            return maxDistance;
        }


        /// <summary>
        /// Determines decoy types to get based on caster language skills.
        /// </summary>
        /// <returns>MobileType[] array of selected units</returns>
        static MobileTypes[] GetDecoyTypes(DaggerfallWorkshop.Game.Entity.DaggerfallEntityBehaviour caster)
        {
            Skills bestish = GetBestishSkill(caster, DecoyTypes.Keys);
            if (DecoyTypes.TryGetValue(bestish, out MobileTypes[] availableTypes))
                return availableTypes;
            else
                return new MobileTypes[] { };
        }


        /// <summary>
        /// Determines the best flying decoy type to get based on caster language skills.
        /// </summary>
        /// <returns>MobileType of selected unit</returns>
        static MobileTypes GetFlyingDecoyType(DaggerfallWorkshop.Game.Entity.DaggerfallEntityBehaviour caster)
        {
            switch (GetBestishSkill(caster, Flyers))
            {
                case Skills.Dragonish:
                    return Dragonling;
                case Skills.Harpy:
                    return Harpy;
                default:
                    return Imp;
            }
        }



        /// <summary>
        /// Selects one of the highest skills from a collection of skills, somewhat nebulous.
        /// </summary>
        static Skills GetBestishSkill(DaggerfallWorkshop.Game.Entity.DaggerfallEntityBehaviour caster, ICollection<Skills> skills)
        {
            if (skills.Count == 0)
                return Skills.None;

            List<Skills> possible = GetBestSkills(caster, skills);

            return possible.ElementAt(Random.Range(0, possible.Count()));
        }



        /// <summary>
        /// Gets the caster's best skills (plural) from a collection of skills
        /// </summary>
        static List<Skills> GetBestSkills(DaggerfallWorkshop.Game.Entity.DaggerfallEntityBehaviour caster, ICollection<Skills> skills)
        {
            if (skills.Count == 0)
                return new List<Skills>();

            IOrderedEnumerable<Skills> sortedSkills = skills.OrderByDescending(sk => caster.Entity.Skills.GetLiveSkillValue(sk));

            int highest = caster.Entity.Skills.GetLiveSkillValue(sortedSkills.First());

            IEnumerable<Skills> possible = sortedSkills.Where(sk => caster.Entity.Skills.GetLiveSkillValue(sk) >= highest - 6);

            return new List<Skills>(possible);
        }


        /// <summary>
        /// Does altitude checks along path to see if a flying decoy is required.
        /// </summary>
        /// <returns>true if a flying unit should be used</returns>
        static bool NeedsFlyingDecoy(Vector3 startLocation, Vector3 destination)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                return false;

            if (GetAltitude(startLocation) > 2.3f || GetAltitude(destination) > 3.3f)
                return true;

            //shift upward closer to eye-height to reduce clipping through stairs and what-not
            startLocation += Vector3.up;

            //checks how close the path is to the floor at various points
            float distance = Vector3.Distance(startLocation, destination);
            Vector3 offsetDirection = (destination - startLocation).normalized;
            for (float offset = 0f; offset <= distance; offset += 2.5f)
            {
                Vector3 position = startLocation + offsetDirection * offset;
                if (GetAltitude(position) > 4.0f)
                    return true;
            }

            return false;
        }



    } // class IllusoryDecoyCatalog



} //namespace
