/*
 * Copyright 2019,2020 Sony Corporation
 */

using UnityEngine;

namespace SRD.Utils
{
    /// <summary>
    /// A class for Project Settings of Spatial Reality Display
    /// </summary>
    [SerializeField]
    public class SRDProjectSettings : ScriptableObject
    {
        private static SRDProjectSettings _instance;
        private SRDProjectSettings() { }

        /// <summary>
        /// A flag to run the app with no Spatial Reality Display.
        /// If this is true, the app is able to run with no Spatial Reality Display.
        /// </summary>
        [Tooltip("Check this if you want to run your app with no Spatial Reality Display")]
        public bool RunWithoutSRDisplay;

        internal static SRDProjectSettings GetDefault()
        {
            if(_instance != null)
            {
                _instance = null;
            }
            _instance = ScriptableObject.CreateInstance<SRDProjectSettings>();
            _instance.RunWithoutSRDisplay = false;
            return _instance;
        }

        /// <summary>
        /// Static function to get SRDProjectSettings
        /// </summary>
        /// <returns>SRDProjectSettings instance</returns>
        public static SRDProjectSettings LoadResourcesOrDefault()
        {
            if(_instance != null)
            {
                return _instance;
            }

            _instance = Resources.Load<SRDProjectSettings>("SRDProjectSettings");
            if(_instance != null)
            {
                return _instance;
            }

            return SRDProjectSettings.GetDefault();
        }

        /// <summary>
        /// Just returns current RunWithoutSRDisplay
        /// </summary>
        /// <returns> A flag that shows RunWithoutSRDisplay is ON or not </returns>
        public static bool IsRunWithoutSRDisplayMode()
        {
            return SRDProjectSettings.LoadResourcesOrDefault().RunWithoutSRDisplay;
        }
    }
}
