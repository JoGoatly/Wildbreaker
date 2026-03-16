using UnityEngine;
using UnityEngine.UI;

public class GifPlayer : MonoBehaviour
{
    public Texture2D[] frames;
    public float fps = 24f;

    private RawImage rawImage;
    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        if (frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            rawImage.texture = frames[currentFrame];
        }
    }
}