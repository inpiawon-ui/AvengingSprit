# UI 시안 적용 키트 — 다른 Unity 프로젝트에 들이기

> **받는 쪽 Claude 세션에게:** 이 폴더(키트)를 읽고 아래 «적용 순서»를 그대로 수행한다. 끝나면 «적용 확인» 표를 채워 사용자에게 보고한다.
> 사용자는 «이 키트대로 시스템 적용해 줘» 라고만 말한다 — 이 문서가 무엇을 할지의 전부다.

## 이 키트가 주는 것

1. **UI 하네스** — 화면 상태마다 여러 해상도로 실제 게임 뷰를 캡처하고 레이아웃·리소스를 자동 검사한다
   (판 밖·마스크 잘림·화면 밖·글자 넘침·글자 겹침·찌그러짐·9분할 누락·확대 흐림·원본 과대). 결과는 `Temp/UiHarness/` 의 캡처 + `report.md`.
   C6(가림)은 **TMP 글자가 뒤의 그림(Image)에 가리는지**를 근사 검사한다. 시나리오에서 클릭한 대상은 레이캐스트 가림도 검사하지만,
   모든 버튼이나 Input System·레거시 입력 모듈의 실제 입력 동작을 검증하지는 않는다(클릭은 포인터 이벤트를 직접 보낸다).
2. **스킬 `ui-mockup-to-game`** — 시안을 숫자로 바꾸고 → 글자까지 같게 → 해상도 대응 → 하네스로 재고 → 시안과 나란히 대조 → 교차 검수 → 보고하는 작업 흐름.
   세션이 UI·시안 작업을 할 때 스스로 불러 쓴다.
3. **비교판 도구** — 시안과 해상도별 캡처를 나란히 붙이고 위반 상자를 그린다(Python + Pillow).
4. **예시 프로필** — 로드랜드(RDL) 로비 프로필 원본. 형식 참고용이다(그대로 컴파일하지 않는다).

## 키트 구성

```
UiMockupKit/
├── APPLY.md                         ← 이 문서
├── Editor/UiHarness/                ← Unity 에 넣을 하네스 (C# 6개 + README.md + UiHarness.Editor.asmdef)
├── skill/ui-mockup-to-game/SKILL.md ← .claude/skills/ 에 넣을 스킬 (게임 무관)
├── tools/uiharness/                 ← contact_sheet.py · summary.py
└── example/RdlLobbyUiHarnessProfile.cs.txt
```

## 전제

- **uGUI + TextMeshPro, Canvas 렌더 모드 Screen Space - Overlay** 화면만 지원한다. Screen Space - Camera·World Space·UI Toolkit 화면에는
  그대로 적용하지 않는다 — 좌표 검사·클릭이 틀린다. 하네스에 맞추려고 기존 UI 의 렌더 모드를 바꾸지 않는다(그런 화면이면 사용자에게 알린다).
- Unity 버전: **6000.3 엔진 DLL 로 단독 컴파일만 확인**했다. 하네스는 Game 뷰 크기를 바꾸려고 Unity 내부 API(`GameView.selectedSizeIndex` ·
  `SizeSelectionCallback` · `GameViewSizes`)를 리플렉션으로 쓴다 — 다른 버전(특히 2022.3)과 다른 프로젝트에서의 메뉴 실행은 **미검증**이다.
  대상 버전에서 세 해상도 전환 · 실제 캡처 크기 · report.md 생성을 확인한 뒤에야 «적용 완료»로 보고한다.
