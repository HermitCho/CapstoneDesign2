using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이미지 우선순위 설정 컨트롤러
/// </summary>
public class ImageController : MonoBehaviour
{
    protected void ChangeSortingImage( GameObject sortingImage , GameObject sortingImage2)
    {
        int index_1 = GetSortingIndex(sortingImage);
        int index_2 = GetSortingIndex(sortingImage2);

        SetSortingIndex(sortingImage, index_2);
        SetSortingIndex(sortingImage2, index_1);
    }

    protected void SetSortingIndex( GameObject sortingImage , int index)
    {
        sortingImage.transform.SetSiblingIndex(index);

    }

    protected int GetSortingIndex( GameObject sortingImage)
    {
        int index = sortingImage.transform.GetSiblingIndex();
        Debug.Log("index : " + index);
        return index;
    }

}
