using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace DayNightCycleTweaks_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static ManualLogSource bepInExLogger;
        private static Plugin instance;

        private static ConfigEntry<bool> configModifyDayNightCycle;
        private ConfigEntry<float> configDayTimeInSeconds;
        private ConfigEntry<float> configNightTimeInSeconds;
        private ConfigEntry<float> configDayToNightTransitionTimeInSeconds;
        private ConfigEntry<float> configNightToDayTransitionTimeInSeconds;
        private static ConfigEntry<float> configDayNightUpdatePeriodInSeconds;

        private const float DAY_LERP_VALUE = 0.0f;
        private const float NIGHT_LERP_VALUE = 100.0f;

        private float dayNightLerpValue = 0.0f;
        private float startLoopTime = 0.0f;

        private float dayEndLoopTime = 0.0f;
        private float nightStartLoopTime = 0.0f;
        private float nightEndLoopTime = 0.0f;
        private float endLoopTime = 0.0f;

        private void Awake()
        {
            bepInExLogger = Logger;
            instance = this;

            configModifyDayNightCycle = Config.Bind<bool>("Times", "Modify_Day_Night_Cycle", true, "Whether or not to change the game day/night cycle.");
            configDayTimeInSeconds = Config.Bind<float>("Times", "Day_Time_In_Seconds", 960.0f, "How long the day should last in seconds.");
            configNightTimeInSeconds = Config.Bind<float>("Times", "Night_Time_In_Seconds", 180.0f, "How long the night should last in seconds.");
            configDayToNightTransitionTimeInSeconds = Config.Bind<float>("Times", "Day_To_Night_Transition_Time_In_Seconds", 140.0f, 
                "How long for the day->night transition in seconds.");
            configNightToDayTransitionTimeInSeconds = Config.Bind<float>("Times", "Night_To_Day_Transition_Time_In_Seconds", 140.0f, 
                "How long for the night->day transition in seconds.");
            configDayNightUpdatePeriodInSeconds = Config.Bind<float>("Times", "Day_Night_Update_Period_In_Seconds", 0.5f, 
                "How much time to wait between recalculating the day/night cycle. Smaller is smoother but more CPU.");

            dayEndLoopTime = configDayTimeInSeconds.Value;
            nightStartLoopTime = dayEndLoopTime + configDayToNightTransitionTimeInSeconds.Value;
            nightEndLoopTime = nightStartLoopTime + configNightTimeInSeconds.Value;
            endLoopTime = nightEndLoopTime + configNightToDayTransitionTimeInSeconds.Value;

            harmony.PatchAll(typeof(DayNightCycleTweaks_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnvironmentDayNightCycle), "Start")]
        private static bool EnvironmentDayNightCycle_Start_Prefix(EnvironmentDayNightCycle __instance)
        {
            bepInExLogger.LogInfo($"fullDayStayTime={__instance.fullDayStayTime}, nightFallSpeed={__instance.nightFallSpeed}");
            if (configModifyDayNightCycle.Value)
            {
                instance.StartDayNightCycle();
                return false;
            }
            return true;
        }

        private void StartDayNightCycle()
        {
            startLoopTime = Time.time;
            StartCoroutine(SetDayNightLerpValue(configDayNightUpdatePeriodInSeconds.Value));
        }

        private IEnumerator SetDayNightLerpValue(float timeRepeat)
        {
            for (; ; )
            {
                float curLoopTime = Time.time - startLoopTime;
                if (curLoopTime <= dayEndLoopTime)
                {
                    dayNightLerpValue = DAY_LERP_VALUE;
                }
                else if (curLoopTime <= nightStartLoopTime)
                {
                    dayNightLerpValue = Mathf.Lerp(DAY_LERP_VALUE, NIGHT_LERP_VALUE, (curLoopTime - dayEndLoopTime)/configDayToNightTransitionTimeInSeconds.Value);
                }
                else if (curLoopTime <= nightEndLoopTime)
                {
                    dayNightLerpValue = NIGHT_LERP_VALUE;
                }
                else if (curLoopTime <= endLoopTime)
                {
                    dayNightLerpValue = Mathf.Lerp(NIGHT_LERP_VALUE, DAY_LERP_VALUE, (curLoopTime - nightEndLoopTime)/configNightToDayTransitionTimeInSeconds.Value);
                }
                else
                {
                    dayNightLerpValue = DAY_LERP_VALUE;
                    startLoopTime = Time.time;
                }

                yield return new WaitForSeconds(timeRepeat);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnvironmentDayNightCycle), "GetDayNightLerpValue")]
        private static void EnvironmentDayNightCycle_GetDayNightLerpValue_Postfix(ref float __result)
        {
            __result = instance.dayNightLerpValue;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
