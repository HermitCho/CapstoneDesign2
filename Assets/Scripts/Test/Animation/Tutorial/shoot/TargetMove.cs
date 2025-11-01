using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMove : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveRange = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patternChangeTime = 2f;

    private Vector3 startPos;
    private Vector3 moveDir;
    private float nextPatternTime;
    private bool canMove = false;
    [HideInInspector] public Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        gameObject.SetActive(false); // 처음엔 숨김
    }

    void Start()
    {
        startPos = transform.position;
        SetRandomPattern();
    }

    void Update()
    {
        if (!canMove) return;

        float offset = Mathf.PingPong(Time.time * moveSpeed, moveRange) - moveRange / 2f;
        transform.position = startPos + moveDir * offset;

        if (Time.time >= nextPatternTime)
            SetRandomPattern();
    }

    public void EnableMovementAfter(float delay)
    {
        StartCoroutine(EnableMoveRoutine(delay));
    }

    private IEnumerator EnableMoveRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        canMove = true;
    }

    private void SetRandomPattern()
    {
        moveDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        nextPatternTime = Time.time + patternChangeTime;
    }
}