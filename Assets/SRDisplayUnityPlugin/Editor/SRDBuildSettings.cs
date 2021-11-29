/*
 * Copyright 2019,2020 Sony Corporation
 */

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace SRD.Editor
{
    internal class SRDBuildSettings : IActiveBuildTargetChanged
    {
        public int callbackOrder { get { return 0; } }

        [UnityEditor.InitializeOnLoadMethod]
        public static void CheckBuildTargetOnLoad()
        {
            ForceSwitchBuildTargetIfNeeded(EditorUserBuildSettings.activeBuildTarget);
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            ForceSwitchBuildTargetIfNeeded(newTarget);
        }

        public static void ForceSwitchBuildTargetIfNeeded(BuildTarget currentTarget)
        {
            if(currentTarget != BuildTarget.StandaloneWindows64)
            {
                var message = "Spatial Reality Display supports Windows x64 only. Force to switch build target to Windows x64.";
                if(EditorUtility.DisplayDialog("Confirm", message, "OK"))
                {
                    EditorApplication.update += WaitForSwitchBuildTargetComplete;
                    Debug.Log(message);
                }
                else
                {
                    Debug.LogError(string.Format("Current BuildPlatform({0}) is NOT supported. Switch to Windows x64", currentTarget));
                }
            }
        }

        static void WaitForSwitchBuildTargetComplete()
        {
            var result = EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            if(result)
            {
                EditorApplication.update -= WaitForSwitchBuildTargetComplete;
            }
        }
    }
}
