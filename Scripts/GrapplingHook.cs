// Project:     GrapplingHook, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Feb 2022

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using System.Collections;
using UnityEngine;


namespace ThePenwickPapers
{

    public static class GrapplingHook
    {
        public const int HookAndRopeItemIndex = 544; //value from thiefoverhaul/skulduggery mod
        public const string PenwickHookName = "Penwick Hook";
        public const string PenwickRopeName = "Penwick Rope";
        public const string PenwickFlyingHookName = "Penwick Flying Hook";
        public const float MaxRopeLength = 14.0f;

        static bool throwing;
        static GameObject hook;
        static GameObject rope;



        /// <summary>
        /// Checks activated location to see if it conditions are appropriate to attach a grappling hook.
        /// If deemed appropriate, then a coroutine is called to create the hook and rope.
        /// </summary>
        public static bool AttemptHook(RaycastHit hitInfo)
        {
            if (!hitInfo.collider)
                return false;

            bool hitTerrain = GameObjectHelper.IsStaticGeometry(hitInfo.collider.gameObject);
            if (!hitTerrain)
                return false;

            //Don't hook action objects, like switches and platforms
            if (hitInfo.transform.gameObject.GetComponent<DaggerfallAction>())
                return false;

            if (throwing || Utility.IsShowingHandAnimation())
                return false;  //already throwing, spellcasting or attacking, skip

            //check for equipped hook and rope item, from Skulduggery mod
            DaggerfallUnityItem hookAndRopeSkulduggeryItem = Utility.GetEquippedItem(HookAndRopeItemIndex);
            DaggerfallUnityItem hookAndRopeItem = Utility.GetEquippedItem(GrapplingHookAndRope.HookAndRopeTemplateIndex);
            if (hookAndRopeSkulduggeryItem == null && hookAndRopeItem == null)
                return false;
            else if (hookAndRopeItem == null)
                hookAndRopeItem = hookAndRopeSkulduggeryItem;

            Vector3 anchorPoint;

            anchorPoint = FindUpperLedgeAnchor(hitInfo);
            if (anchorPoint == Vector3.zero)
                anchorPoint = FindLowerLedgeAnchor(hitInfo);

            if (anchorPoint == Vector3.zero)
                return false;

            float ropeLength;
            if (Physics.Raycast(anchorPoint, Vector3.down, out RaycastHit floorHit, MaxRopeLength))
                ropeLength = floorHit.distance - 0.4f;
            else
                ropeLength = MaxRopeLength;

            if (ropeLength < 3f)
                return false; //shouldn't need a rope for such a short distance

            if (Dice100.SuccessRoll(70))
                hookAndRopeItem.LowerCondition(1, GameManager.Instance.PlayerEntity);

            if (hookAndRopeItem.currentCondition > 0)
                ThePenwickPapersMod.Instance.StartCoroutine(ThrowHook(anchorPoint, ropeLength));

            return true;
        }


        /// <summary>
        /// Tries to locate appropriate anchoring point for hook.
        /// </summary>
        static Vector3 FindUpperLedgeAnchor(RaycastHit hitInfo)
        {
            //back away from wall a tad
            Vector3 anchorPoint = hitInfo.point + hitInfo.normal * 0.05f;

            Ray ray;

            //check if position above contact point is blocked
            ray = new Ray(anchorPoint, Vector3.up);
            if (Physics.Raycast(ray, 0.5f))
                return Vector3.zero;

            //Won't assume all normals are level with coordinate axes
            Vector3 effectiveNormal = Vector3.ProjectOnPlane(hitInfo.normal, Vector3.up);

            //up and over ledge
            anchorPoint += Vector3.up * 0.5f;
            ray = new Ray(anchorPoint, -effectiveNormal);
            if (Physics.Raycast(ray, 0.5f))
                return Vector3.zero;

            anchorPoint -= effectiveNormal * 0.1f;

            //Find height of ledge by looking down
            ray = new Ray(anchorPoint, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit ledgeHit, 0.5f))
                return Vector3.zero;

            //anchor the hook close enough to the wall to grab wall if needed
            anchorPoint = hitInfo.point + effectiveNormal * 0.05f;
            anchorPoint.y = ledgeHit.point.y;

            return anchorPoint;
        }


