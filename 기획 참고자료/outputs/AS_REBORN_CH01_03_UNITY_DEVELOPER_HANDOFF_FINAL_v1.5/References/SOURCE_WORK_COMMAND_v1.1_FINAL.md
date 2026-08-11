# AVENGING SPIRIT RE:BORN
## DEVELOPMENT HANDOFF / WORK COMMAND
### UNITY DEVELOPER HANDOFF DESIGN PACKAGE v1.1 FINAL

---

# ROLE

당신은 Unity 모바일 액션 로그라이크의 **Lead Gameplay Programmer / Technical Game Designer / Tools Programmer / Technical Level Designer**다.

이번 작업의 목표는 Unity 프로젝트를 직접 개발하는 것이 아니다.

현재 확정된 `AVENGING SPIRIT RE:BORN`의 CH01~CH03 설계 데이터를 개발자에게 전달하여, 개발자가 추가 기획이나 임의 판단 없이 **Unity Import → Room Prefab 생성 → Collider/Nav/Path Bake → 플레이 → Boss Clear → 성장 → 다음 Chapter 진입**을 구현할 수 있도록 모든 Technical Spec, Runtime Data, Data Sheet, Prefab Contract, Importer Contract와 QA Protocol을 완성하는 것이다.

최종 목표는 다음 문장을 만족하는 개발 인계 패키지를 만드는 것이다.

> “개발자는 이 패키지만 전달받아 Placeholder Prefab 기반 CH01~CH03 플레이어블을 구현할 수 있고, 이후 실제 캐릭터/배경 리소스는 View/Animator만 교체하여 동일한 전투 로직을 유지할 수 있다.”

## 작업 범위 LOCK

- 이번 Work의 범위는 **개발 착수 가능한 최종 설계·데이터·기술 인계 패키지 제작**이다.
- 실제 Unity 프로젝트 생성, C# 구현, Prefab 생성, Nav Bake, 실행 및 빌드는 개발자가 수행한다.
- Work는 실행하지 않은 기능을 PASS로 판정하지 않는다.
- Work의 완료 기준은 “Unity에서 동작했다”가 아니라 **“개발자가 추가 설계 없이 구현·검증할 수 있다”**이다.
- 코드 블록은 구현 계약, Interface, Enum, 직렬화 구조와 핵심 의사코드 수준으로 작성한다. 완성된 실행 코드라고 허위 표기하지 않는다.

---

# 0. SOURCE OF TRUTH

반드시 아래 파일을 먼저 읽고 서로 대조한다.

1. `AS_REBORN_CH01_03_FULL_MAP_LEVELDESIGN_FINAL_PACKAGE`
2. `CH01_03_VERTICAL_SLICE_MASTER_v1.2_FULL_MAP_LEVELDESIGN.xlsx`
3. `ROOM_LAYOUT_UNITY_DATA.json`
4. `ROOM_LAYOUT_UNITY_DATA.csv`
5. `AVENGING_SPIRIT_REBORN_HOST_SYSTEM_FINAL_LOCK_v1.1.xlsx`
6. `CORE COMBAT DESIGN v3.1 FINAL LOCK`
7. 원작 Avenging Spirit 참고자료

## Source Priority

충돌 시 우선순위:

1. Core Combat FINAL LOCK
2. Host System FINAL LOCK v1.1
3. Experience / Level Design LOCK
4. Master v1.2
5. JSON/CSV Export

## 절대 변경 금지

- Boss는 Possession 불가
- Ghost → Possession → Host → Combat 구조
- 생존 중 Tactical Possession 가능
- Host 사망 → Ghost 복귀 → 재빙의
- Ghost HP는 핵심 Risk Resource
- 이동 중 일반 공격 OFF / 정지 후 Auto Attack ON 기본
- Host 유지와 교체 모두 가치가 있어야 함
- 방마다 교체 강제 금지
- 특정 Host 하드카운터 금지
- Host Synergy는 단순 수치보다 행동 변화 우선
- CH01~03 Experience Beat 훼손 금지

---

# 1. 작업 전 TECHNICAL AUDIT

코드를 만들기 전에 현재 Package에서 “실제 플레이에 필요한데 누락된 것”을 전수 검사한다.

`TECHNICAL_GAP_AUDIT.xlsx` 또는 Master Sheet를 만든다.

필드:

- Category
- Required Feature
- Current Source
- Present / Missing / Partial
- Risk
- Needed Data
- Needed Runtime Code
- Needed Prefab
- Needed Tool
- Action
- Status

최소 아래 범주를 전부 점검:

