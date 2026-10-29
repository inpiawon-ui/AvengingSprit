$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 글자를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 **연출 시안 — 3컷 스토리보드** : 스테이지(챕터) 클리어 팝업 — **C 원혼 회로 틀로 새로 그린다**(내용만 Projects/AVSR/_exchange/in/ui_new_chapter_result_v1.png 를 따른다 · 틀은 Projects/AVSR/_exchange/ref/popup_c_ingame/levelup.png 와 같은 흑철 기계 틀 · 금빛 원혼 관로 · 유리관 문법, 글자는 한국어: 「CHAPTER 1 CLEAR」 · 「쓰레기 집적장」 · 「골드 획득 +1,240」 · 「은빛 상자」 · 「상자 칸이 가득 찼습니다」 · 「OK」)
[공통]
- 지금 게임 화면(틀 · 글자 · 배치)은 Projects/AVSR/_exchange/ref/skill_qx_check/robot.png (위 HUD · 아래 조작 버튼은 이 지금 게임 화면 그대로 — 옛 HUD 금지) 그대로 — 그 위에 **연출 · 이펙트를 더한** 모습을 그린다. 틀 디자인을 바꾸지 않는다.
- 글자는 칸 안에 딱 맞게(삐져나가거나 테두리에 걸치면 불합격). 한국어 글자는 기준 캡처 그대로.
- 이펙트는 게임과 같은 도트(픽셀 아트) — 빛 줄기 · 원혼(유령 기운) 흐름 · 도트 입자 · 연기 · 불씨 · 빛 번짐.
  대표 색: 레벨업 금 · 회복 청록 · 악마 진홍 · 상점 호박 · 클리어 금+청백.
- 별 · 표창 · 십자 반짝이 모양 금지. 화면 전체를 한 색으로 물들이지 않는다(빛은 창 둘레 · 대상 둘레에만).
- 3컷을 가로로 나란히(각 컷 720 x 1280, 전체 2160 x 1280), 컷 왼쪽 위에 작은 번호 · 제목 「1 등장」 「2 최대」 「3 …」. 컷마다 화살표 · 짧은 메모로 움직임을 적는다(한국어).
[컷]
- 1 등장 — C 원혼 회로 틀(금빛 + 청백)로 다시 그린 결과 창. 화면 가운데서 금빛 원혼 폭발 · 빛줄기가 퍼지며 「CHAPTER 1 CLEAR」 판이 쾅 박힌다, 위 유령이 날아올라 자리 잡는다
- 2 최대 — 골드 칸: 금화 도트가 쏟아지며 숫자가 0 에서 +1,240 으로 굴러 올라간다. 상자 칸: 은빛 상자가 위에서 떨어져 통 튀고 둘레에 빛 고리가 돈다
- 3 끝 — OK 버튼이 금빛으로 숨쉬고, 창 둘레로 유령 기운 입자가 천천히 떠오른다(별 · 꽃가루 말고 둥근 도트 입자)
[저장] Projects/AVSR/_exchange/in/mock_fxstory_clear_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
