
#pragma strict

@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Camera/Vignette and Chromatic Aberration")

class Vignetting /* And Chromatic Aberration */ extends PostEffectsBase {

	public enum AberrationMode {
		Simple = 0,
		Advanced = 1,
	}

	public var mode : AberrationMode = AberrationMode.Simple;
	
	public var intensity : float = 0.375f; // intensity == 0 disables pre pass (optimization)
	public var chromaticAberration : float = 0.2f;
	public var axialAberration : float = 0.5f;

	public var blur : float = 0.0f; // blur == 0 disables blur pass (optimization)
	public var blurSpread : float = 0.75f;

	public var luminanceDependency : float = 0.25f;

	public var blurDistance : float = 2.5f;
		
	public var vignetteShader : Shader;
	private var vignetteMaterial : Material;
	
	public var separableBlurShader : Shader;
	private var separableBlurMaterial : Material;	
	
	public var chromAberrationShader : Shader;
	private var chromAberrationMaterial : Material;
	
	function CheckResources () : boolean {	
		CheckSupport (false);	
	
		vignetteMaterial = CheckShaderAndCreateMaterial (vignetteShader, vignetteMaterial);
		separableBlurMaterial = CheckShaderAndCreateMaterial (separableBlurShader, separableBlurMaterial);
		chromAberrationMaterial = CheckShaderAndCreateMaterial (chromAberrationShader, chromAberrationMaterial);
		
		if (!isSupported)
			ReportAutoDisable ();
		return isSupported;		
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture) {	
		if( CheckResources () == false) {
			Graphics.Blit (source, destination);
			return;
		}

		var rtW : int = source.width;
		var rtH : int = source.height;
				
		var doPrepass : boolean = (Mathf.Abs(blur)>0.0f || Mathf.Abs(intensity)>0.0f);

		var widthOverHeight : float = (1.0f * rtW) / (1.0f * rtH);
		var oneOverBaseSize : float = 1.0f / 512.0f;				
		
		var color : RenderTexture = null;
		var color2a : RenderTexture = null;
		var color2b : RenderTexture = null;

		if (doPrepass) {
			color = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);	
		
			// Blur corners
			if (Mathf.Abs (blur)>0.0f) {
				color2a = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);		

				Graphics.Blit (source, color2a, chromAberrationMaterial, 0);

				for(var i : int = 0; i < 2; i++) {	// maybe make iteration count tweakable
					separableBlurMaterial.SetVector ("offsets", Vector4 (0.0f, blurSpread * oneOverBaseSize, 0.0f, 0.0f));	
					color2b = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);
					Graphics.Blit (color2a, color2b, separableBlurMaterial);
					RenderTexture.ReleaseTemporary (color2a);

					separableBlurMaterial.SetVector ("offsets", Vector4 (blurSpread * oneOverBaseSize / widthOverHeight, 0.0f, 0.0f, 0.0f));	
					color2a = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);
					Graphics.Blit (color2b, color2a, separableBlurMaterial);	
					RenderTexture.ReleaseTemporary (color2b);
				}	
			}

			vignetteMaterial.SetFloat ("_Intensity", intensity); 		// intensity for vignette
			vignetteMaterial.SetFloat ("_Blur", blur); 					// blur intensity
			vignetteMaterial.SetTexture ("_VignetteTex", color2a);	// blurred texture

			Graphics.Blit (source, color, vignetteMaterial, 0); 		// prepass blit: darken & blur corners
		}		

		chromAberrationMaterial.SetFloat ("_ChromaticAberration", chromaticAberration);
		chromAberrationMaterial.SetFloat ("_AxialAberration", axialAberration);
		chromAberrationMaterial.SetVector ("_BlurDistance", Vector2 (-blurDistance, blurDistance));
		chromAberrationMaterial.SetFloat ("_Luminance", 1.0f/Mathf.Max(Mathf.Epsilon, luminanceDependency));

		if(doPrepass) color.wrapMode = TextureWrapMode.Clamp;
		else source.wrapMode = TextureWrapMode.Clamp;		
		Graphics.Blit (doPrepass ? color : source, destination, chromAberrationMaterial, mode == AberrationMode.Advanced ? 2 : 1);	
		
		RenderTexture.ReleaseTemporary (color);
		RenderTexture.ReleaseTemporary (color2a);
	}

}