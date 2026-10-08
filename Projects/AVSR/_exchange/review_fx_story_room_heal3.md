# 판정: 통과

직전 2회차의 필수 변경 3건이 모두 반영됐다. `room_heal_capsule_glow`는 시안 수준으로 낮아졌고, `room_heal_pillar`는 다가감 구간의 전면 가산광으로 정상 표시되며, `room_heal_absorb`는 제단 아래에서 플레이어까지 이어지는 작동광으로 읽힌다. PD에게 보여 줄 수 없을 만큼 시안과 다른 부분은 없다. 글자·버튼 가림, 별/십자 반짝이, 화면 전체 물들이기도 없다.

## 1컷 — 대기 (1.00초)

### 시안과 다른 점

- 필수 수정이 필요한 차이 없음. 발밑 링, 캡슐 내부광, 상단 steam이 시안처럼 은은한 대기 장식으로 보인다.
- 2회차에서 지적한 캡슐 과광은 해소됐다. 유령 실루엣과 금속 테두리가 광에 묻히지 않는다.

### `layers_room_heal()`에서 바꿀 값

- 변경 없음.
- `room_heal_ring_ripple`: `(x=0, y=140, w=250, h=96, step=0.18, t0=0.00, t1=0, loop=True, alpha=0.42, fade=0.18, add=False, back=True)` 유지.
- `room_heal_capsule_glow`: `(x=0, y=-8, w=138, h=238, step=0.16, t0=0.00, t1=0, loop=True, alpha=0.38, fade=0.18, add=False, back=False)` 유지.
- `room_heal_steam`: `(x=-12, y=-174, w=126, h=150, step=0.16, t0=0.20, t1=0, loop=True, alpha=0.42, fade=0.18, add=False, back=False)` 유지.
- 재발주 없음.

## 2컷 — 다가감 / 최대 (2.00초)

### 시안과 다른 점

- 필수 수정이 필요한 차이 없음. 세로 빛기둥이 실제로 표시되고, 대기 컷보다 반응이 분명하면서도 제단 형태를 지우지 않는다.
- 2회차의 `near + back=True` 문제는 `back=False`로 해소됐다. 밝기도 시안의 최대 상태 범위다.

### `layers_room_heal()`에서 바꿀 값

- 변경 없음.
- `room_heal_pillar`: `(x=0, y=-158, w=124, h=300, step=0.12, t0=0.00, t1=0, loop=True, alpha=0.70, fade=0.08, add=True, back=False, phase='near')` 유지.
- 대기 링·캡슐·steam도 현재 값 그대로 유지한다. 시안보다 밝지 않으므로 추가 감광하지 않는다.
- 재발주 없음.

## 3컷 — 닿음 / 작동 (3.05초)

### 시안과 다른 점

- 필수 수정이 필요한 차이 없음. 사용 완료 제단은 시안처럼 어두워지고, 치유 흡수광은 제단 발밑에서 아래의 플레이어 몸까지 이어진다.
- 작동광은 누를 때만 시작하고 0.35초 안에 끝나므로, 뒤이어 열리는 창의 글자와 선택지를 가리지 않는다.

### `layers_room_heal()`에서 바꿀 값

- 변경 없음.
- `room_heal_absorb`: `(x=0, y=112, w=118, h=250, step=0.08, t0=0.00, t1=0.35, loop=False, alpha=0.90, fade=0.04, add=True, back=False, phase='accept', fromAvatar=False)` 유지.
- `room_heal_ring_ripple`, `room_heal_capsule_glow`, `room_heal_steam`, `room_heal_pillar`가 작동 시 꺼지는 현재 phase 분리도 유지한다.
- 재발주 없음.

## 작동 뒤 회복의 제단 창

- 수정 없음. 제목·설명·세 선택지의 글자와 아이콘이 읽히며 FX가 버튼이나 선택 칸을 가리지 않는다.
- `popup_c_layout.py`의 창 배치와 현재 글자 규칙도 그대로 유지한다.

## Claude가 바로 적용할 변경 목록

1. 적용할 변경 없음.
2. `layers_room_heal()`의 현재 5개 층과 값, 합성 순서, phase 분리를 그대로 유지한다.
3. 부품 재발주 없음.
