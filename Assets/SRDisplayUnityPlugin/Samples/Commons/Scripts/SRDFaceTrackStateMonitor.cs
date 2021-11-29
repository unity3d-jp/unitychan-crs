/*
 * Copyright 2019,2020 Sony Corporation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using SRD.Core;
using SRD.Utils;


namespace SRD.Sample.Common
{
    public class SRDFaceTrackStateMonitor : MonoBehaviour
    {
        public GameObject FaceTrackErrorObject;
        public Material FaceTrackErrorEffectMat;

        public int ContinuousFailCountThreshForStartEffect = 50;
        public int ContinuousSuccessCountThreshForBreakEffect = 20;

        public float FaceTrackErrorEffectFadeInSec = 3f;
        public float FaceTrackErrorEffectFadeBackSec = 0.5f;
        [Tooltip("Max rate for the effect when FaceTrack failed. If this value is zero, the screen will be completely black after `FaceTrackErrorEffectFadeInSec` seconds of FaceTrack failture")]
        public float FaceTrackErrorEffectFadeMaxRate = 0.2f;

        class CommandBufferController
        {
            public Camera Cam;
            public CommandBuffer CmdBuf;
            public CameraEvent CamEvent;

            public bool IsActive { get; private set; }

            public CommandBufferController(Camera cam, CommandBuffer buf, CameraEvent camEvent)
            {
                Cam = cam;
                CmdBuf = buf;
                CamEvent = camEvent;
            }

            public void SetActive(bool active)
            {
                if(IsActive && !active)
                {
                    Cam.RemoveCommandBuffer(CamEvent, CmdBuf);
                    IsActive = false;
                }
                else if(!IsActive && active)
                {
                    Cam.AddCommandBuffer(CamEvent, CmdBuf);
                    IsActive = true;
                }
            }
        }

        private SRDManager _srdManager;
        private CommandBufferController _cmdBufCtrlerL;
        private CommandBufferController _cmdBufCtrlerR;

        private Material _faceTrackErrorObjMat;
        private Coroutine _fadingCoroutine = null;

        private bool _isSuccessing = true;
        private int _faceTrackContinuousFailCounter = 0;
        private int _faceTrackContinuousSuccessCounter = 0;

        private float _currentErrorEffectFadeRate = 1.0f;
        private float _currentErrorObjAlpha;
        private float _defaultErrorObjAlpha;

        private float _errorEffectUpdateDeltaSec = 0.01f;


        void Start()
        {
            if(FaceTrackErrorEffectMat == null)
            {
                Debug.LogError("FaceTrackErrorEffectMat is not set. You must set it.");
                return;
            }

            _srdManager = SRDSceneEnvironment.GetSRDManager();
            var srdCameras = new SRDCameras(_srdManager);

            var camEvent = CameraEvent.BeforeImageEffects;
            var commandBufNameBase = "FaceTrackErrorEffect";
            _cmdBufCtrlerL = new CommandBufferController(srdCameras.LeftEyeCamera,
                                                         CreateFaceTrackErrorEffectCommand(commandBufNameBase + "L"),
                                                         camEvent);
            _cmdBufCtrlerR = new CommandBufferController(srdCameras.RightEyeCamera,
                                                         CreateFaceTrackErrorEffectCommand(commandBufNameBase + "R"),
                                                         camEvent);

            _srdManager.OnFaceTrackStateEvent.AddListener(this.GetFaceTrackState);

            FaceTrackErrorObject.SetActive(false);
            _faceTrackErrorObjMat = FaceTrackErrorObject.GetComponent<MeshRenderer>().material;
            _defaultErrorObjAlpha = _currentErrorObjAlpha = _faceTrackErrorObjMat.GetFloat("_Alpha");
        }

        void OnDisable()
        {
            if(_srdManager == null)
            {
                return;
            }
            _srdManager.OnFaceTrackStateEvent.RemoveListener(this.GetFaceTrackState);
        }

        public void GetFaceTrackState(bool isSuccess)
        {
            if(_isSuccessing)
            {
                _faceTrackContinuousFailCounter = isSuccess ? 0 : (_faceTrackContinuousFailCounter + 1);
                if(ContinuousFailCountThreshForStartEffect < _faceTrackContinuousFailCounter)
                {
                    _isSuccessing = false;
                    _faceTrackContinuousSuccessCounter = 0;
                    StartFaceTrackErrorEffect();
                }
            }
            else
            {
                _faceTrackContinuousSuccessCounter = !isSuccess ? 0 : (_faceTrackContinuousSuccessCounter + 1);
                if(ContinuousSuccessCountThreshForBreakEffect < _faceTrackContinuousSuccessCounter)
                {
                    _isSuccessing = true;
                    _faceTrackContinuousFailCounter = 0;
                    BreakFaceTrackErrorEffect();
                }
            }
        }

        private void StartFaceTrackErrorEffect()
        {
            if(_fadingCoroutine != null)
            {
                StopCoroutine(_fadingCoroutine);
            }

            FaceTrackErrorObject.SetActive(true);
            _cmdBufCtrlerL.SetActive(true);
            _cmdBufCtrlerR.SetActive(true);
            _fadingCoroutine = StartCoroutine(EffectCoroutine(_currentErrorEffectFadeRate, FaceTrackErrorEffectFadeMaxRate,
                                                              _defaultErrorObjAlpha, _defaultErrorObjAlpha,
                                                              FaceTrackErrorEffectFadeInSec, () =>
            {
                _srdManager.IsSRRenderingActive = false;
            }));
        }

        private void BreakFaceTrackErrorEffect()
        {
            if(_fadingCoroutine != null)
            {
                StopCoroutine(_fadingCoroutine);
            }
            _srdManager.IsSRRenderingActive = true;
            _fadingCoroutine = StartCoroutine(EffectCoroutine(_currentErrorEffectFadeRate, 1.0f, _currentErrorObjAlpha, 0.0f,
                                                              FaceTrackErrorEffectFadeBackSec, () =>
            {
                _cmdBufCtrlerL.SetActive(false);
                _cmdBufCtrlerR.SetActive(false);
                FaceTrackErrorObject.SetActive(false);
            }));
        }

        private IEnumerator EffectCoroutine(float fromFadeRate, float toFadeRate, float fromAlpha, float toAlpha,
                                            float durationSec, Action onFinished)
        {
            SetErrorEffectFade(fromFadeRate);
            SetErrorObjAlpha(fromAlpha);

            var yielder = new WaitForSeconds(_errorEffectUpdateDeltaSec);
            var elaspedTime = 0f;
            var startTime = Time.time;
            yield return null;
            while(elaspedTime < durationSec)
            {
                yield return yielder;
                elaspedTime = Time.time - startTime;
                SetErrorEffectFade(fromFadeRate + (toFadeRate - fromFadeRate) * (elaspedTime / durationSec));
                SetErrorObjAlpha(fromAlpha + (toAlpha - fromAlpha) * (elaspedTime / durationSec));
            }
            SetErrorEffectFade(toFadeRate);
            SetErrorObjAlpha(toAlpha);
            if(onFinished != null)
            {
                onFinished();
            }
        }

        private void SetErrorEffectFade(float fadeRate)
        {
            _currentErrorEffectFadeRate = fadeRate;
            FaceTrackErrorEffectMat.SetFloat("_FadeRate", _currentErrorEffectFadeRate);
        }

        private void SetErrorObjAlpha(float alpha)
        {
            _currentErrorObjAlpha = alpha;
            _faceTrackErrorObjMat.SetFloat("_Alpha", _currentErrorObjAlpha);
        }

        private CommandBuffer CreateFaceTrackErrorEffectCommand(string name)
        {
            var buf = new CommandBuffer { name = name };
            int temp = Shader.PropertyToID("_StencilTemp");
            buf.GetTemporaryRT(temp, -1, -1);
            buf.Blit(BuiltinRenderTextureType.CameraTarget, temp);
            buf.Blit(temp, BuiltinRenderTextureType.CameraTarget, FaceTrackErrorEffectMat);
            buf.ReleaseTemporaryRT(temp);

            return buf;
        }
    }
}
