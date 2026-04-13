using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum SettingTab
{
    Volume,
    Pad,
    Other
}

public class SettingTabController : MonoBehaviour
{
    [Serializable]
    private class TabEntry
    {
        public SettingTab tab;
        public Button button;
        public GameObject content;
        public Graphic tabGraphic;

        [NonSerialized] public UnityAction clickAction;
    }

    [SerializeField] private SettingTab initialTab = SettingTab.Volume;
    [SerializeField] private TabEntry[] tabs;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private bool disableSelectedButton;

    private SettingTab activeTab;
    private bool hasActiveTab;

    private void Awake()
    {
        RegisterButtonEvents();
    }

    private void Start()
    {
        SelectTab(initialTab);
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    public void SelectVolumeTab()
    {
        SelectTab(SettingTab.Volume);
    }

    public void SelectPadTab()
    {
        SelectTab(SettingTab.Pad);
    }

    public void SelectOtherTab()
    {
        SelectTab(SettingTab.Other);
    }

    public void SelectTab(SettingTab tab)
    {
        activeTab = tab;
        hasActiveTab = true;
        ApplyTabState();
    }

    private void RegisterButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null || entry.button == null)
            {
                continue;
            }

            SettingTab tab = entry.tab;
            entry.clickAction = () => SelectTab(tab);
            entry.button.onClick.AddListener(entry.clickAction);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null || entry.button == null || entry.clickAction == null)
            {
                continue;
            }

            entry.button.onClick.RemoveListener(entry.clickAction);
            entry.clickAction = null;
        }
    }

    private void ApplyTabState()
    {
        if (tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null)
            {
                continue;
            }

            bool isSelected = hasActiveTab && entry.tab == activeTab;

            if (entry.content != null)
            {
                entry.content.SetActive(isSelected);
            }

            if (entry.button != null)
            {
                entry.button.interactable = !disableSelectedButton || !isSelected;
            }

            Graphic graphic = entry.tabGraphic != null
                ? entry.tabGraphic
                : entry.button != null ? entry.button.targetGraphic : null;

            if (graphic != null)
            {
                graphic.color = isSelected ? selectedColor : unselectedColor;
            }
        }
    }
}
