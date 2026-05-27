using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindingsMenu : MonoBehaviour
{
    [System.Serializable]
    public class KeybindingEntry
    {
        public string actionName;       // z.B. "Vorne"
        public Sprite keyboardIcon;     // Icon für Tastatur
        public Sprite controllerIcon;   // Icon für Controller
    }

    [System.Serializable]
    public class KeybindingCategory
    {
        public string categoryName;     // z.B. "Bewegung"
        public List<KeybindingEntry> entries;
    }

    [Header("Daten")]
    public List<KeybindingCategory> categories;

    [Header("Referenzen")]
    public Transform contentParent;             // Content im Scroll View
    public GameObject categoryHeaderPrefab;
    public GameObject keybindingEntryPrefab;
    public Toggle inputDeviceToggle;            // Aus = Tastatur, An = Controller

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
        // Alte Einträge löschen
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        bool useController = inputDeviceToggle != null && inputDeviceToggle.isOn;

        foreach (var category in categories)
        {
            // Header
            GameObject header = Instantiate(categoryHeaderPrefab, contentParent);
            TMP_Text headerText = header.GetComponentInChildren<TMP_Text>();
            if (headerText != null)
                headerText.text = category.categoryName;

            // Einträge
            foreach (var entry in category.entries)
            {
                GameObject entryGO = Instantiate(keybindingEntryPrefab, contentParent);

                TMP_Text label = entryGO.transform.Find("ActionLabel")?.GetComponent<TMP_Text>();
                Image icon = entryGO.transform.Find("KeyIcon")?.GetComponent<Image>();

                if (label != null)
                    label.text = entry.actionName;

                if (icon != null)
                {
                    Sprite sprite = useController ? entry.controllerIcon : entry.keyboardIcon;
                    icon.sprite = sprite;
                    icon.enabled = sprite != null;
                }
            }
        }
    }
}