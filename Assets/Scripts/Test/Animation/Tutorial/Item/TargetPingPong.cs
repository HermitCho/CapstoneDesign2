using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetPingPong : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [SerializeField] private Axis moveAxis = Axis.X;
    [SerializeField] private float range = 4f;   // 총 이동 거리(좌↔우 합계)
    [SerializeField] private float speed = 2f;   // 속도
    [SerializeField] private bool useLocalSpace = false;

    private Vector3 startPos;

    void Start()
    {
        startPos = useLocalSpace ? transform.localPosition : transform.position;
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * speed, range) - (range * 0.5f); // -range/2 ~ +range/2
        Vector3 off = Vector3.zero;
        if (moveAxis == Axis.X) off.x = t;
        else if (moveAxis == Axis.Y) off.y = t;
        else off.z = t;

        if (useLocalSpace) transform.localPosition = startPos + off;
        else               transform.position     = startPos + off;
    }
}