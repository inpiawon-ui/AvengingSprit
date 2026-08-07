---
name: avsr-ui-naming-glue
description: AVSR Stage 2b에서 확립한 요소 네이밍 규칙 — 공유 HUD는 TopHudGroup, 재사용 요소는 이름을 동일하게 지어 에셋 1종으로 수렴
metadata:
  type: project
---

AVSR Stage 2b(2026-08-07)에서 확립한 요소 네이밍 결정 2건.

**1. 상단 HUD = `TopHudGroup`** (`TopHudPanel` 아님).
로비·호스트선택이 공유하며 `LobbyMainUI`가 소유한다.
**Why:** Stage 2 문서 2개가 서로 다르게 표기(`TopHudPanel` / `TopHudGroup`)해 통일이 필요했고,
`~Panel` 접미사는 `06_ui.md`에서 "버튼으로 여는 콘텐츠 창(고정 높이 1152)"에 예약되어 있어
상시 노출 HUD에 붙이면 프리팹 타입을 오판하게 만든다.

**2. 재사용 요소는 요소 이름 자체를 동일하게 짓는다** — 부모를 달리해 형제 유일성을 만족시킨다.
AVSR 적용: `NotifyBadge`(6곳) · `PlusButton`(3곳) · `ArrowIcon`(2곳).
**Why:** "파일명 = 요소 이름" 규약 아래서 이름을 다르게 지으면(`MailBadge`/`MissionTabBadge`…)
동일한 그림을 에셋 6장으로 중복 제작하게 된다. 이름을 같게 두면 **에셋이 1종으로 수렴**한다.

**How to apply:** 새 화면 설계에서 (a) 상시 노출 HUD·바 같은 그룹에는 `~Panel` 접미사를 쓰지 않는다,
(b) 같은 그림이 여러 곳에 쓰이면 이름을 나누지 말고 부모를 나눈다. 단 상태 변형은 `{요소명}_{상태}`,
데이터 종속은 `{요소명}_{key}` 접미사를 쓴다(`hostslotframe_selected` · `hostslotportrait_amazoness`).

미해결로 남긴 규약 충돌: `05_prefabs.md`의 `~Panel` anchor 기술(`0,0~1,1` + sizeDelta `(0,1152)`)은
계산상 높이가 `1280+1152`가 되어 모순이다. AVSR은 anchorMin `(0,1)`/anchorMax `(1,1)`/pivot `(0.5,1)`/
anchoredPos `(0,-128)` 해석을 채택했고 **규약 문구 정정을 게이트 B2에 상신**했다.
