using UnityEngine;
using System.Collections;

public class StageDirector : MonoBehaviour
{
    // Control options.
    public bool ignoreFastForward = true;


    // Camera points.
    public Transform[] cameraPoints;

    // Exposed to animator.
    public float overlayIntensity = 1.0f;

    // Objects to be controlled.
   public  GameObject musicPlayer;
    CameraSwitcher mainCameraSwitcher;
    ScreenOverlay[] screenOverlays;
    public GameObject[] objectsNeedsActivation;
    public GameObject[] objectsOnTimeline;

	public GameObject cameraRig;

    void Awake()
    {
        mainCameraSwitcher = cameraRig.GetComponentInChildren<CameraSwitcher>();
        screenOverlays = cameraRig.GetComponentsInChildren<ScreenOverlay>();
    }

    void Update()
    {
        foreach (var so in screenOverlays)
        {
            so.intensity = overlayIntensity;
            so.enabled = overlayIntensity > 0.01f;
        }
    }

    public void StartMusic()
    {
        foreach (var source in musicPlayer.GetComponentsInChildren<AudioSource>())
            source.Play();
    }

    public void ActivateProps()
    {
        foreach (var o in objectsNeedsActivation) o.BroadcastMessage("ActivateProps");
    }

    public void SwitchCamera(int index)
    {
        if (mainCameraSwitcher)
            mainCameraSwitcher.ChangePosition(cameraPoints[index], true);
    }

    public void StartAutoCameraChange()
    {
        if (mainCameraSwitcher)
            mainCameraSwitcher.StartAutoChange();
    }

    public void StopAutoCameraChange()
    {
        if (mainCameraSwitcher)
            mainCameraSwitcher.StopAutoChange();
    }

    public void FastForward(float second)
    {
        if (!ignoreFastForward)
        {
            FastForwardAnimator(GetComponent<Animator>(), second, 0);
            foreach (var go in objectsOnTimeline)
                foreach (var animator in go.GetComponentsInChildren<Animator>())
                    FastForwardAnimator(animator, second, 0.5f);
        }
    }

    void FastForwardAnimator(Animator animator, float second, float crossfade)
    {
        for (var layer = 0; layer < animator.layerCount; layer++)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (crossfade > 0.0f)
                animator.CrossFade(info.nameHash, crossfade / info.length, layer, info.normalizedTime + second / info.length);
            else
                animator.Play(info.nameHash, layer, info.normalizedTime + second / info.length);
        }
    }

    public void EndPerformance()
    {
        Application.LoadLevel(0);
    }
}
