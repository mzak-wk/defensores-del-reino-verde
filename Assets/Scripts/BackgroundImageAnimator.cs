// BackgroundImageAnimator.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundImageAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 2f;
    int i; float t;
    Image img;

    void Awake() => img = GetComponent<Image>();

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.unscaledDeltaTime;
        if (t >= 1f / fps)
        {
            i = (i + 1) % frames.Length;
            img.sprite = frames[i];
            t = 0f;
        }
    }
}
