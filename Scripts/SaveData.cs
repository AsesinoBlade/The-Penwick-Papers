// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Utility.ModSupport;


namespace ThePenwickPapers
{

    public class SaveData : IHasModSaveData
    {
        public Dictionary<string, List<LandmarkLocation>> Towns;
        public List<LandmarkLocation> DungeonLocations;

        public SaveData()
        {
            Towns = new Dictionary<string, List<LandmarkLocation>>();
            DungeonLocations = new List<LandmarkLocation>();
        }


        //---------IHasSaveData implementation

        public Type SaveDataType
        {
            get { return typeof(SaveData); }
        }

        public object NewSaveData()
        {
            return new SaveData();
        }

        public object GetSaveData()
        {
            return this;
        }

        public void RestoreSaveData(object obj)
        {
            SaveData other = (SaveData)obj;

            Towns = other.Towns;
            DungeonLocations = other.DungeonLocations;
        }

    } //class SaveData


    public class LandmarkLocation : IComparable<LandmarkLocation>
    {
        public string Name;
        public Vector3 Position;

        public LandmarkLocation(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }

        public int CompareTo(LandmarkLocation other)
        {
            if (other == null)
                return 1;
            else
                return Name.CompareTo(other.Name);
        }

    } //class LandmarkLocation



} //namespace
