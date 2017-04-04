using UnityEngine;
using System.Collections;


public class Visualizer : MonoBehaviour
{
	public Reaktion.ReaktorLink spectrum1;
	public Reaktion.ReaktorLink spectrum2;
	public Reaktion.ReaktorLink spectrum3;
	public Reaktion.ReaktorLink spectrum4;
	public Vector4 spectrum;

	void Update()
	{
		spectrum = new Vector4(spectrum1.Output, spectrum2.Output, spectrum3.Output, spectrum4.Output);
	}

	void OnWillRenderObject()
	{
		if (GetComponent<Renderer>() == null || GetComponent<Renderer>().sharedMaterial == null) { return; }
		Material mat = GetComponent<Renderer>().material;

		if (Vector4.Dot(spectrum, spectrum) <= 1.0f)
		{
			mat.SetVector("_Spectra", spectrum);
		}

		Camera cam = Camera.current;
		if (cam != null) {
			Matrix4x4 view = cam.worldToCameraMatrix;
			Matrix4x4 proj = cam.projectionMatrix;
			proj[2, 0] = proj[2, 0] * 0.5f + proj[3, 0] * 0.5f;
			proj[2, 1] = proj[2, 1] * 0.5f + proj[3, 1] * 0.5f;
			proj[2, 2] = proj[2, 2] * 0.5f + proj[3, 2] * 0.5f;
			proj[2, 3] = proj[2, 3] * 0.5f + proj[3, 3] * 0.5f;
			Matrix4x4 viewprojinv = (proj * view).inverse;
			mat.SetMatrix("_ViewProjectInverse", viewprojinv);
		}
	}
}
