/************************************************************************************

Filename    :   OVRUtils.cs
Content     :   Base component OVR class
Created     :  	May 13, 2013
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
using System.Collections.Generic;
using OVR;


//-------------------------------------------------------------------------------------
// ***** OVRUtils
//

/// <summary>
/// OVRUtils holds misc. static utility functions that can be used by any script.
/// </summary>
public class OVRUtils
{
	/// <summary>
	/// Sets the local transform identity.
	/// </summary>
	/// <param name="gameObject">Game object.</param>
	public static void SetLocalTransformIdentity(ref GameObject gameObject)
	{
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale    = Vector3.one;
	}

	/// <summary>
	/// Sets the local transform.
	/// </summary>
	/// <param name="gameObject">Game object.</param>
	/// <param name="xfrm">Xfrm.</param>
	public static void SetLocalTransform(ref GameObject gameObject, ref Transform xfrm)
	{
		gameObject.transform.localPosition = xfrm.position;
		gameObject.transform.localRotation = xfrm.rotation;
		gameObject.transform.localScale    = xfrm.localScale;
	}

	/// <summary>
	/// Blit - Copies one render texture onto another through a material
	/// flip will flip the render horizontally
	/// </summary>
	/// <param name="source">Source.</param>
	/// <param name="dest">Destination.</param>
	/// <param name="m">M.</param>
	/// <param name="flip">If set to <c>true</c> flip.</param>
	public void Blit (RenderTexture source, RenderTexture dest, Material m, bool flip) 
	{
		Material material = m;
		
		// Make the destination texture the target for all rendering
		RenderTexture.active = dest;  		
		
		// Assign the source texture to a property from a shader
		source.SetGlobalShaderProperty ("_MainTex");	
		
		// Set up the simple Matrix
		GL.PushMatrix ();
		GL.LoadOrtho ();
		for(int i = 0; i < material.passCount; i++)
		{
			material.SetPass(i);
			DrawQuad(flip);
		}
		GL.PopMatrix ();
	}
	
	/// <summary>
	/// Draws the quad.
	/// </summary>
	/// <param name="flip">If set to <c>true</c> flip.</param>
	public void DrawQuad(bool flip)
	{
		GL.Begin (GL.QUADS);
		
		if(flip == true)
		{
			GL.TexCoord2( 0.0f, 1.0f ); GL.Vertex3( 0.0f, 0.0f, 0.1f );
			GL.TexCoord2( 1.0f, 1.0f ); GL.Vertex3( 1.0f, 0.0f, 0.1f );
			GL.TexCoord2( 1.0f, 0.0f ); GL.Vertex3( 1.0f, 1.0f, 0.1f );
			GL.TexCoord2( 0.0f, 0.0f ); GL.Vertex3( 0.0f, 1.0f, 0.1f );
		}
		else
		{
			GL.TexCoord2( 0.0f, 0.0f ); GL.Vertex3( 0.0f, 0.0f, 0.1f );
			GL.TexCoord2( 1.0f, 0.0f ); GL.Vertex3( 1.0f, 0.0f, 0.1f );
			GL.TexCoord2( 1.0f, 1.0f ); GL.Vertex3( 1.0f, 1.0f, 0.1f );
			GL.TexCoord2( 0.0f, 1.0f ); GL.Vertex3( 0.0f, 1.0f, 0.1f );
		}
		
		GL.End();
	}
}

public static class OVRExtensions
{
	public static Matrix4x4 ToMatrix4x4(this ovrMatrix4f ovrMat)
	{
        Matrix4x4 mat = new Matrix4x4();

		mat[0, 0] = ovrMat.m[0, 0];
		mat[0, 1] = ovrMat.m[0, 1];
		mat[0, 2] = ovrMat.m[0, 2];
		mat[0, 3] = ovrMat.m[0, 3];
		mat[1, 0] = ovrMat.m[1, 0];
		mat[1, 1] = ovrMat.m[1, 1];
		mat[1, 2] = ovrMat.m[1, 2];
		mat[1, 3] = ovrMat.m[1, 3];
		mat[2, 0] = ovrMat.m[2, 0];
		mat[2, 1] = ovrMat.m[2, 1];
		mat[2, 2] = ovrMat.m[2, 2];
		mat[2, 3] = ovrMat.m[2, 3];
		mat[3, 0] = ovrMat.m[3, 0];
		mat[3, 1] = ovrMat.m[3, 1];
		mat[3, 2] = ovrMat.m[3, 2];
		mat[3, 3] = ovrMat.m[3, 3];

		return mat;
	}

	public static Vector3 ToVector3(this ovrVector3f ovrVec, bool rhToLh = true)
	{
		Vector3 vec = new Vector3(ovrVec.x, ovrVec.y, ovrVec.z);

		if (rhToLh)
			vec.z = -vec.z;

		return vec;
	}
	
	public static Quaternion ToQuaternion(this ovrQuatf ovrQuat, bool rhToLh = true)
	{
		Quaternion q = new Quaternion(ovrQuat.x, ovrQuat.y, ovrQuat.z, ovrQuat.w);

		if (rhToLh)
		{
			q.x = -q.x; 
			q.y = -q.y;	
		}

		return q;
	}
}