        /// <summary>
        /// Tries to locate appropriate anchoring point for hook.
        /// </summary>
        static Vector3 FindLowerLedgeAnchor(RaycastHit hitInfo)
        {
            Vector3 throwDirection = (hitInfo.point - GameManager.Instance.PlayerController.transform.position).normalized;
            Vector3 lookDirection = Vector3.ProjectOnPlane(throwDirection, Vector3.up).normalized;

            Vector3 anchorPoint = hitInfo.point;
            anchorPoint -= throwDirection * 0.02f; //back up a bit
            anchorPoint.y += 0.05f;

            Ray ray;

            ray = new Ray(anchorPoint, lookDirection);
            if (Physics.Raycast(ray, 0.6f))
                return Vector3.zero; //expecting open space

            //extend into open space past expected ledge
            anchorPoint += lookDirection * 0.6f;

            //drop below ledge
            anchorPoint.y -= 0.1f;

            if (Physics.CheckSphere(anchorPoint, 0.1f))
                return Vector3.zero; //was expecting open space

            ray = new Ray(anchorPoint, -lookDirection);
            if (!Physics.Raycast(ray, out RaycastHit ledgeHit, 0.6f))
                return Vector3.zero; //should have hit ledge wall there

            //anchor the hook close enough to the wall to grab wall if needed
            anchorPoint = ledgeHit.point + lookDirection * 0.05f;
            anchorPoint.y = hitInfo.point.y;

            return anchorPoint;
        }


        /// <summary>
        /// Animate throwing the hook and create the hook and rope GameObjects at the specified anchoring point.
        /// </summary>
        static IEnumerator ThrowHook(Vector3 location, float length)
        {
            if (hook)
            {
                hook.SetActive(false);
                GameObject.Destroy(hook);
            }

            WeaponManager weapons = GameManager.Instance.WeaponManager;
            if (!weapons.Sheathed)
                weapons.ToggleSheath();

            yield return new WaitForSeconds(0.1f);

            GameObject player = GameManager.Instance.PlayerObject;
            bool throwUp = location.y > player.transform.position.y + 1.3f;
            float distance = Vector3.Distance(location, player.transform.position);

            if (distance < 4)
            {
                //skip the animation for such a short throw
                SetHook(location, length);
                yield break;
            }

            throwing = true;

            //GrapplingHook component should be disabled after animation complete.
            FPSGrapplingHook fpsGrapplingHook = ThePenwickPapersMod.GrapplingHookAnimator;
            fpsGrapplingHook.enabled = true;

            fpsGrapplingHook.DoWindUp(distance);
            while (fpsGrapplingHook.WindingUp)
                yield return null;

            fpsGrapplingHook.enabled = false;


            DaggerfallUI.Instance.DaggerfallAudioSource.PlayOneShot(SoundClips.SwingMediumPitch2, 0f, distance / 40);

            //show hand swipe to indicate throwing...
            ThePenwickPapersMod.HandWaveAnimator.DoHandWave(true);

            if (throwUp)
            {
                //showing visible hook/rope flying to destination
                DaggerfallBillboard flyingHook = CreateFlyingHook();
                Vector3 direction = (location - player.transform.position).normalized;
                flyingHook.transform.position = player.transform.position + direction;

                float countDown = Vector3.Distance(flyingHook.transform.position, location);
                float speed = Time.smoothDeltaTime * 7f;
                while (countDown > 0)
                {
                    yield return null;
                    flyingHook.transform.position += direction * speed;
                    countDown -= speed;
                }
                GameObject.Destroy(flyingHook.gameObject);
            }
            else
            {
                //provide some time for hook to travel to destination
                yield return new WaitForSeconds(distance / 7f);
            }

            SetHook(location, length);

            throwing = false;
        }

        static DaggerfallBillboard CreateFlyingHook()
        {
            GameObject flyingHook = new GameObject(PenwickFlyingHookName);

            Texture2D texture = Assets.GrapplingHookFlying.Get<Texture2D>();

            DaggerfallBillboard dfBillboard = flyingHook.AddComponent<DaggerfallBillboard>();
            float width = (float)texture.width / 40f;
            float height = (float)texture.height / 40f;
            Vector2 size = new Vector2(width, height);

            dfBillboard.SetMaterial(texture, size, false);

            flyingHook.transform.parent = GameObjectHelper.GetBestParent();

            flyingHook.SetActive(true);

            return dfBillboard;
        }