- **UniTask** 패키지(`com.cysharp.unitask`). 없으면 설치한다: Package Manager → Add package from git URL →
  `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- 화면 클릭은 `EventSystem` 경로로 누른다 — 입력 모듈은 Input System·레거시 무엇이든 된다.

## 적용 순서

1. **하네스 넣기** — `Editor/UiHarness/` 폴더를 통째로 그 프로젝트의 `Assets/` 아래 편한 곳(예: `Assets/Editor/UiHarness/`)에 복사한다.
   `.asmdef` 가 같이 들어간다(`UiHarness.Editor` — 에디터 전용, UniTask·TextMeshPro·uGUI 참조).
   키트 안 README 의 «게임 에디터 어셈블리가 `GameFramework.Game.Editor` 를 참조» 는 원본 리포 기준이다 — **키트에서는 `UiHarness.Editor`** 를 참조한다.
   asmdef 를 빼고 `Editor/` 폴더(Assembly-CSharp-Editor)에 두는 선택지는 **프로필도 Assembly-CSharp-Editor 에 둘 때만** 된다 —
   게임의 이름 있는 에디터 asmdef 가 하네스를 참조해야 하면 asmdef 를 남긴다.
   네임스페이스는 `GameFramework.Game.Editor.UiHarness` 그대로 둬도 된다(코드는 다른 프레임워크에 의존하지 않는다). 바꾸려면 6개 파일을 함께 바꾼다.
2. **컴파일 확인** — 에디터가 플레이 중이 아닌지 먼저 본다. 컴파일 오류 0 을 확인한다(asmdef 참조 이름이 그 프로젝트와 다르면 고친다).
3. **해상도 등록** — Game 뷰 크기 목록에 최소 세 개를 Fixed Resolution 으로 등록한다(이름은 자유, 프로필과 같게):
   16:9(1920×1080) · 20:9(2400×1080) · 4:3(2048×1536). 그 게임의 대상 기기에 맞춰 더한다.
4. **프로필 만들기** — 그 게임의 에디터 코드에 `IUiHarnessProfile` 구현 하나(예시: `example/…cs.txt`, 설명: `Editor/UiHarness/README.md` «다른 게임에서 쓰는 법»):
   - `[MenuItem("<게임>/Verify/UI Harness (<화면>)")]` → `UiHarnessRunner.Run(new …Profile())`
   - `BootScene`(플레이 시작 씬) · `Root()`(검사할 캔버스) · `GotoStart`(상태마다 첫 화면으로 — 씬을 다시 로드하고 기다린다)
   - `Resolutions`(3번에서 등록한 이름 · 파일 꼬리표) · `MockFolder`(시안 폴더, 없으면 null)
   - `Scenarios` — 첫 화면 하나로 시작한다. **`Enter = d => UniTask.CompletedTask` 를 꼭 지정**하고(비우면 실행기가 null 을 불러 도달 실패한다),
     `Expect` 는 실제 첫 화면에 왔는지 검사한다. 그다음 화면마다 늘린다.
   - 프로필은 하네스와 **다른** 어셈블리(게임 에디터 asmdef 또는 Assembly-CSharp-Editor)에 둔다. 그 어셈블리가 `UiHarness.Editor`·`UniTask` 를 참조해야 한다.
5. **스킬 넣기** — `skill/ui-mockup-to-game/` 를 그 리포의 `.claude/skills/ui-mockup-to-game/` 로 복사한다.
   그리고 같은 폴더에 **`project.md` 를 새로 쓴다** — 그 게임의 하네스 위치·메뉴 이름·해상도 이름·시안 폴더·화면 조립 방식·사용자 취향.
   (스킬 본문은 «project.md 가 있으면 먼저 읽는다»고 적혀 있다. 로드랜드 것을 복사하지 않는다.)
6. **도구 넣기** — `tools/uiharness/` 를 그 리포의 `Tools/uiharness/` 로 복사한다. 실행: `python Tools/uiharness/contact_sheet.py <결과 폴더>` (Pillow 필요).
   이 `APPLY.md` 도 그 리포의 `Tools/ui-mockup-kit/APPLY.md` 로 복사한다 — 스킬이 «하네스가 없으면 이 안내서를 따르라»고 가리킨다.
7. **안내 한 줄** — 그 리포의 `CLAUDE.md`(세션이 매번 읽는 문서)에 «UI·시안 작업은 `ui-mockup-to-game` 스킬, 하네스 메뉴 ○○» 한 줄을 더한다 —
   스킬을 놓친 세션도 걸리게.
8. **첫 실행** — 메뉴를 돌려 `Temp/UiHarness/<이름>_<시각>/report.md` 가 생기는지 본다. **합격은 맨 위 «기계 검사 통과» 줄이다** —
   실패 개수만으로 판단하지 않는다(도달 실패·실행 오류·샷 누락도 그 줄이 잡는다). 시나리오·해상도가 각각 1개 이상이고,
   기대한 PNG(상태 × 해상도)가 다 있으며 크기가 해상도와 맞는지도 확인한다.
   실패가 나오면 고치거나(진짜 결함) 예외를 «경로 + 이유» 로 좁게 단다(의도된 것 — 배경을 덮게 까는 그림 등). 실행이 끝나도 플레이는 켜진 채다 — 끄고 다음 작업을 한다.

## 적용 확인 (보고용)

| 항목 | 확인 |
|---|---|
| 컴파일 오류 0 | |
| Game 뷰 해상도 3개 등록 | |
| 메뉴 실행 → report.md 맨 위 «기계 검사 통과» · PNG 가 상태×해상도만큼 있고 크기가 맞음 | |
| 세 해상도 캡처를 눈으로 봄 | |
| 스킬 + project.md 들어감 · CLAUDE.md 안내 한 줄 | |
| 비교판(contact_sheet) 생성됨(시안이 있으면) | |

## 하지 않는 것

- 그 프로젝트의 기존 UI 코드를 하네스에 맞춰 고치지 않는다 — 하네스는 재기만 한다. 고칠 것은 리포트를 보고 사용자와 정한다.
- 이미 다른 검증 하네스가 있는 프로젝트에는 사용자에게 먼저 묻는다(둘이 같은 메뉴·씬을 잡을 수 있다).
  **WA-Game-Cabal 은 적용 대상이 아니다 — 하네스가 달라 손대지 않는다(사용자 2026-10-15).**

## 키트를 다시 묶을 때 (원본 리포에서)

`python Tools/ui-mockup-kit/make_kit.py [출력 폴더]` — 기본 `Temp/UiMockupKit` + zip. **출력 폴더는 내용 전체가 지워진다** —
키트 전용 폴더만 지정한다(스크립트도 비어 있지 않은 남의 폴더면 거부한다).
