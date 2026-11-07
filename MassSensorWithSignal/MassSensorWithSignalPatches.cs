using HarmonyLib;
using UnityEngine;

namespace MassSensorWithSignal;

internal static class MassSensorWithSignalPatches
{
    private const int DefaultCharacterLimit = 6;
    private const int SignalSensorCharacterLimit = 10;

    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    private static class GeneratedBuildings_LoadGeneratedBuildings_Patch
    {
        private static void Prefix()
        {
            ModUtil.AddBuildingToPlanScreen("Automation", SignalMassSensorConfig.ID);
        }
    }

    /// <summary>
    /// The vanilla threshold input is limited to 6 characters, which is too short for total mass values.
    /// </summary>
    [HarmonyPatch(typeof(ThresholdSwitchSideScreen), nameof(ThresholdSwitchSideScreen.SetTarget))]
    private static class ThresholdSwitchSideScreen_SetTarget_Patch
    {
        private static void Postfix(ThresholdSwitchSideScreen __instance, GameObject new_target)
        {
            if (new_target == null) return;

            try
            {
                var inputField = Traverse.Create(__instance).Field("numberInput").Field("inputField");
                if (!inputField.FieldExists()) return;

                int limit = new_target.GetComponent<SignalMassSensor>() != null
                    ? SignalSensorCharacterLimit
                    : DefaultCharacterLimit;

                var property = inputField.Property("characterLimit");
                if (property.PropertyExists())
                    property.SetValue(limit);
                else
                    inputField.Field("characterLimit").SetValue(limit);
            }
            catch (System.Exception e)
            {
                MassSensorWithSignalLog.Error("Failed to adjust the threshold input limit: " + e);
            }
        }
    }
}
