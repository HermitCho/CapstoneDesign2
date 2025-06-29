using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데이터베이스
/// 카메라, 플레이어, 테디베어 데이터 설정 가능
/// 해당 스크립트에 존재하는 변수 값을을 직접 수정 하지 말것
/// </summary>


public class DataBase : Singleton<DataBase>
{
    [System.Serializable]
    public class CameraData
    {
        [Header("플레이어 설정")]
        [Tooltip("플레이어 태그")]
        [SerializeField] private string playerTag = "PlayerPosition";
        public string PlayerTag
        {
            set { playerTag = value; }
            get { return playerTag; }
        }

        [Space(10)]
        [Header("플레이어를를 찾는 간격 (초) 설정")]
        [Tooltip("Tag에 맞게 플레이어를 찾는 간격")]
        [Range(0.1f, 20f)]
        [SerializeField] private float findPlayerInterval = 0.5f;
        public float FindPlayerInterval
        {
            set { findPlayerInterval = value; }
            get { return findPlayerInterval; }
        }

        [Space(10)]
        [Header("마우스 수직 감도 설정")]
        [Tooltip("Y축 감도 조절 - 높을수록 더 민감")]
        [Range(0.1f, 30f)]
        [SerializeField] private float mouseSensitivityY = 1f;
        public float MouseSensitivityY
        {
            set { mouseSensitivityY = value; }
            get { return mouseSensitivityY; }
        }

        [Space(10)]
        [Header("줌할 시 마우스 수직 감도 설정")]
        [Tooltip("줌 상태 Y축 감도 조절 - 높을수록 더 민감")]
        [Range(0.1f, 10f)]
        [SerializeField] private float zoomMouseSensitivityY = 0.3f;
        public float ZoomMouseSensitivityY
        {
            set { zoomMouseSensitivityY = value; }
            get { return zoomMouseSensitivityY; }
        }

        [Space(10)]
        [Header("상단 회전 제한 값 설정")]
        [Tooltip("높을수록 카메라를 위쪽으로 올릴 수 있음 ")]
        [Range(-100f, 0f)]
        [SerializeField] private float minVerticalAngle = 0.5f;
        public float MinVerticalAngle
        {
            set { minVerticalAngle = value; }
            get { return minVerticalAngle; }
        }

        [Space(10)]
        [Header("하단 회전 제한 값 설정")]
        [Tooltip("높을 수록 카메라를 아래쪽으로 내릴 수 있음")]
        [Range(-100f, 100f)]
        [SerializeField] private float maxVerticalAngle = 5f;
        public float MaxVerticalAngle
        {
            set { maxVerticalAngle = value; }
            get { return maxVerticalAngle; }
        }

        [Space(10)]
        [Header("줌 배율 수치 설정")]
        [Tooltip("줌 배율 - 높을수록 줌 정도 증가")]
        [Range(0.1f, 5f)]
        [SerializeField] private float zoomValue = 2f;
        public float ZoomValue
        {
            set { zoomValue = value; }
            get { return zoomValue; }
        }

        [Space(10)]
        [Header("줌 애니메이션 시간 설정")]
        [Tooltip("줌 인/아웃 애니메이션 시간 - 높을수록 줌 애니메이션 시간 증가")]
        [Range(0.1f, 2f)]
        [SerializeField] private float zoomDuration = 0.3f;
        public float ZoomDuration
        {
            set { zoomDuration = value; }
            get { return zoomDuration; }
        }

        [Space(10)]
        [Header("카메라 회전 부드러움 설정")]
        [Tooltip("카메라 회전 부드러움 시간 - 높을 수록 회전에 딜레이 증가")]
        [Range(0f, 2f)]
        [SerializeField] private float rotationSmoothTime = 0f;
        public float RotationSmoothTime
        {
            set { rotationSmoothTime = value; }
            get { return rotationSmoothTime; }
        }
        

        
        [Space(10)]
        [Header("벽 충돌 방지 시스템 설정")]
        [Tooltip("벽 충돌 방지 시스템 사용 여부")]
        [SerializeField] private bool useWallCollisionAvoidance = true;
        public bool UseWallCollisionAvoidance
        {
            set { useWallCollisionAvoidance = value; }
            get { return useWallCollisionAvoidance; }
        }
        

        
        [Space(10)]
        [Tooltip("벽 충돌 시 보정값 (플레이어 쪽으로 당기는 거리)")]
        [Range(0.5f, 3f)]
        [SerializeField] private float cameraFix = 1f;
        public float CameraFix
        {
            set { cameraFix = value; }
            get { return cameraFix; }
        }
        
