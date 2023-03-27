using Verse;

namespace Universum.Utilities {
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.WeatherDecider), "CurrentWeatherCommonality")]
    public static class WeatherDecider_CurrentWeatherCommonality {
        public static void Postfix(WeatherDef weather, ref float __result, RimWorld.WeatherDecider __instance) {
            if (!Cache.allowed_utility(__instance.map, "universum.disable_weather_change")) return;
            if (__instance.map.weatherManager.curWeather == null || weather.defName == __instance.map.weatherManager.curWeather.defName) {
                __result = 1.0f;
                return;
            }
            if (weather.defName == "OuterSpaceWeather") {
                __result = 1.0f;
                return;
            }
            __result = 0.0f;
        }
    }
}
