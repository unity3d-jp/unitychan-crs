/*
 * Copyright 2019,2020 Sony Corporation
 */

using UnityEngine;
using UnityEditor;

public class PPSChecker
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
#endif
    static void CheckPPS()
    {
#if !UNITY_POST_PROCESSING_STACK_V2
        Debug.LogWarning("You should import Post Processing package from Package Manager if you want to use SRDPostProcessingSample");
#endif
    }
}
