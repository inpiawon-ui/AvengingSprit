# AVSR — 인게임 콘텐츠 세부 기획 (Stage 2)

> 상위: [`AVSR_GameComposition.md`](AVSR_GameComposition.md) 콘텐츠 1~4 (`P0`)
> 확정 근거: [`AVSR_Decisions.md`](AVSR_Decisions.md)
> 레이아웃 단일 출처: `_layout_ingame.py` → `UISpec/_layout_InGame.json`

---

## 1. 목업 채택

| 목업 | 비율 | 채택 |
|------|------|:---:|
| `ingame_concept.jpeg` | 스펙 시트 (원형 조이스틱·원형 버튼) | 규칙·플로우 참조용 |
| `ingame_hd_scene.jpeg` | **576×1024 = 9:16 네이티브** (사각 D-패드·사각 버튼) | ✅ **레이아웃 정본** |

GameComposition 미결 항목 **#8 (인게임 조작 UI 형태 — 두 목업이 서로 다름)** 을 이것으로 닫는다.
근거: 9:16 네이티브라 비율 변환 없이 **균일 ×1.25** 로 끝나고, 완성도가 더 높다.

---

## 2. 진행 단위

```
CHAPTER(3) > STAGE(30/챕터) > ROOM(6/스테이지)
```

룸 입장 → 오토어택 교전 → [호스트 사망] 고스트 복귀 → 재빙의
→ 전멸 → 룸 클리어 → 다음 룸 → (6번째 룸) **보스** → 스테이지 클리어

- 1~5 룸: 잡몹 4~7기 (룸이 깊을수록 증가)
- 6 룸: 보스 1기. 보스는 **빙의 대상이 아니다**
- 클리어 = 골드 + 고스트 EXP 지급 후 로비 복귀. **실패는 진행도를 올리지 않는다**

---

## 3. 코어 규칙

| # | 규칙 | 구현 |
|---|------|------|
| 1 | **빙의** — 고스트가 적에게 접근하면 `POSSESS` 활성. 누르면 그 적의 몸을 뺏는다 | `BattleDirector.TryPossess` |
| 2 | **오토어택** — 빙의 중에는 사거리 안 최근접 적에게 자동 발사 | `TickPlayer` |
| 3 | **투사체** — 기본 공격은 탄이 날아가 맞아야 피해가 들어간다. **유도하지 않는다** | `Projectile` |
| 4 | **상실** — 호스트 HP 0 → 그 자리에서 고스트로 복귀. 고스트 HP 0 → 스테이지 실패 | `LoseHost` / `Finish(false)` |
| 5 | **얼티밋** — 시간으로 자동 충전. 발동 시 화면 전체 광역 | `TryUltimate` |

**고스트는 공격할 수 없다.** 핵심 동사(`빙의한다 · 싸운다 · 잃는다 · 다시 빙의한다`)를 강제하기 위함이다.
대신 고스트는 **받는 피해가 22%** 로 줄고 이동이 가장 빠르다 — 빙의할 틈을 만들기 위한 수치다.

> 투사체를 유도로 만들면 전탄 명중이 보장되어 이동 조작이 무의미해진다. 그래서 발사 방향 고정이다.

---

## 4. 화면 요소 (요소 이름 = 프리팹 GameObject 이름 = 클라 바인딩 키)

| 그룹 | 요소 | 표시 데이터 |
|------|------|-------------|
| `TopHudGroup` | `GhostHudIcon` · `GhostLabel` · `GhostHpBarBg/Fill` · `GhostHpText` | 고스트 체력 |
| | `HostPortraitFrame` · `HostPortraitImage` · `HostLabel` · `HostHpBarBg/Fill` · `HostHpText` · `HostNameText` | 빙의 중인 호스트. 미빙의 시 숨김 |
| | `BossGroup`(`BossLabel` · `BossHpBarBg/Fill` · `BossHpText`) | 보스 룸에서만 표시 |
| | `GoldIcon`/`GoldText` · `GemIcon`/`GemText` · `PauseButton` · `StageText` | 재화 · 일시정지 · `ROOM n / 6` |
| `RoomField` | `RoomFloor` · `UnitLayer` · `ShotLayer`(런타임 생성) | 전투 필드 |
| `ControlGroup` | `DPadBase` + `DPadKnob` | 이동. 패드 영역 드래그 |
| | `UltimateButton` + `UltimateCooldown` | 충전 완료 시 발동 가능 |
| | `PossessButton` | 빙의 가능 대상이 있을 때만 활성 |

> `ULTIMATE` · `POSSESS` 글자는 **버튼 스프라이트에 이미 들어 있다**(목업 크롭). 별도 TMP 라벨을 얹지 않는다.

---

## 5. 수치

모든 전투 수치는 `GameConfig` SO 한 곳에서 관리한다 (`04_scenes.md` 규약 — 스크립트 하드코딩 금지).
에셋 `Assets/BundleResource/TableData/GameConfig.asset` · 주소 `TableData/GameConfig`.

호스트 테이블의 스탯은 **0~100 표시 스케일**이다. 전투 수치는 `GameConfig` 계수로 환산한다 —
표시값을 건드리지 않고 밸런스만 조정하기 위함이다.

---

## 6. 미결 (`[TBD]`)

| 항목 | 이유 |
|------|------|
| 로그라이크 버프 **3택1** | GameComposition 콘텐츠 5. 룸 클리어 시 선택 UI 미기획 |
| 호스트별 **공격 방식 차등** (근접/원거리/탄속) | 현재 전 호스트 동일 투사체. `HostTable` 에 공격 타입 컬럼 필요 |
| **보스 패턴** | 현재 단일 근접 공격. 챕터 보스 3체 고유 패턴 미기획 |
| 챕터/스테이지별 **룸 구성표** | 현재 룸 인덱스 기반 절차 생성 |
