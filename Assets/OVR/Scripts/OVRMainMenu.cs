/************************************************************************************

Filename    :   OVRMainMenu.cs
Content     :   Main script to run various Unity scenes
Created     :   January 8, 2013
Authors     :   Peter Giokaris, Homin Lee

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

//#define SHOW_DK2_VARIABLES

using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// OVRMainMenu is used to control the loading of different scenes. It also renders out 
/// a menu that allows a user to modify various Rift settings, and allow for storing 
/// these settings for recall later.
/// 
/// A user of this component can add as many scenes that they would like to be able to 
/// have access to.
///
/// OVRMainMenu is currently attached to the OVRPlayerController prefab for convenience, 
/// but can safely removed from it and added to another GameObject that is used for general 
/// Unity logic.
///
/// </summary>
public class OVRMainMenu : MonoBehaviour
{
	private OVRPresetManager	PresetManager 	= new OVRPresetManager();
	
	public float 	FadeInTime    	= 2.0f;
	public Texture 	FadeInTexture 	= null;
	public Font 	FontReplace		= null;

	public KeyCode	MenuKey			= KeyCode.Space;
	public KeyCode	QuitKey			= KeyCode.Escape;
	
	// Scenes to show onscreen
	public string [] SceneNames;
	public string [] Scenes;
	
	private bool ScenesVisible   	= false;
	
	// Spacing for scenes menu
	private int    	StartX			= 490;
	private int    	StartY			= 250;
	private int    	WidthX			= 300;
	private int    	WidthY			= 23;
	
	// Spacing for variables that users can change
	private int    	VRVarsSX		= 553;
	private int		VRVarsSY		= 250;
	private int    	VRVarsWidthX 	= 175;
	private int    	VRVarsWidthY 	= 23;

	private int    	StepY			= 25;
		
	// Handle to OVRCameraController
	private OVRCameraController CameraController = null;
	
	// Handle to OVRPlayerController
	private OVRPlayerController PlayerController = null;
	
	// Controller buttons
	private bool  PrevStartDown;
	private bool  PrevHatDown;
	private bool  PrevHatUp;
	
	private bool  ShowVRVars;
	
	private bool  OldSpaceHit;
	
	// FPS 
	private float  UpdateInterval 	= 0.5f;
	private float  Accum   			= 0; 	
	private int    Frames  			= 0; 	
	private float  TimeLeft			= 0; 				
	private string strFPS			= "FPS: 0";
	
	// IPD shift from physical IPD
	public float   IPDIncrement		= 0.0025f;
	private string strIPD 			= "IPD: 0.000";	
	
	// Prediction (in ms)
	public float   PredictionIncrement = 0.001f; // 1 ms
	private string strPrediction       = "Pred: OFF";	
	
	// FOV Variables
	public float   FOVIncrement		= 0.2f;
	private string strFOV     		= "FOV: 0.0f";
	
	// Height adjustment
	public float   HeightIncrement   = 0.01f;
	private string strHeight     	 = "Height: 0.0f";
	
	// Speed and rotation adjustment
	public float   SpeedRotationIncrement   	= 0.05f;
	private string strSpeedRotationMultipler    = "Spd. X: 0.0f Rot. X: 0.0f";
	
	private bool   LoadingLevel 	= false;	
	private float  AlphaFadeValue	= 1.0f;
	private int    CurrentLevel		= 0;
	
	// Rift detection
	private bool   HMDPresent           = false;
	private bool   SensorPresent        = false;
	private float  RiftPresentTimeout   = 0.0f;
	private string strRiftPresent		= "";

	// Replace the GUI with our own texture and 3D plane that
	// is attached to the rendder camera for true 3D placement
	private OVRGUI  		GuiHelper 		 = new OVRGUI();
	private GameObject      GUIRenderObject  = null;
	private RenderTexture	GUIRenderTexture = null;

	// We can set the layer to be anything we want to, this allows
	// a specific camera to render it
	public string 			LayerName 		 = "Default";

	// Crosshair system, rendered onto 3D plane
	public Texture  CrosshairImage 			= null;
	private OVRCrosshair Crosshair        	= new OVRCrosshair();

	// Low Persistence mode on/off 
	private bool LowPersistenceMode = true;

    // Resolution Eye Texture
    private string strResolutionEyeTexture = "Resolution: 0 x 0";

    // Latency values
    private string strLatencies = "Ren: 0.0f TWrp: 0.0f PostPresent: 0.0f";

#if	SHOW_DK2_VARIABLES
	private string strLPM = "LowPersistenceMode: ON";
#endif
	
	// Vision mode on/off
	private bool VisionMode = true;
#if	SHOW_DK2_VARIABLES
	private string strVisionMode = "Vision Enabled: ON";
#endif

	// We want to hold onto GridCube, for potential sharing
	// of the menu RenderTarget
	OVRGridCube GridCube = null;

	// We want to hold onto the VisionGuide so we can share
	// the menu RenderTarget
	OVRVisionGuide VisionGuide = null;

	#region MonoBehaviour Message Handlers
	/// <summary>
	/// Awake this instance.
	/// </summary>
	void Awake()
	{    
        // Find camera controller
		OVRCameraController[] CameraControllers;
		CameraControllers = gameObject.GetComponentsInChildren<OVRCameraController>();
		
		if(CameraControllers.Length == 0)
			Debug.LogWarning("OVRMainMenu: No OVRCameraController attached.");
		else if (CameraControllers.Length > 1)
			Debug.LogWarning("OVRMainMenu: More then 1 OVRCameraController attached.");
		else{
			CameraController = CameraControllers[0];
		}
	
		// Find player controller
		OVRPlayerController[] PlayerControllers;
		PlayerControllers = gameObject.GetComponentsInChildren<OVRPlayerController>();
		
		if(PlayerControllers.Length == 0)
			Debug.LogWarning("OVRMainMenu: No OVRPlayerController attached.");
		else if (PlayerControllers.Length > 1)
			Debug.LogWarning("OVRMainMenu: More then 1 OVRPlayerController attached.");
		else{
            PlayerController = PlayerControllers[0];
        }
    }

	void Start()
	{
		AlphaFadeValue = 1.0f;	
		CurrentLevel   = 0;
		PrevStartDown  = false;
		PrevHatDown    = false;
		PrevHatUp      = false;
		ShowVRVars	   = false;
		OldSpaceHit    = false;
		strFPS         = "FPS: 0";
		LoadingLevel   = false;	
		ScenesVisible  = false;
		
		// Ensure that camera controller variables have been properly
		// initialized before we start reading them
		if(CameraController != null)
			CameraController.InitCameraControllerVariables();

		// Set the GUI target
		GUIRenderObject = GameObject.Instantiate(Resources.Load("OVRGUIObjectMain")) as GameObject;

		if(GUIRenderObject != null)
		{
			// Chnge the layer
			GUIRenderObject.layer = LayerMask.NameToLayer(LayerName);

			if(GUIRenderTexture == null)
			{
				int w = Screen.width;
				int h = Screen.height;

				// We don't need a depth buffer on this texture
				GUIRenderTexture = new RenderTexture(w, h, 0);	
				GuiHelper.SetPixelResolution(w, h);
				// NOTE: All GUI elements are being written with pixel values based
				// from DK1 (1280x800). These should change to normalized locations so 
				// that we can scale more cleanly with varying resolutions
				//GuiHelper.SetDisplayResolution(OVRDevice.HResolution, 
				//								 OVRDevice.VResolution);
				GuiHelper.SetDisplayResolution(1280.0f, 800.0f);
			}
		}
		
		// Attach GUI texture to GUI object and GUI object to Camera
		if(GUIRenderTexture != null && GUIRenderObject != null)
		{
			GUIRenderObject.renderer.material.mainTexture = GUIRenderTexture;
			
			if(CameraController != null)
            {
                // Grab transform of GUI object
                Vector3 ls = GUIRenderObject.transform.localScale;
                Vector3 lp = GUIRenderObject.transform.localPosition;
                Quaternion lr = GUIRenderObject.transform.localRotation;

                // Attach the GUI object to the camera
                CameraController.AttachGameObjectToCamera(ref GUIRenderObject);
                // Reset the transform values (we will be maintaining state of the GUI object
                // in local state)

                GUIRenderObject.transform.localScale = ls;
                GUIRenderObject.transform.localRotation = lr;

                // Deactivate object until we have completed the fade-in
                // Also, we may want to deactive the render object if there is nothing being rendered
                // into the UI
                // we will move the position of everything over to account for the IPD camera offset. 
                float ipdOffsetDirection = 1.0f;
                Transform guiParent = GUIRenderObject.transform.parent;
                if (guiParent != null)
                {
                    OVRCamera ovrCamera = guiParent.GetComponent<OVRCamera>();
                    if (ovrCamera != null && ovrCamera.RightEye)
                        ipdOffsetDirection = -1.0f;
                }

                float ipd = 0.0f;
                CameraController.GetIPD(ref ipd);
                lp.x += ipd * 0.5f * ipdOffsetDirection;
                GUIRenderObject.transform.localPosition = lp;

                GUIRenderObject.SetActive(false);
            }
		}
		
		// Save default values initially
		StoreSnapshot("DEFAULT");
		
		// Make sure to hide cursor 
		if(Application.isEditor == false)
		{
			Screen.showCursor = false; 
			Screen.lockCursor = true;
		}
		
		// CameraController updates
		if(CameraController != null)
		{
			// Set LPM on by default
			OVRDevice.SetLowPersistenceMode(LowPersistenceMode);

			// Add a GridCube component to this object
			GridCube = gameObject.AddComponent<OVRGridCube>();
			GridCube.SetOVRCameraController(ref CameraController);

			// Add a VisionGuide component to this object
			VisionGuide = gameObject.AddComponent<OVRVisionGuide>();
			VisionGuide.SetOVRCameraController(ref CameraController);
			VisionGuide.SetFadeTexture(ref FadeInTexture);
			VisionGuide.SetVisionGuideLayer(ref LayerName);
		}
		
		// Crosshair functionality
		Crosshair.Init();
		Crosshair.SetCrosshairTexture(ref CrosshairImage);
		Crosshair.SetOVRCameraController (ref CameraController);
		Crosshair.SetOVRPlayerController(ref PlayerController);
		
		// Check for HMD and sensor
        CheckIfRiftPresent();
	} 
	
	void Update()
	{		
		if(LoadingLevel == true)
			return;

		// Main update

		UpdateFPS();
		
		// CameraController updates
		if(CameraController != null)
		{
			UpdateIPD();
			UpdatePrediction();
			
			// Set LPM on by default
			UpdateLowPersistenceMode();
			UpdateVisionMode();
			UpdateFOV();
			UpdateEyeHeightOffset();
			UpdateResolutionEyeTexture();
			UpdateLatencyValues();
		}
		
		// PlayerController updates
		if(PlayerController != null)
		{
			UpdateSpeedAndRotationScaleMultiplier();
			UpdatePlayerControllerMovement();
		}
		
		// MainMenu updates
		UpdateSelectCurrentLevel();
		UpdateHandleSnapshots();
		
		// Device updates
		UpdateDeviceDetection();
		
		// Crosshair functionality
		Crosshair.UpdateCrosshair();
		
		// Toggle Fullscreen
		if(Input.GetKeyDown(KeyCode.F11))
			Screen.fullScreen = !Screen.fullScreen;

		if (Input.GetKeyDown(KeyCode.M))
			CameraController.Mirror = !CameraController.Mirror;
		
		// Escape Application
		if (Input.GetKeyDown(QuitKey))
			Application.Quit();
	}

	void OnGUI()
	{	
		// Important to keep from skipping render events
		if (Event.current.type != EventType.Repaint)
			return;
		
		// Fade in screen
		if(AlphaFadeValue > 0.0f)
		{
			AlphaFadeValue -= Mathf.Clamp01(Time.deltaTime / FadeInTime);
			if(AlphaFadeValue < 0.0f)
			{
				AlphaFadeValue = 0.0f;	
			}
			else
			{
				GUI.color = new Color(0, 0, 0, AlphaFadeValue);
				GUI.DrawTexture( new Rect(0, 0, Screen.width, Screen.height ), FadeInTexture ); 
				return;
			}
		}
		// We can turn on the render object so we can render the on-screen menu
		if(GUIRenderObject != null)
		{
			if (ScenesVisible || 
			    ShowVRVars || 
			    Crosshair.IsCrosshairVisible() || 
			    RiftPresentTimeout > 0.0f || 
			    VisionGuide.GetFadeAlphaValue() > 0.0f)
			{
				GUIRenderObject.SetActive(true);
			}
			else
			{
				GUIRenderObject.SetActive(false);
			}
		}
		
		//***
		// Set the GUI matrix to deal with portrait mode
		Vector3 scale = Vector3.one;
		Matrix4x4 svMat = GUI.matrix; // save current matrix
		// substitute matrix - only scale is altered from standard
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
		
		// Cache current active render texture
		RenderTexture previousActive = RenderTexture.active;
		
		// if set, we will render to this texture
		if(GUIRenderTexture != null && GUIRenderObject.activeSelf)
		{
			RenderTexture.active = GUIRenderTexture;
			GL.Clear (false, true, new Color (0.0f, 0.0f, 0.0f, 0.0f));
		}
		
		// Update OVRGUI functions (will be deprecated eventually when 2D renderingc
		// is removed from GUI)
		GuiHelper.SetFontReplace(FontReplace);
		
		// If true, we are displaying information about the Rift not being detected
		// So do not show anything else
		if(GUIShowRiftDetected() != true)
		{	
			GUIShowLevels();
			GUIShowVRVariables();
		}
		
		// The cross-hair may need to go away at some point, unless someone finds it 
		// useful
		Crosshair.OnGUICrosshair();
		
		// Since we want to draw into the main GUI that is shared within the MainMenu,
		// we call the OVRVisionGuide GUI function here
		VisionGuide.OnGUIVisionGuide();
		
		// Restore active render texture
		if (GUIRenderObject.activeSelf)
		{
			RenderTexture.active = previousActive;
		}
		
		// ***
		// Restore previous GUI matrix
		GUI.matrix = svMat;
	}
	#endregion

	#region Internal State Management Functions
	/// <summary>
	/// Updates the FPS.
	/// </summary>
	void UpdateFPS()
	{
    	TimeLeft -= Time.deltaTime;
   	 	Accum += Time.timeScale/Time.deltaTime;
    	++Frames;
 
    	// Interval ended - update GUI text and start new interval
    	if( TimeLeft <= 0.0 )
    	{
        	// display two fractional digits (f2 format)
			float fps = Accum / Frames;
			
			if(ShowVRVars == true)// limit gc
				strFPS = System.String.Format("FPS: {0:F2}",fps);

       		TimeLeft += UpdateInterval;
        	Accum  = 0.0f;
        	Frames = 0;
    	}
	}
	
	/// <summary>
	/// Updates the IPD.
	/// </summary>
	void UpdateIPD()
	{
		if(Input.GetKeyDown (KeyCode.Equals))
		{
			float ipd = 0;
			CameraController.GetIPD(ref ipd);
			ipd += IPDIncrement;
			CameraController.SetIPD (ipd);
		}
		else if(Input.GetKeyDown (KeyCode.Minus))
		{
			float ipd = 0;
			CameraController.GetIPD(ref ipd);
			ipd -= IPDIncrement;
			CameraController.SetIPD (ipd);
		}
		
		if(ShowVRVars == true) // limit gc
		{	
			float ipd = 0;
			CameraController.GetIPD (ref ipd);
			strIPD = System.String.Format("IPD (mm): {0:F4}", ipd * 1000.0f);
		}
	}
	
	/// <summary>
	/// Updates the prediction.
	/// </summary>
	void UpdatePrediction()
	{
		// Turn prediction on/off
		if(Input.GetKeyDown (KeyCode.P))
			CameraController.PredictionOn = !CameraController.PredictionOn;
		
		// Update prediction value (only if prediction is on)
		if(CameraController.PredictionOn)
		{
			float pt = OVRDevice.PredictionTime; 
			if(Input.GetKeyDown (KeyCode.Comma))
				pt -= PredictionIncrement;
			else if(Input.GetKeyDown (KeyCode.Period))
				pt += PredictionIncrement;
			
			OVRDevice.PredictionTime = pt;
			
			// re-get the prediction time to make sure it took
			pt = OVRDevice.PredictionTime;
			
			if(ShowVRVars == true)// limit gc
				strPrediction = System.String.Format ("Pred (ms): {0:F3}", pt);								 
		}
		else
		{
			strPrediction = "Pred: OFF";
		}
	}

	/// <summary>
	/// Updates the low Persistence mode.
	/// </summary>
	void UpdateLowPersistenceMode()
	{
		if(Input.GetKeyDown (KeyCode.F1))
		{
			if(LowPersistenceMode == false)
			{
				LowPersistenceMode = true;
#if	SHOW_DK2_VARIABLES
				strLPM = "Low Persistence Mode: ON";
#endif
				OVRDevice.SetLowPersistenceMode(LowPersistenceMode);
			}
			else
			{
				LowPersistenceMode = false;
#if	SHOW_DK2_VARIABLES
				strLPM = "Low Persistence Mode: OFF";
#endif
				OVRDevice.SetLowPersistenceMode(LowPersistenceMode);
			}
		}
	}
	
	/// <summary>
	/// Updates the vision mode.
	/// </summary>
	void UpdateVisionMode()
	{
		if(Input.GetKeyDown (KeyCode.F2))
		{
			if(VisionMode == false)
			{
				VisionMode = true;
#if	SHOW_DK2_VARIABLES
				strVisionMode = "Vision Enabled: ON";
#endif
				OVRDevice.SetVisionEnabled(VisionMode);
			}
			else
			{
				VisionMode = false;
#if	SHOW_DK2_VARIABLES
				strVisionMode = "Vision Enabled: OFF";
#endif
				OVRDevice.SetVisionEnabled(VisionMode);
			}
		}
	}
	
	/// <summary>
	/// Updates the FOV.
	/// </summary>
	void UpdateFOV()
	{
		if(Input.GetKeyDown (KeyCode.LeftBracket))
		{
			float cfov = 0;
			CameraController.GetVerticalFOV(ref cfov);
			cfov -= FOVIncrement;
			CameraController.SetVerticalFOV(cfov);
		}
		else if (Input.GetKeyDown (KeyCode.RightBracket))
		{
			float cfov = 0;
			CameraController.GetVerticalFOV(ref cfov);
			cfov += FOVIncrement;
			CameraController.SetVerticalFOV(cfov);
		}
		
		if(ShowVRVars == true)// limit gc
		{
			float cfov = 0;
			CameraController.GetVerticalFOV(ref cfov);
			strFOV = System.String.Format ("FOV (deg): {0:F3}", cfov);
		}
	}

    /// <summary>
    /// Updates resolution of eye texture
    /// </summary>
    void UpdateResolutionEyeTexture()
    {
        if (ShowVRVars == true) // limit gc
        {
            int w = 0, h = 0;
            OVRDevice.GetResolutionEyeTexture(ref w, ref h);
            strResolutionEyeTexture = System.String.Format("Resolution : {0} x {1}", w, h);
        }
    }

    /// <summary>
    /// Updates latency values
    /// </summary>
    void UpdateLatencyValues()
    {

        if (ShowVRVars == true) // limit gc
        {
            float Ren = 0.0f, TWrp = 0.0f, PostPresent = 0.0f;
            OVRDevice.GetLatencyValues(ref Ren, ref TWrp, ref PostPresent);
            if (Ren < 0.000001f && TWrp < 0.000001f && PostPresent < 0.000001f)
                strLatencies = System.String.Format("Ren : N/A TWrp: N/A PostPresent: N/A");
            else
                strLatencies = System.String.Format("Ren : {0:F3} TWrp: {1:F3} PostPresent: {2:F3}", Ren, TWrp, PostPresent);
        }
    }
		
	/// <summary>
	/// Updates the eye height offset.
	/// </summary>
	void UpdateEyeHeightOffset()
	{
		// We will update neck position, since camera root and eye center should
		// be set differently.
		if(Input.GetKeyDown(KeyCode.Alpha5))
		{	
			Vector3 neckPosition = Vector3.zero;
			CameraController.GetNeckPosition(ref neckPosition);
			neckPosition.y -= HeightIncrement;
			CameraController.SetNeckPosition(neckPosition);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha6))
		{
			Vector3 neckPosition = Vector3.zero;;
			CameraController.GetNeckPosition(ref neckPosition);
			neckPosition.y += HeightIncrement;
			CameraController.SetNeckPosition(neckPosition);
		}
			
		if(ShowVRVars == true)// limit gc
		{
			float eyeHeight = 0.0f;
			CameraController.GetPlayerEyeHeight(ref eyeHeight);
			
			strHeight = System.String.Format ("Eye Height (m): {0:F3}", eyeHeight);
		}
	}
	
	/// <summary>
	/// Updates the speed and rotation scale multiplier.
	/// </summary>
	void UpdateSpeedAndRotationScaleMultiplier()
	{
		float moveScaleMultiplier = 0.0f;
		PlayerController.GetMoveScaleMultiplier(ref moveScaleMultiplier);
		if(Input.GetKeyDown(KeyCode.Alpha7))
			moveScaleMultiplier -= SpeedRotationIncrement;
		else if (Input.GetKeyDown(KeyCode.Alpha8))
			moveScaleMultiplier += SpeedRotationIncrement;		
		PlayerController.SetMoveScaleMultiplier(moveScaleMultiplier);
		
		float rotationScaleMultiplier = 0.0f;
		PlayerController.GetRotationScaleMultiplier(ref rotationScaleMultiplier);
		if(Input.GetKeyDown(KeyCode.Alpha9))
			rotationScaleMultiplier -= SpeedRotationIncrement;
		else if (Input.GetKeyDown(KeyCode.Alpha0))
			rotationScaleMultiplier += SpeedRotationIncrement;	
		PlayerController.SetRotationScaleMultiplier(rotationScaleMultiplier);
		
		if(ShowVRVars == true)// limit gc
			strSpeedRotationMultipler = System.String.Format ("Spd.X: {0:F2} Rot.X: {1:F2}", 
									moveScaleMultiplier, 
									rotationScaleMultiplier);
	}
	
	/// <summary>
	/// Updates the player controller movement.
	/// </summary>
	void UpdatePlayerControllerMovement()
	{
		if(PlayerController != null)
			PlayerController.SetHaltUpdateMovement(ScenesVisible);
	}
	
	/// <summary>
	/// Updates the select current level.
	/// </summary>
	void UpdateSelectCurrentLevel()
	{
		ShowLevels();
				
		if(ScenesVisible == false)
			return;
			
		CurrentLevel = GetCurrentLevel();
		
		if((Scenes.Length != 0) && 
		   ((OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.A) == true) ||
			 Input.GetKeyDown(KeyCode.Return)))
		{
			LoadingLevel = true;
			Application.LoadLevelAsync(Scenes[CurrentLevel]);
		}
	}
	
	/// <summary>
	/// Shows the levels.
	/// </summary>
	/// <returns><c>true</c>, if levels was shown, <c>false</c> otherwise.</returns>
	bool ShowLevels()
	{
		if(Scenes.Length == 0)
		{
			ScenesVisible = false;
			return ScenesVisible;
		}
		
		bool curStartDown = false;
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Start) == true)
			curStartDown = true;
		
		if((PrevStartDown == false) && (curStartDown == true) ||
			Input.GetKeyDown(KeyCode.RightShift) )
		{
			if(ScenesVisible == true) 
				ScenesVisible = false;
			else 
				ScenesVisible = true;
		}
		
		PrevStartDown = curStartDown;
		
		return ScenesVisible;
	}
	
	/// <summary>
	/// Gets the current level.
	/// </summary>
	/// <returns>The current level.</returns>
	int GetCurrentLevel()
	{
		bool curHatDown = false;
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Down) == true)
			curHatDown = true;
		
		bool curHatUp = false;
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Down) == true)
			curHatUp = true;
		
		if((PrevHatDown == false) && (curHatDown == true) ||
			Input.GetKeyDown(KeyCode.DownArrow))
		{
			CurrentLevel = (CurrentLevel + 1) % SceneNames.Length;	
		}
		else if((PrevHatUp == false) && (curHatUp == true) ||
			Input.GetKeyDown(KeyCode.UpArrow))
		{
			CurrentLevel--;	
			if(CurrentLevel < 0)
				CurrentLevel = SceneNames.Length - 1;
		}
					
		PrevHatDown = curHatDown;
		PrevHatUp = curHatUp;
		
		return CurrentLevel;
	}

	#endregion

	#region Internal GUI Functions

	/// <summary>
	/// Show the GUI levels.
	/// </summary>
	void GUIShowLevels()
	{
		if(ScenesVisible == true)
		{   
			// Darken the background by rendering fade texture 
			GUI.color = new Color(0, 0, 0, 0.5f);
  			GUI.DrawTexture( new Rect(0, 0, Screen.width, Screen.height ), FadeInTexture );
 			GUI.color = Color.white;
		
			if(LoadingLevel == true)
			{
				string loading = "LOADING...";
				GuiHelper.StereoBox (StartX, StartY, WidthX, WidthY, ref loading, Color.yellow);
				return;
			}
			
			for (int i = 0; i < SceneNames.Length; i++)
			{
				Color c;
				if(i == CurrentLevel)
					c = Color.yellow;
				else
					c = Color.black;
				
				int y   = StartY + (i * StepY);
				
				GuiHelper.StereoBox (StartX, y, WidthX, WidthY, ref SceneNames[i], c);
			}
		}				
	}
	
	/// <summary>
	/// Show the VR variables.
	/// </summary>
    void GUIShowVRVariables()
    {
        bool SpaceHit = Input.GetKey(MenuKey);
        if ((OldSpaceHit == false) && (SpaceHit == true))
        {
            if (ShowVRVars == true)
            {
                ShowVRVars = false;
            }
            else
            {
                ShowVRVars = true;
            }
        }

        OldSpaceHit = SpaceHit;

        // Do not render if we are not showing
        if (ShowVRVars == false)
            return;

        int y = VRVarsSY;

#if	SHOW_DK2_VARIABLES
		// Print out Low Persistence Mode
		GuiHelper.StereoBox (VRVarsSX, y, VRVarsWidthX, VRVarsWidthY, 
							 ref strLPM, Color.red);
 
		// Print out Vision Mode
		GuiHelper.StereoBox (VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY, 
							 ref strVisionMode, Color.green);
#endif

        // Draw FPS
        GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                             ref strFPS, Color.green);

        // Don't draw these vars if CameraController is not present
        if (CameraController != null)
        {
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                             ref strPrediction, Color.white);
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                             ref strIPD, Color.yellow);
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                             ref strFOV, Color.white);
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                             ref strResolutionEyeTexture, Color.white);
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                             ref strLatencies, Color.white);
        }

        // Don't draw these vars if PlayerController is not present
        if (PlayerController != null)
        {
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                                 ref strHeight, Color.yellow);
            GuiHelper.StereoBox(VRVarsSX, y += StepY, VRVarsWidthX, VRVarsWidthY,
                                 ref strSpeedRotationMultipler, Color.white);
        }
    }
	
	// SNAPSHOT MANAGEMENT
	
	/// <summary>
	/// Handle update of snapshots.
	/// </summary>
	void UpdateHandleSnapshots()
	{
		// Default shapshot
		if(Input.GetKeyDown(KeyCode.F2))
			LoadSnapshot ("DEFAULT");
		
		// Snapshot 1
		if(Input.GetKeyDown(KeyCode.F3))
		{	
			if(Input.GetKey(KeyCode.Tab))
				StoreSnapshot ("SNAPSHOT1");
			else
				LoadSnapshot ("SNAPSHOT1");
		}
		
		// Snapshot 2
		if(Input.GetKeyDown(KeyCode.F4))
		{	
			if(Input.GetKey(KeyCode.Tab))
				StoreSnapshot ("SNAPSHOT2");
			else
				LoadSnapshot ("SNAPSHOT2");
		}
		
		// Snapshot 3
		if(Input.GetKeyDown(KeyCode.F5))
		{	
			if(Input.GetKey(KeyCode.Tab))
				StoreSnapshot ("SNAPSHOT3");
			else
				LoadSnapshot ("SNAPSHOT3");
		}
		
	}
	
	/// <summary>
	/// Stores the snapshot.
	/// </summary>
	/// <returns><c>true</c>, if snapshot was stored, <c>false</c> otherwise.</returns>
	/// <param name="snapshotName">Snapshot name.</param>
	bool StoreSnapshot(string snapshotName)
	{
		float f = 0;
		
		PresetManager.SetCurrentPreset(snapshotName);
		
		if(CameraController != null)
		{
			CameraController.GetIPD(ref f);
			PresetManager.SetPropertyFloat("IPD", ref f);
	
			f = OVRDevice.PredictionTime;
			PresetManager.SetPropertyFloat("PREDICTION", ref f);
		
			CameraController.GetVerticalFOV(ref f);
			PresetManager.SetPropertyFloat("FOV", ref f);
		
			Vector3 neckPosition = Vector3.zero;
			CameraController.GetNeckPosition(ref neckPosition);
			PresetManager.SetPropertyFloat("HEIGHT", ref neckPosition.y);
		}
			
		if(PlayerController != null)
		{
			PlayerController.GetMoveScaleMultiplier(ref f);
			PresetManager.SetPropertyFloat("SPEEDMULT", ref f);

			PlayerController.GetRotationScaleMultiplier(ref f);
			PresetManager.SetPropertyFloat("ROTMULT", ref f);
		}
	
		return true;
	}
	
	/// <summary>
	/// Loads the snapshot.
	/// </summary>
	/// <returns><c>true</c>, if snapshot was loaded, <c>false</c> otherwise.</returns>
	/// <param name="snapshotName">Snapshot name.</param>
	bool LoadSnapshot(string snapshotName)
	{
		float f = 0;
		
		PresetManager.SetCurrentPreset(snapshotName);
		
		if(CameraController != null)
		{
			if(PresetManager.GetPropertyFloat("IPD", ref f) == true)
				CameraController.SetIPD(f);
		
			if(PresetManager.GetPropertyFloat("PREDICTION", ref f) == true)
				OVRDevice.PredictionTime = f;
		
			if(PresetManager.GetPropertyFloat("FOV", ref f) == true)
				CameraController.SetVerticalFOV(f);
		
			if(PresetManager.GetPropertyFloat("HEIGHT", ref f) == true)
			{
				Vector3 neckPosition = Vector3.zero;
				CameraController.GetNeckPosition(ref neckPosition);
				neckPosition.y = f;
				CameraController.SetNeckPosition(neckPosition);
			}
		}
		
		if(PlayerController != null)
		{
			if(PresetManager.GetPropertyFloat("SPEEDMULT", ref f) == true)
				PlayerController.SetMoveScaleMultiplier(f);

			if(PresetManager.GetPropertyFloat("ROTMULT", ref f) == true)
				PlayerController.SetRotationScaleMultiplier(f);
		}
			
		return true;
	}
	
	// RIFT DETECTION
	
	/// <summary>
	/// Checks to see if HMD and / or sensor is available, and displays a 
	/// message if it is not.
	/// </summary>
	void CheckIfRiftPresent()
	{
		HMDPresent    = OVRDevice.IsHMDPresent();
		SensorPresent = OVRDevice.IsSensorPresent();
		
		if((HMDPresent == false) || (SensorPresent == false))
		{
			RiftPresentTimeout = 5.0f; // Keep message up for 10 seconds
			
			if((HMDPresent == false) && (SensorPresent == false))
				strRiftPresent = "NO HMD AND SENSOR DETECTED";
			else if (HMDPresent == false)
				strRiftPresent = "NO HMD DETECTED";
			else if (SensorPresent == false)
				strRiftPresent = "NO SENSOR DETECTED";
		}
	}
	
	/// <summary>
	/// Show if Rift is detected.
	/// </summary>
	/// <returns><c>true</c>, if show rift detected was GUIed, <c>false</c> otherwise.</returns>
	bool GUIShowRiftDetected()
	{
		if(RiftPresentTimeout > 0.0f)
		{
			GuiHelper.StereoBox (StartX, StartY, WidthX, WidthY, 
								 ref strRiftPresent, Color.white);
		
			return true;
		}
		return false;
	}
	
	/// <summary>
	/// Updates the device detection.
	/// </summary>
	void UpdateDeviceDetection()
	{
		if(RiftPresentTimeout > 0.0f)
			RiftPresentTimeout -= Time.deltaTime;
	}
	#endregion
}
