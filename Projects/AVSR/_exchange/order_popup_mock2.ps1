$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
게임 UI 시안 한 장을 다시 그려라. 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라.

[지난 시안]
Projects/AVSR/_exchange/in/popup_mock_v1.png — 방향은 통과. 검수 결과 Projects/AVSR/_exchange/review_popup1.md 의 고칠 점 5가지를 반영한다.
1. 3줄 문구가 들어가도 성립하게 — 본문 칸과 버튼 칸을 나누고, 본문은 최대 3줄을 가운데 정렬로 담는다.
2. 늘려 쓸 구간(각 변의 가운데)에는 빛 장식을 두지 않는다. 빛 · 볼트 · 사선 조립부는 네 모서리 안에만 둔다.
3. 테두리 네온을 줄인다 — 금속 틀이 먼저 보이고 파란 빛은 모서리 근처 포인트로만.
4. 「취소」 버튼은 본체보다 한 단계 밝은 쇠 면 + 약한 하이라이트 — 비활성처럼 보이면 안 된다. 「확인」 은 지금 노란 버튼 그대로.
5. 뒤 화면 어둡게는 조금 덜(뒤 로비가 무엇인지 알아볼 만큼). 본문 글자 그림자는 얇게.

[무엇 — 한 장에 두 화면을 나란히]
캔버스 1440 x 1280. 왼쪽 720 x 1280 과 오른쪽 720 x 1280 에 같은 팝업을 한 번씩.
- 왼쪽: 본문 「골드가 부족합니다」(1줄), 버튼 「취소」 · 「확인」 둘
- 오른쪽: 본문 「지금 나가면 이번 판에서 얻은 골드만 받습니다.」 / 「상자와 경험치는 받지 못합니다.」 / 「정말 포기하시겠습니까?」(3줄), 버튼 「확인」 하나(가운데)
- 두 팝업은 폭이 같고, 오른쪽은 3줄만큼 높이만 더 크다(같은 틀을 늘린 것처럼 보여야 한다).
- 뒤: Projects/AVSR/_exchange/ref/popup_ref_lobby.png 로비를 어둡게.
- 참조(같은 가족): Projects/AVSR/_exchange/ref/popup_ref_result_frame.png · popup_ref_chest_frame.png · popup_ref_button_yellow.png
- 캐릭터 장식은 넣지 않는다. 색 · 디자인 세부는 너에게 맡긴다.

[저장]
Projects/AVSR/_exchange/in/popup_mock_v2.png (1440 x 1280)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
