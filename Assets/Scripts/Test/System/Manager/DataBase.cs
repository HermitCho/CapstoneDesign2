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
        [Header("플레이어 할당 관련 설정")]
        [Tooltip("플레이어 태그")]
        [SerializeField] private string playerTag = "PlayerPosition";
        public string PlayerTag
        {
            set { playerTag = value; }
            get { return playerTag; }
        }
        [Space(10)]
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
        [Tooltip("기본 마우스 수직 감도 설정")]
        [Range(0.1f, 30f)]
        [SerializeField] private float mouseSensitivityY = 1f;
        public float MouseSensitivityY
        {
            set { mouseSensitivityY = value; }
            get { return mouseSensitivityY; }
        }
        [Space(10)]
        [Tooltip("줌할 시 마우스 수직 감도 설정")]
        [Range(0.1f, 10f)]
        [SerializeField] private float zoomMouseSensitivityY = 0.3f;
        public float ZoomMouseSensitivityY
        {
            set { zoomMouseSensitivityY = value; }
            get { return zoomMouseSensitivityY; }
        }

        [Space(10)]
        [Header("카메라 회전 관련 설정")]
        [Tooltip("상단 회전 제한 값 설정 - 작을 수록 위쪽을 더 볼 수 있음")]
        [Range(-100f, 100f)]
        [SerializeField] private float minVerticalAngle = 0.5f;
        public float MinVerticalAngle
        {
            set { minVerticalAngle = value; }
            get { return minVerticalAngle; }
        }
        [Space(10)]
        [Tooltip("하단 회전 제한 값 설정 - 높을 수록 아래쪽을 더 볼 수 있음")]
        [Range(-100f, 100f)]
        [SerializeField] private float maxVerticalAngle = 5f;
        public float MaxVerticalAngle
        {
            set { maxVerticalAngle = value; }
            get { return maxVerticalAngle; }
        }
        [Space(10)]
        [Tooltip("카메라 회전 부드러움 시간 설정 - 높을 수록 회전에 딜레이 증가")]
        [Range(0f, 2f)]
        [SerializeField] private float rotationSmoothTime = 0f;
        public float RotationSmoothTime
        {
            set { rotationSmoothTime = value; }
            get { return rotationSmoothTime; }
        }

        [Space(10)]
        [Header("줌 관련 설정")]
        [Tooltip("줌 배율 수치 설정 - 높을수록 줌 정도 증가")]
        [Range(0.1f, 5f)]
        [SerializeField] private float zoomValue = 2f;
        public float ZoomValue
        {
            set { zoomValue = value; }
            get { return zoomValue; }
        }
        [Space(10)]
        [Tooltip("줌 인/아웃 애니메이션 시간 설정 - 높을수록 줌 애니메이션 시간 증가")]
        [Range(0.1f, 2f)]
        [SerializeField] private float zoomDuration = 0.3f;
        public float ZoomDuration
        {
            set { zoomDuration = value; }
            get { return zoomDuration; }
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
        [Header("카메라 오프셋 관련 설정")]
        [Tooltip("카메라 거리 설정 - 높을수록 카메라가 플레이어 기준 뒤쪽에 위치")]
        [Range(2f, 15f)]
        [SerializeField] private float maxCameraDistance = 5f;
        public float MaxCameraDistance
        {
            set { maxCameraDistance = value; }
            get { return maxCameraDistance; }
        } 
        [Space(10)]
        [Tooltip("카메라 높이 설정")]
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
        [Header("캐릭터 이동 관련 설정")]
        [Tooltip("캐릭터 이동 속도 설정")]
        [Range(0, 10)]
        [SerializeField] private float speed = 5f;
        public float Speed
        {
            set { speed = value; }
            get { return speed; }
        }
       
        [Space(10)]
        [Header("캐릭터 감도 관련 설정")]
        [Tooltip("캐릭터 기본 수평 감도 설정")]
        [Range(0.1f, 20f)]
        [SerializeField] private float rotationSpeed = 6f;
        public float RotationSpeed
        {
            set { rotationSpeed = value; }
            get { return rotationSpeed; }
        }
        [Space(10)]
        [Tooltip("줌할 시 캐릭터 수평 감도 설정")]
        [Range(0.1f, 20f)]
        [SerializeField] private float zoomRotationSpeed = 2f;
        public float ZoomRotationSpeed
        {
            set { zoomRotationSpeed = value; }
            get { return zoomRotationSpeed; }
        }
        [Space(10)]
        [Tooltip("마우스 입력이 없을 때 회전 정지까지의 시간")]
        [Range(0f, 1f)]
        [SerializeField] private float mouseInputTimeout = 0f;
        public float MouseInputTimeout
        {
            set { mouseInputTimeout = value; }
            get { return mouseInputTimeout; }
        }

        [Space(10)]
        [Header("캐릭터 점프 관련 설정")]
        [Tooltip("캐릭터 점프 쿨다운 설정 - 높을수록 점프 딜레이 증가")]
        [Range(0f, 3f)]
        [SerializeField] private float jumpCooldown = 3f;
        public float JumpCooldown
        {
            set { jumpCooldown = value; }
            get { return jumpCooldown; }
        }

        [Space(10)]
        [Tooltip("IsGround 체크 거리 설정")]
        [Range(0f, 10f)]
        [SerializeField] private float groundCheckDistance = 1.1f;
        public float GroundCheckDistance
        {
            set { groundCheckDistance = value; }
            get { return groundCheckDistance; }
        }

        [Space(10)]
        [Tooltip("착지 시 마찰력 배율 설정")]
        [Range(0f, 1f)]
        [SerializeField] private float landingFriction = 0.3f;
        public float LandingFriction
        {
            set { landingFriction = value; }
            get { return landingFriction; }
        }
        [Space(10)]
        [Tooltip("공중 이동 제어력 설정- 높을수록 공중에서 더 잘 조작됨")]
        [Range(0f, 20f)]
        [SerializeField] private float airControlForce = 10f;
        public float AirControlForce
        {
            set { airControlForce = value; }
            get { return airControlForce; }
        } 
        [Space(10)]
        [Tooltip("최대 공중 속도 배율 설정- 높을 수록 지상 대비 공중 속도 증가")]
        [Range(1f, 3f)]
        [SerializeField] private float maxAirSpeedMultiplier = 1.5f;
        public float MaxAirSpeedMultiplier
        {
            set { maxAirSpeedMultiplier = value; }
            get { return maxAirSpeedMultiplier; }
        }
        [Space(10)]
        [Tooltip("점프 높이 설정")]
        [Range(1f, 15f)]
        [SerializeField] private float jumpHeight = 3f;
        public float JumpHeight
        {
            set { jumpHeight = value; }
            get { return jumpHeight; }
        } 
        [Space(10)]
        [Tooltip("공중 가속도 설정 - 공중에서 방향 전환 속도")]
        [Range(5f, 50f)]
        [SerializeField] private float airAcceleration = 25f;
        public float AirAcceleration
        {
            set { airAcceleration = value; }
            get { return airAcceleration; }
        }
        [Space(10)]
        [Tooltip("공중 최대 속도 - 공중에서 도달할 수 있는 최대 속도 제한 값값")]
        [Range(5f, 20f)]
        [SerializeField] private float airMaxSpeed = 8f;
        public float AirMaxSpeed
        {
            set { airMaxSpeed = value; }
            get { return airMaxSpeed; }
        }
        [Space(10)]
        [Tooltip("점프 버퍼 시간 - 착지 직전 점프 입력을 허용하는 시간")]
        [Range(0f, 0.3f)]
        [SerializeField] private float jumpBufferTime = 0.1f;
        public float JumpBufferTime
        {
            set { jumpBufferTime = value; }
            get { return jumpBufferTime; }
        }
        [Space(10)]
        [Tooltip("벽 충돌 낙하 힘 설정정- 벽에 부딪혔을 때 아래로 가하는 힘")]
        [Range(3f, 15f)]
        [SerializeField] private float wallCollisionFallForce = 8f;
        public float WallCollisionFallForce
        {
            set { wallCollisionFallForce = value; }
            get { return wallCollisionFallForce; }
        }
        [Space(10)]
        [Tooltip("최소 수평 속도 유지 비율 설정 - 비스듬한 충돌 시 유지되는 수평 속도 비율")]
        [Range(0.1f, 1f)]
        [SerializeField] private float minHorizontalSpeedRatio = 0.7f;
        public float MinHorizontalSpeedRatio
        {
            set { minHorizontalSpeedRatio = value; }
            get { return minHorizontalSpeedRatio; }
        }
        [Space(10)]
        [Tooltip("최대 수평 속도 감소 비율 설정 - 수직 충돌 시 유지되는 수평 속도 비율")]
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
        [Header("테디베어 부착 관련 설정")]
        [Tooltip("플레이어 앞에 부착될 위치 오프셋")]
        [SerializeField] private Vector3 attachOffset = new Vector3(0f, 1f, 1f);
        public Vector3 AttachOffset
        {
            set { attachOffset = value; }
            get { return attachOffset; }
        }
        [Space(10)]
        [Tooltip("부착 시 회전값")]
        [SerializeField] private Vector3 attachRotation = new Vector3(0f, 0f, 0f);
        public Vector3 AttachRotation
        {
            set { attachRotation = value; }
            get { return attachRotation; }
        }  
        [Space(10)]
        [Tooltip("테디베어 부착 해제 시 재부착 방지 시간 설정")]
        [Range(0f, 30f)]
        [SerializeField] private float detachReattachTime = 5f;
        public float DetachReattachTime
        {
            set { detachReattachTime = value; }
            get { return detachReattachTime; }
        }

        [Space(10)]
        [Header("테디베어 점수 관련 설정")]
        [Tooltip("테디베어 점수 저장 변수")]
        [Range(0f, 100000f)]
        [SerializeField] private float teddyBearScore = 0f;
        public float TeddyBearScore
        {
            set { teddyBearScore = value; }
            get { return teddyBearScore; }
        }
        [Space(10)]
        [Tooltip("시간에 따른 초기 점수 값 설정 - 높을수록 초기 점수 증가")]
        [Range(0f, 100f)]
        [SerializeField] private float initialScore = 1f;
        public float InitialScore
        {
            set { initialScore = value; }
            get { return initialScore; }
        }
        [Space(10)]
        [Tooltip("점수 증가 비율 값 (기본 : 1배  ~  최대 10배)")]
        [Range(1f, 10f)]
        [SerializeField] private float scoreIncreaseRate = 1f;
        public float ScoreIncreaseRate
        {
            set { scoreIncreaseRate = value; }
            get { return scoreIncreaseRate; }
        }
        [Space(10)]
        [Tooltip("(점수 증가 비율이 적용되는 시간 값) 기본 : 1초  ~  최대 3600초")]
        [Range(1f, 3600f)]
        [SerializeField] private float scoreIncreaseTime = 1f;
        public float ScoreIncreaseTime
        {
            set { scoreIncreaseTime = value; }
            get { return scoreIncreaseTime; }
        }
        [Space(10)]
        [Tooltip("점수가 증가되는 시간(초) 틱 설정 - 설정 시간 초 마다 점수 증가")]
        [Range(0f, 300f)]
        [SerializeField] private float scoreGetTick = 1f;
        public float ScoreGetTick
        {
            set { scoreGetTick = value; }
            get { return scoreGetTick; }
        }

        [Space(10)]
        [Header("테디베어 발광 관련 설정")]
        [Tooltip("테디베어 발광 강도 비율 설정 - 높을수록 발광 강도 증가")]
        [Range(0f, 1f)]
        [SerializeField] private float glowingIntensity = 0.5f;
        public float GlowingIntensity
        {
            set { glowingIntensity = value; }
            get { return glowingIntensity; }
        }
        [Space(10)]
        [Tooltip("테디베어 발광 색상 설정 - 발광 시 테디베어 색상 변경")]
        [SerializeField] private Color glowingColor = Color.white;
        public Color GlowingColor
        {
            set { glowingColor = value; }
            get { return glowingColor; }
        }
        [Space(10)]
        [Tooltip("테디베어 발광 외곽선 두께 설정")]
        [SerializeField] private float glowingOutlineWidth = 3f;
        public float GlowingOutlineWidth
        {
            set { glowingOutlineWidth = value; }
            get { return glowingOutlineWidth; }
        }
        [Space(10)]
        [Tooltip("테디베어 발광 깜박임 주기 설정 - 높을수록 깜박임 주기 증가")]
        [Range(0f, 10f)]
        [SerializeField] private float glowingColorChangeTime = 0.5f;
        public float GlowingColorChangeTime
        {
            set { glowingColorChangeTime = value; }
            get { return glowingColorChangeTime; }
        }

        [Space(10)]
        [Header("테디베어 소지 시 상태 설정")]
        [Tooltip("소지 시 아이템 사용 가능 여부")]
        [SerializeField] private bool canUseItem = true;
        public bool CanUseItem
        {
            set { canUseItem = value; }
            get { return canUseItem; }
        }
        [Space(10)]
        [Tooltip("소지 시 스킬 사용 가능 여부")]
        [SerializeField] private bool canUseSkill = true;
        public bool CanUseSkill
        {
            set { canUseSkill = value; }
            get { return canUseSkill; }
        }
        [Space(10)]
        [Tooltip("소지 시 총기 사용 가능 여부")]
        [SerializeField] private bool canUseGun = false;
        public bool CanUseGun
        {
            set { canUseGun = value; }
            get { return canUseGun; }
        }
    }   



    [System.Serializable]
    public class GameData
    {
        [Header("전체 게임 시간 설정")]
        [SerializeField] private float playTime = 360f;
        public float PlayTime
        {
            set { playTime = value; }
            get { return playTime; }
        }
    }

    [System.Serializable]
    public class UIData
    {
        [Header("HUD Panel 설정")]
        [Space(5)]
        [Header("조준점 설정")]
        [Tooltip("기본 상태 조준점 색상 설정")]
        [SerializeField] private Color crosshairNormalColor = Color.white;
        public Color CrosshairNormalColor
        {
            set { crosshairNormalColor = value; }
            get { return crosshairNormalColor; }
        }
        [Space(10)]
        [Tooltip("타겟 상태 조준점 색상 설정")]
        [SerializeField] private Color crosshairTargetColor = Color.red;
        public Color CrosshairTargetColor
        {
            set { crosshairTargetColor = value; }
            get { return crosshairTargetColor; }
        }
        [Space(10)]
        [Tooltip("조준점 이미지 크기 설정")]
        [Range(0.1f, 10f)]
        [SerializeField] private float crosshairSize = 1f;
        public float CrosshairSize
        {
            set { crosshairSize = value; }
            get { return crosshairSize; }
        }
        [Space(10)]
        [Header("체력바 설정")]
        [Tooltip("건강 양호 상태 체력바 색상 설정")]
        [SerializeField] private Color healthyColor = Color.green;
        public Color HealthyColor
        {
            set { healthyColor = value; }
            get { return healthyColor; }
        }   
        [Space(10)]
        [Tooltip("건강 경고 상태 체력바 색상 설정")]
        [SerializeField] private Color warningColor = Color.yellow;
        public Color WarningColor
        {
            set { warningColor = value; }
            get { return warningColor; }
        }   
        [Space(10)]
        [Tooltip("건강 위험 상태 체력바 색상 설정")]
        [SerializeField] private Color dangerColor = Color.red;
        public Color DangerColor
        {
            set { dangerColor = value; }
            get { return dangerColor; }
        }   
        [Space(10)]
        [Tooltip("건강 경고 상태 체력바 임계값 설정")]
        [Range(0f, 1f)]
        [SerializeField] private float waringThreshold = 0.5f;
        public float WaringThreshold
        {
            set { waringThreshold = value; }
            get { return waringThreshold; }
        }
        [Space(10)]
        [Tooltip("건강 위험 상태 체력바 임계값 설정")]
        [Range(0f, 1f)]
        [SerializeField] private float dangerThreshold = 0.2f;
        public float DangerThreshold
        {
            set { dangerThreshold = value; }
            get { return dangerThreshold; }
        }

        [Space(10)]
        [Header("점수 및 시간 설정")]
        [Tooltip("현재 점수 텍스트")]
        [SerializeField] private string scoreText = "점수: {0:F0}";
        public string ScoreText

        {
            set { scoreText = value; }
            get { return scoreText; }
        }
        [Space(10)]
        [Tooltip("점수 텍스트 색상 설정")]
        [SerializeField] private Color scoreFormatColor = Color.black;
        public Color ScoreFormatColor
        {
            set { scoreFormatColor = value; }
            get { return scoreFormatColor; }
        }
        [Space(10)]
        [Tooltip("기본 점수 배율 텍스트 설정")]
        [SerializeField] private string generalMultiplierText = "점수 배율 {0:F0}x";
        public string GeneralMultiplierText
        {
            set { generalMultiplierText = value; }
            get { return generalMultiplierText; }
        }
        [Space(10)]
        [Tooltip("기본 점수 배율 텍스트 색상 설정")]
        [SerializeField] private Color generalMultiplierFormatColor = Color.black;
        public Color GeneralMultiplierFormatColor
        {
            set { generalMultiplierFormatColor = value; }
            get { return generalMultiplierFormatColor; }
        }
        [Space(10)]
        [Tooltip("점수 배율 텍스트 설정")]
        [SerializeField] private string multiplierText = "피버타임! {0:F0}x";
        public string MultiplierText
        {
            set { multiplierText = value; }
            get { return multiplierText; }
        }
        [Space(10)]
        [Tooltip("점수 배율 텍스트 색상 설정")]
        [SerializeField] private Color multiplierFormatColor = Color.black;
        public Color MultiplierFormatColor
        {
            set { multiplierFormatColor = value; }
            get { return multiplierFormatColor; }
        }
        [Space(10)] 
        [Tooltip("게임 시간 텍스트 설정")]
        [SerializeField] private string gameTimeText = "시간: {0:F0}초";
        public string GameTimeText
        {
            set { gameTimeText = value; }
            get { return gameTimeText; }
        }   
        [Space(10)]
        [Tooltip("게임 시간 텍스트 색상 설정")]
        [SerializeField] private Color gameTimeFormatColor = Color.black;
        public Color GameTimeFormatColor
        {
            set { gameTimeFormatColor = value; }
            get { return gameTimeFormatColor; }
        }
        [Space(10)]
        [Tooltip("체력 상태 텍스트 설정")]
        [SerializeField] private string healthText = "{0:F0} / {1:F0}";
        public string HealthText
        {
            set { healthText = value; }
            get { return healthText; }
        }
        [Space(10)]
        [Tooltip("체력 상태 텍스트 색상 설정")]
        [SerializeField] private Color healthFormatColor = Color.black;
        public Color HealthFormatColor
        {
            set { healthFormatColor = value; }
            get { return healthFormatColor; }
        }

        [Space(20)]
        [Header("SelectCharacter Panel 설정")]
        [Tooltip("최대 캐릭터 슬롯 수")]
        [SerializeField] private int maxCharacterSlots = 4;
        public int MaxCharacterSlots
        {
            set { maxCharacterSlots = value; }
            get { return maxCharacterSlots; }
        }
        [Space(10)]
        [Tooltip("현재 선택된 캐릭터 인덱스 - 초기 설정 용도")]
        [SerializeField] private int currentSelectedIndex = 0;
        public int CurrentSelectedIndex
        {
            set { currentSelectedIndex = value; }
            get { return currentSelectedIndex; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 텍스트 설정")]
        [SerializeField] private string selectionTimeText = "남은 시간: {0:F0}초";
        public string SelectionTimeText
        {
            set { selectionTimeText = value; }
            get { return selectionTimeText; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 설정 - 초기 상태태")]
        [SerializeField] private float selectionTime = 10f;
        public float SelectionTime
        {
            set { selectionTime = value; }
            get { return selectionTime; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 텍스트 색상 설정 - 정상 상태")]
        [SerializeField] private Color selectionTimeNormalFormatColor = Color.black;
        public Color SelectionTimeNormalFormatColor
        {
            set { selectionTimeNormalFormatColor = value; }
            get { return selectionTimeNormalFormatColor; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 설정 - 경고 상태")]
        [SerializeField] private float selectionWarningTime = 5f;
        public float SelectionWarningTime
        {
            set { selectionWarningTime = value; }
            get { return selectionWarningTime; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 텍스트 색상 설정 - 경고 상태")]
        [SerializeField] private Color selectionTimeWarningFormatColor = Color.yellow;
        public Color SelectionTimeWarningFormatColor
        {
            set { selectionTimeWarningFormatColor = value; }
            get { return selectionTimeWarningFormatColor; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 설정 - 위험 상태")]
        [SerializeField] private float selectionDangerTime = 3f;
        public float SelectionDangerTime
        {
            set { selectionDangerTime = value; }
            get { return selectionDangerTime; }
        }
        [Space(10)]
        [Tooltip("캐릭터 선택 시간 텍스트 색상 설정 - 위험 상태")]
        [SerializeField] private Color selectionTimeDangerFormatColor = Color.red;
        public Color SelectionTimeDangerFormatColor
        {
            set { selectionTimeDangerFormatColor = value; }
            get { return selectionTimeDangerFormatColor; }
        }

       
    }


    [System.Serializable]
    public class PlayerData
    {

        [Space(20)]
        [Header("플레이어 데이터")] 
        [Tooltip("플레이어 프리팹 데이터 설정")]
        [SerializeField] private List<Transform> playerPrefabData = new List<Transform>();
        public List<Transform> PlayerPrefabData
        {
            set { playerPrefabData = value; }
            get { return playerPrefabData; }
        }        
    }


    [System.Serializable]
    public class ItemData
    {
        [Header("아이템 데이터")]
        [Tooltip("아이템 프리팹 데이터 설정")]
        [SerializeField] private List<Transform> itemPrefabData = new List<Transform>();
        public List<Transform> ItemPrefabData
        {
            set { itemPrefabData = value; }
            get { return itemPrefabData; }
        }
    }

    

    [Header("카메라 데이터")]
    public CameraData cameraData;

    [Header("플레이어 움직임 데이터")]
    public PlayerMoveData playerMoveData;

    [Header("테디베어 데이터")]
    public TeddyBearData teddyBearData;

    [Header("게임 데이터")]
    public GameData gameData;

    [Header("UI 데이터")]
    public UIData uiData;

    [Header("플레이어 데이터")]
    public PlayerData playerData;

    [Header("아이템 데이터")]
    public ItemData itemData;
}
