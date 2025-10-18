using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CircleEffect : MonoBehaviour
{
    public Image circleImage;
    public float duration = 1.5f; // 한 번 커지는 데 걸리는 시간
    public float maxScale = 3f;   // 최대 크기 배율

    private void Start()
    {
        StartAnimation();
    }

    void StartAnimation()
    {
        circleImage.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(circleImage.transform.DOScale(maxScale, duration)
            .SetEase(Ease.OutCubic));
        seq.Join(circleImage.DOFade(0, duration));
        seq.OnComplete(() =>
        {
            circleImage.color = new Color(1, 1, 1, 1); 
            circleImage.transform.localScale = Vector3.zero;
            StartAnimation();
        });
    }
}