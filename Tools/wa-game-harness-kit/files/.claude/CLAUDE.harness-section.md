<!-- 이 조각을 새 게임 리포의 `.claude/CLAUDE.md`(게임 소유) 끝에 붙인다. 붙인 뒤 이 파일은 지운다. -->

## 게임 하네스 (wa-game-harness-kit)

> 이 리포는 L2 `WA-Framework-Unity` 를 부모로 둔 게임(L3)이다. 아래는 게임 하네스 키트로 들인 것이다(YYYY-MM-DD).
> ⚠ **`@` 참조는 한 단계만 펼쳐진다** — 이 파일 안의 `@…` 는 세션에 안 실린다(AVSR 실측). 매번 필요한 규칙은 `.claude/rules/` 에 두고,
> 가끔 필요한 문서는 아래 「이 일을 할 때 읽는다」 표로 연다.

### 매 세션 실리는 게임 규칙 (`.claude/rules/project/`)
| 파일 | 무엇 |
|---|---|
| `pd_working_style.md` | PD 와 일하는 방식 — 한국어 · 현황 → 결정 → 제작 · 발주 단계 · 커밋 |
| `verify_and_report.md` | 못 잰 것은 미검 · 찍힌 것을 확인 · 게임 안 검증 녹화 · 세이브 되돌리기 |
| `art_fx.md` | 이펙트 — 파티클 / 그림 / 셰이더 고르기 · 성능 · 연출 순서 |
| `art_resource_format.md` | 임포트 설정 · ASTC · 아이콘 규격(정사각 · 2의 거듭제곱) · 점검 |
| `codex_art_orders.md` | 코덱스 그림 발주 요령 · 함정 |
| `ui_screen_fit.md` | 해상도 contain · 가로 앵커 · 글자 넘침 — L2 05_prefabs 의 옛 match 규칙보다 우선 |

### 이 일을 할 때 읽는다
| 일 | 먼저 열 것 |
|---|---|
| 이펙트 · 연출 · 아트 리소스 · 아이콘 | 스킬 `fx-art-pipeline`(+ 같은 폴더 `project.md` · 브리프) |
| UI 시안을 게임 화면으로 | 스킬 `ui-mockup-to-game`(UI 키트로 들인 경우) |
| 기획(Stage 0 ~ 4 · 승인 게이트) | `Template/Planning_Flow.md`(기획 공정을 들인 경우) |
| 게임 값(해상도 · 씬 · 주소) | `.claude/project/constants.md` |
| L2 프레임워크 사용법 | L2 `CLAUDE.md` 의 「착수할 때 읽는다」 표 · `.claude/refs/game/` |
