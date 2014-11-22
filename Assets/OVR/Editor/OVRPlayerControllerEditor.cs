/************************************************************************************

Filename    :   OVRPlayerControllerEditor.cs
Content     :   Player controller interface. 
				This script adds editor functionality to the OVRPlayerController
Created     :   January 17, 2013
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
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(OVRPlayerController))]

//-------------------------------------------------------------------------------------
// ***** OVRPlayerControllerEditor
//
// OVRPlayerControllerEditor adds extra functionality in the inspector for the currently
// selected OVRPlayerController.
//
public class OVRPlayerControllerEditor : Editor
{
	// target component
	private OVRPlayerController m_Component;

	// OnEnable
	void OnEnable()
	{
		m_Component = (OVRPlayerController)target;
	}

	// OnDestroy
	void OnDestroy()
	{
	}

	// OnInspectorGUI
	public override void OnInspectorGUI()
	{
		GUI.color = Color.white;
		
		Undo.RecordObject(m_Component, "OVRPlayerController");

		{
			m_Component.Acceleration 	  = EditorGUILayout.Slider("Acceleration", 			m_Component.Acceleration, 	  0, 1);
			m_Component.Damping 		  = EditorGUILayout.Slider("Damping", 				m_Component.Damping, 		  0, 1);
			m_Component.BackAndSideDampen = EditorGUILayout.Slider("Back and Side Dampen", 	m_Component.BackAndSideDampen,0, 1);
//			m_Component.JumpForce 		  = EditorGUILayout.Slider("Jump Force", 			m_Component.JumpForce, 		  0, 10);
			m_Component.RotationAmount 	  = EditorGUILayout.Slider("Rotation Amount", 		m_Component.RotationAmount,   0, 5);
			m_Component.RotationRatchet   = EditorGUILayout.Slider("Rotation Ratchet", 		m_Component.RotationRatchet,  0, 180);
				
			OVREditorGUIUtility.Separator();

			m_Component.GravityModifier = EditorGUILayout.Slider("Gravity Modifier", 		m_Component.GravityModifier, 0, 1);

			OVREditorGUIUtility.Separator();
		}
		
		if (GUI.changed)
		{
			EditorUtility.SetDirty(m_Component);
		}
	}		
}

