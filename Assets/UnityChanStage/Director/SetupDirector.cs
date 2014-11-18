using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SetupDirector : MonoBehaviour 
{
#if UNITY_EDITOR
	[SerializeField]
	private GameObject cameraRigPrefab, musicPlayer;

	[SerializeField]
	private GameObject[] 	objectNeedsActivation = null,
							objectsOnTimeline = null, 
							mips = null;

	[HideInInspector]
	public List<Object> generatedItemList = new List<Object>();

	[ContextMenu("Stage Setup")]
	void Setup()
	{
		Clean();

		var stageDirector = FindObjectOfType<StageDirector>();
		if( stageDirector == null )
		{
			Debug.LogWarning("Stage Director is not found");
			return;
		}

		stageDirector.cameraRig = Instantate<GameObject>(cameraRigPrefab);

		stageDirector.musicPlayer = Instantate<GameObject>(musicPlayer);

		stageDirector.objectsNeedsActivation = Instantate<GameObject>(objectNeedsActivation);
		stageDirector.objectsOnTimeline = Instantate<GameObject>(objectsOnTimeline);
		Instantate<GameObject>(mips);
	}

	[ContextMenu("Stage Clean")]
	void Clean()
	{
		var stageDirector = FindObjectOfType<StageDirector>();
		if( stageDirector == null )
		{
			Debug.LogWarning("Stage Director is not found");
			return;
		}

		foreach( var item in generatedItemList )
			DestroyImmediate(item);

		stageDirector.objectsOnTimeline = new GameObject[0];
		stageDirector.objectsNeedsActivation = new GameObject[0];
		stageDirector.cameraRig = null;
		stageDirector.musicPlayer = null;

		generatedItemList.Clear();
	}

	public T Instantate<T>(T obj) where T : Object
	{

		T item;

		if( UnityEditor.EditorApplication.isPlaying )
		{
			item = (T)GameObject.Instantiate(obj);
		}else{
			item = (T)UnityEditor.PrefabUtility.InstantiatePrefab(obj);
		}
		generatedItemList.Add(item);
		return item;
	}

	public T[] Instantate<T>(T[] obj) where T : Object
	{
		T[] item = new T[obj.Length];

		for(int i=0; i<item.Length; i++)
		{
			item[i] = Instantate<T>(obj[i]);
		}
		return item;
	}
#endif
}
