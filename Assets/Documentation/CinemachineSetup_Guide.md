# Cinemachine 카메라 설정 가이드

## 개요
이 가이드는 LoadingScene에서 테디베어 드롭 시네마틱을 위한 Cinemachine 카메라 설정 방법을 설명합니다.

## 필요한 컴포넌트

### 1. Package Manager 설치
1. Window → Package Manager
2. Unity Registry에서 "Cinemachine" 검색
3. Install 클릭

### 2. 기본 카메라 설정

#### 2.1 메인 카메라 (MainCamera)
```
GameObject: MainCamera
- CinemachineVirtualCamera 컴포넌트 추가
- Priority: 10
- Follow: RunnerCharacter
- LookAt: RunnerCharacter
- Lens FOV: 60
- Body: Tracked Dolly
  - Path: DollyPath (생성 필요)
  - Path Position: 0
  - Auto Dolly Speed: 0
- Aim: Composer
- Noise: Basic Multi-Channel Perlin
  - Amplitude Gain: 0.5
  - Frequency Gain: 2.0
```

#### 2.2 러너 클로즈업 카메라 (RunnerCloseUpCamera)
```
GameObject: RunnerCloseUpCamera
- CinemachineVirtualCamera 컴포넌트 추가
- Priority: 5 (기본값, 스크립트에서 동적 조정)
- Follow: RunnerCharacter
- LookAt: RunnerCharacter
- Lens FOV: 35
- Body: Transposer
  - Follow Offset: (0, 1.5, -2)
  - Binding Mode: Lock To Target No Roll
  - Damping: (1, 1, 1)
- Aim: Composer
  - Tracked Object Offset: (0, 1.5, 0)
  - Lookahead Time: 0.2
  - Lookahead Smoothing: 10
  - Dead Zone Width: 0.1
  - Dead Zone Height: 0.1
```

#### 2.3 테디베어 추적 카메라 (TeddyBearTrackingCamera)
```
GameObject: TeddyBearTrackingCamera
- CinemachineVirtualCamera 컴포넌트 추가
- Priority: 5 (기본값, 스크립트에서 동적 조정)
- Follow: null (스크립트에서 동적 할당)
- LookAt: null (스크립트에서 동적 할당)
- Lens FOV: 45
- Body: Framing Transposer
  - Lookahead Time: 0.5
  - Lookahead Smoothing: 20
  - X Damping: 0.5
  - Y Damping: 0.5
  - Z Damping: 0.5
  - Screen X: 0.5
  - Screen Y: 0.4
  - Camera Distance: 5
  - Dead Zone Width: 0.2
  - Dead Zone Height: 0.2
- Aim: Composer
  - Tracked Object Offset: (0, 0, 0)
  - Lookahead Time: 0.3
  - Lookahead Smoothing: 15
- Noise: Basic Multi-Channel Perlin
  - Amplitude Gain: 0.2
  - Frequency Gain: 1.0
```

#### 2.4 체이서 점프 카메라 (ChaserJumpCamera)
```
GameObject: ChaserJumpCamera
- CinemachineVirtualCamera 컴포넌트 추가
- Priority: 5 (기본값, 스크립트에서 동적 조정)
- Follow: null (스크립트에서 동적 할당)
- LookAt: null (스크립트에서 동적 할당)
- Lens FOV: 50
- Body: Transposer
  - Follow Offset: (0, 2, -3)
  - Binding Mode: Lock To Target No Roll
  - Damping: (0.5, 0.5, 0.5)
- Aim: Composer
  - Tracked Object Offset: (0, 1, 0)
  - Lookahead Time: 0.3
  - Lookahead Smoothing: 15
  - Dead Zone Width: 0.15
  - Dead Zone Height: 0.15
- Noise: Basic Multi-Channel Perlin
  - Amplitude Gain: 0.3
  - Frequency Gain: 1.5
```

### 3. Dolly Track 설정

