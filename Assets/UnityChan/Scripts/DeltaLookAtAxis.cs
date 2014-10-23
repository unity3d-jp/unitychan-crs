//
// Mecanim IKLookと連動して、ユニティちゃんの瞳テクスチャのUVオフセットを動かす.
// DeltaLookAtAxis.cs
// 本スクリプトは、瞳の中央付近に設定したロケータにアタッチして使用する.
// 2014/09/10 N.Kobayashi @UTJ
//

using UnityEngine;
using System.Collections;

namespace UnityChan
{

	public class DeltaLookAtAxis : MonoBehaviour {

		//瞳のメッシュレンダラの参照.
		public MeshRenderer eyeObj;
		//オフセットの調整値（初期値は推奨値）.
		public float multiplierX = 0.5f;
		public float multiplierY = 1.0f;

		private Quaternion orgRotation;
		private Quaternion lookAtRotation;
		//private Vector3 orgEuler;
		private Vector3 orgAxis;
		private Vector3 lookAtAxis;
		private Vector3 dirVector;
		private Vector2 xyDelta;

		//初期回転の取得と初期化.
		void Awake(){
			//LookAtの入っていない瞳の初期回転を取得しておく.
			orgRotation = this.transform.localRotation;

			//検証中//
			//orgEuler = new Vector3(
			//	orgRotation.eulerAngles.x,
			//	orgRotation.eulerAngles.y,
			//	orgRotation.eulerAngles.z);

			//初期回転に合わせて正面向きに瞳の軸ベクトルを設定する.
			orgAxis = orgRotation * Vector3.forward;
			//uvオフセットのdeltaを初期化.
			xyDelta = Vector2.zero;
		}


		// Update is called once per frame
		void Update () {
			//LookAt中の瞳の回転を取得する.
			lookAtRotation = this.transform.localRotation;

			//検証中//
			//Vector3 lookAtEuler = new Vector3(
			//	lookAtRotation.eulerAngles.x,
			//	lookAtRotation.eulerAngles.y,
			//	lookAtRotation.eulerAngles.z);
			//Debug.Log("orgEuler: " + orgEuler);
			//Debug.Log("lookAtEuler: " + lookAtEuler);
			//float angle = Quaternion.Angle(orgRotation, lookAtRotation);
			//Debug.Log ("angle is " + angle);

			//現在の軸ベクトルを計算する.
			lookAtAxis = lookAtRotation * Vector3.forward;
			//現在の軸ベクトルと元の軸ベクトルより変化の方向ベクトルを求める.
			dirVector = lookAtAxis - orgAxis;
			//Debug.Log ("dirVector is " + dirVector);
			//瞳テクスチャのオフセット方向に合わせて、uvオフセットのdeltaをマッピングする.
			xyDelta = new Vector2((-dirVector.z) * multiplierX, dirVector.x * multiplierY);
			//Debug.Log ("xyDelta is " + xyDelta);
			//マッピングしたdeltaをuvオフセット値に設定する.
			eyeObj.material.SetTextureOffset("_MainTex", xyDelta);
		}
	}
}