        [Space(10)]
        [Tooltip("벽 충돌 방지 시 카메라 이동 속도")]
        [Range(1f, 10f)]
        [SerializeField] private float wallAvoidanceSpeed = 5f;
        public float WallAvoidanceSpeed
        {
            set { wallAvoidanceSpeed = value; }
            get { return wallAvoidanceSpeed; }
        }
        
        [Space(10)]
        [Header("카메라 거리 설정")]
        [Tooltip("최대 카메라 거리")]
        [Range(2f, 15f)]
        [SerializeField] private float maxCameraDistance = 5f;
        public float MaxCameraDistance
        {
            set { maxCameraDistance = value; }
            get { return maxCameraDistance; }
        }
        
        [Space(10)]
        [Header("카메라 위치 설정")]
        [Tooltip("플레이어 피벗 높이 오프셋 (카메라가 바라보는 지점)")]
        [Range(0f, 5f)]
        [SerializeField] private float pivotHeightOffset = 1.5f;
        public float PivotHeightOffset
        {
            set { pivotHeightOffset = value; }
            get { return pivotHeightOffset; }
        }
            
    }

    [System.Serializable]
    public class PlayerMoveData
    {
        [Tooltip("캐릭터 이동 속도 설정")]
        [Header("캐릭터 이동 속도 - 높을수록 더 빠름")]
        [Range(0, 10)]
        [SerializeField] private float speed = 5f;
        public float Speed
        {
            set { speed = value; }
            get { return speed; }
        }
  
        [Space(10)]
        [Tooltip("캐릭터 수평 감도 설정")]
        [Header("X축 감도 조절 - 높을수록 더 민감")]
        [Range(0.1f, 20f)]
        [SerializeField] private float rotationSpeed = 6f;
        public float RotationSpeed
        {
            set { rotationSpeed = value; }
            get { return rotationSpeed; }
        }

        [Space(10)]
        [Tooltip("줌할 시 캐릭터 수평 감도 설정")]
        [Header("줌 상태 X축 감도 조절 - 높을수록 더 민감")]
        [Range(0.1f, 20f)]
        [SerializeField] private float zoomRotationSpeed = 2f;
        public float ZoomRotationSpeed
        {
            set { zoomRotationSpeed = value; }
            get { return zoomRotationSpeed; }
        }

        [Space(10)]
        [Tooltip("마우스 입력 타임아웃 설정")]
        [Header("마우스 입력 타임아웃 - 마우스 입력이 없을 때 회전 정지까지의 시간")]
        [Range(0f, 1f)]
        [SerializeField] private float mouseInputTimeout = 0f;
        public float MouseInputTimeout
        {
            set { mouseInputTimeout = value; }
            get { return mouseInputTimeout; }
        }

        
        [Space(10)]
        [Tooltip("캐릭터 점프 쿨다운 설정")]
        [Header("캐릭터 점프 쿨다운 - 높을수록 점프 딜레이 증가")]
        [Range(0f, 3f)]
        [SerializeField] private float jumpCooldown = 3f;
        public float JumpCooldown
        {
            set { jumpCooldown = value; }
            get { return jumpCooldown; }
        }

        [Space(10)]
        [Tooltip("착지 시 마찰력 설정")]
        [Header("착지 시 마찰력 - 낮을수록 더 많이 미끄러짐 방지 (0~1)")]
        [Range(0f, 1f)]
        [SerializeField] private float landingFriction = 0.3f;
        public float LandingFriction
        {
            set { landingFriction = value; }
            get { return landingFriction; }
        }
        
        [Space(10)]
        [Tooltip("공중 이동 제어력 설정")]
        [Header("공중 이동 제어력 - 높을수록 공중에서 더 잘 조작됨")]
        [Range(0f, 20f)]
        [SerializeField] private float airControlForce = 10f;
        public float AirControlForce
        {
            set { airControlForce = value; }
            get { return airControlForce; }
        }
        
