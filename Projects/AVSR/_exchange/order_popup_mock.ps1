$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
게임 UI 시안 한 장을 그려라. 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라.

[무엇]
모바일 세로 게임(720 x 1280)의 「공통 알림 팝업」 이 떠 있는 화면 한 장. 다 적용된 모습(시안)이다.
- 뒤: 로비 화면이 어둡게 깔려 있다(Projects/AVSR/_exchange/ref/popup_ref_lobby.png 를 어둡게 한 느낌).
- 가운데: 가로로 넓은 메시지 팝업(화면 폭의 약 78%, 높이는 폭의 절반쯤).
  · 본문 글자: 「골드가 부족합니다」 (한국어, 가운데 정렬)
  · 아래쪽 버튼 둘: 왼쪽 「취소」(어두운 쇠 색 버튼), 오른쪽 「확인」(노란 버튼)

[결 — 지금 게임 UI 와 한 가족으로 보여야 한다]
- 틀은 Projects/AVSR/_exchange/ref/popup_ref_result_frame.png · popup_ref_chest_frame.png 와 같은 결:
  어두운 남색 금속 판, 볼트 · 금속 모서리, 가장자리의 파란 네온 빛. 팝업이 작으니 장식은 그보다 절제한다.
- 「확인」 버튼은 Projects/AVSR/_exchange/ref/popup_ref_button_yellow.png 와 같은 노란 버튼.
- 「취소」 버튼은 같은 모양의 어두운 쇠 · 남색 버튼.
- 지금 쓰는 팝업(popup_ref_current.png)은 색 상자뿐이라 바꾸려는 것이다 — 이것을 따라 하지 마라.
- 원작 유령 캐릭터를 장식으로 쓰지 않는다(이 팝업에는 캐릭터를 넣지 않는다).
- 색 · 디자인 세부는 너에게 맡긴다.

[나중에 부품으로 쪼갠다]
- 틀은 가운데가 비어 있고 가장자리만 장식이 있게 — 나중에 9-slice 로 늘려 쓴다(글 줄 수에 따라 높이가 바뀐다).
- 버튼 두 개는 같은 크기 · 같은 모양.

[저장]
Projects/AVSR/_exchange/in/popup_mock_v1.png (720 x 1280)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