        static void SetHook(Vector3 location, float length)
        {
            //play a clanking noise
            DaggerfallUI.Instance.DaggerfallAudioSource.PlayClipAtPoint(SoundClips.Parry1, location);


            //hook and rope turns invisible when city lights come on, see worldtime.oncitylightson and DayNight class

            //create the basic hook
            hook = new GameObject(PenwickHookName);

            Assets asset = Settings.UsingHiResSprites ? Assets.GrapplingHookHi : Assets.GrapplingHook;
            Texture2D hookTexture = asset.Get<Texture2D>();

            DaggerfallBillboard dfBillboard = hook.AddComponent<DaggerfallBillboard>();
            float divisor = Settings.UsingHiResSprites ? 140 : 70;
            Vector2 hookSize = new Vector2((float)hookTexture.width / divisor, (float)hookTexture.height / divisor);
            dfBillboard.SetMaterial(hookTexture, hookSize, false);

            hook.transform.parent = GameObjectHelper.GetBestParent();
            hook.transform.position = location + Vector3.down * 0.06f; //with adjustment for hook graphic

            MeshRenderer meshRenderer = hook.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            //for making rope creak noises
            hook.AddComponent<DaggerfallAudioSource>();


            //create variable length rope attached to the hook
            rope = new GameObject(PenwickRopeName);
            rope.transform.parent = hook.transform;

            asset = Settings.UsingHiResSprites ? Assets.RopeHi : Assets.Rope;
            Texture2D ropeTexture = asset.Get<Texture2D>();

            dfBillboard = rope.AddComponent<DaggerfallBillboard>();
            Vector2 ropeSize = new Vector2(0.05f, length);
            dfBillboard.SetMaterial(ropeTexture, ropeSize, false);

            //rope placement depends on hook placement
            float y = -length / 2;
            y -= hookSize.y / 2;

            rope.transform.localPosition = new Vector3(0f, y, 0f);

            meshRenderer = rope.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            //tiling texture
            meshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, length * 10);

            //add the collider for climbing, kind of the point
            BoxCollider collider = rope.AddComponent<BoxCollider>();
            Vector3 size = collider.size;
            size.x *= 2; //make the collider a bit wider so it is easier to climb onto
            collider.size = size;

