# Technical QA Handoff v1.5

판정: **READY FOR DEVELOPER TECHNICAL QA**

완료: Core/Host/Experience lock 대조, 34 Room route/entry/exit, typed possession conditions, attack/projectile/status/AI/boss profiles, prefab/nav/collider contracts, 37 runtime/editor class contracts, smoke/milestone acceptance, runtime JSON.

Gameplay Lock: S01 기본 발동, CH1 Tag Buff 잠금, Host 12종 Attack Feel/MaintainHook, Tactical 6/8과 A/B/C, Boss 행동 변화, DESIGN_EXPECTATION/PLAYTEST_RESULT 분리.

v1.5 Technical Lock: Spawn Source를 layout.enemySpawns 104개로 단일화하고 최상위 spawns를 제거했다. E004→H02, E006→H02, E007→H01, E013→H05를 확정해 Possessable null HostID를 제거했다. Gate는 HostID/HostPoolID만 사용하며 E015 MinChapter=3으로 고정했다. DATA_CONTRACT에는 Required Collection 전부의 Field Order/Type/Required/Default를 포함한다.

개발 전 집중 검수: Prototype 공격 수치와 Tactical Cost Variant 승인, Unity/Nav 기술 선택, Placeholder art 규격, Save 파일 위치, Analytics SDK 결정. 재미 판정은 PLAYTEST_RESULT 실측 후에만 수행한다.

실행 상태: Unity 구현 NOT STARTED, Unity execution NOT EXECUTED.