- Chapter Route
- Room Exit
- Entry Point
- Player Spawn
- Enemy Spawn
- Wave Trigger
- Boss Phase
- Possession
- Condition Parser
- Host Conversion
- Ghost State
- Tactical Possession
- Death Possession
- Buff
- Synergy
- Ultimate
- Reward
- Growth
- Save
- Analytics
- Prefab Registry
- Camera
- Collider
- Pathfinding / Nav
- Hazard
- VFX Hook
- SFX Hook
- Animation Hook
- Debug
- Validation

누락된 것이 발견되면 질문만 하지 말고 **현재 LOCK을 보존하는 최선안으로 보충 설계하고 구현 스펙에 추가한다.**

---

# 2. DATA CONTRACT v1.3 — 가장 먼저 수정

현재 XLSX/JSON/CSV를 그대로 Runtime Source로 사용하지 않는다.

`Data Contract v1.3`을 만든다.

## Source of Truth

`MASTER XLSX → Export Validator → Runtime JSON`

- XLSX = 기획 원본
- JSON = Unity Import Source of Truth
- CSV = QA / 디버그 / 외부 검토용
- 런타임에서 XLSX 직접 읽기 금지

---

# 3. 반드시 JSON에 추가해야 할 ROUTE DATA

현재 Room Layout만 있어서는 Chapter 진행이 완성되지 않는다.

다음 구조를 추가한다.

```csharp
ChapterRouteData
{
    string chapterId;
    string startRoomId;
    List<RouteNodeData> nodes;
}

RouteNodeData
{
    string roomId;
    List<RouteExitData> exits;
}

RouteExitData
{
    string exitId;
    string nextRoomId;
    string routeCondition;
    Vector2 exitPosition;
    float exitFacing;
    string gatePrefabId;
    string unlockCondition;
}
```

## 반드시 포함

- CH1 MAIN route
- CH2 branch A/B
- CH2 merge
- CH3 branch
- CH3 merge
- Boss clear route
- Chapter clear
- Growth transition
- Next Chapter unlock

Branch 선택 후 잘못된 Node에 진입하지 않는 Validation 필수.

---

# 4. ENTRY / EXIT DATA 추가

각 Room은 반드시 아래를 가진다.

```text
EntryPointID
EntryX
EntryY
EntryFacing

ExitPointID
ExitX
ExitY
ExitFacing

ExitTriggerID
GateID
GateState
UnlockRule
NextRoomID
```

## 기본 룰

- Room Clear 전 Exit LOCK
- Room Clear 후 Gate Open
- Recovery/Choice Room은 Interaction 완료 후 Open
- Boss Room은 Boss Dead 후 Open
- Branch Room은 선택한 Exit만 Open
- Exit Trigger 진입 시 NextRoom Load

---

# 5. ID NORMALIZATION

이름 문자열을 Runtime Reference로 사용하지 않는다.

잘못된 예:

```text
Host = "Gangster"
Enemy = "Fighter"
```

올바른 구조:

```text
HostID = H01
EnemyID = E001
BossID = B01
EliteID = EL01
BuffID = BU001
RoomID = CH1_N01
```

## 반드시 정규화할 대상

- Enemy
- Host
- Boss
- Elite
- Buff
- Synergy
- Room
- Prefab
- Projectile
- VFX
- SFX
- Animation
- Hazard
- Reward

DisplayName과 RuntimeID를 분리한다.

---

# 6. POSSESSION CONDITION 구조화

현재 문자열:

```text
BackAttack OR Down
Burn3 OR ArmorBreak
ArmorBreak AND Burn3
```

을 Runtime에서 문자열 Parse하지 않는다.

다음 구조로 변환:

```csharp
PossessionConditionGroup
{
    LogicOperator logic; // AND / OR
    List<PossessionCondition> conditions;
}

PossessionCondition
{
    PossessionConditionType type;
    float threshold;
    int stack;
    string sourceTag;
}
```

Enum 예:

```text
None
HPBelow
ArmorBroken
ShieldBroken
Stunned
Down
WeakPointBroken
BackAttack
Marked
BurnStack
Frozen
Cursed
Custom
```

---

# 7. PREFAB REGISTRY — 반드시 생성

Importer는 문자열 ID를 보고 Prefab을 찾을 수 있어야 한다.

`GamePrefabRegistry.asset`

예:

```text
E001 -> PF_ENEMY_GANGSTER
E002 -> PF_ENEMY_FIGHTER
...
H01 -> PF_HOST_GANGSTER
H02 -> PF_HOST_FIGHTER
...
EL01 -> PF_ELITE_01
B01 -> PF_BOSS_ROBOT_SNAKES
PILLAR_A -> PF_OBSTACLE_PILLAR_A
```

## Registry 데이터

- RuntimeID
- Prefab Reference
- Addressable Key optional
- Fallback Prefab
- Validation Status