            //handles creaking noises while climbing rope
            rope.AddComponent<RopeClimbing>();
        }


    } //class GrapplingHook



    public class FPSGrapplingHook : MonoBehaviour
    {
        float windUp; //how much of the graphic is shown
        float windUpMax; //the maximum amount of graphic that will be shown
        float windUpDelta; //per-frame increment in graphic shown
        Rect screenRect;
        float offsetHeightForLargeHUD;
        float scaleX;
        float scaleY;
        Texture2D texture;

        public bool WindingUp { get { return windUp > 0; } }

        public void DoWindUp(float distance)
        {
            windUp = 0.01f;
            windUpDelta = Time.smoothDeltaTime * 20;

            //longer distances need a bigger wind up
            windUpMax = 0.3f + Mathf.Clamp(distance / 14f, 0, 0.7f);
            if (distance > 12)
                windUpMax = 1f;
        }


        void Start()
        {
            texture = Assets.GrapplingHookIdle.Get<Texture2D>();
        }


        void OnEnable()
        {
            //Temporarily disabling these.  Will be re-enabled on the OnDisable() call.
            GameManager.Instance.PlayerSpellCasting.enabled = false;
            GameManager.Instance.WeaponManager.enabled = false;

            if (DaggerfallUI.Instance.CustomScreenRect != null)
                screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
            else
                screenRect = new Rect(0, 0, Screen.width, Screen.height);

            // Offset animation by large HUD height when both large HUD and undocked weapon offset enabled
            // Animation is forced to offset when using docked HUD else it would appear underneath HUD
            // This helps user avoid such misconfiguration or it might be interpreted as a bug
            // Same logic as in FPSWeapon
            offsetHeightForLargeHUD = 0;
            if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                DaggerfallUnity.Settings.LargeHUD &&
                (DaggerfallUnity.Settings.LargeHUDUndockedOffsetWeapon || DaggerfallUnity.Settings.LargeHUDDocked))
            {
                offsetHeightForLargeHUD = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
            }

            const float nativeScreenWidth = 300f;
            const float nativeScreenHeight = 200f;
            scaleX = screenRect.width / nativeScreenWidth;
            scaleY = screenRect.height / nativeScreenHeight;

            // Adjust scale to be slightly larger when not using point filtering
            // This reduces the effect of filter shrink at edge of display
            if (DaggerfallUnity.Instance.MaterialReader.MainFilterMode != FilterMode.Point)
            {
                scaleX *= 1.01f;
                scaleY *= 1.01f;
            }

        }


        void OnDisable()
        {
            GameManager.Instance.PlayerSpellCasting.enabled = true;
            GameManager.Instance.WeaponManager.enabled = true;
        }


        void OnGUI()
        {
            //only handling repaint events
            if (!Event.current.type.Equals(EventType.Repaint))
                return;

            if (!WindingUp || GameManager.IsGamePaused)
                return;

            if (windUp >= windUpMax)
                windUpDelta = -windUpDelta;

            //calculating delta of delta (acceleration)
            float delta = windUpDelta * (windUpMax - windUp);
            if (Mathf.Abs(delta) < 0.005f)
                delta = windUpDelta > 0 ? 0.005f : -0.005f;

            windUp = Mathf.Clamp(windUp + delta, 0f, windUpMax);

            if (windUp <= 0)
                return; //finished

            GUI.depth = 1;

            Rect animRect = new Rect(0, 0, 1, 1);

            float imageWidth = scaleX * texture.width;
            float imageHeight = scaleY * texture.height;

            //start x on right side and move left during windup
            float x = screenRect.x + imageWidth / 6 - (windUp * imageWidth / 6);

            //texture/image will slide up from bottom off-screen
            //In Unity GUI, the y-axis increases downward (y=0 is top of screen)
            float y = screenRect.y + screenRect.height - offsetHeightForLargeHUD;
            y -= imageHeight * windUp; //push image upward the appropriate amount

            Rect position = new Rect(
                x,
                y,
                imageWidth,
                imageHeight);

            Color playerTint = ThePenwickPapersMod.Instance.GetPlayerTint();

            DaggerfallUI.DrawTextureWithTexCoords(position, texture, animRect, true, playerTint);

        }


    } //class FPSGrapplingHook



    class RopeClimbing : MonoBehaviour
    {
        ClimbingMotor climbingMotor;
        CharacterController controller;
        DaggerfallAudioSource dfAudio;
        DaggerfallBillboard dfBillboard;
        float lastCreakTime;


        void Start()
        {
            climbingMotor = GameManager.Instance.ClimbingMotor;
            controller = GameManager.Instance.PlayerController;
            dfAudio = transform.parent.GetComponent<DaggerfallAudioSource>();
            dfBillboard = GetComponent<DaggerfallBillboard>();
        }


        void Update()
        {
            if (GameManager.IsGamePaused)
                return;

            if (!climbingMotor.IsClimbing)
            {
                dfAudio.AudioSource.Stop();
                return;
            }

            //Is the player climbing while close to the rope?            
            Vector3 ropeXZ = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 playerXZ = Vector3.ProjectOnPlane(climbingMotor.transform.position, Vector3.up);

            float distance = Vector3.Distance(playerXZ, ropeXZ);

            if (distance >= controller.radius + 0.06f)
                return; //player far from rope, no sounds

            //Is the player at same height as the rope?
            float length = dfBillboard.Summary.Size.y;
            float ropeY = transform.position.y;
            float playerY = climbingMotor.transform.position.y;
            if (Mathf.Abs(playerY - ropeY) > (length + 1f) / 2f)
                return;

            //player appears to be climbing the rope, make occasional creaking sounds
            if (Time.time > lastCreakTime + 2.0f && Dice100.SuccessRoll(2))
            {
                Assets asset = Dice100.SuccessRoll(50) ? Assets.Creak1 : Assets.Creak2;

                AudioClip clip = asset.Get<AudioClip>();

                dfAudio.AudioSource.PlayOneShot(clip, 0.5f);

                lastCreakTime = Time.time;
            }

        }

    } //class RopeClimbing


    public class GrapplingHookAndRope : DaggerfallUnityItem
    {
        public const int HookAndRopeTemplateIndex = 1771;

        const int baseValue = 155;    // Base gold value


        public GrapplingHookAndRope() : this(baseValue)
        {
        }


        public GrapplingHookAndRope(int baseValue) : base(ItemGroups.UselessItems2, HookAndRopeTemplateIndex)
        {
            value = baseValue;
        }


        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(GrapplingHookAndRope).ToString();
            return data;
        }

        public override string ItemName
        {
            get { return Text.HookAndRope.Get(); }
        }


        public override string LongName
        {
            get { return ItemName; }
        }


        public override bool UseItem(ItemCollection collection)
        {
            Utility.MessageBox(Text.HookAndRopeUsage.Get());

            return true;
        }


        public override bool IsStackable()
        {
            return false;
        }

        public override EquipSlots GetEquipSlot()
        {
            return GameManager.Instance.PlayerEntity.ItemEquipTable.GetFirstSlot(EquipSlots.Bracer0, EquipSlots.Bracer1);
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipFlail;
        }


    } //class HookAndRope



} //namespace