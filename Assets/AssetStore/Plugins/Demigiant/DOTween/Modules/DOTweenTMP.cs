using System;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

namespace DG.Tweening
{
    public static class DOTweenTMP
    {
        public static Tweener DOText(this TextMeshProUGUI target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
        {
            if (target == null) return null;
            return DOTween.To(() => target.text, x => target.text = x, endValue, duration)
                .SetOptions(richTextEnabled, scrambleMode, scrambleChars)
                .SetTarget(target);
        }

        public static Tweener DOColor(this TextMeshProUGUI target, Color endValue, float duration)
        {
            if (target == null) return null;
            return DOTween.To(() => target.color, x => target.color = x, endValue, duration)
                .SetTarget(target);
        }

        public static Tweener DOFade(this TextMeshProUGUI target, float endValue, float duration)
        {
            if (target == null) return null;
            return DOTween.ToAlpha(() => target.color, x => target.color = x, endValue, duration)
                .SetTarget(target);
        }
    }
}