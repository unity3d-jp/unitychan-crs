/************************************************************************************

Filename    :   OVRCrosshair.cs
Content     :   Implements a hud cross-hair, rendered into a texture and mapped to a
				3D plane projected in front of the camera
Created     :   May 21 8, 2013
Authors     :   Peter Giokaris

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

//-------------------------------------------------------------------------------------
// ***** OVRCrosshair
//
 
/// <summary>
/// OVRCrosshair is a component that adds a stereoscoppic cross-hair into a scene.
/// </summary>
public class OVRCrosshair
{
	#region Variables
	public Texture ImageCrosshair 	  = null;
	
	public OVRCameraController CameraController = null;
	public OVRPlayerController PlayerController = null;
	
	public float   FadeTime			  = 0.3f;
	public float   FadeScale      	  = 0.6f;
	public float   CrosshairDistance  = 1.0f;

	public KeyCode CrosshairKey       = KeyCode.C;
		
	private float  DeadZoneX          =  400.0f;
	private float  DeadZoneY          =   75.0f;
	
	private float  ScaleSpeedX	      =   7.0f;
	private float  ScaleSpeedY	 	  =   7.0f;
	
	private bool   DisplayCrosshair;
	private bool   CollisionWithGeometry;
	private float  FadeVal;
	private Camera MainCam;
	
	private float  XL 				  = 0.0f;
	private float  YL 				  = 0.0f;
	
	private float  ScreenWidth		  = 1280.0f;
	private float  ScreenHeight 	  =  800.0f;
	
	#endregion
	
	#region Public Functions
	
	/// <summary>
	/// Sets the crosshair texture.
	/// </summary>
	/// <param name="image">Image.</param>
	public void SetCrosshairTexture(ref Texture image)
	{
		ImageCrosshair = image;
	}
	
	/// <summary>
	/// Sets the OVR camera controller.
	/// </summary>
	/// <param name="cameraController">Camera controller.</param>
	public void SetOVRCameraController(ref OVRCameraController cameraController)
	{
		CameraController = cameraController;
		CameraController.GetCamera(ref MainCam);
	}

	/// <summary>
	/// Sets the OVR player controller.
	/// </summary>
	/// <param name="playerController">Player controller.</param>
	public void SetOVRPlayerController(ref OVRPlayerController playerController)
	{
		PlayerController = playerController;
	}
	
	/// <summary>
	/// Determines whether the crosshair is visible.
	/// </summary>
	/// <returns><c>true</c> if this instance is crosshair visible; otherwise, <c>false</c>.</returns>
	public bool IsCrosshairVisible()
	{
		if(FadeVal > 0.0f)
			return true;
		
		return false;
	}
	
	/// <summary>
	/// Init this instance.
	/// </summary>
	public void Init()
	{
		DisplayCrosshair 		= false;
		CollisionWithGeometry 	= false;
		FadeVal 		 		= 0.0f;
	
		ScreenWidth  = Screen.width;
		ScreenHeight = Screen.height;
		
		// Initialize screen location of cursor
		XL = ScreenWidth * 0.5f;
		YL = ScreenHeight * 0.5f;
	}
	
	/// <summary>
	/// Updates the crosshair.
	/// </summary>
	public void UpdateCrosshair()
	{
		// Do not do these tests within OnGUI since they will be called twice
		ShouldDisplayCrosshair();
		CollisionWithGeometryCheck();
	}
	
	/// <summary>
	/// The GUI crosshair event.
	/// </summary>
	public void  OnGUICrosshair()
	{
		if ((DisplayCrosshair == true) && (CollisionWithGeometry == false))
			FadeVal += Time.deltaTime / FadeTime;
		else
			FadeVal -= Time.deltaTime / FadeTime;
		
		FadeVal = Mathf.Clamp(FadeVal, 0.0f, 1.0f);
		
		// Check to see if crosshair influences mouse rotation
		if(PlayerController != null)
			PlayerController.SetSkipMouseRotation(false);
		
		if ((ImageCrosshair != null) && (FadeVal != 0.0f))
		{
			// Assume cursor is on-screen (unless it goes into the dead-zone)
			// Other systems will check this to see if it is false for example 
			// allowing rotation to take place
			if(PlayerController != null)
				PlayerController.SetSkipMouseRotation(true);

			GUI.color = new Color(1, 1, 1, FadeVal * FadeScale);
			
			// Calculate X
			XL += Input.GetAxis("Mouse X") * ScaleSpeedX;
			if(XL < DeadZoneX) 
			{
				if(PlayerController != null)
					PlayerController.SetSkipMouseRotation(false);
				
				XL = DeadZoneX - 0.001f;	
			}
			else if (XL > (Screen.width - DeadZoneX))
			{
				if(PlayerController != null)
					PlayerController.SetSkipMouseRotation(false);
				
				XL = ScreenWidth - DeadZoneX + 0.001f;
			}
			
			// Calculate Y
			YL -= Input.GetAxis("Mouse Y") * ScaleSpeedY;
			if(YL < DeadZoneY) 
			{
				//CursorOnScreen = false;
				if(YL < 0.0f) YL = 0.0f;
			}
			else if (YL > ScreenHeight - DeadZoneY)
			{
				//CursorOnScreen = false;
				if(YL > ScreenHeight) YL = ScreenHeight;
			}
			
			// Finally draw cursor
			bool skipMouseRotation = true;
			if(PlayerController != null)
				PlayerController.GetSkipMouseRotation(ref skipMouseRotation);
		
			if(skipMouseRotation == true)
			{
				// Left
				GUI.DrawTexture(new Rect(	XL - (ImageCrosshair.width * 0.5f),
					                     	YL - (ImageCrosshair.height * 0.5f), 
											ImageCrosshair.width,
											ImageCrosshair.height), 
											ImageCrosshair);
			}
				
			GUI.color = Color.white;
		}
	}
	#endregion
	
	#region Private Functions
	/// <summary>
	/// Shoulds the crosshair be displayed.
	/// </summary>
	/// <returns><c>true</c>, if display crosshair was shoulded, <c>false</c> otherwise.</returns>
	bool ShouldDisplayCrosshair()
	{	
		if(Input.GetKeyDown (CrosshairKey))
		{
			if(DisplayCrosshair == false)
			{
				DisplayCrosshair = true;
				
				// Always initialize screen location of cursor to center
				XL = ScreenWidth * 0.5f;
				YL = ScreenHeight * 0.5f;
			}
			else
				DisplayCrosshair = false;
		}
					
		return DisplayCrosshair;
	}
	
	/// <summary>
	/// Do a collision raycast on geometry for crosshair.
	/// </summary>
	/// <returns><c>true</c>, if with geometry check was collisioned, <c>false</c> otherwise.</returns>
	bool CollisionWithGeometryCheck()
	{
		CollisionWithGeometry = false;
		
		Vector3 startPos = MainCam.transform.position;
		Vector3 dir = Vector3.forward;
		dir = MainCam.transform.rotation * dir;
		dir *= CrosshairDistance;
		Vector3 endPos = startPos + dir;
		
		RaycastHit hit;
		if (Physics.Linecast(startPos, endPos, out hit)) 
		{
			if (!hit.collider.isTrigger)
			{
				CollisionWithGeometry = true;
			}
		}
		
		return CollisionWithGeometry;
	}
	#endregion
}
