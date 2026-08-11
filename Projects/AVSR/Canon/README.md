# Canon — AVSR 정본 (Developer Handoff v1.5)

기획자가 넘긴 최종 설계·데이터 패키지다. **2026-08-11부로 이 폴더가 AVSR 기획의 정본이다.**

원본 ZIP 3종은 `기획 참고자료/`에 있고, 그중 구현에 필요한 것만 여기로 옮겼다
(4K 슬라이드 PNG 17장은 용량 때문에 제외 — 원본 ZIP 참조).

---

## 1. 우선순위 — 문서가 서로 다를 때

정본 자체가 순서를 못 박아 뒀다. 이 순서를 바꾸지 않는다.

| 순위 | 문서 | 지위 |
|------|------|------|
| 1 | **Developer Runtime v1.5** (`Runtime/*.json`) | **권위** — 값이 다르면 이게 이긴다 |
| 2 | Host System v1.2 (`Workbooks/HOST_SYSTEM_*.xlsx`) | 참조 — 호스트 정체성·플레이스타일 |
| 3 | Core Combat v3.1 | 참조 — 전투 철학 |
| 4 | Experience Master v1.2 (`Workbooks/CH01_03_*.xlsx`) | 참조 — 방·경험 의도 |
| 5 | Archive | **쓰지 말 것** |

**기획 워크북에서 수치를 직접 손보지 않는다.** 튜닝은 런타임 쪽에서 하고 변경 이력을 남긴다.

---

## 2. 무엇이 들어 있나

```
Canon/
├── Runtime/          ← 진짜 데이터. 여기서 Unity 로 들어간다
│   ├── CH01_03_RUNTIME_DATA_v1.5.json   34방·18적·12호스트·104스폰
│   ├── DATA_CONTRACT_v1.5.json          위 JSON 의 필드 순서·타입·기본값 스키마
│   └── HANDOFF_VALIDATION_REPORT.json
├── Docs/             ← 읽는 순서는 00_README_FIRST → TECHNICAL_QA_HANDOFF → 나머지
├── Workbooks/        ← 마스터 엑셀(75시트) + 정합성 리포트
└── VisualGuide/      ← 개발자 브리핑 17장의 텍스트·인덱스·컨택트시트
```

`Runtime/*.json` 은 **위치 기반 배열(positional array)** 이다. 필드 이름이 JSON 안에 없고
`DATA_CONTRACT_v1.5.json` 의 `collectionSchemas` 가 순서를 정의한다. 반드시 계약을 보고 역직렬화한다.

```
hosts[0] = ["H01","Gangster","PRECISION","Immediate","None",1,1,"Mark relay"]
           HostID HostName   Role       PossessionType Condition MinCH Enabled MaintainHook
```

---

## 3. 잠긴 값 (constants)

| 키 | 값 | 우리 `GameConfig.asset` |
|---|---|---|
| GhostMaxHP | 100 | 일치 |
| GhostDrainPerSecond | 2 | 일치 |
| DeathPossessionCost | 20 | 일치 (`_emergencyGhostCost`) |
| HostStartHPRatio | 0.7 | 일치 (`_hostStartHpPercent: 70`) |
| StopToAttackDelay | 0.15 | `_attackResumeSeconds: 0.12` — 맞출 것 |
| PossessionChannel | 0.35 | **없음** |
| **TacticalPossessionCost** | **6** | **없음** |
| **TacticalCooldown** | **8** | **없음** |
| GhostMoveSpeed | 5.2 m/s | 우리는 픽셀 단위(320) — 좌표계 환산 필요 |

좌표계도 정본이 정해 뒀다 — 원점 좌하단, 방 **8.4 × 14 m**(보스방 16 m). 우리는 UI 픽셀 좌표를 쓰고 있어
임포터에서 미터 → 픽셀 환산이 필요하다.

---

## 4. 이관 상태

정본은 설계 100% / Unity 구현 0% 로 스스로 선언한다(`Docs/README_START_HERE.md`).
우리 리포는 그 반대로 구현이 먼저 나가 있다. 대조·이관 계획은
[`../AVSR_Decisions.md`](../AVSR_Decisions.md) 10절을 단일 출처로 한다.
