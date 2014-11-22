/************************************************************************************

Filename    :   OVRNewGUI.cs
Content     :   Main script to use new Unity GUI
Created     :   July 8, 2014
Authors     :   Homin Lee

Copyright   :   Copyright 2014 Oculus VR, Inc. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.1 (the "License"); 
you may not use the Oculus VR Rift SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.1 

Unless required by applicable law or agreed to in writing, the Oculus VR SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/
using UnityEngine;
#if USE_NEW_GUI
using UnityEngine.UI;
# endif
using System.Collections;

//-------------------------------------------------------------------------------------
/// <summary>
/// Class for Unity new GUI built in 4.6
/// </summary>
public class OVRUGUI
{
#if USE_NEW_GUI

    #region UIGameObject

    private static GameObject NewGUIManager;
    private static GameObject RiftPresent;
    private static GameObject LowPersistence;
    private static GameObject VisionMode;
    private static GameObject FPS;
    private static GameObject Prediction;
    private static GameObject IPD;
    private static GameObject FOV;
    private static GameObject Height;
    private static GameObject SpeedRotationMutipler;
    private static GameObject DeviceDetection;
    private static GameObject ResolutionEyeTexture;
    private static GameObject Latencies;
    #endregion

    #region VRVariables
    [HideInInspector]
    public static string strRiftPresent = null;
    [HideInInspector]
    public static string strLPM = null; //"LowPersistenceMode: ON";
    [HideInInspector]
    public static string strVisionMode = null;//"Vision Enabled: ON";
    [HideInInspector]
    public static string strFPS = null;//"FPS: 0";
    [HideInInspector]
    public static string strIPD = null;//"IPD: 0.000";
    [HideInInspector]
    public static string strPrediction = null;//"Pred: OFF";
    [HideInInspector]
    public static string strFOV = null;//"FOV: 0.0f";
    [HideInInspector]
    public static string strHeight = null;//"Height: 0.0f";
    [HideInInspector]
    public static string strSpeedRotationMultipler = null;//"Spd. X: 0.0f Rot. X: 0.0f";
    [HideInInspector]
    public static OVRPlayerController PlayerController = null;
    [HideInInspector]
    public static OVRCameraController CameraController = null;
    [HideInInspector]
    public static string strDeviceDetection = null;// Device attach / detach
    [HideInInspector]
    public static string strResolutionEyeTexture = null;// "Resolution : {0} x {1}"
    [HideInInspector]
    public static string strLatencies = null;// Device attach / detach
    #endregion

    [HideInInspector]
    public static bool InitUIComponent = false;
    private static float offsetY = 55.0f;
    private static bool isInited = false;
    private static int numOfGUI = 0;
    private static GameObject text;

    /// <summary>
    /// It's for rift present GUI
    /// </summary>
    public static void RiftPresentGUI(GameObject GUIMainOBj)
    {
        RiftPresent = ComponentComposition(RiftPresent);
        RiftPresent.transform.parent = GUIMainOBj.transform;
        RiftPresent.name = "RiftPresent";
        RiftPresent.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        RiftPresent.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        RiftPresent.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
        RiftPresent.GetComponentInChildren<Text>().text = strRiftPresent;
        RiftPresent.GetComponentInChildren<Text>().fontSize = 20;        
    }

    /// <summary>
    /// It's for rift present GUI
    /// </summary>
    public static void UpdateGUI()
    {
        if (InitUIComponent && !isInited)
        {
            InitUIComponents();
        }

        UpdateVariable();
    }

    /// <summary>
    /// Update VR Variables
    /// </summary>
    static void UpdateVariable()
    {
        NewGUIManager.transform.localPosition = new Vector3(0.0f, 100.0f, 0.0f);

        if (!string.IsNullOrEmpty(strLPM))
            LowPersistence.GetComponentInChildren<Text>().text = strLPM;
        if (!string.IsNullOrEmpty(strVisionMode))
            VisionMode.GetComponentInChildren<Text>().text = strVisionMode;
        if (!string.IsNullOrEmpty(strFPS))
            FPS.GetComponentInChildren<Text>().text = strFPS;
        if (!string.IsNullOrEmpty(strPrediction))
            Prediction.GetComponentInChildren<Text>().text = strPrediction;
        if (!string.IsNullOrEmpty(strIPD))
            IPD.GetComponentInChildren<Text>().text = strIPD;
        if (!string.IsNullOrEmpty(strFOV))
            FOV.GetComponentInChildren<Text>().text = strFOV;
        if (!string.IsNullOrEmpty(strResolutionEyeTexture))
            ResolutionEyeTexture.GetComponentInChildren<Text>().text = strResolutionEyeTexture;
        if (!string.IsNullOrEmpty(strLatencies))
            Latencies.GetComponentInChildren<Text>().text = strLatencies;

        if (PlayerController != null)
        {
            if (!string.IsNullOrEmpty(strHeight))
                Height.GetComponentInChildren<Text>().text = strHeight;
            if (!string.IsNullOrEmpty(strSpeedRotationMultipler))
                SpeedRotationMutipler.GetComponentInChildren<Text>().text = strSpeedRotationMultipler;
        }
    }

    /// <summary>
    /// Initialize UI GameObjects
    /// </summary>
    static void InitUIComponents()
    {
        float posY = 0.0f;
        int fontSize = 20;

        NewGUIManager = new GameObject();
        NewGUIManager.name = "GUIManager";
        NewGUIManager.transform.parent = GameObject.Find("OVRGUIMain").transform;
        NewGUIManager.transform.localPosition = Vector3.zero;
        NewGUIManager.transform.localEulerAngles = Vector3.zero;
        NewGUIManager.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        
        // Print out for Low Persistence Mode
        if (!string.IsNullOrEmpty(strLPM))
        {
            LowPersistence = ComponentComposition(LowPersistence);
            LowPersistence.name = "LowPersistence";
            LowPersistence.transform.parent = NewGUIManager.transform;
            LowPersistence.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            LowPersistence.GetComponentInChildren<Text>().text = strLPM;
            LowPersistence.GetComponentInChildren<Text>().fontSize = fontSize;
            LowPersistence.transform.localEulerAngles = Vector3.zero;
            LowPersistence.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }



        // Print out for VisionMode
        if (!string.IsNullOrEmpty(strVisionMode))
        {
            VisionMode = ComponentComposition(VisionMode);
            VisionMode.name = "VisionMode";
            VisionMode.transform.parent = NewGUIManager.transform;
            VisionMode.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            VisionMode.GetComponentInChildren<Text>().text = strVisionMode;
            VisionMode.GetComponentInChildren<Text>().fontSize = fontSize;
            VisionMode.transform.localEulerAngles = Vector3.zero;
            VisionMode.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);

        }

        if (!string.IsNullOrEmpty(strFPS))
        {
            FPS = ComponentComposition(FPS);
            FPS.name = "FPS";
            FPS.transform.parent = NewGUIManager.transform;
            FPS.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            FPS.GetComponentInChildren<Text>().text = strFPS;
            FPS.GetComponentInChildren<Text>().fontSize = fontSize;
            FPS.transform.localEulerAngles = Vector3.zero;
            FPS.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }


        // Print out for Prediction
        if (!string.IsNullOrEmpty(strPrediction))
        {
            Prediction = ComponentComposition(Prediction);
            Prediction.name = "Prediction";
            Prediction.transform.parent = NewGUIManager.transform;
            Prediction.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            Prediction.GetComponentInChildren<Text>().text = strPrediction;
            Prediction.GetComponentInChildren<Text>().fontSize = fontSize;
            Prediction.transform.localEulerAngles = Vector3.zero;
            Prediction.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        // Print out for IPD
        if (!string.IsNullOrEmpty(strIPD))
        {
            IPD = ComponentComposition(IPD);
            IPD.name = "IPD";
            IPD.transform.parent = NewGUIManager.transform;
            IPD.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            IPD.GetComponentInChildren<Text>().text = strIPD;
            IPD.GetComponentInChildren<Text>().fontSize = fontSize;
            IPD.transform.localEulerAngles = Vector3.zero;
            IPD.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        // Print out for FOV{
        if (!string.IsNullOrEmpty(strFOV))
        {
            FOV = ComponentComposition(FOV);
            FOV.name = "FOV";
            FOV.transform.parent = NewGUIManager.transform;
            FOV.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            FOV.GetComponentInChildren<Text>().text = strFOV;
            FOV.GetComponentInChildren<Text>().fontSize = fontSize;
            FOV.transform.localEulerAngles = Vector3.zero;
            FOV.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        if (PlayerController != null)
        {
            // Print out for Height
            if (!string.IsNullOrEmpty(strHeight))
            {
                Height = ComponentComposition(Height);
                Height.name = "Height";
                Height.transform.parent = NewGUIManager.transform;
                Height.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
                Height.GetComponentInChildren<Text>().text = strHeight;
                Height.GetComponentInChildren<Text>().fontSize = fontSize;
                Height.transform.localEulerAngles = Vector3.zero;
                Height.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }

            // Print out for Speed Rotation Multiplier
            if (!string.IsNullOrEmpty(strSpeedRotationMultipler))
            {
                SpeedRotationMutipler = ComponentComposition(SpeedRotationMutipler);
                SpeedRotationMutipler.name = "SpeedRotationMutipler";
                SpeedRotationMutipler.transform.parent = NewGUIManager.transform;
                SpeedRotationMutipler.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
                SpeedRotationMutipler.GetComponentInChildren<Text>().text = strSpeedRotationMultipler;
                SpeedRotationMutipler.GetComponentInChildren<Text>().fontSize = fontSize;
                SpeedRotationMutipler.transform.localEulerAngles = Vector3.zero;
                SpeedRotationMutipler.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }
        }

        if (!string.IsNullOrEmpty(strResolutionEyeTexture))
        {
            ResolutionEyeTexture = ComponentComposition(ResolutionEyeTexture);
            ResolutionEyeTexture.name = "Resolution";
            ResolutionEyeTexture.transform.parent = NewGUIManager.transform;
            ResolutionEyeTexture.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            ResolutionEyeTexture.GetComponentInChildren<Text>().text = strResolutionEyeTexture;
            ResolutionEyeTexture.GetComponentInChildren<Text>().fontSize = fontSize;
            ResolutionEyeTexture.transform.localEulerAngles = Vector3.zero;
            ResolutionEyeTexture.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        if (!string.IsNullOrEmpty(strLatencies))
        {
            Latencies = ComponentComposition(Latencies);
            Latencies.name = "Latency";
            Latencies.transform.parent = NewGUIManager.transform;
            Latencies.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, posY -= offsetY, 0.0f);
            Latencies.GetComponentInChildren<Text>().text = strLatencies;
            Latencies.GetComponentInChildren<Text>().fontSize = 17;
            Latencies.transform.localEulerAngles = Vector3.zero;
            Latencies.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);

            posY = 0.0f;
        }

        InitUIComponent = false;
        isInited = true;

    }

    /// <summary>
    /// Component composition
    /// </summary>
    /// <returns> Composed game object. </returns>
    static GameObject ComponentComposition(GameObject GO)
    {
        GO = new GameObject();
        GO.AddComponent<RectTransform>();
        GO.AddComponent<CanvasRenderer>();
        GO.AddComponent<Image>();
        GO.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 50f);
        GO.GetComponent<Image>().color = new Color(7f / 255f, 45f / 255f, 71f / 255f, 200f / 255f);

        text = new GameObject();
        text.AddComponent<RectTransform>();
        text.AddComponent<CanvasRenderer>();
        text.AddComponent<Text>();
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 50f);
        text.GetComponent<Text>().font = (Font)Resources.Load("DINPro-Bold");
        text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        text.transform.parent = GO.transform;
        text.name = "TextBox";

        return GO;
    }

#endif
}
