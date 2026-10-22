# 게임 하네스 키트 — 새 게임 리포(L3)에 들이기

> **받는 쪽 Claude 세션에게:** 이 폴더(키트)를 읽고 아래 «적용 순서»를 그대로 수행한다. 끝나면 «적용 확인» 표를 채워 사용자에게 보고한다.
> 사용자는 «하네스 키트대로 적용해 줘» 라고만 말한다 — 이 문서가 무엇을 할지의 전부다.
> 만든 곳: AVSR(Avenging Spirit RE:BORN) 2026-10-22. 무엇을 왜 넣었는지는 `CHANGES.md`.

## 이 키트가 주는 것
1. **게임 규칙 6개**(`.claude/rules/project/`, 매 세션 실린다) — PD 와 일하는 방식 · 검증 · 보고 · 이펙트 · 리소스 포맷 · 아이콘 규격 · 코덱스 발주 · 화면 맞춤.
2. **스킬 `fx-art-pipeline`** — 이펙트 · 아트 리소스를 코덱스 자유 시안 → PD 선택 → 부품 발주 → 반영 → 나란히 검수로 만드는 순서 + 코덱스 브리프 양식.
3. **도구 3개**(`Tools/harness/`) — 코덱스 발주 실행기(한 줄) · 아이콘 검수 · 맞춤 · 리소스 포맷 점검.
4. **(선택) 기획 공정** — `Template/Planning_Flow.md` + 양식 6개(콘텐츠 먼저 · 씬은 나중 · 단계마다 승인 게이트).
5. UI 시안 → 게임 화면 작업은 **별도 키트** `Tools/ui-mockup-kit/`(AVSR 리포)를 그 키트의 `APPLY.md` 대로 들인다.

## 전제
- 새 게임 리포는 L2 공식 절차로 먼저 만든다 — L2 `Docs/game/new-game-repo.md` 0 ~ 4절(L2 복제 · `upstream` 연결 · 계층 선언 · 패키지 git URL).
  **이 키트는 그 다음에 얹는다.** 키트가 그 절차를 대신하지 않는다.
- 키트 파일은 전부 **게임 소유 자리**에만 놓인다 — `.claude/rules/project/` · `.claude/skills/fx-art-pipeline/` · `Tools/harness/` · `Template/`(새 파일만) ·
  `.claude/CLAUDE.md`(게임 소유). L2 소유(`.claude/rules/game/` · `.claude/refs/game/` · `.claude/agents/game/` · `Docs/game/` · `Template/00~09`)는
  **고치지 않는다** — 고칠 것이 있으면 `CHANGES.md` 「L2 로 올릴 후보」에 적고 L2 에 PR 한다(`.wa-layers/framework.json` 의 owns).
- Python 3 + Pillow · numpy(도구), PowerShell 5.1(실행기), 코덱스 CLI(ChatGPT 로그인 — API 키 금지).

## 적용 순서
1. **파일 놓기** — `files/` 아래를 **같은 상대 경로로** 새 리포 루트에 복사한다. **이미 있는 파일은 덮지 않는다** — 다르면 사용자에게 알리고 멈춘다.
   ```powershell
   $kit = '<키트 경로>\files'; $dst = '<새 리포 루트>'
   Get-ChildItem $kit -Recurse -File | % { $rel = $_.FullName.Substring($kit.Length + 1); $to = Join-Path $dst $rel
     if (Test-Path $to) { "이미 있음: $rel" } else { New-Item -ItemType Directory -Force (Split-Path $to) | Out-Null; Copy-Item $_.FullName $to; "놓음: $rel" } }
   ```
   기획 공정(`Template/*`)은 **선택** — 넣을지 사용자에게 먼저 묻는다(L2 는 이 공정을 뺐다).
2. **`.claude/CLAUDE.md` 에 조각 붙이기** — `files/.claude/CLAUDE.harness-section.md` 내용을 새 리포 `.claude/CLAUDE.md` 끝에 붙이고, 조각 파일은 지운다.
   날짜(YYYY-MM-DD)를 채운다. 기획 공정 · UI 키트를 안 들였으면 표의 그 줄을 지운다.
3. **게임 값 채우기**
   - `.claude/skills/fx-art-pipeline/project.md` — `<ProjCode>` · 교환 폴더 · 세이브 위치 · 반영 도구 · 이징 · 파티클 풀 · 화풍.
   - 교환 폴더 만들기: `Projects/<ProjCode>/_exchange/{in,ref}`.
   - 코덱스 브리프는 **첫 자유 시안이 통과한 뒤** `codex_brief_template.md` 로 만든다(색의 뜻 표는 게임 코드의 실제 색에서 뽑는다 — 지어내지 않는다).
   - 실행기 기본 폴더: 환경 변수 `PROJ_CODE` 를 두거나 `-Exchange` 를 준다.
4. **에이전트는 필요한 것만 켠다** — L2 `agent-profiles/` 에서 그 게임이 쓸 것만 `.claude/agents/project/` 로 복사한다(L2 `Docs/game/agent-disposition.md`).
   켜 둔 에이전트의 설명은 **매 요청** 실린다(AVSR 47개 = 약 2.8만 자). 쓰지 않는 것은 켜지 않는다.
5. **PowerShell 파일 인코딩** — `Tools/harness/run_codex_orders.ps1` 은 한글이 있어 **UTF-8 BOM** 이어야 PowerShell 5.1 이 읽는다(키트는 BOM 으로 둔다 — 복사 뒤 깨지지 않았는지 본다).
6. **돌려 보기**(«적용 확인» 표를 채운다)
   - 새 세션을 열고(규칙은 세션 시작 때 실린다) `.claude/rules/project/` 의 6개가 실렸는지 본다.
   - `python Tools/harness/resource_audit.py --out Temp/resource_audit` — `report.md` 가 생기는지.
   - `python Tools/harness/icon_tool.py review --in <아무 png 폴더> --names <이름>` — 시트가 생기는지.
   - 스킬 목록에 `fx-art-pipeline` 이 보이는지.

## 적용 확인 (보고용)
| 항목 | 결과 |
|---|---|
| 규칙 6개 놓음 · 새 세션에 실림 | |
| 스킬 `fx-art-pipeline` · `project.md` 채움 | |
| 도구 3개 놓음 · `resource_audit` · `icon_tool` 시험 | |
| `.claude/CLAUDE.md` 조각 붙임 | |
| 기획 공정(선택) | 넣음 / 안 넣음 |
| UI 키트(별도) | 넣음 / 안 넣음 |
| 켠 에이전트 | (목록) |
| 이미 있어 안 놓은 파일 | (목록 — 없으면 「없음」) |

## 하지 말 것
- L2 소유 파일 고치기(규칙이 틀렸으면 L2 PR — 키트 규칙이 「이 문서가 이긴다」고 적은 곳은 그렇게 따른다)
- 키트 규칙을 L2 `rules/game/` 에 복사하기(같은 규칙이 두 벌 실린다)
- AVSR 고유 값(유령 · 호스트 · 챕터 수 등)을 새 게임 규칙에 들이기 — 키트는 그것을 뺀 판이다