Missing Prefab은 Import 실패가 아니라 Placeholder Prefab으로 대체하고 Error Log를 남긴다.

---

# 8. PLACEHOLDER PREFAB SYSTEM

아트를 기다리지 않는다.

최종 Character Resource가 없어도 플레이 가능한 Placeholder Prefab을 만든다.

## Placeholder Host

최소 컴포넌트:

```text
HostController
HostMovement
HostCombatController
AutoTargetSystem
AttackController
Health
UltimateController
BuffReceiver
SynergyReceiver
PossessionHostState
StatusController
AnimatorBridge
Hitbox
Hurtbox
CharacterView
AudioHook
VFXHook
```

## Placeholder Enemy

```text
EnemyController
EnemyAI
EnemyMovement
EnemyAttackController
Health
PossessionReceiver
PossessionConditionController
StatusController
Nav/SteeringAgent
AnimatorBridge
Hitbox
Hurtbox
EnemyView
```

## Placeholder Boss

```text
BossController
BossPhaseController
BossAttackController
BossMovement
BossMinionSpawner
Health
StatusController
BossArenaController
AnimatorBridge
Hitbox
Hurtbox
```

시각은 Color Capsule/Sprite로 대체 가능.

---

# 9. LOGIC / VIEW 완전 분리

실제 캐릭터 리소스가 들어올 때 전투 로직을 수정하지 않도록 한다.

```text
Gameplay Prefab
├ LogicRoot
│  ├ Movement
│  ├ Combat
│  ├ AI
│  ├ Health
│  ├ Possession
│  └ Status
│
└ ViewRoot
   ├ Sprite/Model
   ├ Animator
   ├ VFX Socket
   └ SFX Hook
```

실제 Character Resource는 ViewRoot만 교체 가능해야 한다.

---

# 10. CHARACTER RESOURCE INTEGRATION SPEC

개발자가 아티스트에게 그대로 전달할 수 있도록 `CHARACTER_RESOURCE_SPEC.md`를 만든다.

Host/Enemy/Boss 리소스 최소 요구:

- Sprite or Model
- Idle
- Move
- Attack
- Hit
- Death
- Ultimate
- Possession In
- Possession Out
- Character Pivot
- Character Scale
- Feet Position
- Weapon Socket
- Projectile Socket
- VFX Socket
- Hitbox Reference
- Hurtbox Reference
- Sorting Layer
- Shadow
- Portrait optional

Host별 Signature Asset도 정의.

예:

Gangster
- Bullet
- Muzzle Flash
- Mark FX

Salamander
- Breath Cone
- Burn FX

Robot
- Drone
- Mine
- Turret
- Missile

Ninja
- Shuriken
- Blink
- Execution

Wizard
- Projectile
- Zone FX

등.

누락 Host도 Host v1.1 기준으로 모두 작성.

---

# 11. RUNTIME COMBAT FOUNDATION

다음 Framework를 반드시 구현 스펙에 포함한다.

```text
GameplayRoot
│
├ GhostSystem
│  ├ GhostController
│  ├ GhostMovement
│  ├ GhostHealth
│  ├ GhostTargetSearch
│  └ GhostProtection
│
├ PossessionSystem
│  ├ ImmediatePossession
│  ├ ConditionalPossession
│  ├ TacticalPossession
│  ├ DeathPossession
│  └ PossessionTargetUI
│
├ HostSystem
│  ├ HostController
│  ├ HostMovement
│  ├ AutoAttack
│  ├ Ultimate
│  └ HostDeath
│
├ CombatSystem
│  ├ Targeting
│  ├ Projectile
│  ├ Hit
│  ├ Damage
│  ├ Status
│  └ Threat
│
├ EnemySystem
│  ├ AI
│  ├ Movement
│  ├ Attack
│  └ PossessionCondition
│
├ BuffSystem
├ SynergySystem
├ BossSystem
├ RoomSystem
├ SpawnSystem
├ WaveSystem
├ RouteSystem
├ RewardSystem
├ GrowthSystem
└ AnalyticsSystem
```

---

# 12. CORE COMBAT 실제 구현 규칙

## Host

Default:

```text
Moving -> Normal Attack OFF
Stop -> StopDelay -> Auto Attack ON
```

Host v1.1에 예외가 있으면 Data Flag로 처리.

예:

```text
CanAttackWhileMoving
AttackMode
TargetMode
Range
AttackInterval
```

코드 하드코딩 금지.

---

# 13. GHOST SYSTEM

다음 상태 머신 구현:

```text
GhostSpawn
-> Protection
-> TargetSearch
-> PossessionReady
-> PossessionTravel
-> HostSpawn
```

