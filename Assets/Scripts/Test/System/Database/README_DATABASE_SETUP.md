# Unity 구글 시트 서비스 계정 로그인 시스템 설정 가이드

## 📋 개요
이 시스템은 Unity에서 구글 시트 API를 서비스 계정으로 사용하여 로그인/회원가입 및 게임 통계 관리 기능을 제공합니다.

## 🛠️ 필요 구성요소

### 1. Google Cloud Console 설정

#### Step 1: 프로젝트 생성
1. [Google Cloud Console](https://console.cloud.google.com/)에 접속
2. 새 프로젝트 생성 또는 기존 프로젝트 선택

#### Step 2: Google Sheets API 활성화
1. **API 및 서비스 > 라이브러리**로 이동
2. "Google Sheets API" 검색 후 선택
3. **"사용 설정"** 클릭

#### Step 3: 서비스 계정 생성
1. **API 및 서비스 > 사용자 인증 정보**로 이동
2. **"+ 사용자 인증 정보 만들기" > "서비스 계정"** 선택
3. 서비스 계정 정보 입력:
   - **서비스 계정 이름**: `unity-sheets-service`
   - **서비스 계정 ID**: 자동 생성됨
   - **설명**: `Unity Google Sheets 연동용 서비스 계정`
4. **"만들기"** 클릭

#### Step 4: 서비스 계정 키 생성
1. 생성된 서비스 계정 클릭
2. **"키"** 탭으로 이동
3. **"키 추가" > "새 키 만들기"** 선택
4. **JSON** 형식 선택 후 **"만들기"** 클릭
5. **JSON 파일 다운로드** (안전한 곳에 보관)

#### Step 5: JSON 파일에서 정보 추출
다운로드한 JSON 파일을 열어서 다음 정보를 확인:
```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "여기가 Private Key ID",
  "private_key": "-----BEGIN PRIVATE KEY-----\n여기가 Private Key\n-----END PRIVATE KEY-----\n",
  "client_email": "여기가 Service Account Email",
  "client_id": "client-id",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token"
}
```

### 2. 구글 시트 준비

#### Step 1: 스프레드시트 생성
1. [Google Sheets](https://sheets.google.com/)에서 새 스프레드시트 생성
2. 첫 번째 행에 다음 헤더 추가:
   ```
   | ID | Password | Nickname | Win | Lose | Rate |
   ```

#### Step 2: 서비스 계정에 권한 부여 (중요!)
1. 스프레드시트에서 **"공유"** 버튼 클릭
2. **JSON 파일의 `client_email` 값을 복사**
3. 공유 대상에 **서비스 계정 이메일 추가**
4. 권한을 **"편집자"**로 설정
5. **"전송"** 클릭

#### Step 3: 스프레드시트 ID 확인
- URL에서 스프레드시트 ID 복사
- 예: `https://docs.google.com/spreadsheets/d/1hAEhskFqhVJhzuly7l1c6xTNfdz0m3filSbBDMC6nRk/edit`
- → `1hAEhskFqhVJhzuly7l1c6xTNfdz0m3filSbBDMC6nRk`

## 📊 구글 시트 구조

### 필수 컬럼 구조
| ID | Password | Nickname | Win | Lose | Rate |
|----|----------|----------|-----|------|------|
| 사용자 아이디 | 비밀번호 | 닉네임 | 승리 횟수 | 패배 횟수 | 레이팅 |

**컬럼 설명:**
- **ID**: 로그인용 사용자 아이디 (중복 불가)
- **Password**: 로그인용 비밀번호
- **Nickname**: 게임 내 닉네임 (5글자 이하)
- **Win**: 게임에서 1등 달성 횟수
- **Lose**: 게임에서 2,3,4등 달성 횟수  
- **Rate**: 레이팅 점수 (시작: 1000점, 최소: 0점)

### 레이팅 시스템
- **1등**: +14점
- **2등**: +6점
- **3등**: ±0점
- **4등**: -9점
- **최소값**: 0점 (음수 불가)

## 🎮 Unity 설정

### 1. GoogleSheetsManager 설정
Inspector에서 다음 정보를 입력:

```csharp
[Header("구글 시트 설정")]
[SerializeField] private string spreadsheetId = "1hAEhskFqhVJhzuly7l1c6xTNfdz0m3filSbBDMC6nRk"; // 실제 스프레드시트 ID
[SerializeField] private string sheetName = "Sheet1"; // 시트 이름

[Header("서비스 계정 인증")]
[SerializeField] private string serviceAccountEmail = "unity-sheets-service@your-project.iam.gserviceaccount.com"; // JSON의 client_email
[SerializeField] private string privateKeyId = "abc123def456..."; // JSON의 private_key_id
[SerializeField] private string privateKey = "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----\n"; // JSON의 private_key (개행 포함)
```

### 2. LoginButtonController 설정
Inspector에서 다음 요소들을 연결:

**회원가입 화면:**
- `createNicknameInputField`: 닉네임 입력 필드
- `createIdInputField`: 아이디 입력 필드
- `createpPasswordInputField`: 비밀번호 입력 필드
- `createPasswordConfirmInputField`: 비밀번호 확인 필드
- `createButton`: 회원가입 시도 버튼 (ButtonManager)
- `createCancelButton`: 취소 버튼 (ButtonManager)

**로그인 화면:**
- `loginIdInputField`: 아이디 입력 필드
- `loginPasswordInputField`: 비밀번호 입력 필드
- `loginButton`: 로그인 버튼 (ButtonManager)
- `signUpButton`: 회원가입 버튼 (ButtonManager)

**모달창:**
- `signUpSuccessModalWindowManager`: 회원가입 성공 모달
- `signUpFailModalWindowManager`: 회원가입 실패 모달
- `loginFailModalWindowManager`: 로그인 실패 모달

**패널 관리:**
- `panelManager`: 패널 매니저 (PanelManager)
- `loginPanelName`: "Login" (로그인 패널 이름)
- `signUpPanelName`: "SignUp" (회원가입 패널 이름)

## 🔧 사용법

### 기본 사용법
1. Unity에서 플레이 모드 실행
2. 회원가입 또는 로그인 시도
3. Console에서 연결 상태 확인

### 디버깅 및 진단
**GoogleSheetsManager 컴포넌트에서:**
1. **우클릭** → **"연결 진단"** 선택
2. Console에서 설정 상태 확인

## ⚠️ 주의사항

### 보안
- **개인 키는 절대 공개하지 마세요!**
- 프로덕션 환경에서는 개인 키를 암호화하여 저장
- JSON 파일을 버전 관리에 포함하지 마세요

### 제한사항
- Google Sheets API는 분당 100회 요청 제한
- 대용량 데이터에는 적합하지 않음
- 실시간 동기화에는 한계가 있음

## 🚨 문제 해결

### 일반적인 오류들

#### 1. "인증 실패" 오류
- 서비스 계정 이메일이 스프레드시트에 공유되었는지 확인
- JSON 파일의 정보가 올바르게 입력되었는지 확인
- 개인 키에 개행 문자(`\n`)가 포함되었는지 확인

#### 2. "데이터 로드 실패" 오류
- 스프레드시트 ID가 올바른지 확인
- 시트 이름이 정확한지 확인
- Google Sheets API가 활성화되었는지 확인

#### 3. "권한 오류" 오류
- 서비스 계정에 스프레드시트 편집 권한이 있는지 확인
- 스프레드시트가 삭제되지 않았는지 확인

### 디버깅 단계
1. **연결 진단** 실행
2. Console 로그 확인
3. 서비스 계정 설정 재확인
4. 스프레드시트 권한 재확인

## 📞 지원

문제가 지속되면 다음을 확인하세요:
1. Google Cloud Console 프로젝트 상태
2. 서비스 계정 활성 상태
3. 스프레드시트 공유 설정
4. Unity Console 오류 메시지

---

**✅ 설정이 완료되면 Unity에서 로그인/회원가입 시스템을 사용할 수 있습니다!**