# Unity 데이터베이스 로그인 시스템 설정 가이드

## 📋 개요
이 시스템은 Unity에서 MySQL/MariaDB를 사용한 로그인/회원가입 기능을 제공합니다.

## 🛠️ 필요 구성요소

### 1. MariaDB 설치 (권장 버전: 10.5.10)
- [MariaDB 다운로드](https://dlm.mariadb.com/browse/mariadb_server/)
- Windows: `mariadb-10.5.10-winx64.msi` 설치
- 설치 시 주의사항:
  - Root 계정 비밀번호 설정
  - "Use UTF8 as default server's character set" 체크
  - 포트: 3306 (기본값)

### 2. MySQL Connector/NET 설치
- [MySQL Connector/NET 다운로드](https://downloads.mysql.com/archives/c-net/)
- 권장 버전: `mysql-connector-net-8.0.25.msi`
- 설치 경로: `C:\Program Files (x86)\MySQL\MySQL Connector Net 8.0.25\Assemblies\v4.5.2`

### 3. Unity 프로젝트 설정
1. `MySql.Data.dll` 파일을 `Assets/Plugins/` 폴더에 복사
2. 다음 스크립트들이 프로젝트에 포함되어 있는지 확인:
   - `DatabaseManager.cs`
   - `UserData.cs`
   - `LoginButtonController.cs`
   - `NickNameController.cs` (업데이트됨)

## 🗄️ 데이터베이스 구조

### 자동 생성되는 테이블: `users`
```sql
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(50) NOT NULL UNIQUE,
    nickname VARCHAR(20) NOT NULL,
    password VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

## ⚙️ 설정 방법

### 1. DatabaseManager 설정
`DatabaseManager.cs`의 연결 설정을 환경에 맞게 수정:

```csharp
[Header("데이터베이스 연결 설정")]
[SerializeField] private string server = "127.0.0.1";      // 서버 주소
[SerializeField] private string database = "gamedb";        // 데이터베이스 이름
[SerializeField] private string uid = "root";               // 사용자명
[SerializeField] private string password = "root";          // 비밀번호
[SerializeField] private int port = 3306;                   // 포트
```

### 2. LoginButtonController 설정
Inspector에서 다음 요소들을 연결:

**회원가입 화면 설정:**
- `createNicknameInputField`: 닉네임 입력 필드
- `createIdInputField`: 아이디 입력 필드
- `createpPasswordInputField`: 비밀번호 입력 필드
- `createPasswordConfirmInputField`: 비밀번호 확인 필드
- `createButton`: 회원가입 시도 버튼 (ButtonManager)
- `createCancelButton`: 취소 버튼 (ButtonManager)

**로그인 화면 설정:**
- `loginIdInputField`: 아이디 입력 필드
- `loginPasswordInputField`: 비밀번호 입력 필드
- `loginButton`: 로그인 버튼 (ButtonManager)
- `signUpButton`: 회원가입 버튼 (ButtonManager)

**모달창 설정:**
- `signUpSuccessModalWindowManager`: 회원가입 성공 모달
- `signUpFailModalWindowManager`: 회원가입 실패 모달
- `loginFailModalWindowManager`: 로그인 실패 모달

**패널 관리:**
- `panelManager`: PanelManager 컴포넌트
- `loginPanelName`: 로그인 패널 이름 (기본값: "Login")
- `signUpPanelName`: 회원가입 패널 이름 (기본값: "SignUp")

**로딩 UI:**
- `loadingIndicator`: 로딩 인디케이터 GameObject

### 3. Heat UI ButtonManager 설정
이 시스템은 Heat UI의 `ButtonManager` 컴포넌트를 사용합니다:

- **자동 이벤트 연결**: `Start()` 메서드에서 자동으로 버튼 이벤트가 연결됩니다
- **상태 관리**: `isInteractable` 속성으로 버튼 활성화/비활성화 제어
- **Inspector 설정**: 각 버튼은 `ButtonManager` 컴포넌트가 있는 GameObject를 할당해야 합니다

**주의사항:**
- Unity 기본 `Button` 컴포넌트가 아닌 Heat UI의 `ButtonManager`를 사용해야 합니다
- Inspector에서 버튼 이벤트를 수동으로 연결할 필요가 없습니다 (코드에서 자동 연결)

## 🎮 사용 방법

### 로그인 프로세스
1. 사용자가 아이디/비밀번호 입력
2. `OnClickLoginButton()` 호출
3. 데이터베이스에서 사용자 인증
4. 성공 시 `CurrentUser`에 사용자 정보 저장
5. Intro 씬으로 자동 전환

### 회원가입 프로세스
1. `OnClickSignUpButton()`으로 회원가입 패널 이동
2. 사용자 정보 입력 (아이디, 닉네임, 비밀번호, 비밀번호 확인)
3. `OnClickSignUpTryButton()` 호출
4. 입력 검증 (닉네임 5글자 이하, 아이디 중복 검사 등)
5. 성공 시 성공 모달 표시
6. `OnClickLoginCancelButton()`으로 로그인 패널 복귀

### 유효성 검사 규칙
- **아이디**: 중복 불가, 필수 입력
- **닉네임**: 5글자 이하, 필수 입력
- **비밀번호**: 4자 이상, 확인 비밀번호와 일치

## 🔧 디버깅

### DatabaseManager 디버그 메서드
```csharp
[ContextMenu("데이터베이스 연결 테스트")]
private void TestDatabaseConnection()

[ContextMenu("현재 사용자 정보 확인")]
private void CheckCurrentUser()
```

### 로그 메시지 확인
- ✅: 성공 메시지
- ❌: 오류 메시지
- ⚠️: 경고 메시지
- 🔐: 로그인 관련
- 📝: 회원가입 관련
- 🎬: 씬 전환 관련

## 🔒 보안 고려사항

### 현재 구현된 보안 기능
- 비밀번호 해시화 (Base64 + Salt)
- SQL Injection 방지 (Parameterized Query)
- 입력 검증

### 프로덕션 환경 권장사항
- BCrypt 등 더 강력한 해시 알고리즘 사용
- HTTPS 연결 사용
- 데이터베이스 연결 정보 암호화
- 비밀번호 복잡도 정책 강화

## 🚀 성능 최적화

### 현재 적용된 최적화
- 연결 풀링 (MySqlConnection using 문)
- 비동기 처리 (Coroutine)
- 중복 요청 방지 (isProcessing 플래그)
- 자동 테이블 생성

### 추가 최적화 방안
- 연결 풀 설정
- 캐싱 시스템 도입
- 배치 처리

## 📞 문제 해결

### 자주 발생하는 오류

1. **"Assembly will not be loaded due to errors"**
   - MySql.Data.dll 버전 호환성 문제
   - 제공된 dll 파일 사용 권장

2. **"The TCP Port you selected is already in use"**
   - 기존 MySQL 서비스와 충돌
   - 서비스에서 MySQL 중지 후 MariaDB 설치

3. **"데이터베이스 연결에 실패했습니다"**
   - 연결 정보 확인 (서버, 포트, 사용자명, 비밀번호)
   - MariaDB 서비스 실행 상태 확인

## 📝 변경 사항

### NickNameController 업데이트
- 데이터베이스 기반 닉네임 시스템과 호환
- 로그인된 사용자의 경우 DB 닉네임 우선 사용
- InputField 읽기 전용 모드 지원
- 기존 시스템과의 하위 호환성 유지

## 🎯 향후 개선 계획
- 이메일 인증 시스템
- 비밀번호 재설정 기능
- 사용자 프로필 관리
- 게임 통계 저장
- 친구 시스템