Ghost HP:
- Max HP
- Host Death Cost
- Tactical Possession Cost
- Direct Damage
- Recovery

모두 `BalanceConstants`에서 읽는다.

---

# 14. TACTICAL POSSESSION

Runtime 필수.

조건:

```text
Current Host Alive
Cooldown Ready
GhostHP > Cost
Valid Target Exists
Player Control Available
```

처리:

```text
Hold Input
-> Target Lock
-> Host Input Lock
-> Ghost Split
-> Ghost HP Cost
-> Old Host EndReason = TacticalReleased
-> Ghost Travel
-> Enemy EndReason = Possessed
-> New Host Spawn
-> Protection
-> Cooldown Start
```

Killed와 Possessed를 반드시 구분한다.

---

# 15. ENEMY END REASON

Enum 생성:

```text
Killed
Possessed
Despawned
RoomCleanup
Scripted
```

Killed만 EXP/Drop/kill count를 발생시킨다.

Possessed는 Drop 금지.

---

# 16. STATUS SYSTEM

최소 CH01~03에서 사용하는 상태를 구현:

```text
Burn
Freeze
Mark
Down
Stun
ArmorBreak
ShieldBreak
Curse
Slow
```

모든 상태는:

- Duration
- Stack
- MaxStack
- RefreshRule
- Source
- EventHook

을 가진다.

---

# 17. BUFF SYSTEM

현재 Buff DB를 ScriptableObject/Runtime Data로 Import.

Buff State:

```text
Active
Inactive
Stored
```

Host 변경 시 Tag 호환성 재평가.

3택 UI:
- Universal 1
- Current Host Compatible 1
- Global/Forward 1

---

# 18. SYNERGY SYSTEM

현재 `SYNERGY_MATRIX_USED`를 실제 Runtime Rule로 만든다.

수치 보너스보다 상태/행동 변환 지원.

예:

```text
PreviousHostTag
CurrentHostTag
PersistentEffect
TransformRule
Duration
Trigger
```

CH01~03에서 실제 사용하는 Synergy를 우선 구현.

모든 Host Synergy 전체를 처음부터 하드코딩하지 않는다.
Data Driven Rule로 구현한다.

---

# 19. ROOM RUNTIME

`RoomController`

상태:

```text
Loading
Entering
IntroSafe
Combat
WaveTransition
Clear
Reward
ExitOpen
Leaving
```

RoomData에서:

- Layout
- Player Spawn
- Enemy Spawn
- Object
- Hazard
- Wave
- Candidate
- Camera
- Exit
- Reward

를 읽는다.

---

# 20. SPAWN DIRECTOR

현재 SpawnData를 실제 Runtime Spawn으로 변환.

지원 Trigger:

```text
ROOM_START
TIME
ENEMY_COUNT_BELOW
WAVE_CLEAR
ELITE_HP
BOSS_PHASE
INTERACTION
```

Spawn 전에 Validation:

- Room Bounds
- Collider overlap
- Player Safe Radius
- MaxConcurrent
- Offscreen unfair attack
- Candidate availability

---

# 21. NAV / PATHFINDING 결정 및 구현

프로젝트가 2D 세로 Room Action이라는 전제에서 다음 중 가장 단순하고 안정적인 방식을 선택한다.

권장:

```text
Unity NavMesh / NavMeshPlus 또는 Grid Path
+
Local Steering
```

단, 기존 프로젝트 구조가 있으면 그것을 우선.

## 반드시 정의

- Player Radius
- Normal Enemy Radius
- Elite Radius
- Boss Radius
- Stopping Distance
- Separation
- Obstacle Avoidance
- Carving
- Area Mask
- Flying/NonNav movement rule

`NAV_PROFILE` 데이터 작성.

Nav Bake가 필요 없는 AI는 명시.

---

# 22. COLLIDER PROFILE

Prefab 유형별 Collider Preset:

```text
Player
Host
NormalEnemy
LargeEnemy
Elite
Boss
Projectile
Obstacle
Hazard
Trigger
Exit
```

Layer Matrix를 정의.

필수 충돌 규칙:

- Player ↔ Enemy body
- Projectile ↔ Hurtbox
- Ghost ↔ World
- Ghost ↔ Candidate
- Exit ↔ Player
- Hazard ↔ Host
- Enemy Projectile ↔ Player Host

불필요한 Physics 충돌 제거.

---

# 23. UNITY IMPORTER

Editor Tool 생성:

Menu:

```text
AVENGING SPIRIT
> Import Game Data
> Validate Data
> Generate Room Prefabs
> Generate Placeholder Prefabs
> Bake Navigation
> Run Smoke Test
> Export Validation Report
```

## Import Pipeline

