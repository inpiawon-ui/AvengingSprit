# 템플릿 프로젝트 → "피디야 게임만들어줘" 자동 개발 환경 (전체 설계)

---

## ▶ 새 세션에서 이어가기 (RESUME)

이 파일이 다음 세션의 실행 계획이다. (새 세션은 이 파일을 자동으로 읽지 않는다.)

**새 세션 시작 메시지 (그대로 복붙):**
```
이전 세션에서 만든 계획 파일을 읽고 이어서 진행해줘:
C:\Users\Art\.claude\plans\wiggly-wobbling-quilt.md

먼저 이 계획을 리포 안 `.claude/project/bootstrap_plan.md` 로 복사해 영구 보관하고,
PD·PM·기획팀 에이전트가 등록됐는지 /agents 로 확인한 뒤,
Phase A부터 순서대로 실행해줘.
```

**현재까지 상태 (2026-06-07 갱신 — Phase A~D 구현 완료):**
- ✅ **Phase A 완료**: `.claude/project/project_state.md` 신규(MODE 마커, 기본 TEMPLATE) + `.claude/CLAUDE.md` 모드 가드 블록·@자동로드 + constants.md·terminology.md EXAMPLE 라벨.
- ✅ **Phase B 완료**: `.claude/agents/COLLABORATION.md` 신규(보고·에스컬레이션·Sync·변경관리·핸드오프DoD·재작업루프) + PD/PM server-lead 편입 + art/sound-lead 재작업 5회 루프 + 전 팀장 COLLABORATION 참조.
- ✅ **Phase C 완료**: `wa-manager-pd.md` 0절 부트스트랩 7단계 워크플로우 + plan-lead/concept Game_Concept 확장·복사 예외.
- ✅ **Phase D 완료**: `Template/00_Master_Index.md` Unity_GameDev_Concept 등재·MON 코드·OPS 주석·자동화 범위 안내절(v1.5.0).
- `Template/` 에 마스터 문서 12종 + 테스트용 `Game_Concept.md`(삼국지 플라이트, ProjCode P0002) 존재.
- **남은 작업**: ⏳ 세션 재시작(PD·PM·기획팀 에이전트 로드) → ⏳ 드라이런 검증(아래 "검증" 절).
- **주의**: PD·PM·기획팀(`wa-manager-pd/pm/plan-lead`, `wa-plan-team-*`)은 파일은 디스크 존재하나 이 세션 subagent 레지스트리 미등록 → **세션 재시작 후** 실제 호출 가능.

**실행 순서 요약**: ✅Phase A → ✅Phase B → ✅Phase C → ✅Phase D → ⏳세션 재시작 → ⏳드라이런. (상세는 아래 본문)

---

## Context (왜)

이 리포는 **모든 게임의 부모(main 브랜치) 템플릿**이다. `.claude/`·CLAUDE.md 포함 전체를 복사해 새 게임을 시작한다. 목표:
- 새 게임 = 리포 복사 → `Template/Game_Concept.md`(간략 컨펌 컨셉, ProjCode 포함) 추가 → **"피디야 개발 시작해줘"** 한마디로 [세부 기획 → 회사형 협업 구현 → 통합]까지 자동 구동.
- **게임 시작 전까지 이 리포는 단일 게임이 아니라 범용 템플릿**으로 취급(Claude가 늘 명심).

현재 막힌 곳: ① 템플릿/게임 모드 구분 장치 없음 ② PD 부트스트랩 흐름 없음 ③ 회사형 협업(보고·협의·PD상의·재할당) 배선 70%·갭 다수 ④ Template 문서 세트 정합성 결함 ⑤ PD·PM·기획팀이 이 세션 미등록(재시작 필요).

## 확정 결정 (사용자)
1. 세부 기획서 위치 = **`Projects/[ProjCode]/` 유지** (Template/은 빈 양식). ProjCode는 `Game_Concept.md`에서 취득.
2. `Game_Concept.md` = **정식 컨펌·간략**. 세부는 에이전트가 확장. 평소 템플릿엔 없음(새 게임 시 추가, 테스트 후 삭제).
3. 템플릿 모드 = **상태 마커 + CLAUDE.md 가드**.
4. 범위 = **전체 설계** (회사형 협업 프로세스 포함).

---

## Workstream A — 템플릿/게임 모드 시스템

