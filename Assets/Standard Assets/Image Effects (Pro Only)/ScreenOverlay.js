
#pragma strict

@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Other/Screen Overlay")

class ScreenOverlay extends PostEffectsBase {
	
	enum OverlayBlendMode {
		Additive = 0,
		ScreenBlend = 1,
		Multiply = 2,
        Overlay = 3,
        AlphaBlend = 4,	
	}
	
	public var blendMode : OverlayBlendMode = OverlayBlendMode.Overlay;
	public var intensity : float = 1.0f;
	public var texture : Texture2D;
			
	public var overlayShader : Shader;
	private var overlayMaterial : Material = null;

	function CheckResources () : boolean {
		CheckSupport (false);
		
		overlayMaterial = CheckShaderAndCreateMaterial (overlayShader, overlayMaterial);
		
		if 	(!isSupported)
			ReportAutoDisable ();
		return isSupported;
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture) {		
		if (CheckResources() == false) {
			Graphics.Blit (source, destination);
			return;
		}

		var UV_Transform : Vector4 = Vector4(1, 0, 0, 1);
		
		#if UNITY_WP8
		// WP8 has no OS support for rotating screen with device orientation,
		// so we do those transformations ourselves.
		if (Screen.orientation == ScreenOrientation.LandscapeLeft) {
			UV_Transform = Vector4(0, -1, 1, 0);
		}
		if (Screen.orientation == ScreenOrientation.LandscapeRight) {
			UV_Transform = Vector4(0, 1, -1, 0);
		}
		if (Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
			UV_Transform = Vector4(-1, 0, 0, -1);
		}	
		#endif
		
		overlayMaterial.SetVector("_UV_Transform", UV_Transform);
		overlayMaterial.SetFloat ("_Intensity", intensity);
		overlayMaterial.SetTexture ("_Overlay", texture);
		Graphics.Blit (source, destination, overlayMaterial, blendMode);
	}
}