```text
MASTER Export JSON
-> Schema Validation
-> ID Reference Validation
-> Runtime Data Assets
-> Prefab Registry Resolve
-> Room Prefab Generate
-> Spawn Point Generate
-> Object Generate
-> Trigger Generate
-> Camera Bounds
-> Nav/Collider Bake
-> Validation
```

---

# 24. ROOM PREFAB GENERATOR

생성 구조:

```text
PF_ROOM_CH1_N01
├ RoomController
├ Geometry
├ Obstacles
├ Hazards
├ PlayerSpawn
├ EnemySpawnPoints
├ PossessionCandidatePoints
├ WaveTriggers
├ SafeZones(Debug only)
├ AttackLanes(Debug only)
├ EntryGate
├ ExitGate
├ CameraBounds
└ NavRoot
```

SafeZone / AttackLane은 Debug Visualizer이며 Release에서는 비활성 가능.

---

# 25. PREFAB 생성 시 Designer Overlay

Scene View Gizmo:

- Player Spawn = Blue
- Enemy Spawn = Red
- Immediate Candidate = Cyan
- Conditional Candidate = Purple
- Boss/Elite = Dark Red
- Safe Zone = Green
- Attack Lane = Red Arrow
- Possession Route = Cyan Dashed
- Camera Bounds = White
- Exit = Yellow

Editor에서 RoomID 선택 시 관련 데이터가 Inspector에 보이게 한다.

---

# 26. DEBUG HUD

Playtest용 필수.

화면에 Toggle 가능:

```text
RoomID
Chapter
Current Host
Host HP
Ghost HP
Tactical Possession CD
Current Target
Candidate Condition
Buff List
Active Synergy
Enemy Count
Wave
Room Timer
Threat
FPS
```

Keyboard/Editor Debug:

- Kill All Enemies
- Ghost HP Set
- Host HP Set
- Spawn Host Candidate
- Jump Room
- Jump Boss Phase
- Give Buff
- Reset Room
- Slow Motion
- Invincible

---

# 27. SAVE / PROGRESS

CH01→CH02→CH03 연속 플레이를 위해 최소 SaveData 구현.

```text
CurrentChapter
UnlockedChapter
Gold
SpiritCore
HostMemory
Gem
GhostGrowth
HostMastery
DiscoveredHosts
UnlockedBuffs
```

Vertical Slice에서 Cloud Save 불필요.
Local Save로 충분.

---

# 28. GROWTH FLOW

CH1 Boss Clear:
- Reward
- Growth Screen
- 1~2 Upgrade
- CH2 Unlock

CH2 Boss Clear:
- Reward
- Host Memory Growth
- CH3 Unlock

CH3 Boss Clear:
- Reward
- Next Growth Goal Display

기존 Growth DB 사용.

---

# 29. BASEBALL PLAYER EXPOSURE 회귀 수정

현재 MASTER에서 `E015 Baseball Player`가 CH1_N02에 등장하고,
HOST_EXPOSURE에서도 CH1 = YES로 되어 있다.

이는 이전 Experience 방향:

```text
CH1: 제한된 Host 학습
CH2: 확장
CH3: Baseball / Medium / Dragoon 등 새로운 발견
```

과 충돌한다.

## 수정 지시

- Baseball Player의 첫 실질 노출을 CH3 Ricochet Beat로 이동
- CH1_N02의 E015를 CH1 Host Pool에 맞는 다른 Enemy로 교체
- Threat Budget과 Wave를 재계산
- Host Exposure 수정
- CH1 Experience Beat 훼손 금지
- CH3_N07 Ricochet Room에서 Baseball Player의 강점이 처음 “발견”되도록 배치

만약 Host v1.1 또는 Source 자료에서 CH1 등장 근거가 반드시 필요하다면:
- 원작 등장과 “플레이어가 Host로 체험하는 시점”을 분리
- Enemy cameo는 가능하되 Possession 가능 상태는 CH3에서 첫 공개
- 변경 근거를 Change Log에 남긴다.

---

# 30. CHARACTER RESOURCE가 들어오면 바로 교체 가능해야 함

최종 실제 캐릭터 리소스 Integration 절차:

```text
1. View Prefab 생성
2. Animator 연결
3. Socket 연결
4. Hitbox/Hurtbox Verify
5. Gameplay Prefab ViewRoot 교체
6. Animation Event 연결
7. VFX/SFX 연결
8. Character Smoke Test
```

Gameplay Logic 수정 없이 완료되어야 PASS.

---

# 31. SMOKE TEST — 최소 CH1_N01

개발자가 Importer 구현 후 가장 먼저 자동/수동 검증할 수 있도록 다음 Test Case의 입력값, 실행 순서, 기대 결과, 실패 조건을 데이터 시트로 설계한다.