        [Space(10)]
        [Tooltip("최대 공중 속도 배율 설정")]
        [Header("최대 공중 속도 배율 - 지상 속도 대비 공중 최대 속도")]
        [Range(1f, 3f)]
        [SerializeField] private float maxAirSpeedMultiplier = 1.5f;
        public float MaxAirSpeedMultiplier
        {
            set { maxAirSpeedMultiplier = value; }
            get { return maxAirSpeedMultiplier; }
        }
        
        [Space(10)]
        [Tooltip("점프 높이")]
        [Header("점프 높이 - 높을수록 더 높게 점프")]
        [Range(1f, 15f)]
        [SerializeField] private float jumpHeight = 3f;
        public float JumpHeight
        {
            set { jumpHeight = value; }
            get { return jumpHeight; }
        }
        
        [Space(10)]
        [Tooltip("공중 가속도")]
        [Header("공중 가속도 - 공중에서 방향 전환 속도")]
        [Range(5f, 50f)]
        [SerializeField] private float airAcceleration = 25f;
        public float AirAcceleration
        {
            set { airAcceleration = value; }
            get { return airAcceleration; }
        }
        
        [Space(10)]
        [Tooltip("공중 최대 속도")]
        [Header("공중 최대 속도 - 공중에서 도달할 수 있는 최대 속도")]
        [Range(5f, 20f)]
        [SerializeField] private float airMaxSpeed = 8f;
        public float AirMaxSpeed
        {
            set { airMaxSpeed = value; }
            get { return airMaxSpeed; }
        }
        
        [Space(10)]
        [Tooltip("점프 버퍼 시간")]
        [Header("점프 버퍼 시간 - 착지 직전 점프 입력을 허용하는 시간")]
        [Range(0f, 0.3f)]
        [SerializeField] private float jumpBufferTime = 0.1f;
        public float JumpBufferTime
        {
            set { jumpBufferTime = value; }
            get { return jumpBufferTime; }
        }

        [Space(10)]
        [Tooltip("벽 충돌 시 떨어뜨리는 힘")]
        [Header("벽 충돌 낙하 힘 - 벽에 부딪혔을 때 아래로 가하는 힘")]
        [Range(3f, 15f)]
        [SerializeField] private float wallCollisionFallForce = 8f;
        public float WallCollisionFallForce
        {
            set { wallCollisionFallForce = value; }
            get { return wallCollisionFallForce; }
        }
        
        [Space(10)]
        [Tooltip("벽 충돌 시 최소 수평 속도 유지율")]
        [Header("최소 수평 속도 유지 - 비스듬한 충돌 시 유지되는 수평 속도 비율")]
        [Range(0.1f, 1f)]
        [SerializeField] private float minHorizontalSpeedRatio = 0.7f;
        public float MinHorizontalSpeedRatio
        {
            set { minHorizontalSpeedRatio = value; }
            get { return minHorizontalSpeedRatio; }
        }
        
        [Space(10)]
        [Tooltip("벽 충돌 시 최대 수평 속도 감소율")]
        [Header("최대 수평 속도 감소 - 수직 충돌 시 유지되는 수평 속도 비율")]
        [Range(0f, 1f)]
        [SerializeField] private float maxHorizontalSpeedReduction = 0.2f;
        public float MaxHorizontalSpeedReduction
        {
            set { maxHorizontalSpeedReduction = value; }
            get { return maxHorizontalSpeedReduction; }
        }


    }

    [System.Serializable]
    public class TeddyBearData
    {   
        [Header("테디베어 부착 위치")]
        [Tooltip("플레이어 앞에 부착될 위치 오프셋")]
        [SerializeField] private Vector3 attachOffset = new Vector3(0f, 1f, 1f);
        public Vector3 AttachOffset
        {
            set { attachOffset = value; }
            get { return attachOffset; }
        }

        [Space(10)]
        [Header("테디베어 부착 회전 값")]
        [Tooltip("부착 시 회전값")]
        [SerializeField] private Vector3 attachRotation = new Vector3(0f, 0f, 0f);
        public Vector3 AttachRotation
        {
            set { attachRotation = value; }
            get { return attachRotation; }
        }   
    }

    [Header("카메라 데이터")]
    public CameraData cameraData;

    [Header("플레이어 움직임 데이터")]
    public PlayerMoveData playerMoveData;

    [Header("테디베어 데이터")]
    public TeddyBearData teddyBearData;
}
