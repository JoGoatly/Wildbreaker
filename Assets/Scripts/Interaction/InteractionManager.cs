using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    [System.Serializable]
    public class InteractionEntry
    {
        public Collider zone;
        [TextArea]
        public string displayText;
        public Sprite displayImage;
    }

    public List<InteractionEntry> interactions;

    [Header("Text Anzeige")]
    public GameObject interactionPanel;
    public TMP_Text interactionText;

    [Header("Bild Anzeige")]
    public GameObject imagePanel;
    public Image displayImage;

    [Header("Timing")]
    public float fadeOutDelay = 1.5f;

    private float hideTimer = 0f;
    private bool isVisible = false;
    private bool inZone = false;
    private bool initialized = false;
    private string overrideText = null;

    void Start()
    {
        interactionPanel.SetActive(false);
        imagePanel.SetActive(false);
        StartCoroutine(PreloadPanels());
    }

    IEnumerator PreloadPanels()
    {
        yield return new WaitForEndOfFrame();
        interactionPanel.SetActive(true);
        imagePanel.SetActive(true);
        foreach (var entry in interactions)
        {
            if (entry.displayImage != null)
            {
                displayImage.sprite = entry.displayImage;
                yield return new WaitForEndOfFrame();
            }
        }
        yield return new WaitForEndOfFrame();
        interactionPanel.SetActive(false);
        imagePanel.SetActive(false);
        initialized = true;
    }

    public void OverridePrompt(string text)
    {
        overrideText = text;
        interactionText.text = text;
        interactionPanel.SetActive(true);
        imagePanel.SetActive(false);
        isVisible = true;
        hideTimer = 0f;
    }

    public void OverridePromptImage(Sprite sprite)
    {
        overrideText = "image";
        displayImage.sprite = sprite;
        imagePanel.SetActive(true);
        interactionPanel.SetActive(false);
        isVisible = true;
        hideTimer = 0f;
    }

    public void ClearOverridePrompt()
    {
        overrideText = null;
        interactionPanel.SetActive(false);
        imagePanel.SetActive(false);
        isVisible = false;
    }

    void Update()
    {
        if (!initialized) return;
        if (overrideText != null) return;

        inZone = false;

        foreach (var entry in interactions)
        {
            if (entry.zone == null) continue;

            if (entry.zone.bounds.Contains(transform.position))
            {
                inZone = true;

                if (!isVisible)
                {
                    bool hasImage = entry.displayImage != null;
                    bool hasText = !string.IsNullOrEmpty(entry.displayText);

                    if (hasImage)
                    {
                        displayImage.sprite = entry.displayImage;
                        imagePanel.SetActive(true);
                        interactionPanel.SetActive(false);
                    }
                    else if (hasText)
                    {
                        interactionText.text = entry.displayText;
                        interactionPanel.SetActive(true);
                        imagePanel.SetActive(false);
                    }

                    isVisible = true;
                    hideTimer = 0f;
                }

                break;
            }
        }

        if (!inZone && isVisible)
        {
            hideTimer += Time.deltaTime;

            if (hideTimer >= fadeOutDelay)
            {
                interactionPanel.SetActive(false);
                imagePanel.SetActive(false);
                isVisible = false;
                hideTimer = 0f;
            }
        }
    }
}