## CH1_N01 Smoke Test

1. Room Load
2. Player Spawn
3. Starting Host Spawn
4. Enemy Spawn
5. Enemy AI 이동
6. Player 이동
7. Player 정지
8. Auto Attack
9. Projectile
10. Enemy Damage
11. Enemy Death
12. EXP
13. Immediate Possession
14. Host Change
15. Room Clear
16. Exit Open
17. Next Room Load

이 17단계는 개발 단계의 P0 Acceptance Gate다. Work는 각 단계의 TestID, Precondition, Action, ExpectedResult, FailureCode, RequiredLog를 작성한다.

---

# 32. POSSESSION SMOKE TEST

별도 Test Scene:

### Test A
Immediate Possession

### Test B
Condition:
BackAttack OR Down

### Test C
Burn3 OR ArmorBreak

### Test D
Tactical Possession

### Test E
Host Death -> Ghost -> Repossess

### Test F
No Candidate Fail-safe

모두 자동/수동 QA Sheet 작성.

---

# 33. BOSS SMOKE TEST

B01 Robot Snakes:

- Boss Not Possessable
- Phase Change
- Minion Spawn
- Candidate Ready
- Tactical Switch
- Boss Kill
- Reward
- Exit
- Growth

개발자는 B01을 우선 구현·검증한 뒤 동일 계약으로 B02/B03을 확장한다. Work는 세 Boss의 Phase/Spawn/Switch/Reward 계약을 모두 제공한다.

---

# 34. PLAYABLE MILESTONE

## Milestone P0
CH1_N01 playable

## Milestone P1
CH1 full clear

## Milestone P2
CH1 → Growth → CH2

## Milestone P3
CH1 → CH2 → CH3 full clear

## Milestone P4
Actual Character View Swap

개발자는 각 Milestone마다 Build를 남긴다. Work는 각 Milestone의 진입 조건, 필수 기능, Test Case, Exit Criteria, 산출 로그를 정의한다.

---

# 35. AUTOMATED VALIDATION

Editor Validation:

- Duplicate ID
- Missing Reference
- Missing Prefab
- Missing Exit
- Invalid NextRoom
- Spawn Outside Bounds
- Spawn Collider Overlap
- Candidate Host Missing
- Condition Invalid
- Boss Possessable Error
- Unknown Buff
- Unknown Synergy
- Missing Projectile
- Missing Attack Profile
- Missing Nav Profile

Error는 Build Block.
Warning은 Report.

---

# 36. DEVELOPMENT OUTPUTS

반드시 다음을 제출한다.

## 0. 통합 MASTER — 최우선 산출물

`AS_REBORN_CH01_03_UNITY_DEVELOPER_HANDOFF_MASTER_v1.3.xlsx`

기존 MASTER v1.2의 모든 시트를 보존하고 아래 Technical 시트를 추가·갱신한다.

- TECHNICAL_GAP_AUDIT
- DATA_DICTIONARY
- ID_REGISTRY
- CHAPTER_ROUTE
- ROOM_ENTRY_EXIT
- CONDITION_GROUP
- ATTACK_PROFILE
- PROJECTILE_PROFILE
- STATUS_PROFILE
- AI_PROFILE
- BOSS_PHASE_RUNTIME
- BUFF_RUNTIME
- SYNERGY_RUNTIME
- PREFAB_REGISTRY
- UNITY_RUNTIME_CLASS_LIST
- UNITY_DEVELOPER_CONTRACT
- NAV_PROFILE
- COLLIDER_PROFILE
- LAYER_MATRIX
- SMOKE_TEST_PROTOCOL
- MILESTONE_ACCEPTANCE
- IMPLEMENTATION_STATUS
- AUTO_COMPLETION_LOG
- FILE_MANIFEST
- TECHNICAL_QA

통합 MASTER와 Runtime JSON의 ID/값은 상호 검증한다.

## 0-1. 통합 최종 ZIP

`AS_REBORN_CH01_03_UNITY_DEVELOPER_HANDOFF_FINAL_PACKAGE.zip`

최상단 README에서 모든 파일의 열람 순서와 개발 순서를 안내한다.

## A. `UNITY_PLAYABLE_IMPLEMENTATION_SPEC.md`

전체 Architecture / Component / Flow.

## B. `DATA_CONTRACT_v1.3.json`

실제 Schema 예 포함.

## C. `CH01_03_RUNTIME_DATA_v1.3.json`

수정 완료된 Runtime Source.

## D. `PREFAB_REGISTRY.xlsx`

RuntimeID / Prefab / Placeholder / Resource Need.

## E. `CHARACTER_RESOURCE_SPEC.md`

아티스트 전달용.

