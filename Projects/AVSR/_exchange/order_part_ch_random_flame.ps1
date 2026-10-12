$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 로비 「?(랜덤 선택)」 카드의 **유령 불꽃 문장** 부품 — PD 가 고른 시안 Projects/AVSR/_exchange/in/mock_random_cardback.png 의 그 문장.
[기준 그림] Projects/AVSR/_exchange/ref/hostinfo_mock/anchor_flame_card.png · anchor_flame_pane.png — 이 모양 · 화풍 · 색 그대로:
  푸른(하늘색~짙은 파랑) 유령 불꽃이 위로 솟고, 가운데 어두운 남색 구멍 안에 굵은 흰 「?」 도트 글자, 바깥 테두리는 진한 남색 도트 외곽선.
만들 것: **6장 흔들림 반복(루프)** — 불꽃 끝이 일렁이며 1→6→1 로 끊김 없이 이어지게. 「?」 와 몸통 자리는 6장 모두 같은 곳에 고정(흔들리는 것은 불꽃 끝뿐).
- 한 장 크기 256x320, 6장을 **가로로 한 줄**(전체 1536x320). 배경 **투명**(알파 PNG).
- 빛 번짐 · 후광 · 그림자 · 반짝 별은 넣지 않는다(빛은 게임에서 따로 준다). 불꽃 자체의 밝은 도트 결은 그대로.
[저장] Projects/AVSR/_exchange/in/ch_random_flame_sheet.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
