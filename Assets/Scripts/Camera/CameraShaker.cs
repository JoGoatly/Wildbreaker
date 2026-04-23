using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        Instance = this;
    }

    public void ShakeHit(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(Shake(duration, magnitude));
    }

    public void ShakeBreak(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(Shake(duration, magnitude));
    }

    IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            shakeOffset = Random.insideUnitSphere * strength;
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }

    public Vector3 GetShakeOffset()
    {
        return shakeOffset;
    }
}