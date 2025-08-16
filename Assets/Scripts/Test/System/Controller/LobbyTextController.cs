using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LobbyTextController : MonoBehaviour
{
    [Header("텍스트 할당")]
    public TextMeshProUGUI Text;

    public void SetText(string text)
    {
        Text.text = text;
    }
}
