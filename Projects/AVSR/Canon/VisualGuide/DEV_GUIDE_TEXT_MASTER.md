# AVENGING SPIRIT RE:BORN — DEVELOPER VISUAL GUIDE TEXT MASTER v1.1

Source Lock: Core Combat v3.1 / Host v1.1 / Developer Handoff v1.5

## 01. 30초 안에 전체 구조를 이해한다

- Purpose: Host를 유지할지 바꿀지 판단하는 전투 루프
- Source: Core Combat v3.1 · Runtime v1.5
- Owner: Gameplay
- PASS: Host 교체가 공격·생존·빌드 판단으로 작동

## 02. Runtime부터 만들고 Importer는 나중에 만든다

- Purpose: 개발 순서와 단계별 Gate를 고정한다
- Source: IMPLEMENTATION_ORDER · MILESTONE_ACCEPTANCE
- Owner: Tech Lead
- PASS: 각 단계 Exit Criteria 통과 후 다음 단계 진행

## 03. 데이터는 한 방향으로만 Unity에 들어간다

- Purpose: ID·검증·Fallback 경계를 명확히 한다
- Source: MASTER v1.5 · DATA_CONTRACT v1.5
- Owner: Tools/Data
- PASS: Spawn Source 1개 · HostRef null 0 · 참조 오류 0

## 04. Room Prefab은 데이터에서 반복 생성한다

- Purpose: Generated Root와 Designer Override를 분리한다
- Source: ROOM_LAYOUT_UNITY_DATA · PREFAB_REGISTRY
- Owner: Tools/Level
- PASS: 34 Room 생성·Entry/Exit·Nav 검증 오류 0

## 05. CH1_N01이 첫 번째 실행 가능한 기준이다

- Purpose: 17단계를 순서대로 확인한다
- Source: SMOKE_TEST_PROTOCOL
- Owner: Gameplay/QA
- PASS: 17단계 전부 PASS, 다음 Room 진입 성공

## 06. Ghost와 Host는 서로 다른 상태 머신이다

- Purpose: Tactical과 Death Possession을 별도 경로로 구현한다
- Source: Core Combat v3.1 · GHOST/HOST Contract
- Owner: Gameplay
- PASS: Killed와 Possessed EndReason이 분리

## 07. 빙의 조건은 문자열이 아니라 Typed Rule이다

- Purpose: Immediate / Conditional / Not Possessable을 동일 Contract로 처리한다
- Source: CONDITION_GROUP · POSSESSION_BALANCE_VARIANT
- Owner: Gameplay/Data
- PASS: 조건 진행·READY·종료 이유가 Debug HUD에 표시

## 08. Host마다 공격 템포가 달라야 한다

- Purpose: 역할뿐 아니라 입력 후 체감까지 분화한다
- Source: ATTACK_PROFILE · HOST_MAINTAIN_HOOK
- Owner: Combat
- PASS: 12 Host Feel Signature 중복 0

## 09. KEEP와 SWITCH가 동시에 합리적이어야 한다

- Purpose: 한쪽이 항상 정답이면 핵심 재미가 무너진다
- Source: MaintainHook · Synergy Runtime · Balance Variant
- Owner: Game Design/QA
- PASS: 동일 상황에서 두 선택 모두 생존·공격 근거 보유

## 10. Synergy는 숫자가 아니라 행동을 바꾼다

- Purpose: Host 순서가 공격 패턴을 변환하도록 구현한다
- Source: SYNERGY_RUNTIME · BUFF_UNLOCK_POLICY
- Owner: Combat/FX
- PASS: 8개 Signature Synergy가 화면에서 식별 가능

## 11. Room이 KEEP/SWITCH 판단을 만든다

- Purpose: CH1_N07 실제 좌표로 첫 Directed Synergy를 검증한다
- Source: ROOM_LAYOUT_UNITY_DATA · CH1_N07
- Owner: Level Design/QA
- PASS: 추천 Host 없이 Clear 가능 + Switch Payoff 식별

## 12. Boss Phase는 새로운 행동을 보여줘야 한다

- Purpose: 수치 상승이 아니라 Pattern·Arena·Candidate가 바뀐다
- Source: BOSS_PHASE_RUNTIME v1.5
- Owner: Boss/Level
- PASS: 각 Boss 3개 Phase 행동 Signature 중복 0

## 13. 성장은 다음 플레이의 선택지를 남겨야 한다

- Purpose: CH1→CH3에서 전투·성장·재도전을 연결한다
- Source: REWARD/GROWTH Runtime · CHAPTER_ROUTE
- Owner: Progression
- PASS: CH1~3 사이 성장 행동 2~4회, 전부 구매 불가

## 14. 실측 전에는 재미를 PASS로 판정하지 않는다

- Purpose: Debug HUD와 KPI로 P0→P1 판단을 기록한다
- Source: PLAYTEST_RESULT · PLAYTEST_METRIC
- Owner: QA/Analytics
- PASS: Build·Variant·Room 단위 실측 데이터 확보

## 15. Prefab에는 Logic 계약만 고정한다

- Purpose: Host·Enemy·Boss·Projectile·Hazard 필수 Component
- Source: PREFAB_REGISTRY · UNITY_RUNTIME_CLASS_LIST
- Owner: Gameplay/Tech Art
- PASS: Missing logic component 0, Missing art는 fallback 허용

## 16. Character View는 Logic을 건드리지 않고 교체한다

- Purpose: Animation·Socket·Hitbox 리소스 인수 조건
- Source: CHARACTER_RESOURCE_SPEC
- Owner: Tech Art/Animation
- PASS: ViewRoot 교체 후 Gameplay 결과 동일

## 17. Error는 생성 전에 막고 Warning은 기록한다

- Purpose: 개발 중 가장 자주 보는 QA Quick Reference
- Source: DATA_CONTRACT · HANDOFF_VALIDATION_REPORT
- Owner: Tools/QA
- PASS: Blocker 0에서만 Room/Build 생성

## Runtime Locks

- Spawn Source: layout.enemySpawns only (104 records / 74 unique coordinates); top-level spawns removed.
- Every possessable Enemy/Candidate resolves a HostID; Gate uses HostID or HostPoolID only.
- E015 and H16 Baseball Player: MinChapter 3, first playable CH3_N07.
- DATA_CONTRACT v1.5 defines Field Order / Type / Required / Default for every required collection.
- S01 Gangster→Ninja: ALWAYS BASE, Required Buff NONE.
- CH1 Tag Buff Pool: LOCKED; unlock after CH1 Clear.
- ROOM_SIMULATION: DEPRECATED; DESIGN_EXPECTATION and PLAYTEST_RESULT separated.
- Tactical baseline: TP_B Cost 6 / Cooldown 8; compare TP_A 8/8 and TP_C 5/10.
- Boss: Not Possessable; every phase changes behavior.
