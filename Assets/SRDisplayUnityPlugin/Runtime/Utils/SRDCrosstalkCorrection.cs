/*
 * Copyright 2019,2020,2021 Sony Corporation
 */

using System.Collections.Generic;

using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    internal class SRDCrosstalkCorrection
    {
        private bool _previousFrameActiveState;
        private SrdXrCrosstalkCorrectionType _previousType;

        private static readonly Dictionary<SrdXrCrosstalkCorrectionType, string> _crosstalkCorrectionTypeNames =
            new Dictionary<SrdXrCrosstalkCorrectionType, string>
        {
            { SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_MEDIUM,        "Medium gradation correction" },
            { SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_ALL,           "All gradation correction" },
            { SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_HIGH_PRECISE,  "All gradation correction (High Precise)" }
        };

        public void Init(ref bool isActive, ref SrdXrCrosstalkCorrectionType type)
        {
            var handle = SRDSessionHandler.SessionHandle;
            SRDCorePlugin.SetActiveStateCrosstalkCorrection(handle, isActive, type);

            UpdateState(ref isActive, ref type);
        }

        public void HookUnityInspector(ref bool isActive, ref SrdXrCrosstalkCorrectionType type)
        {
            ToggleActivateStateIfValueChanged(isActive, type);
            UpdateState(ref isActive, ref type);
        }

        private void ToggleActivateStateIfValueChanged(bool isActive, SrdXrCrosstalkCorrectionType type)
        {
            if(_previousFrameActiveState != isActive || _previousType != type)
            {
                var handle = SRDSessionHandler.SessionHandle;
                SRDCorePlugin.SetActiveStateCrosstalkCorrection(handle, isActive, type);
            }
        }

        private bool UpdateState(ref bool appState, ref SrdXrCrosstalkCorrectionType appType)
        {
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                return true;
            }

            var handle = SRDSessionHandler.SessionHandle;
            var result = SRDCorePlugin.GetActiveStateCrosstalkCorrection(handle, out var pluginState, out var pluginType);

            if(result != SrdXrResult.SUCCESS)
            {
                Debug.LogWarning(string.Format("Failed to set CrosstalkCorrection setting: {0}", result));
            }
            else
            {
                if(appState != pluginState)
                {
                    if(appState)
                    {
                        Debug.LogWarning(
                            "Failed to activate Crosstalk Correction. " +
                            "Crosstalk Correction may not be supported in the installed SDK. " +
                            "Try to update SR Display SDK.");
                    }
                    else
                    {
                        Debug.LogWarning(
                            "Failed to deactivate Crosstalk Correction. " +
                            "Crosstalk Correction may not be supported in the installed SDK. " +
                            "Try to update SR Display SDK.");
                    }
                }
                else if(appType != pluginType)
                {
                    if(!_crosstalkCorrectionTypeNames.TryGetValue(pluginType, out var pluginTypeName))
                    {
                        pluginTypeName = pluginType.ToString();
                    }
                    if(!_crosstalkCorrectionTypeNames.TryGetValue(appType, out var appTypeName))
                    {
                        appTypeName = appType.ToString();
                    }
                    Debug.LogWarningFormat(
                        "Failed to set your CrosstalkCorrection setting to SR Display SDK. " +
                        "Forced to set {0}. {1} may not be supported in the installed SDK. " +
                        "Try to update SR Display SDK. ",
                        pluginTypeName, appTypeName);
                }
            }

            appState = _previousFrameActiveState = pluginState;
            appType  = _previousType = pluginType;
            return (result == SrdXrResult.SUCCESS);
        }
    }
}
