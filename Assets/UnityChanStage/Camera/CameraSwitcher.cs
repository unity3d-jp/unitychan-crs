using UnityEngine;
using System.Collections;

public class CameraSwitcher : MonoBehaviour
{
    public string targetName;
    public Transform[] points;
    public float interval = 2.0f;
    public float stability = 0.5f;
    public float rotationSpeed = 2.0f;
    public float minDistance = 0.5f;
    public AnimationCurve fovCurve = AnimationCurve.Linear(1, 30, 10, 30);
    public bool autoChange = true;

    Transform target;
    Vector3 followPoint;

    void Start()
    {
        // Target information.
        target = GameObject.Find(targetName).transform;
        followPoint = target.position;

        // Initialize DOF fx.
        var dofFx = GetComponentInChildren<DepthOfFieldScatter>();
        if (dofFx) dofFx.focalTransform = target;

        // Start auto-changer if it's enabled.
        if (autoChange) StartAutoChange();
    }

    void Update()
    {
        // Update the follow point with the exponential easing function.
        var param = Mathf.Exp(-rotationSpeed * Time.deltaTime);
        followPoint = Vector3.Lerp(target.position, followPoint, param);

        // Look at the follow point.
        transform.LookAt(followPoint);
    }

    // Change the camera position.
    public void ChangePosition(Transform destination, bool forceStable = false)
    {
        // Do nothing if disabled.
        if (!enabled) return;

        // Move to the point.
        transform.position = destination.position;

        // Snap if stable; Shake if unstable.
        if (Random.value < stability || forceStable)
            followPoint = target.position;
        else
            followPoint += Random.insideUnitSphere;

        // Update the FOV depending on the distance to the target.
        var dist = Vector3.Distance(target.position, transform.position);
        GetComponentInChildren<Camera>().fieldOfView = fovCurve.Evaluate(dist);
    }

    // Choose a point other than the current.
    Transform ChooseAnotherPoint(Transform current)
    {
        while (true)
        {
            var next = points[Random.Range(0, points.Length)];
            var dist = Vector3.Distance(next.position, target.position);
            if (next != current && dist > minDistance) return next;
        }
    }

    // Auto-changer.
    IEnumerator AutoChange()
    {
        for (var current = points[0]; true; current = ChooseAnotherPoint(current))
        {
            ChangePosition(current);
            yield return new WaitForSeconds(interval);
        }
    }

    public void StartAutoChange()
    {
        StartCoroutine("AutoChange");
    }

    public void StopAutoChange()
    {
        StopCoroutine("AutoChange");
    }
}
