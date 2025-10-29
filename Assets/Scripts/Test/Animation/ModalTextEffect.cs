using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;


public class ModalTextEffect : MonoBehaviour

{
    [Header("Text Components")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Text Content")]
    [TextArea] public string titleContent = "타이틀 강조!";
    [TextArea] public string descriptionContent = "이것은 설명 텍스트입니다.";

    [Header("Effect Settings")]
    public float typingDuration = 2f;
    public float scaleDuration = 0.5f;
    public float scaleAmount = 1.2f;

    /// <summary>
    /// 모달창이 열릴 때 호출되는 메서드
    /// </summary>
    public void OnOpen()
    {
        // 타이틀 텍스트 초기화 및 타이핑 효과
        titleText.text = "";
        titleText.DOText(titleContent, typingDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            // 타이핑 완료 후 강조 애니메이션 (커졌다가 작아짐)
            titleText.transform.DOScale(scaleAmount, scaleDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        });

        // 설명 텍스트 초기화 및 타이핑 효과만 적용
        descriptionText.text = "";
        descriptionText.DOText(descriptionContent, typingDuration).SetEase(Ease.Linear);
    }
}
