using UnityEngine;

/// <summary>
/// 상점 아이템 회전 애니메이션 컴포넌트
/// </summary>
public class ItemRotator : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 90f; // 초당 회전 각도
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // 회전 축 (Y축)
    
    [Header("애니메이션 설정")]
    [SerializeField] private bool useSineWave = true; // 사인파를 사용한 부드러운 회전
    [SerializeField] private float amplitude = 5f; // 사인파 진폭 (각도)
    [SerializeField] private float frequency = 1f; // 사인파 주파수
    
    private float timeOffset;
    private Vector3 initialRotation;
    
    void Start()
    {
        // 초기 회전값 저장
        initialRotation = transform.rotation.eulerAngles;
        
        // 시간 오프셋을 랜덤하게 설정하여 각 아이템이 다른 패턴으로 회전하도록 함
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        
    }
    
    void Update()
    {
        if (useSineWave)
        {
            // 사인파를 사용한 부드러운 회전
            float sineValue = Mathf.Sin((Time.time + timeOffset) * frequency) * amplitude;
            Vector3 currentRotation = initialRotation + rotationAxis * sineValue;
            transform.rotation = Quaternion.Euler(currentRotation);
        }
        else
        {
            // 기본 회전
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 회전 속도 설정
    /// </summary>
    /// <param name="speed">회전 속도</param>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    /// <summary>
    /// 회전 축 설정
    /// </summary>
    /// <param name="axis">회전 축</param>
    public void SetRotationAxis(Vector3 axis)
    {
        rotationAxis = axis;
    }
    
    /// <summary>
    /// 사인파 애니메이션 설정
    /// </summary>
    /// <param name="useSine">사인파 사용 여부</param>
    /// <param name="amplitude">진폭</param>
    /// <param name="frequency">주파수</param>
    public void SetSineWaveAnimation(bool useSine, float amplitude = 5f, float frequency = 1f)
    {
        useSineWave = useSine;
        this.amplitude = amplitude;
        this.frequency = frequency;
    }
}
