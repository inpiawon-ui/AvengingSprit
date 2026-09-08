// 6챕터 × 10방 = 60방 배정                                v1.0 (2026-09-01)
//
// 새로 만든 것은 **배정뿐**이다. 레이아웃 12종 · 자리 10개 · 잡몹 7종 · 호스트 23명은
// 이미 있던 것을 그대로 쓴다. 새 잡몹 0종 · 새 레이아웃 0종.
//
// spawn 한 줄 = [x, y, 자리태그, 유닛키, H=호스트(빼앗을 수 있다) / T=잡몹]
// 좌표는 발자국 중심(미터). 방은 10 × 13 m · 1 m = 72 px.
//
// ⚠ 지키는 규칙 넷 (전부 코드로 검증했다):
//   1. 모든 전투방에 **빼앗을 몸이 최소 하나** 있다
//   2. 모든 전투방에 **원거리와 근접이 둘 다** 있다
//   3. 같은 방에 같은 몸이 둘 있지 않다
//   4. 데뷔 챕터보다 먼저 나오는 몸이 없다

// 호스트 데뷔 챕터 — 각 챕터의 중간 보스 대장이 그 챕터에 처음 나오는 몸이다
const DEBUT = {
  "1": [
    "amazon",
    "baseball",
    "commando_mg",
    "commando_grenade"
  ],
  "2": [
    "hopper",
    "commando_missile",
    "medium",
    "dragon_blue"
  ],
  "3": [
    "ninja_chain",
    "ninja",
    "snowwoman",
    "white_wizard"
  ],
  "4": [
    "gangster",
    "thug",
    "guru",
    "dragoon"
  ],
  "5": [
    "robot",
    "commando_laser",
    "amazon_elite",
    "hopper_smg"
  ],
  "6": [
    "salamander",
    "vampire",
    "death"
  ]
};

// 잡몹 회전 — CH4~6 은 기존 7종 재조합. 새 잡몹 0종
const TRASH_POOL = {
  "1": [
    "skeleton",
    "bat",
    "scrapgunner"
  ],
  "2": [
    "actor_enforcer",
    "bat",
    "roadwarden"
  ],
  "3": [
    "actor_enforcer",
    "skeleton",
    "coilwalker",
    "turret_cross"
  ],
  "4": [
    "actor_enforcer",
    "roadwarden",
    "bat",
    "coilwalker"
  ],
  "5": [
    "actor_enforcer",
    "turret_cross",
    "skeleton",
    "coilwalker"
  ],
  "6": [
    "actor_enforcer",
    "coilwalker",
    "skeleton",
    "turret_cross",
    "roadwarden"
  ]
};

// 방마다 빼앗을 몸 몇 개
const HOSTS_IN_ROOM = {"001":1,"002":1,"003":1,"006":2,"008":2,"009":2};

// 엘리트 — 중간 보스와 달리 **빼앗을 수 있다**
const ELITE_RULE = {
  "scale": 1.3,
  "hpMul": 2,
  "atkMul": 1.2,
  "possessable": true,
  "note": "중간 보스(1.8배·빙의 불가)와 다르다. **엘리트는 빼앗을 수 있다** — 크고 아픈 몸이 곧 보상이다. 009 는 보스 직전이라 여기서 얻은 몸으로 010 에 들어간다"
};

