using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindingsMenu : MonoBehaviour
{
    [System.Serializable]
    public class KeybindingEntry
    {
        public string actionName;   // z.B. "Vorne"
        public Sprite keyIcon;      // Icon der Taste
    }

    [System.Serializable]
    public class KeybindingCategory
    {
        public string categoryName;             // z.B. "Bewegung"
        public List<KeybindingEntry> entries;   // Tasten in dieser Kategorie
    }

    [Header("Daten")]
    public List<KeybindingCategory> categories;

    [Header("Referenzen")]
    public Transform contentParent;     // Das "Content" GameObject im Scroll View
    public GameObject categoryHeaderPrefab;
    public GameObject keybindingEntryPrefab;

    void OnEnable()
    {
        BuildList();
    }

    void BuildList()
    {
        // Alte Einträge löschen
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Neu aufbauen
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

                if (label != null) label.text = entry.actionName;
                if (icon != null && entry.keyIcon != null) icon.sprite = entry.keyIcon;
            }
        }
    }
}