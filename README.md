# Always On Top

Microsoft **PowerToys의 "Always On Top" 기능만** 그대로 구현한 가벼운 Windows 트레이 프로그램입니다.
단축키 한 번으로 아무 창이나 항상 최상위에 고정할 수 있습니다.

> PowerToys 전체를 설치하지 않고 이 기능 하나만 백그라운드에서 쓰고 싶을 때 사용합니다.

## 주요 기능

| 기능 | 설명 |
|------|------|
| 🔝 **항상 위 고정** | 활성 창을 `SetWindowPos`(HWND_TOPMOST)로 최상위 고정/해제 토글 |
| ⌨️ **전역 단축키** | 기본 `Win + Ctrl + T` (PowerToys와 동일), 설정에서 변경 가능 |
| 🟧 **테두리 표시** | 고정된 창 주위에 색상 테두리 표시 (색상·두께 설정) |
| 🔔 **소리 / 알림** | 고정·해제 시 소리와 트레이 알림 (각각 끄기 가능) |
| 🚫 **제외 창** | 특정 창 제목을 목록에 넣어 고정 대상에서 제외 |
| 🖥️ **백그라운드 실행** | 시스템 트레이에 상주, 창 없이 동작 |
| ❌ **종료** | 트레이 메뉴에서 종료(모든 고정 자동 해제) |
| ❓ **사용법 보기** | 트레이 메뉴에서 사용법 창 표시 |

## 요구 사항

- Windows 10 / 11
- 빌드 시: [.NET 8 SDK](https://dotnet.microsoft.com/download)
  (single-file로 빌드하면 실행 시 .NET 설치가 필요 없습니다.)

## 빌드 & 실행

### 방법 A — 스크립트 (권장)

```bat
build.bat
```

`dist\AlwaysOnTop.exe` 가 생성됩니다. 더블 클릭하면 트레이에서 실행됩니다.

### 방법 B — dotnet CLI 직접 사용

```bat
cd src\AlwaysOnTop

REM 바로 실행 (개발용)
dotnet run -c Release

REM 배포용 단일 실행 파일 생성
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -o ..\..\dist
```

## 사용법

1. 프로그램을 실행하면 시스템 트레이에 **주황색 "T" 아이콘**이 나타납니다.
2. 항상 위에 두고 싶은 창을 클릭해 활성화합니다.
3. **`Win + Ctrl + T`** 를 누르면 그 창이 최상위로 고정됩니다. (테두리 표시)
4. 같은 단축키를 다시 누르면 고정이 해제됩니다.

### 트레이 아이콘 메뉴 (오른쪽 클릭)

- **지금 창 고정/해제** — 활성 창을 즉시 토글 (아이콘 더블 클릭도 동일)
- **설정 (단축키 지정)** — 단축키·테두리·소리·제외 창 설정
- **사용법 보기** — 사용법 창 열기
- **종료** — 프로그램 종료

### 단축키 변경

설정 창에서 `Win / Ctrl / Alt / Shift` 조합을 선택하고, 키 입력칸을 클릭한 뒤
원하는 키를 누르면 됩니다.

## 참고

- 설정은 `%APPDATA%\AlwaysOnTop\config.json` 에 저장됩니다.
- **관리자 권한**으로 실행되는 창(예: 관리자 명령 프롬프트)을 고정하려면,
  이 프로그램도 관리자 권한으로 실행해야 합니다.
- Windows에서 시작 시 자동 실행하려면 `dist\AlwaysOnTop.exe` 바로가기를
  `shell:startup` 폴더에 넣으세요.

## 프로젝트 구조

```
src/AlwaysOnTop/
├─ Program.cs                 진입점 (단일 인스턴스 보장)
├─ TrayApplicationContext.cs  트레이 아이콘 · 메뉴 · 전체 조립
├─ HotKeyManager.cs           전역 단축키 등록 (RegisterHotKey)
├─ WindowManager.cs           최상위 고정/해제, 상태 추적, 소리
├─ BorderOverlay.cs           고정 창을 따라다니는 테두리 오버레이
├─ Config.cs                  설정 로드/저장 (JSON)
├─ SettingsForm.cs            설정 대화상자 (단축키 지정 포함)
├─ UsageForm.cs               사용법 창
├─ NativeMethods.cs           Win32 P/Invoke 선언
└─ app.manifest              DPI 인식 · 실행 수준
```

## 구현 메모 (PowerToys와의 대응)

- **고정 방식**: PowerToys와 동일하게 `SetWindowPos` + `HWND_TOPMOST`.
- **단축키**: PowerToys 기본값 `Win+Ctrl+T` 를 기본으로 사용.
- **테두리**: `WS_EX_LAYERED | WS_EX_TRANSPARENT` 클릭 통과 오버레이로 구현.
- **제외 앱**: 창 제목의 부분 문자열 매칭으로 제외.
