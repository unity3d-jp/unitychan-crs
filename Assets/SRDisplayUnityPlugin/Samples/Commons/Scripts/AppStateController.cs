/*
 * Copyright 2019,2020 Sony Corporation
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SRD.Core;
using SRD.Utils;

namespace SRD.Sample.Common
{
    [RequireComponent(typeof(TextMesh))]
    internal class AppStateController : MonoBehaviour
    {
        public KeyCode AppExitKey = KeyCode.Escape;
        public KeyCode CameraWindowToggleKey = KeyCode.F10;

        private SRDManager _srdManager;
        private bool _isDebugWindowEnabled = false;

        void Start()
        {
            this.GetComponent<TextMesh>().text = GetTextToShowKey();
            _srdManager = SRDSceneEnvironment.GetSRDManager();
        }

        void OnValidate()
        {
            this.GetComponent<TextMesh>().text = GetTextToShowKey();
        }

        string GetTextToShowKey()
        {
            return string.Format("Press {0} to exit the app", AppExitKey.ToString());
        }

        void Update()
        {
            if(Input.GetKeyDown(AppExitKey))
            {
                if(Application.isPlaying)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }

            if(Input.GetKeyDown(CameraWindowToggleKey))
            {
                _isDebugWindowEnabled = !_isDebugWindowEnabled;
                _srdManager.ShowCameraWindow(_isDebugWindowEnabled);
                Debug.Log(string.Format("Debug window: {0}", _isDebugWindowEnabled ? "ON" : "OFF"));
            }
        }
    }
}
