using ICities;
using CitiesHarmony.API;

namespace YoureFired
{
    public class Mod : IUserMod
    {
        public string Name => "You're Fired!";
        public string Description => "Allows businesses to replace under-educated employees with better educated ones.";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public static string Identifier = "WQ.YF/";
    }
}
