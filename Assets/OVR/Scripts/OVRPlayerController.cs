/************************************************************************************

Filename    :   OVRPlayerController.cs
Content     :   Player controller interface. 
				This script drives OVR camera as well as controls the locomotion
				of the player, and handles physical contact in the world.	
Created     :   January 8, 2013
Authors     :   Peter Giokaris, David Borel

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
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]

/// <summary>
/// OVRPlayerController
/// Implements a basic first person controller for the Rift. It is 
/// attached to the OVRPlayerController prefab, which has an OVRCameraController attached
/// to it. 
/// 
/// The controller will interact properly with a Unity scene, provided that the scene has
/// collision assigned to it. 
///
/// The OVRPlayerController prefab has an empty GameObject attached to it called 
/// ForwardDirection. This game object contains the matrix which motor control bases it
/// direction on. This game object should also house the body geometry which will be seen
/// by the player.
/// </summary>
public class OVRPlayerController : MonoBehaviour
{
	#region Public Variables
	/// <summary>
	/// How quickly the player's speed will increase.
	/// </summary>
	public float Acceleration 	   = 0.1f;

	/// <summary>
	/// How quickly the player's speed will dissipate.
	/// </summary>
	public float Damping 		   = 0.3f;

	/// <summary>
	/// How quickly the player's motion in the back and side directions will dissipate.
	/// </summary>
	public float BackAndSideDampen = 0.5f;

	/// <summary>
	/// The strength of the force used to make the player jump.
	/// </summary>
	public float JumpForce 		   = 0.3f;

	/// <summary>
	/// How quickly the player will rotate when the user drags the mouse.
	/// </summary>
	public float RotationAmount    = 1.5f;

	/// <summary>
	/// How much the player will rotate (in degrees) when the user turns with the D-Pad.
	/// </summary>
	public float RotationRatchet   = 45.0f;

	/// <summary>
	/// The strength of gravity, relative to Physics.gravity
	/// </summary>
	public float GravityModifier   = 0.379f;
	#endregion

	#region Static Members
	static float sDeltaRotationOld = 0.0f;
	#endregion
		
	#region Internal Variables
	protected CharacterController 	Controller 		 = null;
	protected OVRCameraController 	CameraController = null;

	private float   MoveScale 	   = 1.0f;
	private Vector3 MoveThrottle   = Vector3.zero;
	private float   FallSpeed 	   = 0.0f;
	
	// Initial direction of controller (passed down into CameraController)
	private Quaternion OrientationOffset = Quaternion.identity;			
	// Rotation amount from inputs (passed down into CameraController)
	private float 	YRotation 	 = 0.0f;
	
	// Transfom used to point player in a given direction; 
	// We should attach objects to this if we want them to rotate 
	// separately from the head (i.e. the body)
	protected Transform DirXform = null;
	
	// We can adjust these to influence speed and rotation of player controller
	private float MoveScaleMultiplier     = 1.0f; 
	private float RotationScaleMultiplier = 1.0f; 
	private bool  SkipMouseRotation       = false;
	private bool  HaltUpdateMovement      = false;

	// For rachet rotation using d-pad
	private bool prevHatLeft 			  = false;
	private bool prevHatRight 			  = false;
	#endregion

	#region MonoBehaviour Message Handlers
	void Awake()
	{		
		// We use Controller to move player around
		Controller = gameObject.GetComponent<CharacterController>();
		
		if(Controller == null)
			Debug.LogWarning("OVRPlayerController: No CharacterController attached.");
					
		// We use OVRCameraController to set rotations to cameras, 
		// and to be influenced by rotation
		OVRCameraController[] CameraControllers;
		CameraControllers = gameObject.GetComponentsInChildren<OVRCameraController>();
		
		if(CameraControllers.Length == 0)
			Debug.LogWarning("OVRPlayerController: No OVRCameraController attached.");
		else if (CameraControllers.Length > 1)
			Debug.LogWarning("OVRPlayerController: More then 1 OVRCameraController attached.");
		else
			CameraController = CameraControllers[0];	
	
		// Instantiate a Transform from the main game object (will be used to 
		// direct the motion of the PlayerController, as well as used to rotate
		// a visible body attached to the controller)
		DirXform = null;
		Transform[] Xforms = gameObject.GetComponentsInChildren<Transform>();
		
		for(int i = 0; i < Xforms.Length; i++)
		{
			if(Xforms[i].name == "ForwardDirection")
			{
				DirXform = Xforms[i];
				break;
			}
		}
		
		if(DirXform == null)
			Debug.LogWarning("OVRPlayerController: ForwardDirection game object not found. Do not use.");
	}

	protected virtual void Start()
	{
		InitializeInputs();	
		SetCameras();
	}
		
	protected virtual void Update()
	{
		UpdateMovement();

		Vector3 moveDirection = Vector3.zero;
		
		float motorDamp = (1.0f + (Damping * OVRDevice.SimulationRate * Time.deltaTime));
		MoveThrottle.x /= motorDamp;
		MoveThrottle.y = (MoveThrottle.y > 0.0f) ? (MoveThrottle.y / motorDamp) : MoveThrottle.y;
		MoveThrottle.z /= motorDamp;

		moveDirection += MoveThrottle * OVRDevice.SimulationRate * Time.deltaTime;
		
		// Gravity
		if (Controller.isGrounded && FallSpeed <= 0)
			FallSpeed = ((Physics.gravity.y * (GravityModifier * 0.002f)));	
		else
			FallSpeed += ((Physics.gravity.y * (GravityModifier * 0.002f)) * OVRDevice.SimulationRate * Time.deltaTime);	

		moveDirection.y += FallSpeed * OVRDevice.SimulationRate * Time.deltaTime;

		// Offset correction for uneven ground
		float bumpUpOffset = 0.0f;
		
		if (Controller.isGrounded && MoveThrottle.y <= 0.001f)
		{
			bumpUpOffset = Mathf.Max(Controller.stepOffset, 
									 new Vector3(moveDirection.x, 0, moveDirection.z).magnitude); 
			moveDirection -= bumpUpOffset * Vector3.up;
		}			
	 
		Vector3 predictedXZ = Vector3.Scale((Controller.transform.localPosition + moveDirection), 
											 new Vector3(1, 0, 1));	
		
		// Move contoller
		Controller.Move(moveDirection);
		
		Vector3 actualXZ = Vector3.Scale(Controller.transform.localPosition, new Vector3(1, 0, 1));
		
		if (predictedXZ != actualXZ)
			MoveThrottle += (actualXZ - predictedXZ) / (OVRDevice.SimulationRate * Time.deltaTime);
		
		// Update rotation using CameraController transform, possibly proving some rules for 
		// sliding the rotation for a more natural movement and body visual
		UpdatePlayerForwardDirTransform();
	}
	#endregion

	#region Public Functions
	/// <summary>
	/// Updates the player's movement.
	/// </summary>
	public virtual void UpdateMovement()
	{
		// Do not apply input if we are showing a level selection display
		if(HaltUpdateMovement == true)
			return;
	
		bool moveForward = false;
		bool moveLeft  	 = false;
		bool moveRight   = false;
		bool moveBack    = false;
				
		MoveScale = 1.0f;
			
		// * * * * * * * * * * *
		// Keyboard input
			
		// Move
			
		// WASD
		if (Input.GetKey(KeyCode.W)) moveForward = true;
		if (Input.GetKey(KeyCode.A)) moveLeft	 = true;
		if (Input.GetKey(KeyCode.S)) moveBack 	 = true; 
		if (Input.GetKey(KeyCode.D)) moveRight 	 = true; 
		// Arrow keys
		if (Input.GetKey(KeyCode.UpArrow))    moveForward = true;
		if (Input.GetKey(KeyCode.LeftArrow))  moveLeft 	  = true;
		if (Input.GetKey(KeyCode.DownArrow))  moveBack 	  = true; 
		if (Input.GetKey(KeyCode.RightArrow)) moveRight   = true; 

		// D-Pad
		bool dpad_move = false;
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Up) == true)
		{
			moveForward = true;
			dpad_move   = true;
	
		}	
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Down) == true)
		{
			moveBack  = true; 
			dpad_move = true;
		}
			
		if ( (moveForward && moveLeft) || (moveForward && moveRight) ||
			 (moveBack && moveLeft)    || (moveBack && moveRight) )
			MoveScale = 0.70710678f;
			
		// No positional movement if we are in the air
		if (!Controller.isGrounded)	
			MoveScale = 0.0f;
			
		MoveScale *= OVRDevice.SimulationRate * Time.deltaTime;
			
		// Compute this for key movement
		float moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;
			
		// Run!
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			moveInfluence *= 2.0f;
		else if (dpad_move)
			moveInfluence *= 3.0f;

		if(DirXform != null)
		{
			if (moveForward)
				MoveThrottle += DirXform.TransformDirection(Vector3.forward * moveInfluence * transform.lossyScale.z);
			if (moveBack)
				MoveThrottle += DirXform.TransformDirection(Vector3.back * moveInfluence * transform.lossyScale.z) * BackAndSideDampen;
			if (moveLeft)
				MoveThrottle += DirXform.TransformDirection(Vector3.left * moveInfluence * transform.lossyScale.x) * BackAndSideDampen;
			if (moveRight)
				MoveThrottle += DirXform.TransformDirection(Vector3.right * moveInfluence * transform.lossyScale.x) * BackAndSideDampen;
		}
			
		// Rotate
			
		// D-Pad rachet

		bool curHatLeft = false;
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Left) == true)
			curHatLeft = true;

		if(curHatLeft && !prevHatLeft)
			YRotation -= RotationRatchet; 

		prevHatLeft = curHatLeft;

		bool curHatRight = false;
		if(OVRGamepadController.GPC_GetButton((int)OVRGamepadController.Button.Right) == true)
			curHatRight = true;

		if(curHatRight && !prevHatRight)
			YRotation += RotationRatchet; 
		
		prevHatRight = curHatRight;

		//Use keys to ratchet rotation
		if (Input.GetKeyDown(KeyCode.Q)) 
			YRotation -= RotationRatchet; 
		if (Input.GetKeyDown(KeyCode.E)) 
			YRotation += RotationRatchet;
		
		// * * * * * * * * * * *
		// Mouse input
			
		// Move
			
		// Rotate

		// compute for key rotation
		float rotateInfluence =  OVRDevice.SimulationRate * Time.deltaTime * RotationAmount * RotationScaleMultiplier;

		float deltaRotation = 0.0f;
		if(SkipMouseRotation == false)
			deltaRotation = Input.GetAxis("Mouse X") * rotateInfluence * 3.25f;
			
		float filteredDeltaRotation = (sDeltaRotationOld * 0.0f) + (deltaRotation * 1.0f);
		YRotation += filteredDeltaRotation;
		sDeltaRotationOld = filteredDeltaRotation;
			
		// * * * * * * * * * * *
		// XBox controller input	
			
		// Compute this for xinput movement
		moveInfluence = OVRDevice.SimulationRate * Time.deltaTime * Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;
			
		// Run!
		moveInfluence *= 1.0f + 
					     OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.LeftTrigger);
			
		// Move
		if(DirXform != null)
		{
			float leftAxisY = 
				OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.LeftYAxis);
				
			float leftAxisX = 
			OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.LeftXAxis);
						
			if(leftAxisY > 0.0f)
	    		MoveThrottle += leftAxisY *
				DirXform.TransformDirection(Vector3.forward * moveInfluence);
				
			if(leftAxisY < 0.0f)
	    		MoveThrottle += Mathf.Abs(leftAxisY) *		
				DirXform.TransformDirection(Vector3.back * moveInfluence) * BackAndSideDampen;
				
			if(leftAxisX < 0.0f)
	    		MoveThrottle += Mathf.Abs(leftAxisX) *
				DirXform.TransformDirection(Vector3.left * moveInfluence) * BackAndSideDampen;
				
			if(leftAxisX > 0.0f)
				MoveThrottle += leftAxisX *
				DirXform.TransformDirection(Vector3.right * moveInfluence) * BackAndSideDampen;
		}
			
		float rightAxisX = 
		OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.RightXAxis);
			
		// Rotate
		YRotation += rightAxisX * rotateInfluence;    
		
		// Update cameras direction and rotation
		SetCameras();
	}

	/// <summary>
	/// This function will be used to 'slide' PlayerController rotation around based on 
	/// CameraController. For now, we are simply copying the CameraController rotation into 
	/// PlayerController, so that the PlayerController always faces the direction of the 
	/// CameraController. When we add a body, this will change a bit..
	/// </summary>
	public virtual void UpdatePlayerForwardDirTransform()
	{
		if ((DirXform != null) && (CameraController != null))
		{
			Quaternion q = Quaternion.identity;
			DirXform.rotation = q * CameraController.transform.rotation;
		}
	}
	
	/// <summary>
	/// Jump! Must be enabled manually.
	/// </summary>
	public bool Jump()
	{
		if (!Controller.isGrounded)
			return false;

		MoveThrottle += new Vector3(0, JumpForce, 0);

		return true;
	}

	/// <summary>
	/// Stop this instance.
	/// </summary>
	public void Stop()
	{
		Controller.Move(Vector3.zero);
		MoveThrottle = Vector3.zero;
		FallSpeed = 0.0f;
	}	
	
	/// <summary>
	/// Initializes the inputs.
	/// </summary>
	public void InitializeInputs()
	{
		// Get our start direction
		OrientationOffset = transform.rotation;
		// Make sure to set y rotation to 0 degrees
		YRotation = 0.0f;
	}
	
	/// <summary>
	/// Sets the cameras.
	/// </summary>
	public void SetCameras()
	{
		if(CameraController != null)
		{
			// Make sure to set the initial direction of the camera 
			// to match the game player direction
			CameraController.SetOrientationOffset(OrientationOffset);
			CameraController.SetYRotation(YRotation);
		}
	}
	
	/// <summary>
	/// Gets the move scale multiplier.
	/// </summary>
	/// <param name="moveScaleMultiplier">Move scale multiplier.</param>
	public void GetMoveScaleMultiplier(ref float moveScaleMultiplier)
	{
		moveScaleMultiplier = MoveScaleMultiplier;
	}
	/// <summary>
	/// Sets the move scale multiplier.
	/// </summary>
	/// <param name="moveScaleMultiplier">Move scale multiplier.</param>
	public void SetMoveScaleMultiplier(float moveScaleMultiplier)
	{
		MoveScaleMultiplier = moveScaleMultiplier;
	}
	
	/// <summary>
	/// Gets the rotation scale multiplier.
	/// </summary>
	/// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
	public void GetRotationScaleMultiplier(ref float rotationScaleMultiplier)
	{
		rotationScaleMultiplier = RotationScaleMultiplier;
	}
	/// <summary>
	/// Sets the rotation scale multiplier.
	/// </summary>
	/// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
	public void SetRotationScaleMultiplier(float rotationScaleMultiplier)
	{
		RotationScaleMultiplier = rotationScaleMultiplier;
	}
	
	/// <summary>
	/// Gets the allow mouse rotation.
	/// </summary>
	/// <param name="skipMouseRotation">Allow mouse rotation.</param>
	public void GetSkipMouseRotation(ref bool skipMouseRotation)
	{
		skipMouseRotation = SkipMouseRotation;
	}
	/// <summary>
	/// Sets the allow mouse rotation.
	/// </summary>
	/// <param name="skipMouseRotation">If set to <c>true</c> allow mouse rotation.</param>
	public void SetSkipMouseRotation(bool skipMouseRotation)
	{
		SkipMouseRotation = skipMouseRotation;
	}
	
	/// <summary>
	/// Gets the halt update movement.
	/// </summary>
	/// <param name="haltUpdateMovement">Halt update movement.</param>
	public void GetHaltUpdateMovement(ref bool haltUpdateMovement)
	{
		haltUpdateMovement = HaltUpdateMovement;
	}
	/// <summary>
	/// Sets the halt update movement.
	/// </summary>
	/// <param name="haltUpdateMovement">If set to <c>true</c> halt update movement.</param>
	public void SetHaltUpdateMovement(bool haltUpdateMovement)
	{
		HaltUpdateMovement = haltUpdateMovement;
	}
	#endregion
}

