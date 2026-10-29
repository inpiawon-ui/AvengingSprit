$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 불타는 집적장 — 7~12챕터 변형 무대(1~6챕터 「쓰레기 집적장」를 다시 쓰는 챕터). 전투 방 바닥의 **물 칸(웅덩이)** 한 장.
[모양 · 재질 · 틀 정답] 1~6챕터용 같은 부품: 아래 [바탕] 파일 — **같은 그림의 색 변형**이다. 구도 · 재질 · 칸 · 띠 자리는 그대로 두고, 벽 장식 한두 개만 이 챕터답게 바꿔도 된다.
[색 정답] Projects/AVSR/_exchange/in/mock_simple_obj_ch7.png (PD 승인 시안 — 바닥 · 벽 색) · 대표 색: 숯빛 · 불씨 주황 벽 조명
  PD 「오브젝트 종류가 중요하지 배경은 약간만 달라도 돼」 — 크게 바꾸지 말고 색 · 분위기만 이 챕터로 옮긴다.
[같이 놓이는 것] Projects/AVSR/_exchange/bg_apply/junkyardv2/obj_junkyardv2_block_1.png ~ _5.png (이 챕터 블록 — 함께 어울려야 한다)
[바탕] Projects/AVSR/_exchange/in/bg_junkyard_water.png (1~6챕터용 웅덩이 — 크기 · 비율 · 둑 두께 · 구도 그대로)
- 분홍(#FF00FF) 바탕 가운데 직사각형 웅덩이 하나, 가로:세로 = 5:4, 약 1000 x 800. 둑은 안쪽 가장자리에 칸의 약 1/3 두께, 이 챕터 벽 재질 · 색.
- 수면은 이 챕터 바닥보다 **확실히 어둡게(밝기 절반쯤)** 하고 물빛이 돌아 한눈에 물로 읽히게. 웅덩이 밖은 전부 완전한 분홍 한 색 — 번짐 · 그림자 금지. 모서리 직각.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_junkyardv2_water.png (PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
