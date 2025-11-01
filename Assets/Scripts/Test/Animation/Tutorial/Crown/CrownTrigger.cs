using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrownTrigger : MonoBehaviour
{
    [Header("왕관 오브젝트 참조")]
    [Tooltip("처음엔 비활성화되어 있다가 트리거 발동 시 보이게 할 오브젝트")]
    public GameObject crownObject;

    private bool hasSpawned = false;

    private void Start()
    {
        if (crownObject != null)
            crownObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 태그 상관없이, 어떤 오브젝트가 들어와도 왕관 생성
        if (!hasSpawned)
        {
            hasSpawned = true;

            if (crownObject != null)
                crownObject.SetActive(true);
        }
    }
}