**A1. 상태 마커 (SSOT)** — 신규 `.claude/project/project_state.md` (CLAUDE.md `@`로 자동 로드)
- 필드: `MODE: TEMPLATE` (기본) 또는 `MODE: GAME` + `ProjCode`/게임명.
- 템플릿 상태에선 `TEMPLATE`. PD 부트스트랩이 활성화 시 `GAME:[ProjCode]`로 전환·기록.

**A2. CLAUDE.md 가드** — `.claude/CLAUDE.md` 최상단에 모드 규칙 블록 추가
- TEMPLATE 모드: "이 리포는 특정 게임이 아니다. 게임 고유 코드/에셋/기획을 임의 생성하지 말 것. `Template/Game_Concept.md` + 개발 시작 지시가 있을 때만 활성화."
- GAME 모드: "이 프로젝트는 [게임명]. constants.md·Game_Concept.md 참조."
- 활성화 신호 = `Template/Game_Concept.md` 존재 + 사용자의 "개발 시작" 지시 (둘 다 필요, 마커가 최종 권위).

**A3. 게임 고유 예시 정리** — `.claude/project/constants.md`·`terminology.md`의 박힌 예시(`char/minji`, `pet/rabbit`, `CharacterSelectPanel`, UI 해상도 등)를 "예시(EXAMPLE)" 라벨로 명시해 템플릿 오염 방지. (최소 변경 — 삭제 아닌 표기)

> 주의: 현재 리포에 테스트용 `Game_Concept.md`가 있어 "활성화처럼" 보임. 마커 기본값 TEMPLATE 유지 + Game_Concept.md는 PD가 명시 지시받을 때만 소비. 부모/main에는 Game_Concept.md를 커밋하지 않음(per-game 입력).

---

## Workstream B — PD 부트스트랩 워크플로우

`.claude/agents/management/wa-manager-pd.md`에 신규 최상위 워크플로우 섹션 추가.

| 단계 | 행위자 | 핵심 | 게이트 |
|---|---|---|---|
| 1 활성화 트리거·가드 | PD | "개발 시작" 수신 → `Template/Game_Concept.md` 존재 확인. 없으면 TEMPLATE 유지·보고·중단 | — |
| 2 ProjCode 취득·모드 전환 | PD | Game_Concept.md에서 ProjCode/게임명 취득 → 마커를 `GAME:[ProjCode]`로 기록 | — |
| 3 기획 위임(직접) | PD→plan-lead | Game_Concept.md를 **확정 컨셉**으로 전달 → plan-lead Phase A(Projects/[ProjCode]/ 생성·양식 복사) → 세부 기획 세트 생성(Game_Concept 확장) | plan-lead 내부 게이트(확장본 검토) |
| 4 상위 휴먼게이트 | PD→사용자 | 기획 세트 검토·구현 착수 승인 | **사용자 승인** |
| 5 기획→PD 스펙화 | PD | 기획 산출물을 팀별 완성조건·품질기준·제외범위로 확정 (갭 G4 해소) | — |
| 6 구현 위임 | PD→PM | client/art/sound/server 팬아웃 (의존순서: 서버계약→클라통합, 아트·사운드 에셋→클라팀 반입) | PM 조율 |
| 7 통합·품질·보고 | PM→PD→사용자 | 통합·PD 품질 게이트·완료 보고 | PD 품질 게이트 |

- `Game_Concept.md`가 이미 컨펌됐으므로 plan-lead concept 단계는 **생성이 아닌 확장**(간략→정식 `[ProjCode]_Concept.md`). 관련 1줄을 plan-lead "입력"·concept 에이전트에 반영.
- plan-lead Phase A "Template 전체 복사"에 **예외**: `Game_Concept.md`는 복사 제외.

---

## Workstream C — 회사형 협업 프로토콜

신규 **`.claude/agents/COLLABORATION.md`** (공유 프로토콜, 관리/팀장 에이전트가 참조). 분산 편집 대신 단일 출처로 DRY 유지. 핵심 갭만 보강(과설계 금지):

