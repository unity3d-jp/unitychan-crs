using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(OVRMainMenu))]
public class OVRUGUIEditor : Editor
{
    public void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;	  
    }

    /// <summary>
    /// Check current Unity version for using new GUI system
    /// </summary>
    float CheckUnityVersion(string version)
    {
        string[] splitText = version.Split('.');
        string unityVersion = null;

        unityVersion += splitText[0] + ".";
        unityVersion += splitText[1];

        return float.Parse(unityVersion);
    }
}
