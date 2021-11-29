/*
 * Copyright 2019,2020 Sony Corporation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRD.Sample.Home
{
    public class SRDFade : MonoBehaviour
    {
        public Material FadeMaterial;
        private Camera _camL;
        private Camera _camR;
        private CommandBuffer _cmdBufL;
        private CommandBuffer _cmdBufR;
        private float _fadeUpdateDeltaSec = 0.01f;

        private enum FadeMode { In, Out };

        void Start()
        {
            if(FadeMaterial == null)
            {
                Debug.LogError("FadeMaterial is not set. Please set it.");
            }
        }

        public void Init(Camera camL, Camera camR)
        {
            SetFadeRate(1f);

            _camL = camL;
            _camR = camR;
            _cmdBufL = new CommandBuffer();
            _cmdBufR = new CommandBuffer();
            CreateFadeCommand(ref _cmdBufL, ref _camL);
            CreateFadeCommand(ref _cmdBufR, ref _camR);
        }

        public void FadeIn(float durationSec, Action onFinished = null)
        {
            StartCoroutine(FadeCoroutine(FadeMode.In, durationSec, () =>
            {
                if(onFinished != null)
                {
                    onFinished();
                }
                RemoveFadeEffect();
            }));
        }

        public void FadeOut(float durationSec, Action onFinished = null)
        {
            StartCoroutine(FadeCoroutine(FadeMode.Out, durationSec, onFinished));
        }

        public void RemoveFadeEffect()
        {
            _camL.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmdBufL);
            _camR.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmdBufR);
        }

        private IEnumerator FadeCoroutine(FadeMode mode, float durationSec, Action onFinished)
        {
            _camL.AddCommandBuffer(CameraEvent.AfterEverything, _cmdBufL);
            _camR.AddCommandBuffer(CameraEvent.AfterEverything, _cmdBufR);
            SetFadeRate((mode == FadeMode.In) ? 0f : 1f);

            var yielder = new WaitForSeconds(_fadeUpdateDeltaSec);
            var elaspedTime = 0f;
            var startTime = Time.time;
            yield return null;
            while(elaspedTime < durationSec)
            {
                yield return yielder;
                elaspedTime = Time.time - startTime;
                var rate = (mode == FadeMode.In) ? (elaspedTime / durationSec) : (1.0f - elaspedTime / durationSec);
                SetFadeRate(rate);
            }
            SetFadeRate((mode == FadeMode.In) ? 1f : 0f);
            if(onFinished != null)
            {
                onFinished();
            }
        }

        private void CreateFadeCommand(ref CommandBuffer buf, ref Camera cam)
        {
            if(FadeMaterial == null)
            {
                return;
            }
            int temp = Shader.PropertyToID("_Temp");
            buf.GetTemporaryRT(temp, -1, -1, 0, FilterMode.Bilinear);
            buf.Blit(cam.targetTexture, temp);
            buf.Blit(temp, cam.targetTexture, FadeMaterial);
            buf.ReleaseTemporaryRT(temp);
        }

        private void SetFadeRate(float rate)
        {
            if(FadeMaterial == null)
            {
                return;
            }
            var r = Mathf.Clamp01(rate);
            FadeMaterial.SetFloat("_FadeRate", r);
        }
    }

}