## F. `UNITY_EDITOR_IMPORTER_SPEC.md`

Importer / Generator / Validator.

## G. `UNITY_RUNTIME_CLASS_LIST.xlsx`

Class / Responsibility / Dependency / Data.

## H. `ROOM_PREFAB_STRUCTURE.md`

Prefab hierarchy.

## I. `NAV_COLLIDER_PROFILE.xlsx`

## J. `SMOKE_TEST_PROTOCOL.xlsx`

## K. `TECHNICAL_GAP_AUDIT.xlsx`

## L. `IMPLEMENTATION_ORDER.md`

개발 순서 / dependency / milestone.

## M. `UNITY_DEVELOPER_CONTRACT.xlsx`

개발자가 구현할 모든 Runtime/Editor 기능을 다음 필드로 정리한다.

- SystemID
- FeatureID
- Class / Component
- Responsibility
- Input Data
- Output / Event
- State
- Dependency
- Failure Case
- Validation Rule
- Related Room / Host / Boss
- Implementation Priority
- Acceptance TestID

## N. `IMPLEMENTATION_STATUS.xlsx`

Work 산출물 상태와 실제 개발 상태를 혼동하지 않도록 분리한다.

- Item
- Design Status
- Data Status
- Developer Implementation Status
- Unity Execution Status
- Evidence
- Blocker
- Owner

초기값:

- Design Status = COMPLETE / PARTIAL / MISSING
- Data Status = COMPLETE / PARTIAL / MISSING
- Developer Implementation Status = NOT STARTED
- Unity Execution Status = NOT EXECUTED

실행하지 않은 항목을 PASS로 작성하지 않는다.

## O. `AUTO_COMPLETION_LOG.xlsx`

누락 자동 보완 내역, 신규 RuntimeID, 변경 이유, LOCK 영향, 관련 데이터와 개발자 확인 필요 여부를 기록한다.

## P. `UNITY_OPEN_AND_IMPLEMENT_GUIDE.md`

개발자가 패키지를 받은 뒤 수행할 실제 순서:

1. Unity 기준 버전/패키지 설정
2. Runtime Data 배치
3. Registry 생성
4. Core Runtime 구현
5. Placeholder 생성
6. CH1_N01 구현
7. Importer/Room Generator 구현
8. Nav/Collider 설정
9. Smoke Test
10. CH01→03 확장

## Q. `TECHNICAL_QA_HANDOFF.md`

개발 착수 전 최종 Technical QA에서 확인할 파일, 핵심 의사결정, 미해결 항목, 예상 개발 리스크와 검수 순서를 한 페이지에서 찾을 수 있게 작성한다.

## 실제 개발 산출물 제외

이번 Work는 다음을 직접 생성·완료했다고 주장하지 않는다.

- 완성 Unity 프로젝트
- 컴파일 완료 C# Source
- 실제 생성된 Unity Prefab
- 실제 Nav Bake 결과
- 실행 Build
- Unity Test Runner PASS 로그

대신 위 항목을 개발자가 구현할 수 있는 **구조, 계약, 데이터, 구현 순서, Acceptance Test**를 빠짐없이 제공한다.

---

# 37. 구현 순서 — 절대 뒤집지 않는다

```text
01 Technical Gap Audit
02 Data Contract v1.3
03 ID Normalize
04 Route / Entry / Exit 보완
05 Possession Condition 구조화
06 Prefab Registry
07 Runtime Core Interfaces
08 Placeholder Prefabs
09 CH1_N01 Smoke Test
10 Importer
11 Room Generator
12 Collider/Nav
13 CH1 Full
14 B01 Boss
15 Growth
16 CH2
17 B02
18 CH3
19 B03
20 Full CH1→3 Playtest
21 Actual Character Resource Swap
```

위 순서는 개발자 구현 순서다. Work는 각 단계에 필요한 선행 데이터와 Acceptance Gate를 같은 순서로 매핑한다.

---

# 38. 실제 개발자에게 전달할 Definition of Done

다음이 모두 가능해야 DONE.

1. Unity 프로젝트를 연다.
2. `Import Game Data` 실행.
3. Error 0.
4. `Generate Room Prefabs` 실행.
5. CH01~03 Room Prefab 생성.
6. Placeholder Host/Enemy/Boss 자동 연결.
7. Nav/Collider 생성/검증.
8. Play.
9. CH1 Possession Gate에서 Host 선택.
10. CH1 Boss Clear.
11. Growth.
12. CH2 진입.
13. Branch 선택.
14. B02 Clear.
15. Growth.
16. CH3 진입.
17. B03 Clear.
18. Host Tactical Possession 정상.
19. Condition Possession 정상.
20. Host Death → Ghost → 재빙의 정상.
21. Buff 정상.
22. Synergy 정상.
23. Save/Load 정상.
24. 실제 Character View Prefab으로 교체해도 Logic 정상.

