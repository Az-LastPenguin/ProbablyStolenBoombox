using HarmonyLib;
using Il2Cpp;

namespace ProbablyStolenBoombox
{
    public static class Patches
    {
        [HarmonyPatch(typeof(MiscItemDirectory), nameof(MiscItemDirectory.MusicCassette))]
        [HarmonyPostfix]
        public static void CassetteMakeSmall(ref GameItem __result)
        {
            __result.shape.Cast<GridShapeBuilder>().SetDataFill(1, 1);
        }
    }
}
