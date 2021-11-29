/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections.Generic;
using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    public enum EyeViewRendererSystem
    {
        UnityRenderCam, Texture,
    }

    internal class SRDEyeViewRendererFactory
    {
        public static ISRDEyeViewRenderer CreateEyeViewRenderer(EyeViewRendererSystem system)
        {
            var switcher = new Dictionary<EyeViewRendererSystem, Func<ISRDEyeViewRenderer>>()
            {
                {
                    EyeViewRendererSystem.UnityRenderCam, () =>
                    {
                        return new SRDEyeViewRenderer();
                    }
                },
                {
                    EyeViewRendererSystem.Texture, () => {
                        return new SRDTexturesBasedEyeViewRenderer(null, null);
                    }
                },
            };
            return switcher[system]();
        }
    }
}
