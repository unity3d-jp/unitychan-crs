/*
 * Copyright 2019,2020 Sony Corporation
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;

using SRD.Core;
using System.Security.Permissions;
using System.Diagnostics;
using System.ComponentModel.Design;

namespace SRD.Editor
{
    [CustomEditor(typeof(SRDManager))]
    internal class SRDManagerInspector : UnityEditor.Editor
    {
        private bool _hasMultipleManagersInScene = false;
        private string _errorMessage = "Multiple SRDManagers in a scene is not supported. Remove unnecessary SRDManagers.";

        private void OnEnable()
        {
            var managersNum = new List<Core.SRDManager>(FindObjectsOfType<Core.SRDManager>()).Count();
            _hasMultipleManagersInScene = (managersNum > 1);
            if(_hasMultipleManagersInScene)
            {
                UnityEngine.Debug.LogError(_errorMessage);
                EditorUtility.DisplayDialog("ERROR", _errorMessage, "OK");
                var instance = (Core.SRDManager)target;
                EditorApplication.delayCall += () => UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}

