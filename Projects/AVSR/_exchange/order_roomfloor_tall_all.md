# 발주 — 방 바닥을 세로로 길게: 나머지 11장 (720 × 1152)

샘플 쓰레기장이 통과했다. **같은 방식으로 아래 11장**을 한 번에 한다.

통과한 샘플(반드시 먼저 열어 볼 것): `Projects/AVSR/_exchange/in/_sample_roomfloor_env_junkyard_tall.png` (720 × 1152)
— 원본 `Assets/BundleResource/RoomFloor/roomfloor_env_junkyard.png` (720 × 936) 과 나란히 보면 무엇을 했는지 보인다.

## 왜 필요한가

방을 세로로 13 m → **16 m** 로 늘린다. 방 안에 적·장애물을 더 넓게 배치하기 위해서다.
바닥 그림을 그대로 늘리면 픽셀아트가 세로로 늘어나 줄무늬가 생긴다 — **바닥 무늬를 이어 그려서** 늘린다.

## 대상 (원본: `Assets/BundleResource/RoomFloor/` — 전부 720 × 936)

| 파일 |
|---|
| `roomfloor_env_street.png` |
| `roomfloor_env_refinery.png` |
| `roomfloor_env_missile.png` |
| `roomfloor_env_lab.png` |
| `roomfloor_env_holding.png` |
| `roomfloor_env_rooftop.png` |
| `roomfloor_robot_snakes.png` |
| `roomfloor_crusher.png` |
| `roomfloor_kingpin.png` |
| `roomfloor_guardian.png` |
| `roomfloor_sludge.png` |

## 무엇을 바꾸나 — 샘플과 같다

**세로만 936 → 1152 px. 늘리는 방법은 이어 그리기다.**

| 부분 | 어떻게 |
|---|---|
| 위쪽 문 자리 · 윗벽 (위에서 약 180 px) | **원본 그대로** 맨 위에 둔다 |
| 아래쪽 담장 · 벽 (아래에서 약 160 px) | **원본 그대로** 맨 아래에 둔다 |
| 그 사이 가운데 바닥 | 원본 바닥 무늬를 **같은 크기로 이어서** 216 px 더 채운다 |
| 좌우 벽 띠 | 원본처럼 위에서 아래까지 이어진다 |

- ⚠ **늘리지(스트레치) 마라.** 픽셀 크기가 원본과 같아야 한다
- 가운데에 큰 무늬(원형 문양·과녁·표식)가 있는 판은 그 무늬를 **늘리지 말고 원래 크기로 가운데에** 두고,
  남는 위아래를 바닥 무늬로 채운다
- 이음매·반복 티가 나지 않게, 명도는 원본과 같게, 새 소품·글자 금지

## 규격

| 항목 | 값 |
|---|---|
| 파일명 | 원본과 **같은 이름** |
| 크기 | **720 × 1152 px** |
| 형식 | PNG, 완전 불투명(RGB). 마젠타·알파 쓰지 마라 |
| 화풍 | 원본과 같은 픽셀 크기 · 같은 팔레트 · 같은 명도 |

## 검수 기준

1. 720 × 1152, 불투명
2. 위 180 px · 아래 160 px 가 원본과 같다
3. 가운데 늘어난 바닥의 무늬 크기가 원본과 같다

## 내보내는 곳

`Projects/AVSR/_exchange/in/` 에 **원본과 같은 파일명**으로 저장한다.
그 폴더가 잠겨 있으면 `Projects/AVSR/_exchange/in/_generated/` 에 저장한다.

11장을 **하나씩 차례로** 만든다. 한 장 끝날 때마다 저장하고 다음 장으로 넘어간다.

API 키를 쓰는 CLI 폴백은 쓰지 마라.
