using System;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using System.Collections;

namespace ThePenwickPapers
{

    public class SummonImp : BaseEntityEffect
    {
        public const string effectKey = "Summon-Imp";

        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = ElementTypes.Magic;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Mysticism;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportChance = true;
            properties.ChanceFunction = ChanceFunction.OnCast;
            properties.ChanceCosts = MakeEffectCosts(4, 16);
        }


        public override string GroupName => Text.SummonImpGroupName.Get();
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (!ChanceSuccess)
                return;

            if (caster == null)
                return;

            bool success = false;

            try
            {
                if (TryGetSpawnLocation(out Vector3 location))
                {
                    Summon(location);
                    success = true;
                }
            }
            catch (Exception e)
            {
                Utility.AddHUDText(Text.DisturbanceInFabricOfReality.Get());
                Debug.LogException(e);
            }

            if (!success)
            {
                RefundSpellCost();
                End();
            }
        }


        /// <summary>
        /// Refund magicka cost of this effect to the caster
        /// </summary>
        void RefundSpellCost()
        {
            FormulaHelper.SpellCost cost = FormulaHelper.CalculateEffectCosts(this, Settings, Caster.Entity);
            Caster.Entity.IncreaseMagicka(cost.spellPointCost);
        }


        static readonly float[] scanDistances = { 2.0f, 3.0f, 1.2f };
        static readonly float[] scanDownUpRots = { 45, 30, 0, -30, -45 };
        static readonly float[] scanLeftRightRots = { 0, 5, -5, 15, -15, 30, -30, 45, -45 };

        /// <summary>
        /// Scans the area in front of the caster and tries to find a location that can fit a medium-sized creature.
        /// </summary>
        bool TryGetSpawnLocation(out Vector3 location)
        {
            //try to find reasonable spawn location in front of the caster
            foreach (float distance in scanDistances)
            {
                foreach (float downUpRot in scanDownUpRots)
                {
                    foreach (float leftRightRot in scanLeftRightRots)
                    {
                        Quaternion rotation = Quaternion.Euler(downUpRot, leftRightRot, 0);
                        Vector3 direction = (Caster.transform.rotation * rotation) * Vector3.forward;

                        //shouldn't be anything between the caster and spawn point
                        Ray ray = new Ray(Caster.transform.position, direction);
                        if (Physics.Raycast(ray, out RaycastHit hit, distance))
                        {
                            continue;
                        }

                        //create a reasonably sized capsule to check if enough space is available for spawning
                        Vector3 scannerPos = Caster.transform.position + (direction * distance) + Vector3.up;
                        Vector3 top = scannerPos + Vector3.up * 0.4f;
                        Vector3 bottom = scannerPos - Vector3.up * 0.4f;
                        float radius = 0.4f; //radius*2 included in height
                        if (!Physics.CheckCapsule(top, bottom, radius))
                        {
                            //just returning first available valid position
                            location = scannerPos;
                            return true;
                        }
                    }
                }
            }

            location = Vector3.zero;
            return false;
        }


        /// <summary>
        /// Creates the atronach and begins the summoning animation.
        /// </summary>
        void Summon(Vector3 location)
        {
            string displayName = string.Format("Penwick Summoned[{0}]", MobileTypes.Imp.ToString());

            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, displayName, parent, location);

            go.SetActive(false);

            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            // Configure summons
            setupEnemy.ApplyEnemySettings(MobileTypes.Imp, MobileReactions.Hostile, MobileGender.Unspecified, 0, true);

            DaggerfallEnemy creature = go.GetComponent<DaggerfallEnemy>();

            //needs a loadID to save/serialize
            creature.LoadID = DaggerfallUnity.NextUID;

            //Have atronach looking in same direction as caster
            creature.transform.rotation = Caster.transform.rotation;

            //start coroutine to animate the 'hatching' process
            IEnumerator coroutine = SpawnCloud(go);
            ThePenwickPapersMod.Instance.StartCoroutine(coroutine);
        }


        IEnumerator SpawnCloud(GameObject go)
        {
            //creating a temporary place-holder collider in case a summoning spell has multiple summons
            GameObject placeHolder = CreatePlaceholder(go);

            yield return new WaitForSeconds(0.05f);

            //that should be enough time, destroy the place-holder
            GameObject.Destroy(placeHolder);

            GameObject bgo = GameObjectHelper.CreateDaggerfallBillboardGameObject(379, 0, null);
            bgo.name = "Penwick Imp Cloud";
            bgo.transform.position = go.transform.position;
            DaggerfallAudioSource audio = bgo.AddComponent<DaggerfallAudioSource>();

            audio.PlayOneShot(SoundClips.SpellImpactShock);

            Billboard billboard = bgo.GetComponent<Billboard>();
            billboard.FramesPerSecond = 15;
            billboard.FaceY = true;
            billboard.OneShot = true;
            billboard.GetComponent<MeshRenderer>().receiveShadows = false;

            yield return new WaitForSeconds(0.6f);

            go.SetActive(true);

            //to allow interaction with the summoned creature
            PenwickMinion.AddNewMinion(go.GetComponent<DaggerfallEntityBehaviour>());

        }


        /// <summary>
        /// Create a collider to take up space, preventing other summons from occupying the same area
        /// </summary>
        GameObject CreatePlaceholder(GameObject go)
        {
            GameObject placeHolder = new GameObject();
            placeHolder.transform.parent = go.transform.parent;
            placeHolder.transform.position = go.transform.position;

            CharacterController impCollider = go.GetComponent<CharacterController>();

            CapsuleCollider collider = placeHolder.AddComponent<CapsuleCollider>();
            collider.height = impCollider.height;
            collider.radius = impCollider.radius;
            placeHolder.SetActive(true);
            return placeHolder;
        }



        TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.SummonImpEffectDescription.Get(),
                Text.SummonImpSpellMakerChance.Get());
        }

        TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                Text.SummonImpSpellBookChance.Get(),
                "",
                "\"" + Text.SummonImpEffectDescription.Get() + "\"",
                "[" + TextManager.Instance.GetLocalizedText("mysticism") + "]");
        }



    } //class SummonImp




} //namespace


