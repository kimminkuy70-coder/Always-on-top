# Always On Top

Microsoft **PowerToys의 "Always On Top" 기능만** 그대로 구현한 가벼운 Windows 트레이 프로그램입니다.
단축키 한 번으로 아무 창이나 항상 최상위에 고정할 수 있습니다.

> PowerToys 전체를 설치하지 않고 이 기능 하나만 백그라운드에서 쓰고 싶을 때 사용합니다.

## 주요 기능

| 기능 | 설명 |
|------|------|
| 🔝 **항상 위 고정** | 활성 창을 `SetWindowPos`(HWND_TOPMOST)로 최상위 고정/해제 토글 |
| ⌨️ **전역 단축키** | 기본 `Win + Ctrl + T` (PowerToys와 동일), 설정에서 변경 가능 |
| 🟦 **테두리 표시** | 고정된 창에 딱 맞는 **둥근 모서리** 색상 테두리(기본 파란색, 색상·두께 설정) |
| 🔔 **소리 / 알림** | 고정·해제 시 소리와 트레이 알림 (각각 끄기 가능) |
| 🚫 **제외 창** | 특정 창 제목을 목록에 넣어 고정 대상에서 제외 |
| 🖥️ **백그라운드 실행** | 시스템 트레이에 상주, 창 없이 동작 |
| ❌ **종료** | 트레이 메뉴에서 종료(모든 고정 자동 해제) |
| ❓ **사용법 보기** | 트레이 메뉴에서 사용법 창 표시 |

## 요구 사항

- Windows 10 / 11
- 빌드 시: [.NET 8 SDK](https://dotnet.microsoft.com/download)
  (single-file로 빌드하면 실행 시 .NET 설치가 필요 없습니다.)

## 실행 파일(.exe) 받기

Windows 러너에서 자동으로 빌드된 실행 파일을 바로 받을 수 있습니다 (직접 빌드 불필요).

- **릴리스**: 저장소의 **Releases → "Always On Top (latest build)"** 에서 `AlwaysOnTop.exe` 다운로드
- **Actions 산출물**: **Actions** 탭 → 최근 "Build Windows EXE" 실행 → Artifacts 의 `AlwaysOnTop-win-x64`

`AlwaysOnTop.exe` 는 self-contained 단일 파일이라 .NET 설치 없이 바로 실행됩니다.
(빌드는 `.github/workflows/build-exe.yml` 이 `windows-latest` 에서 수행합니다.)

## 직접 빌드 & 실행

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

1. 프로그램을 실행하면 시스템 트레이에 **네이비 배경의 라임색 "T" 아이콘**이 나타납니다.
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
- **테두리**: `UpdateLayeredWindow`(픽셀 단위 알파) 기반 클릭 통과 오버레이.
  안쪽은 완전 투명이라 다른 창을 위로 드래그해도 고정 창 내용이 사라지지 않습니다.
  위치는 `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` 로 잡아 **창에 딱 맞게**,
  안티에일리어싱된 둥근 사각형으로 **모서리를 둥글게** 그립니다.
- **제외 앱**: 창 제목의 부분 문자열 매칭으로 제외.

## UI / 디자인

설정·사용법 화면은 **벤토 그리드 + 미니멀리즘**을 조합하고, **네이비(deep navy)와
라임(lime)** 을 브랜드 색으로 사용합니다. 각 설정은 둥근 카드로 묶여 있고, 단축키
조합 키는 라임 토글 칩으로 표시됩니다. 고정 테두리 색은 기본 **파란색(`#0A84FF`)**
이며 설정에서 자유롭게 바꿀 수 있습니다.

> 안정성: 고정한 프로그램이 갑자기 종료되어도(프로세스 강제 종료·크래시) 남은
> 테두리가 화면에 남지 않도록, 창 핸들 재사용까지 감지해 오버레이를 정리합니다.
> 창이 최소화되면 테두리는 자동으로 숨겨집니다. 또한 일부 앱이 최상위(topmost)
> 상태를 스스로 해제해 새 창 뒤로 묻히는 경우를 감지해, 포커스를 뺏지 않고
> 다시 최상위로 올려 줍니다.
