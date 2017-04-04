using UnityEngine;
using System.Collections;

public class GlobalConfig : MonoBehaviour
{
    public int targetFrameRate = -1;
    public int shaderLOD = 1000;

    void Start()
    {
        if (targetFrameRate > 0)
            Application.targetFrameRate = targetFrameRate;

        Shader.globalMaximumLOD = shaderLOD; 

        Cursor.visible = false;
    }
}
