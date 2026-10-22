# 무엇을 왜 — AVSR 에서 키운 것과 근거

> 이 키트의 파일마다 「AVSR 에서 무엇을 겪고 이렇게 정했나」. 새 게임에서 규칙을 고칠 때 근거를 잃지 않으려고 남긴다.
> 맨 아래는 **L2(WA-Framework-Unity)로 올려 줄 후보** — 올리면 다음 게임은 키트 없이도 받는다(L2 쓰기는 PD 확인 뒤 PR).

| 키트 파일 | AVSR 원본 | 근거(겪은 일) |
|---|---|---|
| `skills/fx-art-pipeline/` | `.claude/skills/fx-art-pipeline` · `08_fx` | 숫자만 주고받다 「퀄 엉망」 → 코덱스 자유 시안을 먼저 보이자 한 번에 통과(10-09). 부품 사양 덩이 · 판정 한 줄(10-22) |
| `codex_brief_template.md` | `_exchange/codex_brief_fx.md` | 광원 얘기를 빼먹어 툰으로만 그림(10-09). 색의 뜻 · 장면 분위기 표로 「이유 없는 빛 · 강약 없음」 줄임(10-22) |
| `rules/project/art_fx.md` | `08_fx.md` | 빛을 그림으로 받아 딱딱해짐(10-08) · 이유 없는 빛(10-08) · 파티클 성능(10-22) |
| `rules/project/art_resource_format.md` | `09_import_settings.md` | 아틀라스 57개 압축 없음 — 인게임 하나 272MB → ASTC 4x4 약 32MB(10-09 · 10-12). 아이콘이 옆으로 길게 와 작은 칸에서 눌림 → 정사각 128 · 비율 1.0 ~ 1.3(10-22) |
| `rules/project/codex_art_orders.md` | 메모리 `codex-cli-art-orders` | API 키 폴백 · 묶음 발주 때우기(09-14) · 「못 그렸다」인데 원본 남음(10-21) · 두 줄 동시 실행 429(10-22) |
| `rules/project/verify_and_report.md` | `checklist.md` · 메모리 여럿 | 영상 22개가 스킬 봉인 채 찍힘 · 카드가 몸 조건으로 꺼진 채 찍힘 · 내부 함수 호출로 창이 안 닫힘(10-21) · 세이브를 말없이 지움(10-08) |
| `rules/project/ui_screen_fit.md` | `05_prefabs.md` · `06_ui.md` | 태블릿에서 아래 잘림(비율 문턱 + width/height 로 갈림길이 죽음) · 왼쪽 앵커 쏠림(09-01) · 카드 설명이 상자 밖으로(10-12) |
| `rules/project/pd_working_style.md` | 메모리 피드백 30여 개 | 영어 보고로 10번 지적 · 정본을 근거로 삼음 · 반쪽 요청 · 핑퐁 등 |
| `Tools/harness/run_codex_orders.ps1` | `_exchange/run_rework1021_parts.ps1` | 429 · 재시도 |
| `Tools/harness/icon_tool.py` | `Tools/icon_review_sheet.py` · `fit_step1_card_icons.py` | 분홍 찌꺼기 · 보라를 분홍으로 오판(10-22) · 상점 칸에서 잘라 키운 임시방편을 철회하고 통째 축소로 |
| `Tools/harness/resource_audit.py` | `Tools/resource_audit.py` | 안 쓰는 그림 · 압축 없음 찾기 — 경로를 인자로 |
| `Template/*`(선택) | `Template/Planning_Flow.md` + 양식 | 콘텐츠 먼저 · 씬은 나중 · 단계마다 승인 게이트. L2 는 이 공정을 뺐다 — 들일지는 PD 결정 |

## L2 로 올릴 후보 (PD 확인 뒤 PR)
1. **`refs/game/project/05_prefabs.md` 의 match 규칙 버그** — 「비율 < 1.778 → match 0」은 태블릿에서 잘린다. contain 규칙(`ui_screen_fit.md` 1절)으로 바꾼다. **가장 급하다**(모든 게임이 같은 버그를 받는다).
2. **`Docs/game/image_pipeline.md` 의 GPT 이미지 한계표 정정** — 「알파는 대체로 붙는다」 → 실측은 「배경이 안 뚫려 온다」. 429 · 묶음 때우기 · 「못 그렸다」 원본 위치를 더한다.
3. **`2d-pixel-perfect` 스킬의 「압축 없음」 기본값** — 모바일에서는 ASTC 4x4 근거(272MB → 32MB)로 L2 쪽에 주석을 단다.
4. **아트 파이프라인 스킬** — L2 에 `art-pipeline` 계획이 있다. `fx-art-pipeline` 범용판이 그 씨앗이 된다.
5. **「못 잰 것은 미검」** — L2 `rules/game/checklist.md` 에 한 절로.
