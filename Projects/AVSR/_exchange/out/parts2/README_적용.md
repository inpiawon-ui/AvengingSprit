# 상점 · 소환 · 패스 · 출전 부품 — 유니티 적용 안내 (2026-10-15)

> PD 「나머지 부품도 한꺼번에 발주 · 다른 세션에서 유니티 작업 끝나면 바로 적용할 수 있게 다 뽑아 줘」.
> 부품은 전부 잘라서 배경을 뚫고 최종 이름까지 붙여 여기에 모아 뒀다.
> **2026-10-15 적용 끝** — 반영 툴로 159장 넣음 · 원본 압축 없음 · 새 아틀라스 5개 ASTC 4x4 · `resource_audit.py` 새 파일 경고 0.
> 다시 납품되면 같은 메뉴(또는 `StorePartsImporter.Apply`)를 다시 돌리면 바뀐 것만 들어간다.
> 도구: 발주서 `Tools/order_store_parts2.py` · 자르기 `Tools/cut_store_parts2.py` · 모음판 `Tools/board_store_parts2.py`

## 적용 순서 (유니티가 비면 — 3분)

1. (끝남) 반영 툴은 `Assets/Scripts/Editor/StorePartsImporter.cs` 에 있다
2. 컴파일 오류 0 확인
3. 메뉴 `Tools/Game/상점 · 소환 · 패스 부품 넣기`
   - 해시로 대조해 바뀐 것만 복사 → Sprite · Point · 밉맵 끔 · R/W 끔
   - 새 아틀라스 `store` · `summon` · `summonfx` · `pass` · `profile` 생성 → 안드로이드 ASTC 4x4 · 2048 · 여백 4 · Addressable `atlas/<이름>`
4. 점검: `python Projects/AVSR/Tools/resource_audit.py` — 새 파일에 경고가 없어야 한다

## 폴더 → 들어갈 곳

| 납품 폴더(`out/`) | 프로젝트 폴더 | 아틀라스 |
|---|---|---|
| `parts1/` (카드 키트 10종) · `parts2/startmode/` | `Assets/BaseResource/ChapterHost/` | 지금 카드 부품과 같은 방식(아틀라스 밖, 프리팹 직접 참조) |
| `parts2/bottombar/` · `parts2/topbar/` | `Assets/BaseResource/LobbyMainUI/Nav/` | `lobbymainui`(폴더째 물려 있어 자동 포함) |
| `parts2/shop_a/` · `shop_b/` · `popups/` | `Assets/BaseResource/Store/` | `store` (새로) |
| `parts2/products_gem/` · `products_misc/` | `Assets/BaseResource/Store/Products/` | `store` |
| `parts2/summon_bg/` · `summon_ui/` | `Assets/BaseResource/Summon/` | `summon` (새로) |
| `parts2/fx_*/` (소환 연출 프레임) | `Assets/BaseResource/SummonFx/` | `summonfx` (새로) — **가산 재질로 그린다**(검정 = 투명) |
| `parts2/pass/` · `mission_icons/` | `Assets/BaseResource/Pass/` | `pass` (새로) |
| `parts2/profile/` | `Assets/BaseResource/Profile/` | `profile` (새로) |

## 이름 규칙

- 파일 이름 = 프리팹 GameObject 이름 = 바인딩 키 (Planning_Flow 접착제 규약). 목록은 `../../parts2_manifest.json`.
- `_part01` 처럼 `_` 로 시작하는 파일은 **개수가 목록과 안 맞아 이름을 못 붙인 조각**이다 — 툴이 건너뛴다.
- 소환 연출 프레임은 `fx_<이름>_01 ~ 06(08)` — 재생 순서 그대로.

## 검수 시트

`Projects/AVSR/_exchange/ref/store_mock/check_p2_*.png` — 체크무늬 위에 얹어 뚫림 · 이름 · 크기를 한눈에.

## 반려 기록

- 소환 대박 빛살 · 불꽃 갈래 1차 — 빛 둘레를 갈색 · 보라 반투명 안개로 채워 가산으로 얹으면 탁한 원반이 됐다. 「빛 사이는 순수 검정」으로 다시 받아 통과. 반려본: `ref/store_mock/rejected_p2_fx_*_v1.png`

## 쓸 때 메모

- 계약 칸 판(`card_slot_small · card_slot_big · chapter_pack_plate`)에 유령이 그려져 있다 — 칸에서는 판의 유령을 그림으로 쓰고, `prod_card_*` 는 팝업에서만 쓴다.
- `row_dim` 은 불투명 남색 — Image 색 알파 0.6 으로 덮는다.
- 소환 연출 `fx_*` 는 가산 재질로 그린다. 기둥 · 갈래는 바닥 맞춤, 대박 · 뒤집기 · 정수는 가운데 맞춤으로 칸 크기를 통일해 뒀다.
