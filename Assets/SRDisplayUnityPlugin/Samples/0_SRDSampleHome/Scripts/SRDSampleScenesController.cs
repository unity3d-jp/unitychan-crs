/*
 * Copyright 2019,2020 Sony Corporation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using SRD.Utils;
using SRD.Core;

namespace SRD.Sample.Home
{

    [RequireComponent(typeof(SRDFade))]
    public class SRDSampleScenesController : MonoBehaviour
    {

        public bool IsFadeTransition = true;
        public float FadeSec = 0.5f;

        private bool _isTransitioning = false;

        private static SRDSampleScenesController _instance;
        public static SRDSampleScenesController Instance
        {
            get
            {
                if(_instance != null)
                {
                    return _instance;
                }

                _instance = (SRDSampleScenesController)FindObjectOfType(typeof(SRDSampleScenesController));
                if(_instance == null)
                {
                    var go = new GameObject("SRDSampleScenesController");
                    go.transform.parent = null;
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    _instance = go.AddComponent<SRDSampleScenesController>();
                }
                return _instance;
            }
        }

        private Dictionary<KeyCode, string> _keyToSceneName;
        private SRDFade _srdFade;

        void Awake()
        {
            if(this != Instance)
            {
                Destroy(this.gameObject);
                return;
            }
            DontDestroyOnLoad(this.gameObject);
        }

        void Start()
        {
            _keyToSceneName = new Dictionary<KeyCode, string>()
            {
                {KeyCode.Alpha1, "SRDisplaySimpleSample" },
                {KeyCode.Alpha2, "SRDisplayLookAtSample" },
                {KeyCode.Alpha3, "SRDisplayUISample" },
                {KeyCode.Alpha4, "SRDisplayPPSv2Sample" },
                {KeyCode.Alpha5, "SRDisplay3DRaycastSample" },
                {KeyCode.Alpha0, "SRDisplaySampleHome" },
            };

            _srdFade = this.GetComponent<SRDFade>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitSRDFade();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitSRDFade();
            if(IsFadeTransition)
            {
                _srdFade.FadeIn(FadeSec, () =>
                {
                    _isTransitioning = false;
                });
            }
            else
            {
                _isTransitioning = false;
            }
        }

        void Update()
        {
            if(_isTransitioning)
            {
                return;
            }

            foreach(var kts in _keyToSceneName)
            {
                if(Input.GetKeyDown(kts.Key))
                {
                    if(IsFadeTransition)
                    {
                        _srdFade.FadeOut(FadeSec, () =>
                        {
                            SceneManager.LoadScene(kts.Value);
                        });
                    }
                    else
                    {
                        SceneManager.LoadScene(kts.Value);
                    }
                    _isTransitioning = true;
                }
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if(this == Instance)
            {
                _instance = null;
            }
        }

        private void InitSRDFade()
        {
            var srdCameras = new SRDCameras(SRDSceneEnvironment.GetSRDManager());
            var camLObject = srdCameras.LeftEyeCamera;
            var camRObject = srdCameras.RightEyeCamera;

            if(camLObject == null || camRObject == null)
            {
                if(IsFadeTransition)
                {
                    Debug.LogError("CameraObject not found. Forced to turn off FadeTransition mode.");
                    IsFadeTransition = false;
                    return;
                }
            }

            _srdFade.Init(camLObject.GetComponent<Camera>(), camRObject.GetComponent<Camera>());
        }
    }
}