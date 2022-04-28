using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using UnityEngine.InputSystem;
using UnityEngine;
using MijuTools;
using TMPro;

namespace AutoMove_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<Key> configToggleAutoMoveModifierKey;
        private ConfigEntry<Key> configToggleAutoMoveKey;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static bool autoMoveEnabled = false;

        private void Awake()
        {
            configToggleAutoMoveModifierKey = Config.Bind("General", "Toggle_Auto_Move_Modifier_Key", Key.None,
                "Pick the modifier key to use in combination with the key to toggle auto move off/on.");
            configToggleAutoMoveKey = Config.Bind("General", "Toggle_Auto_Move_Key", Key.CapsLock,
                "Pick the key to use in combination with the modifier key to toggle auto move off/on.");

            harmony.PatchAll(typeof(AutoMove_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMovable), "InputOnMove")]
        private static void PlayerMovable_InputOnMove__Postfix(ref Vector2 ___lastMoveAxis)
        {
            autoMoveEnabled = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseHudHandler), "UpdateHud")]
        private static void BaseHudHandler_UpdateHud_Postfix(BaseHudHandler __instance)
        {
            if (!CanMove())
            {
                return;
            }
            if (autoMoveEnabled)
            {
                __instance.textPositionDecoration.text += " - Auto Move";
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
            bool modifierPressed = configToggleAutoMoveModifierKey.Value == Key.None || Keyboard.current[configToggleAutoMoveModifierKey.Value].isPressed;
            bool toggleKeyPressed = Keyboard.current[configToggleAutoMoveKey.Value].wasPressedThisFrame;
            if (modifierPressed && toggleKeyPressed)
            {
                bool newAutoMove = !autoMoveEnabled;
                PlayerMovable playerMovable = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerMovable();
                playerMovable.InputOnMove(newAutoMove ? Vector2.up :  Vector2.zero);
                autoMoveEnabled = newAutoMove;
                Logger.LogInfo($"AutoMove is now {!autoMoveEnabled}");
            }
        }        

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
