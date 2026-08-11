# Unity Playable Implementation Spec v1.5

## Architecture
GameplayRoot 아래 Ghost, Possession, Host, Combat, Enemy, Buff, Synergy, Boss, Room, Spawn, Wave, Route, Reward, Growth, Save, Analytics 시스템을 분리한다. LogicRoot와 ViewRoot는 완전 분리한다.

## Core state
GhostSpawn → Protection → TargetSearch → PossessionReady → PossessionTravel → HostSpawn. Room은 Loading → Entering → IntroSafe → Combat → WaveTransition → Clear → Reward → ExitOpen → Leaving.

## Combat lock
이동 중 Normal Attack OFF, 정지 후 StopDelay를 거쳐 Auto Attack ON. 예외는 AttackProfile.CanMoveAttack만 허용한다. Boss는 항상 NotPossessable다.

## Possession
Immediate, typed ConditionGroup, NotPossessable을 사용한다. Tactical Possession은 HostAlive, CooldownReady, GhostHP>Cost, ValidTarget, ControlAvailable을 모두 검사한다. P0 기본값은 Cost 6 / CD 8초이며 A=8/8, B=6/8, C=5/10을 동일 빌드 플래그로 비교한다. Death Cost는 20으로 고정한다. EnemyEndReason.Possessed는 EXP/Drop/KillCount를 발생시키지 않는다.

## Fun-critical locks
S01 Gangster→Ninja는 Buff 없이 항상 Mark→Blink Execution으로 변환한다. BUF_T01은 Relay/범위/지속/Chain만 강화한다. CH1 Tag Buff Pool은 잠그고 CH1 Clear 후 G_ACC_01로 해금한다. 모든 Host는 고유 Attack Feel과 MaintainHook을 사용하며, Boss 각 Phase는 서로 다른 AttackPattern과 ArenaBehavior를 실행한다.

## Evidence policy
DESIGN_EXPECTATION은 사전 가설이며 성공 판정 근거가 아니다. 실제 결과는 PLAYTEST_RESULT에 Build/Variant/Room 단위로 기록하고 PLAYTEST_METRIC 8종으로 평가한다.

## Data flow
MASTER XLSX → Export Validator → Runtime JSON → Runtime Assets → Prefab Registry → Room Generator. XLSX를 런타임에서 직접 읽지 않는다.

## Error policy
Duplicate/Missing Reference/Missing Exit/Invalid Condition/Boss Possessable/Spawn Bounds 오류는 개발 Build Block 대상으로 설계한다. Missing final art는 Placeholder fallback과 Warning으로 처리한다.