// Project:     The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: July 2023


namespace ThePenwickPapers
{

    public enum Assets
    {
        //textures
        PeepSlit,
        PeepHole,
        GrapplingHookHand,
        GrapplingHookFlying,
        GrapplingHook,
        GrapplingHookHi,
        Rope,
        RopeHi,
        GrapplingHookIdle,
        LockPickPanel,
        LockPickPlate,
        KeyholePlate,
        Tumbler,

        //sounds
        WarpIn,
        ReanimateWarp,
        Creak1,
        Creak2,
        MaleOi,
        FemaleLaugh,
        MaleBreath,
        FemaleBreath,
        WindNoise,
        LockpickDrop,
        LockpickFail,
        LockpickUp,
        LockpickMetal1,
        LockpickMetal2,
        LockpickMetal3,
        LockpickMetal4,

    } //enum Assets


    public static class AssetsExtension
    {
        /// <summary>
        /// Gets the mod asset corresponding to the provided key.
        /// </summary>
        public static T Get<T>(this Assets key) where T : UnityEngine.Object
        {
            return ThePenwickPapersMod.Mod.GetAsset<T>(key.ToString());
        }

    }

} //namespace

