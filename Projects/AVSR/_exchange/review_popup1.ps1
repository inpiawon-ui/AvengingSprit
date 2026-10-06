$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
디자인 검수를 해 달라. 그림을 새로 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라.

검수할 그림: Projects/AVSR/_exchange/in/popup_mock_v1.png — 모바일 게임(720x1280)의 공통 알림 팝업 시안.
같은 게임의 지금 UI(이 결과 한 가족으로 보여야 한다):
- Projects/AVSR/_exchange/ref/popup_ref_result_frame.png (결과 창 틀)
- Projects/AVSR/_exchange/ref/popup_ref_chest_frame.png (상자 창 틀)
- Projects/AVSR/_exchange/ref/popup_ref_button_yellow.png (노란 버튼)
- Projects/AVSR/_exchange/ref/popup_ref_lobby.png (로비 화면)

이 팝업은 「골드가 부족합니다」 · 「종료하시겠습니까?」 · 「포기하고 나가시겠습니까?」 같은 1~3줄 알림을 모두 띄운다.
버튼은 확인 하나만 있을 때도 있고 취소 · 확인 둘일 때도 있다. 나중에 틀은 9-slice 로 늘려 쓰고 버튼은 낱장으로 쓴다.

판단해 달라:
1. 지금 UI 와 한 가족으로 보이는가(틀 · 빛 · 버튼 결). 어긋나는 점.
2. 글 3줄 · 버튼 하나일 때도 성립하는 구조인가. 9-slice 로 쪼갤 때 문제가 될 장식(가운데에 걸친 무늬 등).
3. 가독성 — 글자와 버튼이 어두운 뒤 화면 위에서 잘 읽히는가. 뒤 화면 어둡게 정도.
4. 고칠 것을 우선순위 순으로 최대 5개. 「통과」 인지 「고쳐서 다시」 인지 한 줄 결론.

결과를 Projects/AVSR/_exchange/review_popup1.md 에 한국어로 써라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
