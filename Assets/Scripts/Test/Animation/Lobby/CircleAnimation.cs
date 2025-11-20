using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleAnimation : MonoBehaviour
{
    [Header("원 오브젝트 개수 (자식 수와 일치시킬 것)")]
    [SerializeField] private int circleCount = 10;

    [Header("크기 설정 (배열 길이는 자식 수와 동일해야 함)")]
    [SerializeField] private float[] startSize1;
    [SerializeField] private float[] startSize2;
    [SerializeField] private float[] endSize1;
    [SerializeField] private float[] endSize2;

    [Header("속도 설정")]
    //[SerializeField] private float growSpeed = 0.08f;  // 커지는 속도
    [SerializeField] private float fadeSpeed = 1.0f;  // 사라지는 속도
    [SerializeField] private float restartDelay = 0.2f; // 반복 전 대기시간

    [SerializeField] private float startDelayBetween = 1f;

    private List<Material> mats = new List<Material>();

    private float[] randomGrowSpeeds;

    void Start()
    {

        randomGrowSpeeds = new float[circleCount];

        // 자식 오브젝트의 머티리얼 가져오기
        for (int i = 0; i < circleCount; i++)
        {
            Renderer rend = transform.GetChild(i).GetComponent<Renderer>();
            if (rend != null)
                mats.Add(rend.material); // 인스턴스화

                randomGrowSpeeds[i] = Random.Range(0.06f, 0.08f);
        }

        StartCoroutine(StartSequentially());
    }

    IEnumerator StartSequentially()
    {
        for (int i = 0; i < mats.Count; i++)
        {
            // 🔸 index 순서대로 시작
            StartCoroutine(CircleRoutine(i));

            // 다음 원까지 딜레이
            yield return new WaitForSeconds(startDelayBetween);
        }
    }

    IEnumerator CircleRoutine(int index)
    {
        Material mat = mats[index];

        while (true)
        {
            // 1️⃣ startSize로 리셋
            float current1 = startSize1[index];
            float current2 = startSize2[index];
            mat.SetFloat("_Circle_size", current1);
            mat.SetFloat("_Circle_size2", current2);

            // 2️⃣ 페이드인 (투명 → 보이게)
            float alpha = 0f;
            while (alpha < 1f)
            {
                alpha += Time.deltaTime * 2.5f; // 빠른 페이드인
                mat.SetFloat("_Alpha", alpha);
                yield return null;
            }

            float thisGrowSpeed = randomGrowSpeeds[index];

            // 3️⃣ MoveTowards로 커지기
            while (!Mathf.Approximately(current1, endSize1[index]) ||
                   !Mathf.Approximately(current2, endSize2[index]))
            {
                current1 = Mathf.MoveTowards(current1, endSize1[index], Time.deltaTime * thisGrowSpeed);
                current2 = Mathf.MoveTowards(current2, endSize2[index], Time.deltaTime * thisGrowSpeed);

                mat.SetFloat("_Circle_size", current1);
                mat.SetFloat("_Circle_size2", current2);

                yield return null;
            }

            // 4️⃣ FadeOut
            alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime * fadeSpeed;
                mat.SetFloat("_Alpha", alpha);
                yield return null;
            }

            // 5️⃣ 반복 전 대기
            yield return new WaitForSeconds(restartDelay);
        }
    }
}