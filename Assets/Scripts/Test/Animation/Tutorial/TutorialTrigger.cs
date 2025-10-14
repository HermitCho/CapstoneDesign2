using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public int messageIndex; // 보여줄 메시지 인덱스

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TutorialMessageManager.Instance.ShowMessage(messageIndex);
            gameObject.SetActive(false); // 중복 실행 방지
        }
    }
}