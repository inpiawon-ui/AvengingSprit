$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 인게임 — **적 호스트가 스킬을 쓸 때 머리 위에 뜨는 작은 스킬 아이콘의 동그라미 테두리** 부품. PD 가 고른 시안 Projects/AVSR/_exchange/in/mock_cast3_flashicon.png 의 그 동그라미.
[기준 그림] Projects/AVSR/_exchange/ref/enemycast_mock/anchor_ring.png (시안 확대) — 이 모양 · 색 그대로:
  안쪽은 아주 어두운 남흑색 원판(아이콘이 그 위에 올라간다), 테두리는 선명한 붉은 고리(도트 2~3칸 굵기), 그 바깥에 진한 검붉은 도트 외곽선 한 줄. 위쪽 테두리에 밝은 붉은 도트 하이라이트 몇 칸.
- 크기 256x256, 원은 가운데 꽉 차게(지름 약 240). 배경 **투명**(알파 PNG). 안쪽 원판은 불투명.
- 아이콘 · 글자 · 빛 번짐 · 후광 · 그림자 · 반짝 별은 넣지 않는다(아이콘은 게임이 따로 얹고, 빛은 게임에서 준다). 테두리만.
[저장] Projects/AVSR/_exchange/in/fx_enemycast_ring.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
