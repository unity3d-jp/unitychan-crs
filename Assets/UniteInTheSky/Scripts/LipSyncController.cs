using UnityEngine;
using System.Collections;

public class LipSyncController : MonoBehaviour
{
    public string targetName;

    public Transform nodeA;
    public Transform nodeE;
    public Transform nodeI;
    public Transform nodeO;
    public Transform nodeU;

    public AnimationCurve weightCurve;

    SkinnedMeshRenderer target;

    void Start()
    {
        target = GameObject.Find(targetName).GetComponent<SkinnedMeshRenderer>();
    }

    float GetWeight(Transform tr)
    {
        return weightCurve.Evaluate(tr.localPosition.z);
    }

    void LateUpdate()
    {
        var total = 100.0f;

        var w = total * GetWeight(nodeA);
        target.SetBlendShapeWeight(6, w);
        total -= w;

        w = total * GetWeight(nodeI);
        target.SetBlendShapeWeight(7, w);
        total -= w;

        w = total * GetWeight(nodeU);
        target.SetBlendShapeWeight(8, w);
        total -= w;

        w = total * GetWeight(nodeE);
        target.SetBlendShapeWeight(9, w);
        total -= w;

        w = total * GetWeight(nodeO);
        target.SetBlendShapeWeight(10, w);
    }
}
