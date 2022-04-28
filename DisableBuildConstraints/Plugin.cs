using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using UnityEngine.InputSystem;
using TMPro;
using MijuTools;

namespace DisableBuildConstraints_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<Key> configToggleBuildConstraintsModifierKey;
        private ConfigEntry<Key> configToggleBuildConstraintsKey;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static bool constraintsDisabled = false;

        private void Awake()
        {
            configToggleBuildConstraintsModifierKey = Config.Bind("General", "Toggle_Build_Constraints_Modifier_Key", Key.LeftCtrl,
                "Pick the modifier key to use in combination with the key to toggle building constraints off/on.");
            configToggleBuildConstraintsKey = Config.Bind("General", "Toggle_Build_Constraints_Key", Key.G,
                "Pick the key to use in combination with the modifier key to toggle building constraints off/on.");

            harmony.PatchAll(typeof(DisableBuildConstraints_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildConstraint), "GetIsRespected")]
        private static void BuildConstraint_GetIsRespected_Postfix(ref bool __result)
        {
            if (constraintsDisabled)
            {
                __result = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseHudHandler), "UpdateHud")]
        private static void BaseHudHandler_UpdateHud_Postfix(BaseHudHandler __instance)
        {
            if (!CanMove())
            {
                return;
            }
            if (constraintsDisabled)
            {
                __instance.textPositionDecoration.text += " - No Build Constraints";
            }
        }

        private static bool CanMove()
        {
            PlayersManager playersManager = Managers.GetManager<PlayersManager>();
            if (playersManager != null)
            {
                return playersManager.GetActivePlayerController().GetPlayerCanAct().GetCanMove();
            }
            return false;
        }

        private void Update()
        {
            if (Keyboard.current[configToggleBuildConstraintsModifierKey.Value].isPressed && Keyboard.current[configToggleBuildConstraintsKey.Value].wasPressedThisFrame)
            {
                constraintsDisabled = !constraintsDisabled;
                Logger.LogInfo($"Building constraints are now {!constraintsDisabled}");
            }
        }        

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
