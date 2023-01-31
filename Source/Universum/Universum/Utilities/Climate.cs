using System.Reflection;
using Verse;

namespace Universum.Utilities {
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.WeatherDecider), "CurrentWeatherCommonality")]
    public static class WeatherDecider_CurrentWeatherCommonality {
        public static void Postfix(WeatherDef weather, ref float __result, RimWorld.WeatherDecider __instance) {
            Map map = (Map) typeof(RimWorld.WeatherDecider).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(map, "universum.disable_weather_change")) return;
            if (map.weatherManager.curWeather == null || weather.defName == map.weatherManager.curWeather.defName) {
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