이 중 하나라도 안 되면 “Playable Vertical Slice Complete”가 아니다.

---

# 39. 추가 누락 자동 보완 지시

작업 중 아래 유형의 누락을 발견하면 멈추지 말고 보완한다.

예:

- 공격 데이터는 있는데 Projectile 데이터 없음
- Boss Pattern은 있는데 Telegraph VFX Hook 없음
- Candidate는 있는데 Condition Event 없음
- Room Exit 위치 없음
- Branch NextRoom 없음
- Enemy AI Pattern이 문자열뿐이고 Runtime Profile 없음
- Host Ultimate에 Runtime Event 없음
- Buff가 Synergy를 참조하지만 Synergy ID 없음

## 처리 원칙

1. Core/Host/Experience LOCK과 충돌하지 않는 최소 구현안을 만든다.
2. 새 ID를 부여한다.
3. Master / JSON / Registry에 추가한다.
4. `AUTO_COMPLETION_LOG`에 기록한다.
5. 왜 필요했는지 기록한다.
6. 추후 교체 가능한 Data Driven 구조로 만든다.

임의로 새로운 게임 시스템을 추가하지 않는다.
**기존 설계를 실행 가능하게 만드는 누락만 보완**한다.

---

# 40. 최종 철학

이 프로젝트는 “데이터를 읽어서 맵을 생성하는 기술 데모”가 아니다.

최종 결과는 실제로 다음 판단을 체험해야 한다.

> “지금 Host를 유지할까?”
>
> “Ghost HP를 써서 갈아탈까?”
>
> “이 적을 죽일까, Condition을 만들어 Host로 가져갈까?”
>
> “현재 Buff를 살릴까, 다음 Host Synergy를 노릴까?”

기술 설계가 이 재미를 망치면 안 된다.

최종 구조는:

```text
Data
-> Runtime
-> Room
-> Combat
-> Possession
-> Host Decision
-> Synergy
-> Boss
-> Growth
-> Next Chapter
```

까지 하나의 연속된 플레이 루프로 검증되어야 한다.

---

# FINAL INSTRUCTION

설명만 제출하지 말고,
**개발자가 바로 구현할 수 있는 수준의 Technical Spec + Runtime Data + Prefab Contract + Importer Contract + QA Protocol**까지 완성한다.

이번 Work의 작업 범위에는 실제 Unity 프로젝트 생성과 개발이 포함되지 않는다.

그러나 “개발자가 알아서 결정”해야 하는 빈칸을 남겨서는 안 된다.

각 System / Class / Prefab / Data / Event / State / Failure Case / Test Case에 대해 최소 다음을 확정한다.

- RuntimeID
- 책임과 범위
- 입력 데이터
- 출력 또는 발생 Event
- 상태 전이
- 의존성
- Unity Component 배치 위치
- Inspector 직렬화 필드
- 기본값과 허용 범위
- 예외 및 Fail-safe
- 구현 우선순위
- Acceptance Test

문서 간 동일 항목은 반드시 동일한 ID와 값을 사용한다.

`MASTER XLSX → Runtime JSON → Registry → Prefab Contract → Class Contract → TestID`가 추적 가능해야 한다.

추적 불가능한 문자열 설명만 존재하면 FAIL이다.

“Character Resource만 있으면 된다”라고 추정하지 않는다.

최종 판정 문구는 다음 중 하나만 사용한다.

1. `READY FOR DEVELOPER TECHNICAL QA`
   - 설계와 데이터가 완성됨
   - Unity 구현/실행은 아직 하지 않음

2. `BLOCKED`
   - 개발자가 구현 전에 결정해야 할 핵심 데이터나 계약이 남아 있음

Work 단계에서는 `PLAYABLE COMPLETE`, `COMPILE PASS`, `UNITY TEST PASS`를 사용하지 않는다.

최종 제출 ZIP 최상단에는 반드시 다음을 둔다.

- `README_START_HERE.md`
- `IMPLEMENTATION_STATUS.xlsx`
- `TECHNICAL_QA_HANDOFF.md`
- `KNOWN_ISSUES.md`
- `FILE_MANIFEST.xlsx`

`FILE_MANIFEST.xlsx` 필드:

- FileID
- FileName
- Purpose
- Source of Truth
- Related System
- Version
- Required / Optional
- Validation Status
- Developer Action

최종 자체 QA 후 모든 필수 설계·데이터가 완료되었고 Unity 실행만 남았다면:

> **READY FOR DEVELOPER TECHNICAL QA**

로 제출한다.
