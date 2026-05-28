using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindingsMenu : MonoBehaviour
{
    [System.Serializable]
    public class KeybindingEntry
    {
        public string actionName;
        public Sprite keyIcon;
    }

    [System.Serializable]
    public class KeybindingCategory
    {
        public string categoryName;
        public List<KeybindingEntry> entries;
    }

    [Header("Tastatur Belegung")]
    public List<KeybindingCategory> keyboardCategories;

    [Header("Controller Belegung")]
    public List<KeybindingCategory> controllerCategories;

    [Header("Referenzen")]
    public Transform contentParent;
    public GameObject categoryHeaderPrefab;
    public GameObject keybindingEntryPrefab;
    public Toggle inputDeviceToggle;

    [Header("Layout")]
    public float entryWidth = 400f;
    public float entryHeight = 50f;
    public float headerWidth = 400f;
    public float headerHeight = 60f;

    void OnEnable()
    {
        if (inputDeviceToggle != null)
        {
            inputDeviceToggle.onValueChanged.RemoveListener(OnToggleChanged);
            inputDeviceToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        BuildList();
    }

    void OnDisable()
    {
        if (inputDeviceToggle != null)
            inputDeviceToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isController)
    {
        BuildList();
    }

    void BuildList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        bool useController = inputDeviceToggle != null && inputDeviceToggle.isOn;
        List<KeybindingCategory> activeList = useController ? controllerCategories : keyboardCategories;

        foreach (var category in activeList)
        {
            // Header
            GameObject header = Instantiate(categoryHeaderPrefab, contentParent);
            ForceSize(header, headerWidth, headerHeight);

            TMP_Text headerText = header.GetComponentInChildren<TMP_Text>();
            if (headerText != null)
                headerText.text = category.categoryName;

            // Einträge
            foreach (var entry in category.entries)
            {
                GameObject entryGO = Instantiate(keybindingEntryPrefab, contentParent);
                ForceSize(entryGO, entryWidth, entryHeight);

                TMP_Text label = entryGO.transform.Find("ActionLabel")?.GetComponent<TMP_Text>();
                Image icon = entryGO.transform.Find("KeyIcon")?.GetComponent<Image>();

                if (label != null)
                    label.text = entry.actionName;

                if (icon != null)
                {
                    icon.sprite = entry.keyIcon;
                    icon.enabled = entry.keyIcon != null;
                }
            }
        }
    }

    void ForceSize(GameObject go, float width, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();

        le.preferredWidth = width;
        le.preferredHeight = height;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;
    }
}