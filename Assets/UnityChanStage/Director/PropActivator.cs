using UnityEngine;
using System.Collections;

public class PropActivator : MonoBehaviour
{
    void ActivateProps()
    {
        foreach (Transform c in transform) c.gameObject.SetActive(true);
    }
}
