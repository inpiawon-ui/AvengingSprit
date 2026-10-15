# AVSR(Avenging Spirit RE:BORN) — 이 프로젝트 값

> 키트 `Tools/ui-mockup-kit/APPLY.md` 로 2026-10-15 들였다. 스킬 본문(`SKILL.md`)보다 이 문서가 이 게임의 값이다.

## 하네스

- 하네스: `Assets/Editor/UiHarness/` (어셈블리 `UiHarness.Editor`, 네임스페이스 `GameFramework.Game.Editor.UiHarness`)
- 프로필: `Assets/Scripts/Editor/UiHarness/AvsrLobbyUiHarnessProfile.cs` (기본 에디터 어셈블리)
- 메뉴: `Tools/Game/Verify/UI Harness (Lobby)` · 해상도 등록 `Tools/Game/Verify/게임 뷰 해상도 등록 (AVSR)`
- 해상도(세로 게임): `AVSR 16x9`(720×1280) · `AVSR 20x9`(1080×2400) · `AVSR 4x3`(1536×2048)
- 결과: `Temp/UiHarness/AvsrLobby_<시각>/report.md` — 맨 위 «기계 검사 통과» 가 합격선
- 비교판: `python Tools/uiharness/contact_sheet.py <결과 폴더>` · 요약 `python Tools/uiharness/summary.py <결과 폴더>`
- 새 화면(상점 · 소환 · 패스 · 프로필)은 프로필을 화면마다 하나 더 만들거나 이 프로필에 상태를 더한다

## 시안

- 시안 폴더: `Projects/AVSR/_exchange/in/` — 장면 시안 `scene_s01_lobby` … `scene_s25_buttonSheet`, 소환 `mock_summon_a`, 프로필 `mock_store_profile`
- 시안 크기: **941 × 1672**(= 게임 720 × 1280 의 1.307 배). 시안 좌표 × 0.7651 = 게임 좌표(왼쪽 위 기준)
- 장면 목록 · 무엇이 어느 화면인지: `Projects/AVSR/_exchange/store_scenes.md` · 기획 `Projects/AVSR/AVSR_Content_Store.md`

## 화면 조립 방식

- 캔버스 Screen Space - Overlay · CanvasScaler 720 × 1280 · `SafeArea.ApplyCanvasMatch`(contain — 05_prefabs.md)
- 화면은 **빌더 · 바인더 에디터 스크립트가 프리팹을 세운다**(손으로 프리팹을 고치지 않는다). 로비 챕터 창은
  `ChapterHostBuilder` → `FixedBarsBinder` → `ChapterHostInfoBinder` → `LobbyS22eBinder`(Run · RunTop · RunNav) 순서
- 배경은 **칸 없는 판**(`ch_screen_s22e`) + 늘려 쓰는 칸(`ch_s22e_box`, 9분할)을 시안 좌표대로 놓는다 — 코덱스가 칸 좌표를 정확히 못 그린다
- 부품 그림은 전부 코덱스 납품(`_exchange/out/…` → `StorePartsImporter`). 코드로 그림을 그리지 않는다(메모리 no-shape-scripts)
- 글자는 언어팩 키(`Projects/AVSR/Localization/AVSR_Strings.tsv` → `TSV → 문자열 표 반영`), 한국어 · 일본어 · 영어 셋 다 넘침 없이

## 사용자 취향

- 답 · 보고는 전부 한국어. 시안이 정답 — «비슷하게» 가 아니라 재서 1:1 로
- 보고에 캡처 PNG 와 **파일 경로**를 같이. 세 해상도 캡처를 함께 보인다
- 유니티는 다른 세션(인게임 · 기획)과 같이 쓴다 — 플레이를 켜기 전에 한 줄 알리고, 끝나면 「다 썼다」
- 찍은 뒤 언어 · 세이브를 찍기 전으로 되돌린다
