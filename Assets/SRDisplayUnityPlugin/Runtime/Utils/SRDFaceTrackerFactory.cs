/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections.Generic;

using SRD.Core;

namespace SRD.Utils
{
    public enum FaceTrackerSystem
    {
        SRD,
        Mouse
    }

    internal class SRDFaceTrackerFactory
    {
        public static ISRDFaceTracker CreateFaceTracker(FaceTrackerSystem system)
        {
            var switcher = new Dictionary<FaceTrackerSystem, Func<ISRDFaceTracker>>()
            {
                {FaceTrackerSystem.SRD, () => { return new SRDFaceTracker(); } },
                {FaceTrackerSystem.Mouse, () => { return new MouseBasedFaceTracker(); } },
            };
            return switcher[system]();
        }
    }
}
