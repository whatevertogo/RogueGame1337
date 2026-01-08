using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageUIDisplay : MonoBehaviour, IMessageUIDisplay
{
    public string messageText { get; set; }

    private TextMeshProUGUI _textUI;      // 私有，不暴露给 外部


    public void Init(string text)
    {
        _textUI = GetComponent<TextMeshProUGUI>();
        messageText = text;
        UpdateTextDisplay();
    }

    public void UpdateTextDisplay()
    {
        if (_textUI != null)
        {
            _textUI.text = messageText;
        }
    }
}
