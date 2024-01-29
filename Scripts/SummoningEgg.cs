// Project:      The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  June 2021

using System.Collections;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;

namespace ThePenwickPapers
{
    public class SummoningEgg
    {
        readonly DaggerfallEnemy creature;
        readonly Texture2D eggTexture;
        readonly Color eggColor;
        readonly GameObject outerEgg;
        readonly GameObject innerEgg;
        readonly Light innerLight;
        readonly DaggerfallAudioSource dfAudio;
        readonly AudioClip sound;


        public SummoningEgg(DaggerfallEnemy creature, Texture2D eggTexture, Color eggColor, AudioClip sound = null)
        {
            this.creature = creature;
            this.eggTexture = eggTexture;
            this.eggColor = eggColor;
            this.sound = sound;

            outerEgg = CreateOuterEgg();
            outerEgg.transform.parent = creature.transform.parent;

            dfAudio = outerEgg.AddComponent<DaggerfallAudioSource>();
            dfAudio.AudioSource.spatialize = true;
            dfAudio.AudioSource.spatialBlend = 1.0f;
            
            innerEgg = CreateInnerEgg();
            innerEgg.transform.parent = outerEgg.transform;
            innerEgg.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_InteriorLightPrefab.gameObject, "Egg Glow", outerEgg.transform, Vector3.zero);
            innerLight = go.GetComponent<Light>();
            innerLight.range = 5f;
            innerLight.intensity = 1f;
        }


        /// <summary>
        /// Coroutine to animate the summoning process
        /// </summary>
        public IEnumerator Hatch()
        {
            Vector2 size = creature.MobileUnit.GetSize();
            float creatureMidHeight = size.y * 0.5f;

            //creating a temporary place-holder collider in case a summoning spell has multiple summons
            GameObject placeHolder = CreatePlaceholder(size);

            yield return new WaitForSeconds(0.05f);

            //that should be enough time, destroy the place-holder
            GameObject.Destroy(placeHolder);

            float yScale = 0.01f;
            float xzScale = size.x - 0.1f;
            outerEgg.transform.localScale = new Vector3(xzScale, yScale, xzScale);
            outerEgg.transform.position = creature.transform.position;
            outerEgg.transform.position -= outerEgg.transform.up * creatureMidHeight;
            outerEgg.SetActive(true);

            float scaleAdjustment = creatureMidHeight * 0.01f;

            if (sound != null)
            {
                dfAudio.AudioSource.PlayOneShot(sound);
            }

            Material mat = innerEgg.GetComponent<Renderer>().material;

            while (yScale < creatureMidHeight)
            {
                yScale += scaleAdjustment;

                //grow the cylinder and adjust the position so that the base stays on the ground
                outerEgg.transform.localScale = new Vector3(xzScale, yScale, xzScale);
                outerEgg.transform.position += outerEgg.transform.up * scaleAdjustment;

                //brighten/dim color cyclically
                Color emissionColor = eggColor * Mathf.Abs(Mathf.Cos(Time.time * 8));
                mat.SetColor("_EmissionColor", emissionColor);
                innerLight.color = emissionColor;

                outerEgg.transform.Rotate(0.0f, 16.0f, 0.0f);

                yield return new WaitForSeconds(.02f);
            }

            GameObject.Destroy(outerEgg);

            creature.gameObject.SetActive(true);
            GameManager.Instance.RaiseOnEnemySpawnEvent(creature.gameObject);

        }


        /// <summary>
        /// Create a collider to take up space, preventing other summons from occupying the same area
        /// </summary>
        GameObject CreatePlaceholder(Vector2 size)
        {
            GameObject placeHolder = new GameObject();
            placeHolder.transform.parent = creature.transform.parent;
            placeHolder.transform.position = creature.transform.position;
            CapsuleCollider collider = placeHolder.AddComponent<CapsuleCollider>();
            collider.height = size.y;
            collider.radius = size.x / 2;
            placeHolder.SetActive(true);
            return placeHolder;
        }



        /// <summary>
        /// Create partially transparent outer cylinder with egg texture.
        /// </summary>
        GameObject CreateOuterEgg()
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Renderer renderer = cylinder.GetComponent<Renderer>();
            Material mat = renderer.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            mat.mainTexture = eggTexture;

            //set some shader values for transparent rendering of outer egg sphere texture
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            return cylinder;
        }


        /// <summary>
        /// Create opaque, colored inner cylinder.
        /// </summary>
        GameObject CreateInnerEgg()
        {
            //create an emissive inner sphere to make the egg more visible and add some color
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            Renderer renderer = cylinder.GetComponent<Renderer>();
            Material mat = renderer.material;

            mat.color = new Color(0, 0, 0);

            //renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            mat.SetColor("_EmissionColor", eggColor);

            return cylinder;
        }

    }
}