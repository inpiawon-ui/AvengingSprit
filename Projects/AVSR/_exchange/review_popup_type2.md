# 팝업 타이포그래피 미리보기 2차 검수 — 한국어·일본어

검수 대상은 `popup_c_type_ko_*.png`, `popup_c_type_ja_*.png` 각 4종이다. 기준은 `consult_popup_typography.md`와 1차 검수 `review_popup_type1.md`이며, 좌표는 `popup_c_layout.py`의 `node()` 값(자식 노드는 부모 기준)으로 적는다.

## 총평

1차 변경 목록은 모두 반영됐다. 회복의 제단 설명은 아이콘과 하단 테두리에서 분리됐고, 악마 본문은 상단 장식선을 벗어났으며, 레벨업 제목은 픽셀 장식으로 복구됐다. 상점 설명도 한국어·일본어 모두 2줄 안에서 읽히고 하단 테두리를 침범하지 않는다.

남은 문제는 일본어 상점의 네 번째 상품 한 곳이다. `ShopItem3Name`의 「体－ギャング」가 최소 크기 18에서도 현재 이름 칸보다 길어 `ShopItem3Price`의 「66 G」와 붙어 보인다. 실제 글자가 잘리지는 않지만 이름과 가격이 한 문자열처럼 읽히므로 통과 조건의 “따로 노는 곳/겹침 없음”에는 미달한다. 같은 묶음은 같은 칸 구조를 써야 하므로 상품 6개의 이름·가격 칸을 함께 조정한다.

## 1. 레벨업 — `BuffChoicePanel`

**한국어·일본어 모두 통과.**

- `BuffTitleText`: 픽셀 폰트가 정상 유지되며 제목 판 안에 충분한 여백이 있다.
- `BuffSubText`, `BuffCard0/1/2ChipText`, `BuffCard0/1/2Name`, `BuffCard0/1/2Desc`: 삐져나감, 테두리 접촉, 겹침이 없다. 이름과 설명의 묶음 크기도 각각 통일돼 있다.
- 좌표·칸 크기·역할·색 및 `ROLES` 변경 없음.

### `LEVEL UP!` 제목 선택

**픽셀 폰트 장식을 유지한다.** 번역되지 않는 9자 영문 표식이라 가이드의 `OriginalPixel` 허용 범위에 들고, 팝업 상단의 기계 장식 및 게임 HUD와 시각적으로 연결된다. 굵은 고딕으로 바꾸면 바로 아래 안내문과 카드 이름의 정보 계층에 섞여 제목의 장식성이 약해진다. 이 예외는 `LEVEL UP!`에만 유지하고 번역되는 CJK 제목에는 확대하지 않는다.

## 2. 회복의 제단 — `ShrinePanel`

**한국어·일본어 모두 통과.**

- `ShrineChoice0/1/2Text`, `ShrineChoice0/1/2Desc`: 아이콘 광원과 글자 사이 간격이 확보됐고, 설명 외곽선도 슬롯 하단 테두리에 닿지 않는다.
- `ShrineTitleText`, `ShrineHintText`: 프레임 및 힌트 필 안에 안정적으로 들어온다.
- 세 선택지의 이름 시작선, 설명 시작선, 역할별 크기가 동일해 목록이 한 묶음으로 보인다.
- 좌표·칸 크기·역할·색 및 `ROLES` 변경 없음.

## 3. 악마 — `EventPanel`

**한국어·일본어 모두 통과.**

- `EventBodyText`: 첫 줄이 제목 아래 장식선과 분리됐고, 마지막 줄과 `EventRewardPill`/`EventCostPill` 사이에도 여백이 있다.
- `EventRewardText`, `EventCostText`: 같은 크기와 기준선으로 묶이며 각 필 테두리에 닿지 않는다.
- `EventAcceptText`, `EventDeclineText`: 일본어의 긴 거절 문구까지 버튼 안에 들어오며 삐져나감이나 테두리 접촉이 없다.
- 좌표·칸 크기·역할·색 및 `ROLES` 변경 없음.

## 4. 상점 — `ShopPanel`

### 남은 문제

- 한국어는 `ShopItem0~5Name`, `ShopItem0~5Price`, `ShopItem0~5Desc` 모두 통과다.
- 일본어 `ShopItem3Name` 「体－ギャング」와 `ShopItem3Price` 「66 G」가 붙어 하나의 문자열처럼 보인다. 이름은 `role='name'`의 최소 크기 18까지 내려간 상태라 글자 크기를 더 줄이면 가이드의 하한을 깨고, `group='shop_name'` 때문에 다른 상품명까지 불필요하게 작아진다.
- 일본어를 포함한 나머지 상품 설명은 2줄 안에 들어가며 슬롯 하단 테두리를 침범하지 않는다. `ShopTitleText`, `ShopGoldText`, `ShopLimitText`, `ShopLeaveText`도 통과다.

### `popup_c_layout.py`에서 바꿀 값

상품 6개에 동일하게 적용한다.

- `ShopItem0Name`~`ShopItem5Name`: `x=76`, `y=5`, `h=30`, `role='name'`, 좌측 정렬, `group='shop_name'`은 유지하고 `w: 116 -> 130`.
- `ShopItem0Price`~`ShopItem5Price`: `y=5`, `h=30`, `role='value'`, `group='shop_price'`는 유지하고 `x: 202 -> 214`, `w: 62 -> 50`.
- 변경 후 이름 칸은 x=206에서 끝나고 가격 칸은 x=214에서 시작해 8px 간격이 생긴다. 가격 칸은 x=264에서 끝나 270px 슬롯 오른쪽에 6px가 남는다. 샘플의 최장 가격 `66 G`도 18px Bold로 50px 안에 들어간다.
- `ShopItem0~5Desc`, 상품 슬롯, 나가기 버튼 및 다른 상점 노드는 변경하지 않는다.
- 색 변경 없음. `ROLES` 표 수정 없음. 특히 `name` 최소 크기 18과 `value` 최소 크기 16은 유지한다.

## Claude가 바로 적용할 변경 목록

1. `ShopItem0Name`~`ShopItem5Name`의 칸을 부모 기준 `x=76, y=5, w=130, h=30`으로 통일한다.
2. `ShopItem0Price`~`ShopItem5Price`의 칸을 부모 기준 `x=214, y=5, w=50, h=30`으로 통일한다.
3. 역할, 폰트 크기, 그룹, 색, 슬롯 크기와 다른 모든 노드는 그대로 둔다. `ROLES`는 수정하지 않는다.
4. 재출력 시 일본어 `ShopItem3Name` 「体－ギャング」와 `ShopItem3Price` 「66 G」 사이의 8px 분리만 확인한다. 그 외 1차 지적 사항은 재수정하지 않는다.

위 한 건 적용 후에는 한국어·일본어 4종 모두 **통과**다.
