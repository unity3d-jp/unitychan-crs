/*
 * Copyright 2019,2020 Sony Corporation
 */

using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
#endif

namespace SRD.Sample.Home
{
    public class BuildSettingsPreparer
    {
        static private List<string> _scenePaths = new List<string>
        {
            "0_SRDSampleHome/Scenes/SRDisplaySampleHome.unity",
            "1_SRDSimpleSample/Scenes/SRDisplaySimpleSample.unity",
            "2_SRDLookAtSample/Scenes/SRDisplayLookAtSample.unity",
            "3_SRDUISample/Scenes/SRDisplayUISample.unity",
            "4_SRDPostProcessingSample/Scenes/SRDisplayPPSv2Sample.unity",
            "5_SRD3DRaycastSample/Scenes/SRDisplay3DRaycastSample.unity",
        };

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorSceneManager.sceneOpened += OnOpened;
        }

        private static void OnOpened(Scene scene, OpenSceneMode mode)
        {
            var targetSceneName = "SRDisplaySampleHome";
            if(scene.name != targetSceneName)
            {
                return;
            }

            var splittedPath = scene.path.Split('/').ToList();
            splittedPath.RemoveRange(splittedPath.Count - 3, 3);
            var basePath = string.Join("/", splittedPath);

            var addScenes = new List<string>() { };
            var currentSetScenePaths = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            foreach(var sceneName in _scenePaths)
            {
                if(Array.IndexOf(currentSetScenePaths, basePath + "/" + sceneName) < 0)
                {
                    addScenes.Add(sceneName);
                }
            }

            if(addScenes.Count == 0)
            {
                return;
            }

            var messageList = new List<string>()
            {
                string.Format("Add the following scenes for this {0} Scene: ", targetSceneName), string.Format("(If you cancel this dialog, {0} does not work)\n", targetSceneName),
            };
            messageList.AddRange(addScenes.Select(s => "  - " + s));

            var message = string.Join("\n", messageList);
            if(EditorUtility.DisplayDialog("Confirm", message, "OK"))
            {
                var currentNum = EditorBuildSettings.scenes.Length;
                var addNum = addScenes.Count;
                var result = new EditorBuildSettingsScene[currentNum + addNum];
                Array.Copy(EditorBuildSettings.scenes, result, currentNum);
                for(var i = 0; i < addNum; i++)
                {
                    result[currentNum + i] = new EditorBuildSettingsScene(basePath + "/" + addScenes[i], true);
                }
                Array.Sort(result, (a, b) => string.Compare(a.path, b.path));
                EditorBuildSettings.scenes = result;
            }
        }
#endif
    }
}
