//
//MusicStarter.cs
//Mecanimアニメーションイベントを使ったMusicStarter
//2014/07/29 N.Kobayashi
//
using UnityEngine;
using System.Collections;

namespace UnityChan
{
	public class MusicStarter : MonoBehaviour {
		
		// オーディオソースへの参照
		public AudioSource refAudioSource;


		// Use this for initialization
		void Start () {
            if (refAudioSource) refAudioSource.Pause();
		}
		
		// Mecanimアニメーションイベントとして指定するOnCallMusicPlay
		public void OnCallMusicPlay(string str){
			// 文字列playを指定で再生開始
			if(str == "play")
                if (refAudioSource) refAudioSource.Play();
			// 文字列stopを指定で再生停止
			else if (str == "stop")
                if (refAudioSource) refAudioSource.Stop();
			// それ以外はポーズ
			else
                if (refAudioSource) refAudioSource.Pause();
		}
	}
}

