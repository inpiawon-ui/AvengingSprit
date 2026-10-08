# 통과

# 스테이지 클리어 창 타이포그래피 미리보기 검수 — 한국어·일본어

검수 대상은 `popup_c_type_ko_ChapterResultPopup.png`, `popup_c_type_ja_ChapterResultPopup.png`이며, `consult_popup_typography.md`, 2회차 통과 문서 `review_popup_type2.md`, PD 통과 연출 시안 `mock_fxstory_clear_v2.png`를 기준으로 확인했다. 좌표는 `popup_c_layout.py`의 `node()` 값이며, 행 안의 자식 노드는 부모 기준이다.

## 총평

한국어·일본어 모두 삐져나감, 테두리 접촉, 글자끼리의 겹침, 계층에서 따로 노는 곳이 없다. 제목, 부제, 두 결과 행, 경고, OK의 굵기와 크기 단계도 명확하다. 일본어 「シルバー宝箱」는 한국어 「은빛 상자」보다 길지만 현재 이름 칸 안에서 한 줄로 들어가고 오른쪽·위아래 안전 여백도 남는다.

따라서 `ChapterResultPopup` 노드의 좌표·칸 크기·역할·색과 `ROLES`의 `result_title`, `hero`, `ok_dark` 값은 **수정하지 않는다.**

## 1. 삐져나감 · 테두리 닿음 · 겹침 · 따로 노는 곳

### 한국어

- `ResultTitleText` 「CHAPTER 1 CLEAR」: 제목 판 중앙에 들어가며 좌우 장식 및 위아래 금속 테두리에 닿지 않는다. 흰 글자와 짙은 외곽선의 경계가 선명해 배경 광원 위에서도 뿌옇지 않다.
- `ResultSubText` 「쓰레기 집적장」: 좁은 부제 판 안에서 중앙 정렬이 안정적이고 테두리 접촉이 없다. 제목과 결과 행 사이의 보조 정보로 읽혀 따로 놀지 않는다.
- `ResultGoldLabelText`, `ResultGoldValueText`: 아이콘과 글자가 겹치지 않는다. 「골드 획득」과 `+1,240`의 좌측 시작선이 같고, 금액만 `hero`로 커져 정보 계층이 분명하다. 행 오른쪽 테두리에도 닿지 않는다.
- `ResultChestNameText` 「은빛 상자」: 상자 그림과 분리되고 행 안에 수직 중앙으로 놓인다. 테두리 접촉 및 잘림이 없다.
- `ResultWarnText` 「상자 칸이 가득 찼습니다」: 경고 아이콘과 겹치지 않고 붉은 경고 바 안에서 한 줄로 끝난다. 위아래 및 오른쪽 테두리에 닿지 않는다.
- `ResultOkText` `OK`: 버튼 중앙에 놓이고 짙은 글자가 노란 버튼에서 충분히 분리된다. 버튼 외곽 장식과 접촉하지 않는다.

### 일본어

- `ResultTitleText`, `ResultSubText`: 한국어와 같은 위치·크기에서 제목 판과 부제 판 안에 안정적으로 들어간다. 제목 외곽선이 선명하며 장식선과 겹치지 않는다.
- `ResultGoldLabelText` 「ゴールド獲得」와 `ResultGoldValueText` `+1,240`: 같은 좌측 시작선을 유지하며 골드 그림 및 행 테두리와 충돌하지 않는다.
- `ResultChestNameText` 「シルバー宝箱」: 한국어보다 길지만 현재 `220 x 56` 칸에서 한 줄로 들어간다. 상자 그림과 글자 사이, 글자 끝과 행 오른쪽 테두리 사이에 모두 여백이 남아 축소나 칸 확장이 필요 없다.
- `ResultWarnText` 「宝箱スロットがいっぱいです」: 경고 아이콘 뒤에서 시작해 한 줄로 끝나며 바 오른쪽 끝과 겹치지 않는다. 글자 외곽선도 위아래 붉은 테두리에 닿지 않는다.
- `ResultOkText` `OK`: 한국어와 동일하게 버튼 중앙 및 대비가 정상이다.

## 2. `popup_c_layout.py` 판정값

바꿀 값은 없다. 아래 값을 그대로 유지한다.

- `ROLES['result_title']`: `(True, 44, 36, 0.35, 0.22, '#07101AF5', '#FFFFFF', 0)` 유지.
- `ROLES['hero']`: `(True, 32, 28, 0.38, 0.16, '#07101AE6', '#F5C044', 0)` 유지.
- `ROLES['ok_dark']`: `(True, 30, 24, 0.30, 0, None, '#2A1A05', 0)` 유지.
- `ResultTitleText`: `x=150, y=402, w=420, h=74`, `role='result_title'`, `color='#FFFFFF'` 유지. (`RF=(30, 220)`을 반영한 `ResultContent` 절대 좌표)
- `ResultSubText`: `x=220, y=485, w=280, h=30`, `role='hint'` 유지.
- `ResultGoldRow`: `x=154, y=554, w=410, h=126` 유지. 자식 `ResultGoldLabelText`는 `176, 16, 220, 34`, `role='name'`; `ResultGoldValueText`는 `176, 54, 220, 56`, `role='hero'` 유지.
- `ResultChestRow`: `x=154, y=686, w=410, h=126` 유지. 자식 `ResultChestNameText`는 `176, 35, 220, 56`, `role='name'` 유지.
- `ResultWarnBar`: `x=154, y=820, w=410, h=42` 유지. 자식 `ResultWarnText`는 `52, 4, 346, 34`, `role='hint'`, `color='#FF8A80'` 유지.
- `ResultOkButton`: `x=215, y=900, w=290, h=76`, `hide=True` 유지. 자식 `ResultOkText`는 `0, 0, 290, 76`, `role='ok_dark'` 유지.

좌표를 움직이거나 일본어만 글자 크기를 줄이면 두 언어의 정렬 기준이 달라지고, 현재 확보된 안전 여백과 역할별 크기 통일을 오히려 해친다.

## 3. 제목 색 판정

**시안의 금빛 제목이 아니라 규칙의 흰색 글자 + 어두운 외곽선을 선택한다.**

시안의 금빛은 클리어 연출의 광원과 보상감을 강조하는 표현이지만, 텍스트 규칙에서 창 제목은 `#FFFFFF`에 가까운 밝은 글자와 `#07101AF5`의 어두운 외곽선으로 이미 정리됐고, PD의 「타이틀이 뿌옇게 보인다」 지적도 이 조합으로 고쳐 통과했다. 다른 4창의 제목과 같은 밝은 제목 계열을 유지해야 팝업 묶음의 통일감이 생기며, 금색은 이 창 안에서 핵심 보상 값 `ResultGoldValueText`에만 남겨 정보 의미도 분명하게 한다.

연출 시안에서는 제목의 **배치·굵기·강조도**를 따르고, 색은 공통 타이포그래피 규칙을 우선한다.

## Claude가 바로 적용할 변경 목록

1. 적용할 코드 변경 없음.
2. `ChapterResultPopup`의 모든 노드 좌표·칸 크기·역할·색을 현재 값 그대로 유지한다.
3. `ROLES['result_title']`, `ROLES['hero']`, `ROLES['ok_dark']`를 현재 값 그대로 유지한다.
4. 제목은 금빛으로 바꾸지 말고 `#FFFFFF` 글자 + `#07101AF5` 외곽선을 유지한다.
5. 일본어 「シルバー宝箱」 대응을 위한 개별 축소·줄바꿈·칸 확장을 하지 않는다.

남은 수정 사항은 없다.
