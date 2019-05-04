using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;

public class FocusPullerHelper : MonoBehaviour {
	private Transform focusObj;
	private FocusPuller focusPuller;
    private Transform _target;

    private Transform target {
        get { return _target; }
        set { _target = value; }
    }

	void Awake()
	{
		focusPuller = GetComponent<FocusPuller>(); 
	}

	// Use this for initialization
	void Start () {
		//フォーカスを合わせる対象にタグ"FocusObj"をつけておくこと.
		focusObj = GameObject.FindGameObjectWithTag("FocusObj").transform;
		focusPuller.target = focusObj;
	}
	
}
