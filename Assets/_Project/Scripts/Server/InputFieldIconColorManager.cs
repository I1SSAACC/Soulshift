using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldIconColorManager : MonoBehaviour
{
    [Serializable]
    public class InputIconElement
    {
        public TMP_InputField inputField;
        public Image icon;
        public Color emptyColor;
        public Color filledColor;
    }

    [SerializeField]
    private List<InputIconElement> elements = new List<InputIconElement>();

    private void Awake()
    {
        foreach (var element in elements)
        {
            element.inputField.onValueChanged.AddListener(text =>
                UpdateIconColor(element, text));

            UpdateIconColor(element, element.inputField.text);
        }
    }

    private void UpdateIconColor(InputIconElement element, string text)
    {
        if (element.icon == null)
            return;

        bool isEmpty = string.IsNullOrEmpty(text);
        element.icon.color = isEmpty
            ? element.emptyColor
            : element.filledColor;
    }

    private void OnDestroy()
    {
        foreach (var element in elements)
            element.inputField.onValueChanged.RemoveAllListeners();
    }
}