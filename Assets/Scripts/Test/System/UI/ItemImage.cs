using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemImage : ImageController
{


    [Header("아이템 이미지 오브젝트")]
    [SerializeField] private GameObject itemImage;
    [SerializeField] private GameObject itemImage2;

    void Awake()
    {
        InitItemImage();
    }

    public void InitItemImage()
    {
        SetSortingIndex(itemImage2, 0);
        SetSortingIndex(itemImage, 1);
    }


}