| 항목 | 내용 | 해소 갭 |
|---|---|---|
| 보고 형식 | 팀원이 블로커·이상 발견 시 팀장에게 보고하는 트리거·형식(무엇을/언제/누구에게) | G1 |
| 에스컬레이션 체인·권한 | 팀원→팀장→PM→PD→사용자, 각 단계 **최종 결정권자** 명시 | G7 |
| Management Sync | 소집 트리거(팀 간 의존·충돌)·의제·산출(결정 기록) 규정 | G2 |
| PD 변경관리 | 개발 중 컨텐츠 추가/방향 수정: PD 결정 → PM 영향 평가 → 재위임 절차 | G3 |
| 핸드오프 DoD | 팀 간 산출물 인수 기준(누가 "완료" 선언, 수신 검증 책임) | G5 |
| 재작업 루프 표준 | 검증 실패 시 최대 5회 + 초과 시 에스컬레이션을 **전 팀장 통일** | art/sound 무한루프 수정 |

**정합성 수정 (병행)**:
- `wa-manager-pd.md`·`wa-manager-pm.md`: server-lead "미존재" 표기 제거 → 현재 팀장 편입.
- `wa-manager-art-lead.md`·`wa-manager-sound-lead.md`: 재작업 루프 "최대 5회 + 에스컬레이션" 추가(client/server-lead와 통일).
- client-lead 수신창구 vs 각 팀장 핸드오프 책임 일원화(핸드오프 DoD로 명문화).
- 관리/팀장 에이전트에 "협업 시 `COLLABORATION.md` 준수" 참조 1줄.

---

## Workstream D — Template 문서 세트 BLOCKER 보강

(앞서 PD+기획팀장 2인 검토 결과 중 파이프라인 정합 필수분만)
- `00_Master_Index.md`: `Unity_GameDev_Concept.md` 파일목록 등재 / Monetization 파트코드(`MON`) 추가 / "에이전트 자동화 범위(인덱스 ⊇ 에이전트 GD슬라이스)" 안내 절 / OPS 미사용 주석.
- 권장(선택): `Game_Concept.md` 형식을 기준으로 새 게임 입력 양식 확정, 오타 수정 등은 후순위.

---

## 실행 순서

1. **Phase A** — 모드 시스템(A1 마커, A2 가드, A3 예시 라벨). *(기반)*
2. **Phase B** — 협업 프로토콜 `COLLABORATION.md` + 정합성 수정(server-lead, 재작업 루프). *(C)*
3. **Phase C** — PD 부트스트랩 워크플로우(B) + plan-lead/concept 연동(Game_Concept 확장·복사 예외).
4. **Phase D** — Template 세트 BLOCKER 보강(D).
5. **세션 재시작** — PD·PM·기획팀 에이전트 로드(현재 미등록).
6. **드라이런** — 아래 검증.

> 각 파일 변경은 섹션/표 단위 최소 편집. 신규 파일은 `project_state.md`, `COLLABORATION.md` 2개만.

---

## 전제·리스크
- **에이전트 미등록**: PD·PM·기획팀(`wa-manager-pd/pm/plan-lead`, `wa-plan-team-*`)이 이 세션 subagent 목록에 없음 → 실제 호출은 **세션 재시작 후** 가능. 파일 편집 자체는 무관.
- 테스트 `Game_Concept.md` 존재로 리포가 활성화처럼 보임 → 마커 기본 TEMPLATE로 명확화 필요.
- 협업 프로토콜은 핵심 갭만 — G6/G8/G9/G10 등은 선택(필요 시 후속).

## 검증
1. **모드 가드**: 마커 `TEMPLATE` 상태에서 일반 작업 시 Claude가 "특정 게임 가정 안 함"을 지키는지. 마커 `GAME:P0002` 전환 후 게임 컨텍스트 인지.
2. **부트스트랩 드라이런**(세션 재시작 후): `Template/Game_Concept.md`(삼국지 플라이트) 둔 채 "피디야 개발 시작해줘" → PD가 ProjCode(P0002) 취득→마커 전환→plan-lead 위임→휴먼게이트까지 진행되는지.
3. **가드 부재 케이스**: Game_Concept.md 없이 "개발 시작" → 1단계 가드가 TEMPLATE 유지·보고·중단.
4. **협업 루프**: 의도적 결함 주입 시 팀원→팀장 보고→재위임(최대 5회)→초과 에스컬레이션이 전 팀 통일 동작. art/sound도 무한루프 없이 중단.
5. **정합성**: PD/PM에서 server-lead 현재 팀장 편입·"미존재" 제거 grep 확인.