const ROOMS = [
 {
  "ch": 1,
  "no": "001",
  "kind": "전투",
  "floor": "roomfloor_env_junkyard",
  "layout": "A",
  "layoutKo": "지그재그 관문",
  "count": 4,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    7.5,
    3.5,
    "FLANK",
    "gangster",
    "H"
   ],
   [
    7,
    7.5,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    1.5,
    6.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    8.5,
    6,
    "RANGED",
    "scrapgunner",
    "T"
   ]
  ]
 },
 {
  "ch": 1,
  "no": "002",
  "kind": "전투",
  "floor": "roomfloor_env_junkyard",
  "layout": "B",
  "layoutKo": "쌍기둥 통로",
  "count": 5,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    10,
    "BACK",
    "amazon",
    "H"
   ],
   [
    2,
    7.5,
    "FLANK",
    "bat",
    "T"
   ],
   [
    8,
    7.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    8,
    10,
    "FLANK",
    "bat",
    "T"
   ],
   [
    5,
    6,
    "RANGED",
    "scrapgunner",
    "T"
   ]
  ]
 },
 {
  "ch": 1,
  "no": "003",
  "kind": "전투",
  "floor": "roomfloor_env_junkyard",
  "layout": "E",
  "layoutKo": "계단",
  "count": 5,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    3,
    6.5,
    "FRONT",
    "commando_grenade",
    "H"
   ],
   [
    3.5,
    8.5,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    2,
    4.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    8.5,
    8,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    5,
    3.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ]
 },
 {
  "ch": 1,
  "no": "004",
  "kind": "이벤트",
  "floor": "roomfloor_env_junkyard"
 },
 {
  "ch": 1,
  "no": "005",
  "kind": "중간보스",
  "floor": "roomfloor_env_holding",
  "layout": "F",
  "layoutKo": "모서리 요새",
  "captain": "salamander",
  "minions": 3,
  "minionFrom": [
   "gangster",
   "amazon",
   "commando_grenade"
  ]
 },
 {
  "ch": 1,
  "no": "006",
  "kind": "전투",
  "floor": "roomfloor_env_junkyard",
  "layout": "J",
  "layoutKo": "엇갈린 문",
  "count": 6,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    8,
    5.5,
    "FLANK",
    "salamander",
    "H"
   ],
   [
    3,
    7.5,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    6.5,
    10,
    "FLANK",
    "bat",
    "T"
   ],
   [
    7,
    7.5,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    8,
    9.5,
    "BACK",
    "scrapgunner",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "scrapgunner",
    "T"
   ]
  ]
 },
 {
  "ch": 1,
  "no": "007",
  "kind": "상점",
  "floor": "roomfloor_env_junkyard"
 },
 {
  "ch": 1,
  "no": "008",
  "kind": "전투",
  "floor": "roomfloor_env_junkyard",
  "layout": "D",
  "layoutKo": "십자 분단",
  "count": 6,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    2.5,
    9.5,
    "BACK",
    "gangster",
    "H"
   ],
   [
    2.5,
    5.5,
    "FLANK",
    "scrapgunner",
    "T"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "scrapgunner",
    "T"
   ],
   [
    7.5,
    5.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "scrapgunner",
    "T"
   ],
   [
    5,
    2.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ]
 },
 {
  "ch": 1,
  "no": "009",
  "kind": "엘리트",
  "floor": "roomfloor_env_junkyard",
  "layout": "C",
  "layoutKo": "중앙 요새",
  "count": 6,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    5,
    3,
    "FRONT",
    "hopper",
    "H"
   ],
   [
    2.5,
    7.5,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    2.5,
    2.5,
    "FRONT",
    "bat",
    "T"
   ],
   [
    7.5,
    7.5,
    "RANGED",
    "scrapgunner",
    "T"
   ],
   [
    5,
    1.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    7.5,
    10,
    "RANGED",
    "scrapgunner",
    "T"
   ]
  ],
  "elite": "commando_mg"
 },
 {
  "ch": 1,
  "no": "010",
  "kind": "보스",
  "floor": "roomfloor_robot_snakes",
  "boss": "robot_snakes"
 },
 {
  "ch": 2,
  "no": "001",
  "kind": "전투",
  "floor": "roomfloor_env_missile",
  "layout": "B",
  "layoutKo": "쌍기둥 통로",
  "count": 5,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    10,
    "BACK",
    "amazon",
    "H"
   ],
   [
    2,
    7.5,
    "FLANK",
    "hopper_smg",
    "H"
   ],
   [
    8,
    7.5,
    "FLANK",
    "bat",
    "T"
   ],
   [
    8,
    10,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    6,
    "RANGED",
    "roadwarden",
    "T"
   ]
  ]
 },
 {
  "ch": 2,
  "no": "002",
  "kind": "전투",
  "floor": "roomfloor_env_missile",
  "layout": "E",
  "layoutKo": "계단",
  "count": 5,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    3,
    6.5,
    "FRONT",
    "thug",
    "H"
   ],
   [
    3.5,
    8.5,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    2,
    4.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    8.5,
    8,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    5,
    3.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 2,
  "no": "003",
  "kind": "전투",
  "floor": "roomfloor_env_missile",
  "layout": "J",
  "layoutKo": "엇갈린 문",
  "count": 6,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    8,
    5.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    3,
    7.5,
    "RANGED",
    "commando_mg",
    "H"
   ],
   [
    6.5,
    10,
    "FLANK",
    "bat",
    "T"
   ],
   [
    7,
    7.5,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    8,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ]
  ]
 },
 {
  "ch": 2,
  "no": "004",
  "kind": "이벤트",
  "floor": "roomfloor_env_missile"
 },
 {
  "ch": 2,
  "no": "005",
  "kind": "중간보스",
  "floor": "roomfloor_env_holding",
  "layout": "F",
  "layoutKo": "모서리 요새",
  "captain": "robot",
  "minions": 3,
  "minionFrom": [
   "amazon",
   "thug",
   "commando_mg"
  ]
 },
 {
  "ch": 2,
  "no": "006",
  "kind": "전투",
  "floor": "roomfloor_env_missile",
  "layout": "D",
  "layoutKo": "십자 분단",
  "count": 6,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    2.5,
    9.5,
    "BACK",
    "robot",
    "H"
   ],
   [
    2.5,
    5.5,
    "FLANK",
    "roadwarden",
    "T"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    7.5,
    5.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 2,
  "no": "007",
  "kind": "상점",
  "floor": "roomfloor_env_missile"
 },
 {
  "ch": 2,
  "no": "008",
  "kind": "전투",
  "floor": "roomfloor_env_missile",
  "layout": "C",
  "layoutKo": "중앙 요새",
  "count": 7,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    5,
    3,
    "FRONT",
    "guru",
    "H"
   ],
   [
    2.5,
    7.5,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    2.5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    7.5,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    5,
    1.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    10,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ]
  ]
 },
 {
  "ch": 2,
  "no": "009",
  "kind": "엘리트",
  "floor": "roomfloor_env_missile",
  "layout": "K",
  "layoutKo": "사선 분단",
  "count": 7,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    7,
    "FLANK",
    "white_wizard",
    "H"
   ],
   [
    8,
    7,
    "RANGED",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    2,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    3,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    8.5,
    1.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    5.5,
    7.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ],
  "elite": "dragon_blue"
 },
 {
  "ch": 2,
  "no": "010",
  "kind": "보스",
  "floor": "roomfloor_crusher",
  "boss": "crusher"
 },
 {
  "ch": 3,
  "no": "001",
  "kind": "전투",
  "floor": "roomfloor_env_street",
  "layout": "E",
  "layoutKo": "계단",
  "count": 5,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    3,
    6.5,
    "FRONT",
    "snowwoman",
    "H"
   ],
   [
    3.5,
    8.5,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    2,
    4.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    8.5,
    8,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    3.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 3,
  "no": "002",
  "kind": "전투",
  "floor": "roomfloor_env_street",
  "layout": "J",
  "layoutKo": "엇갈린 문",
  "count": 6,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    8,
    5.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    3,
    7.5,
    "RANGED",
    "amazon",
    "H"
   ],
   [
    6.5,
    10,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    7,
    7.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    8,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ]
  ]
 },
 {
  "ch": 3,
  "no": "003",
  "kind": "전투",
  "floor": "roomfloor_env_street",
  "layout": "D",
  "layoutKo": "십자 분단",
  "count": 6,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    2.5,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    2.5,
    5.5,
    "FLANK",
    "snowwoman",
    "H"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    7.5,
    5.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 3,
  "no": "004",
  "kind": "이벤트",
  "floor": "roomfloor_env_street"
 },
 {
  "ch": 3,
  "no": "005",
  "kind": "중간보스",
  "floor": "roomfloor_env_holding",
  "layout": "F",
  "layoutKo": "모서리 요새",
  "captain": "vampire",
  "minions": 3,
  "minionFrom": [
   "snowwoman",
   "ninja",
   "amazon"
  ]
 },
 {
  "ch": 3,
  "no": "006",
  "kind": "전투",
  "floor": "roomfloor_env_street",
  "layout": "C",
  "layoutKo": "중앙 요새",
  "count": 7,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    5,
    3,
    "FRONT",
    "vampire",
    "H"
   ],
   [
    2.5,
    7.5,
    "RANGED",
    "actor_enforcer",
    "T"
   ],
   [
    2.5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    7.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    1.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    10,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ]
  ]
 },
 {
  "ch": 3,
  "no": "007",
  "kind": "상점",
  "floor": "roomfloor_env_street"
 },
 {
  "ch": 3,
  "no": "008",
  "kind": "전투",
  "floor": "roomfloor_env_street",
  "layout": "K",
  "layoutKo": "사선 분단",
  "count": 7,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    7,
    "FLANK",
    "ninja",
    "H"
   ],
   [
    8,
    7,
    "RANGED",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    2,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    3,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    8.5,
    1.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5.5,
    7.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 3,
  "no": "009",
  "kind": "엘리트",
  "floor": "roomfloor_env_street",
  "layout": "G",
  "layoutKo": "좁은 문",
  "count": 7,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    8.5,
    "BACK",
    "ninja",
    "H"
   ],
   [
    1.5,
    5.5,
    "FLANK",
    "turret_cross",
    "T"
   ],
   [
    2.5,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    3,
    5.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7,
    5.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ],
  "elite": "ninja"
 },
 {
  "ch": 3,
  "no": "010",
  "kind": "보스",
  "floor": "roomfloor_python",
  "boss": "python"
 },
 {
  "ch": 4,
  "no": "001",
  "kind": "전투",
  "floor": "roomfloor_env_rooftop",
  "layout": "J",
  "layoutKo": "엇갈린 문",
  "count": 6,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    8,
    5.5,
    "FLANK",
    "baseball",
    "H"
   ],
   [
    3,
    7.5,
    "RANGED",
    "hopper_smg",
    "H"
   ],
   [
    6.5,
    10,
    "FLANK",
    "bat",
    "T"
   ],
   [
    7,
    7.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    8,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ]
  ]
 },
 {
  "ch": 4,
  "no": "002",
  "kind": "전투",
  "floor": "roomfloor_env_rooftop",
  "layout": "D",
  "layoutKo": "십자 분단",
  "count": 6,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    2.5,
    9.5,
    "BACK",
    "snowwoman",
    "H"
   ],
   [
    2.5,
    5.5,
    "FLANK",
    "medium",
    "H"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    7.5,
    5.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    5,
    2.5,
    "FRONT",
    "bat",
    "T"
   ]
  ]
 },
 {
  "ch": 4,
  "no": "003",
  "kind": "전투",
  "floor": "roomfloor_env_rooftop",
  "layout": "C",
  "layoutKo": "중앙 요새",
  "count": 7,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    5,
    3,
    "FRONT",
    "white_wizard",
    "H"
   ],
   [
    2.5,
    7.5,
    "RANGED",
    "amazon",
    "H"
   ],
   [
    2.5,
    2.5,
    "FRONT",
    "bat",
    "T"
   ],
   [
    7.5,
    7.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    1.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    10,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ]
  ]
 },
 {
  "ch": 4,
  "no": "004",
  "kind": "이벤트",
  "floor": "roomfloor_env_rooftop"
 },
 {
  "ch": 4,
  "no": "005",
  "kind": "중간보스",
  "floor": "roomfloor_env_holding",
  "layout": "F",
  "layoutKo": "모서리 요새",
  "captain": "dragon_blue",
  "minions": 3,
  "minionFrom": [
   "baseball",
   "snowwoman",
   "white_wizard"
  ]
 },
 {
  "ch": 4,
  "no": "006",
  "kind": "전투",
  "floor": "roomfloor_env_rooftop",
  "layout": "K",
  "layoutKo": "사선 분단",
  "count": 7,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    7,
    "FLANK",
    "dragon_blue",
    "H"
   ],
   [
    8,
    7,
    "RANGED",
    "thug",
    "H"
   ],
   [
    6.5,
    2,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    3,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    8.5,
    1.5,
    "FLANK",
    "bat",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5.5,
    7.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 4,
  "no": "007",
  "kind": "상점",
  "floor": "roomfloor_env_rooftop"
 },
 {
  "ch": 4,
  "no": "008",
  "kind": "전투",
  "floor": "roomfloor_env_rooftop",
  "layout": "G",
  "layoutKo": "좁은 문",
  "count": 7,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    8.5,
    "BACK",
    "ninja",
    "H"
   ],
   [
    1.5,
    5.5,
    "FLANK",
    "commando_laser",
    "H"
   ],
   [
    2.5,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    3,
    5.5,
    "FRONT",
    "bat",
    "T"
   ],
   [
    7,
    5.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 4,
  "no": "009",
  "kind": "엘리트",
  "floor": "roomfloor_env_rooftop",
  "layout": "H",
  "layoutKo": "가시밭",
  "count": 8,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    3,
    3,
    "FRONT",
    "ninja_chain",
    "H"
   ],
   [
    5,
    6,
    "RANGED",
    "actor_enforcer",
    "T"
   ],
   [
    7,
    3,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    8.5,
    "RANGED",
    "roadwarden",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    1.5,
    8.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    8.5,
    8.5,
    "FLANK",
    "bat",
    "T"
   ],
   [
    2.5,
    6.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ]
  ],
  "elite": "guru"
 },
 {
  "ch": 4,
  "no": "010",
  "kind": "보스",
  "floor": "roomfloor_sludge",
  "boss": "sludge"
 },
 {
  "ch": 5,
  "no": "001",
  "kind": "전투",
  "floor": "roomfloor_env_lab",
  "layout": "D",
  "layoutKo": "십자 분단",
  "count": 6,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    2.5,
    9.5,
    "BACK",
    "hopper_smg",
    "H"
   ],
   [
    2.5,
    5.5,
    "FLANK",
    "snowwoman",
    "H"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    7.5,
    5.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    5,
    2.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ]
 },
 {
  "ch": 5,
  "no": "002",
  "kind": "전투",
  "floor": "roomfloor_env_lab",
  "layout": "C",
  "layoutKo": "중앙 요새",
  "count": 7,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    5,
    3,
    "FRONT",
    "commando_laser",
    "H"
   ],
   [
    2.5,
    7.5,
    "RANGED",
    "guru",
    "H"
   ],
   [
    2.5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    7.5,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    5,
    1.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    7.5,
    10,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ]
  ]
 },
 {
  "ch": 5,
  "no": "003",
  "kind": "전투",
  "floor": "roomfloor_env_lab",
  "layout": "K",
  "layoutKo": "사선 분단",
  "count": 7,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    7,
    "FLANK",
    "white_wizard",
    "H"
   ],
   [
    8,
    7,
    "RANGED",
    "commando_grenade",
    "H"
   ],
   [
    6.5,
    2,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    5,
    3,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    8.5,
    1.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    5.5,
    7.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ]
 },
 {
  "ch": 5,
  "no": "004",
  "kind": "이벤트",
  "floor": "roomfloor_env_lab"
 },
 {
  "ch": 5,
  "no": "005",
  "kind": "중간보스",
  "floor": "roomfloor_env_holding",
  "layout": "F",
  "layoutKo": "모서리 요새",
  "captain": "amazon_elite",
  "minions": 3,
  "minionFrom": [
   "hopper_smg",
   "commando_laser",
   "white_wizard"
  ]
 },
 {
  "ch": 5,
  "no": "006",
  "kind": "전투",
  "floor": "roomfloor_env_lab",
  "layout": "G",
  "layoutKo": "좁은 문",
  "count": 7,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    8.5,
    "BACK",
    "amazon_elite",
    "H"
   ],
   [
    1.5,
    5.5,
    "FLANK",
    "ninja_chain",
    "H"
   ],
   [
    2.5,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    3,
    5.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    7,
    5.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 5,
  "no": "007",
  "kind": "상점",
  "floor": "roomfloor_env_lab"
 },
 {
  "ch": 5,
  "no": "008",
  "kind": "전투",
  "floor": "roomfloor_env_lab",
  "layout": "H",
  "layoutKo": "가시밭",
  "count": 8,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    3,
    3,
    "FRONT",
    "ninja",
    "H"
   ],
   [
    5,
    6,
    "RANGED",
    "dragon_blue",
    "H"
   ],
   [
    7,
    3,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    5,
    8.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    1.5,
    8.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    8.5,
    8.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    2.5,
    6.5,
    "FLANK",
    "skeleton",
    "T"
   ]
  ]
 },
 {
  "ch": 5,
  "no": "009",
  "kind": "엘리트",
  "floor": "roomfloor_env_lab",
  "layout": "L",
  "layoutKo": "네 귀퉁이 가시",
  "count": 8,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    6.5,
    "FLANK",
    "commando_missile",
    "H"
   ],
   [
    5,
    8,
    "RANGED",
    "robot",
    "H"
   ],
   [
    8,
    6.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    6.5,
    7.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    3.5,
    10,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    3.5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    2.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ],
  "elite": "hopper_smg"
 },
 {
  "ch": 5,
  "no": "010",
  "kind": "보스",
  "floor": "roomfloor_guardian",
  "boss": "guardian"
 },
 {
  "ch": 6,
  "no": "001",
  "kind": "전투",
  "floor": "roomfloor_env_refinery",
  "layout": "C",
  "layoutKo": "중앙 요새",
  "count": 7,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    5,
    3,
    "FRONT",
    "commando_grenade",
    "H"
   ],
   [
    2.5,
    7.5,
    "RANGED",
    "commando_mg",
    "H"
   ],
   [
    2.5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    7.5,
    7.5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    5,
    1.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    7.5,
    10,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    5,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ]
  ]
 },
 {
  "ch": 6,
  "no": "002",
  "kind": "전투",
  "floor": "roomfloor_env_refinery",
  "layout": "K",
  "layoutKo": "사선 분단",
  "count": 7,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    7,
    "FLANK",
    "snowwoman",
    "H"
   ],
   [
    8,
    7,
    "RANGED",
    "guru",
    "H"
   ],
   [
    6.5,
    2,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    5,
    3,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    8.5,
    1.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    5.5,
    7.5,
    "FRONT",
    "skeleton",
    "T"
   ]
  ]
 },
 {
  "ch": 6,
  "no": "003",
  "kind": "전투",
  "floor": "roomfloor_env_refinery",
  "layout": "G",
  "layoutKo": "좁은 문",
  "count": 8,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    8.5,
    "BACK",
    "thug",
    "H"
   ],
   [
    1.5,
    5.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    2.5,
    9.5,
    "BACK",
    "turret_cross",
    "T"
   ],
   [
    7.5,
    9.5,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    3,
    5.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    7,
    5.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ],
   [
    5,
    6.5,
    "RANGED",
    "coilwalker",
    "T"
   ]
  ]
 },
 {
  "ch": 6,
  "no": "004",
  "kind": "이벤트",
  "floor": "roomfloor_env_refinery"
 },
 {
  "ch": 6,
  "no": "005",
  "kind": "중간보스",
  "floor": "roomfloor_env_holding",
  "layout": "F",
  "layoutKo": "모서리 요새",
  "captain": "medium",
  "minions": 3,
  "minionFrom": [
   "commando_grenade",
   "snowwoman",
   "thug"
  ]
 },
 {
  "ch": 6,
  "no": "006",
  "kind": "전투",
  "floor": "roomfloor_env_refinery",
  "layout": "H",
  "layoutKo": "가시밭",
  "count": 8,
  "comp": "FRONTLINE_PLUS_RANGED",
  "spawn": [
   [
    3,
    3,
    "FRONT",
    "medium",
    "H"
   ],
   [
    5,
    6,
    "RANGED",
    "skeleton",
    "T"
   ],
   [
    7,
    3,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    5,
    8.5,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    5,
    10,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    1.5,
    8.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    8.5,
    8.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    2.5,
    6.5,
    "FLANK",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 6,
  "no": "007",
  "kind": "상점",
  "floor": "roomfloor_env_refinery"
 },
 {
  "ch": 6,
  "no": "008",
  "kind": "전투",
  "floor": "roomfloor_env_refinery",
  "layout": "L",
  "layoutKo": "네 귀퉁이 가시",
  "count": 8,
  "comp": "FLANK_REINFORCEMENT_PLUS_ZONER",
  "spawn": [
   [
    2,
    6.5,
    "FLANK",
    "amazon_elite",
    "H"
   ],
   [
    5,
    8,
    "RANGED",
    "skeleton",
    "T"
   ],
   [
    8,
    6.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    6.5,
    7.5,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    3.5,
    10,
    "BACK",
    "roadwarden",
    "T"
   ],
   [
    6.5,
    10,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    3.5,
    2.5,
    "FRONT",
    "skeleton",
    "T"
   ],
   [
    6.5,
    2.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ]
 },
 {
  "ch": 6,
  "no": "009",
  "kind": "엘리트",
  "floor": "roomfloor_env_refinery",
  "layout": "I",
  "layoutKo": "회전 관문",
  "count": 8,
  "comp": "BACKLINE_PRESSURE_PLUS_HUNTER",
  "spawn": [
   [
    5,
    10,
    "BACK",
    "robot",
    "H"
   ],
   [
    2,
    7.5,
    "FLANK",
    "coilwalker",
    "T"
   ],
   [
    3.5,
    8,
    "BACK",
    "coilwalker",
    "T"
   ],
   [
    8,
    7.5,
    "FLANK",
    "skeleton",
    "T"
   ],
   [
    6.5,
    8,
    "FLANK",
    "actor_enforcer",
    "T"
   ],
   [
    2,
    5,
    "RANGED",
    "coilwalker",
    "T"
   ],
   [
    8,
    5,
    "RANGED",
    "turret_cross",
    "T"
   ],
   [
    3.5,
    3.5,
    "FRONT",
    "actor_enforcer",
    "T"
   ]
  ],
  "elite": "vampire"
 },
 {
  "ch": 6,
  "no": "010",
  "kind": "보스",
  "floor": "roomfloor_kingpin",
  "boss": "kingpin"
 }
];

module.exports = { ROOMS, DEBUT, TRASH_POOL, HOSTS_IN_ROOM, ELITE_RULE };