#### 3.1 Dolly Path 생성
1. Hierarchy에서 우클릭 → Cinemachine → Create Dolly Track with Cart
2. 생성된 DollyTrack을 적절한 위치에 배치
3. DollyPath waypoints 설정:
   - Waypoint 0: 시작 지점 (러너 뒤쪽)
   - Waypoint 1: 중간 지점 (추격 중)
   - Waypoint 2: 넘어지는 지점 (클로즈업)
   - Waypoint 3: 테디베어 드롭 지점
   - Waypoint 4: 체이서 점프 지점

#### 3.2 Waypoint 위치 예시
```
Waypoint 0: (-5, 2, -10)
Waypoint 1: (0, 2, -5)
Waypoint 2: (5, 1.5, 0)
Waypoint 3: (8, 1.5, 3)
Waypoint 4: (10, 2, 5)
```

### 4. LoadingCinematicController 설정

#### 4.1 Inspector 설정
```
Characters:
- Runner Character: RunnerCharacter GameObject
- Chaser Characters: [Chaser1, Chaser2, Chaser3]
- Runner Animator: RunnerCharacter Animator
- Chaser Animators: [Chaser1.Animator, Chaser2.Animator, Chaser3.Animator]
- Runner Controller: RunnerCharacter LoadingRunnerController

Cinemachine Setup:
- Main Camera: MainCamera
- Runner Close Up Camera: RunnerCloseUpCamera
- Teddy Bear Tracking Camera: TeddyBearTrackingCamera
- Chaser Jump Camera: ChaserJumpCamera
- Tracked Dolly: MainCamera CinemachineTrackedDolly
- Dolly Path: DollyPath
- Noise Component: MainCamera BasicMultiChannelPerlin

Camera Settings:
- Runner Close Up FOV: 35
- Teddy Bear Tracking FOV: 45
- Chaser Jump FOV: 50
- Camera Transition Time: 0.5

TeddyBear Cinematic:
- Teddy Bear Focus Delay: 0.7
- Teddy Bear Tracking Duration: 2.0
- Chaser Jump Focus Delay: 1.0
```

### 5. 추가 설정 팁

#### 5.1 카메라 우선순위 시스템
- Priority 값이 높을수록 우선순위가 높음
- 스크립트에서 동적으로 조정하여 카메라 전환 구현
- 전환 시 자연스러운 블렌딩 효과 발생

#### 5.2 성능 최적화
- 사용하지 않는 카메라는 Priority를 낮게 설정
- 너무 많은 카메라가 동시에 높은 우선순위를 가지지 않도록 주의
- Update Mode를 Smart Update로 설정

#### 5.3 디버깅
- Game 뷰에서 카메라 전환 확인
- Scene 뷰에서 각 카메라의 시야 확인
- Console에서 디버그 로그 확인

### 6. 테스트 순서
1. 메인 카메라 추적 동작 확인
2. 러너 클로즈업 카메라 전환 확인
3. 테디베어 추적 카메라 동작 확인
4. 체이서 점프 카메라 전환 확인
5. 전체 시네마틱 시퀀스 테스트

### 7. 문제 해결

#### 7.1 카메라가 전환되지 않는 경우
- Priority 값 확인
- 스크립트의 카메라 참조 확인
- Virtual Camera가 활성화되어 있는지 확인

#### 7.2 카메라 움직임이 부자연스러운 경우
- Damping 값 조정
- Dead Zone 설정 확인
- Lookahead 설정 조정

#### 7.3 테디베어 추적이 안 되는 경우
- TeddyBear GameObject가 활성화되어 있는지 확인
- Follow/LookAt 타겟이 올바르게 설정되었는지 확인
- 테디베어에 Collider가 있는지 확인

## 결론
이 설정을 통해 극적인 테디베어 드롭 시네마틱 시퀀스를 구현할 수 있습니다. 각 카메라의 특성을 살려 몰입도 높은 연출을 만들어보세요. 