using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class ForceAspectRatio : MonoBehaviour
{
    public float horizontal = 16;
    public float vertical = 9;

    void Update()
    {
        GetComponent<Camera>().aspect = horizontal / vertical;
    }
}
