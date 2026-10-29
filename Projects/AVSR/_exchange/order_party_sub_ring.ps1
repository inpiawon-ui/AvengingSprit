$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — **빙의 파티의 보조 호스트 발밑에 까는 「혼령 고리」** 한 장.
[무엇] 유령이 붙들어 둔 혼(보조 호스트)이라는 표시. 메인 캐릭터에는 안 깐다. 보조 몸은 비치는 보랏빛(#CCADFF 쯤)으로 둥실 떠 있다.
[모양 앵커] Assets/BaseResource/InGameMainUI/affinity_adv_ring.png — 발밑에 눕힌 납작한 타원 고리(가로:세로 = 2:1). 이 **크기 · 납작함 · 도트 굵기**를 따른다. 색과 불꽃 결은 따라 하지 마라(그건 초록 「유리한 몸」 표시라 헷갈리면 안 된다).
[화풍] 보랏빛 · 연한 청보라의 **영혼 불꽃 / 혼 연기 결**이 고리를 따라 위로 살짝 일렁인다. 안쪽은 비어 있다(캐릭터 발이 보여야 한다). 작은 혼 알갱이 2~4개가 고리 위에 떠 있어도 좋다.
- 바탕은 **완전한 검정(#000000)** — 게임에서 더하기(가산)로 깐다. 바탕에 다른 색 · 바닥 · 그림자 금지.
- 테두리 외곽선 금지 · 글자 금지 · 십자 반짝이 금지.
- 크기 320 x 160(게임에서 160 x 80 으로 줄여 쓴다), 고리가 그림 칸의 가로 90% 를 채운다.
[저장] Projects/AVSR/_exchange/in/party_sub_ring.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
