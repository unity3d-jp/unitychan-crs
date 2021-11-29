/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using SRD.Utils;

namespace SRD.Core
{
    internal interface ISRDStereoCompositer : ISRDSubsystem
    {
        bool RegisterSourceStereoTextures(Texture renderTextureL, Texture renderTextureR);
        void RenderStereoComposition(bool IsSRRenderingActive);
    }

    internal class SRDStereoCompositer: ISRDStereoCompositer
    {
        private SrdXrTexture _leftTextureData;
        private SrdXrTexture _rightTextureData;
        private SrdXrTexture _outTextureData;
        private RenderTexture _outTexture;

        private Texture _textureForPassThrough;

        public SRDStereoCompositer()
        {
        }

        public bool RegisterSourceStereoTextures(Texture renderTextureL, Texture renderTextureR)
        {
            if((renderTextureL == null) || (renderTextureR == null))
            {
                Debug.LogError("RenderTextures are not set. Set renderTextures with RenderStereoComposition function");
                return false;
            }

            var width = SRDSettings.DeviceInfo.ScreenRect.Width;
            var height = SRDSettings.DeviceInfo.ScreenRect.Height;

            _textureForPassThrough = renderTextureL;
            _leftTextureData.texture = renderTextureL.GetNativeTexturePtr();
            _rightTextureData.texture = renderTextureR.GetNativeTexturePtr();
            if(_outTexture == null)
            {
                _outTexture = new RenderTexture(width, height, depth: 24, RenderTextureFormat.ARGB32);
                _outTexture.filterMode = FilterMode.Point;
                _outTexture.Create();
                _outTextureData.texture = _outTexture.GetNativeTexturePtr();
            }

            _leftTextureData.width = _rightTextureData.width = _outTextureData.width = (uint)width;
            _leftTextureData.height = _rightTextureData.height = _outTextureData.height = (uint)height;

            SRDCorePlugin.GenerateTextureAndShaders(SRDSessionHandler.SessionHandle, ref _leftTextureData, ref _rightTextureData, ref _outTextureData);
            return true;
        }

        public void RenderStereoComposition(bool IsSRRenderingActive)
        {
            RenderTexture backBuffer = null;
            if(IsSRRenderingActive)
            {
                SRDCorePlugin.EndFrame(SRDSessionHandler.SessionHandle, false, true);
                Graphics.Blit(_outTexture, backBuffer);
            }
            else
            {
                Graphics.Blit(_textureForPassThrough, backBuffer);
            }
        }

        public void Start()
        {
            // do nothing
        }

        public void Stop()
        {
            if(_outTexture != null)
            {
                _outTexture.Release();
                MonoBehaviour.Destroy(_outTexture);
            }
        }

        public void Dispose()
        {
            // do nothing
        }

    }

    internal class SRDPassThroughStereoCompositer : ISRDStereoCompositer
    {
        private Texture _leftTexture;
        private Texture _rightTexture;

        public SRDPassThroughStereoCompositer()
        {
        }
        public bool RegisterSourceStereoTextures(Texture renderTextureL, Texture renderTextureR)
        {
            _leftTexture = renderTextureL;
            _rightTexture = renderTextureR;
            return true;
        }

        public void RenderStereoComposition(bool IsSRRenderingActive)
        {
            Graphics.Blit(_leftTexture, (RenderTexture)null);
        }

        public void Start()
        {
            // do nothing
        }

        public void Stop()
        {
            // do nothing
        }

        public void Dispose()
        {
            // do nothing
        }

    }

}
