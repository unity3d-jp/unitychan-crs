using UnityEngine;
using System.Collections;

public class WaitingRoom : MonoBehaviour
{
    public GameObject cameraPrefab;
    public GameObject characterPrefab;
    public float fadeTime = 0.5f;

    ScreenOverlay[] screenOverlays;
    float overlayIntensity = 1.0f;

    Animator animator;
    bool start;

    void Awake()
    {
        // Instantiate the camera.
        var go = (GameObject)Instantiate(cameraPrefab);
        screenOverlays = go.GetComponentsInChildren<ScreenOverlay>();

        // Instantiate the character.
        go = (GameObject)Instantiate(characterPrefab);
        animator = go.GetComponent<Animator>();
    }

    IEnumerator Start()
    {
        // Reset character animation repeatedly.
        while (true)
        {
            yield return new WaitForSeconds(3.0f);

            for (var layer = 0; layer < animator.layerCount; layer++)
            {
                var info = animator.GetCurrentAnimatorStateInfo(layer);
                animator.CrossFade(info.nameHash, 0.5f / info.length, layer, 0);
            }
        }
    }

    void Update()
    {
        start |= Input.GetButtonDown("Jump") | Input.GetMouseButtonDown(0);
        
        if (start)
        {
            // White out.
            overlayIntensity = Mathf.Min(1.0f, overlayIntensity + Time.deltaTime / fadeTime);
            if (overlayIntensity == 1.0f) Application.LoadLevel(1);
        }
        else
        {
            // Fade in.
            overlayIntensity = Mathf.Max(0.0f, overlayIntensity - Time.deltaTime / fadeTime);
        }

        foreach (var so in screenOverlays)
        {
            so.intensity = overlayIntensity;
            so.enabled = overlayIntensity > 0.01f;
        }
    }
}
