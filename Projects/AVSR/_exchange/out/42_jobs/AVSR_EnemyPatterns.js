// 몬스터 행동 패턴 6종 · 잡몹 7종 · 챕터 배정        v1.0 (2026-08-31)
// 단일 출처는 AVSR_JobClasses.md §7. 이 파일은 그 표를 기계가 읽는 형태로 옮긴 것이다.
//
// ⚠ 이미 있어서 새로 만들지 않는 것 셋 — 새 패턴은 전부 이 위에 얹는다:
//     부채꼴 다발  _canonShotCount · _spreadDegrees (PerformAttack 이 읽는다)
//     예고 동작    _canonTelegraph 0.45~0.75초
//     동시 상한    _canonMaxConcurrent (방 전체 기준)

const PATTERNS = {
  CHASE:  { ko:'밀어붙임',   st:'구현됨', move:'추격',   fire:'근접',
            note:'사거리 밖이면 계속 좁히고, 안에 들어오면 예고 후 때린다' },

  STRAFE: { ko:'치고 빠짐',  st:'구현됨', move:'2발 후 옆으로', fire:'단발',
            shotsBeforeMove:2, repositionPx:190,
            note:'옮기는 동안은 쏘지 않는다. 그 틈이 반격할 틈이다' },

  FAN:    { ko:'부채꼴',     st:'값만',   move:'자리 지킴', fire:'3발',
            shots:3, spreadDeg:40,
            note:'발당 피해는 코드가 split 으로 자동으로 나눈다. 탄 수가 화력 배수가 되지 않는다' },

  HOP:    { ko:'쑥쑥 접근',  st:'신규',   move:'도약', fire:'근접',
            idleSec:1.5, crouchSec:0.35, hopMeters:3.0, hopSec:0.2,
            blockedByProps:true, invulnerable:false, lockDirection:true,
            note:'무겁고 느린 놈이 갑자기 3 m 를 좁히는 것이 전부다. 무적 없음 — 접근을 끊을 수 있어야 한다' },

  VAULT:  { ko:'도약 사격',  st:'신규',   move:'점프·체공·낙하', fire:'낙하 중 3발',
            crouchSec:0.4, riseSec:0.35, hangSec:0.35, fallSec:0.35,
            invulnerable:true, invulnFrom:'상승', landCloserMeters:2.0,
            shots:3, spreadDeg:30, intervalSec:2.4,
            note:'체공 1.05초 동안 못 때린다. 잡몹이 처음으로 타이밍 문제가 되는 자리' },

  CROSS:  { ko:'열십자',     st:'신규',   move:'없음', fire:'4방향 고정',
            aims:false, dirs:4, intervalSec:2.2, telegraphSec:0.8, rotateDeg:45,
            note:'조준하지 않으므로 안 맞는 자리에 서는 것이 답. 축이 45° 씩 돌아 그 자리가 계속 바뀐다' },
};

// 잡몹 — 6종은 이미 있다. turret_cross 만 신규(그림은 기존 obj_turret* 재사용).
const TRASH = [
  { key:'skeleton',     ko:'해골',        kind:'근접', hp:28, atk:6,  rng:1.4, itv:1.4, tel:0.45,
    pattern:'CHASE',  ch:[1,3],   st:'기존', note:'벽. 느리고 약하지만 길을 막는다' },
  { key:'bat',          ko:'박쥐',        kind:'근접', hp:18, atk:4,  rng:1.2, itv:0.9, tel:0.25,
    pattern:'CHASE',  ch:[1,2],   st:'기존', note:'추격. 유령이 됐을 때 도주선을 끝까지 따라붙는다' },
  { key:'actor_enforcer',ko:'집행자',     kind:'근접', hp:52, atk:9,  rng:1.6, itv:1.7, tel:0.6,
    pattern:'HOP',    ch:[2,3],   st:'패턴 교체', note:'무겁고 느린데 갑자기 3 m 를 좁힌다' },
  { key:'scrapgunner',  ko:'폐품 사수',   kind:'원거리', hp:24, atk:7, rng:5.2, itv:1.9, tel:0.75,
    pattern:'STRAFE', ch:[1],     st:'기존', note:'CH1 유일 원거리. 배우는 자리라 손대지 않는다' },
  { key:'roadwarden',   ko:'순찰기',      kind:'원거리', hp:34, atk:9, rng:5.6, itv:1.8, tel:0.75,
    pattern:'FAN',    ch:[2],     st:'값만', note:'자리를 지키고 각도로 덮는다. 발당 3' },
  { key:'coilwalker',   ko:'코일 보행기', kind:'원거리', hp:26, atk:10, rng:6.2, itv:2.4, tel:0.7,
    pattern:'VAULT',  ch:[3],     st:'패턴 신규', note:'간격을 1.6 → 2.4 로 올린다. 한 번이 1.45초라 쉴 틈이 없어진다' },
  { key:'turret_cross', ko:'십자 포탑',   kind:'설치', hp:30, atk:8,  rng:0,   itv:2.2, tel:0.8,
    pattern:'CROSS',  ch:[3],     st:'신규 적', possessable:false, slot:'RANGED',
    art:'obj_turret.png · obj_turret_fire.png — 이미 있다. 새로 안 그린다',
    note:'사거리 0 = 무제한. 탄이 방 끝까지 간다' },
];

// 회전 목록 — 목록에 몇 번 넣었는지가 곧 등장 비율이다
const POOL = {
  1: ['skeleton','bat','scrapgunner'],
  2: ['actor_enforcer','bat','roadwarden'],
  3: ['actor_enforcer','skeleton','coilwalker','turret_cross'],
};

// 적으로 나온 호스트가 액티브 스킬을 쓴다 (지금은 플레이어 전용)
const ENEMY_HOST_SKILL = {
  coolMultiplier: 3,      // 8→24 · 14→42 · 20→60 · 28→84초
  firstUseDelaySec: 8,    // 방 입장 후 최소 이만큼 지나야 처음 쓴다
  telegraphSec: 1.0,      // 평타 예고(0.45~0.75)보다 길다. 몸 위에 스킬 아이콘
  maxConcurrent: 1,       // 방에 호스트가 둘이어도 한 번에 하나
  level: 1,               // 플레이어 Lv1 값 고정. 성장하지 않는다
  ignoreBossClauses: true,// 사신 현재체력 8% · 드라군 최대체력 8% 등은 무시
  note: '위협이 아니라 소개다. 한 방에 한두 번 보는 것이 목표',
};

module.exports = { PATTERNS, TRASH, POOL, ENEMY_HOST_SKILL };
