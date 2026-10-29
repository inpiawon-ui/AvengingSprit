$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 미사일 기지 — 전투 방 바닥에 깔리는 **물 칸(웅덩이)** 한 장. 몸은 못 지나가고 탄은 지나가는 칸이다.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/bg_apply/missile/roomfloor_env_missile.png (이 무대의 새 바닥 — 이 위에 깔린다) · Projects/AVSR/_exchange/in/mock_simple2_ch2.png (PD 승인 시안)
[쓰임 정답] Assets/BaseResource/InGameMainUI/obj_missile_water_1.png · obj_missile_water_edge_n.png (지금 게임 물 — 칸 크기 · 둑 두께만 본다. 초록 진흙 · 주황 돌 화풍은 버린다)
- 분홍(#FF00FF) 바탕 가운데에 **위에서 내려다본 직사각형 웅덩이 하나**: 가로 5칸 × 세로 4칸 비율(가로:세로 = 5:4), 칸 하나 약 200 px(웅덩이 약 1000 x 800).
- 웅덩이 **안쪽 가장자리**에 둑(턱)을 두른다: 칸 크기의 약 1/3 두께, 새 바닥 · 벽과 같은 재질(금속 테 · 콘크리트 턱)로 윗면 밝게 + 안쪽 면 어둡게 — 물이 한 단 낮아 보이게.
- 수면: **새 바닥보다 확실히 어둡게(밝기 절반쯤)** 하고 물빛(푸른 · 청록 기운)이 돌아 한눈에 「물 · 구덩이」로 읽혀야 한다 — 바닥과 같은 색이면 철 테두리 두른 바닥으로 보여 실패다(1차 샘플에서 그랬다). 큰 결 하나 · 밝은 잔물결 몇 줄로 단순하게. 쓰레기 · 기름 얼룩 · 돌 · 풀 · 거품 덩어리 금지. 적 탄이 수면 위에서도 또렷이 보여야 한다.
- 웅덩이 네 모서리는 직각(둥글게 깎지 마라). 칸 경계선은 그리지 마라. 분홍은 웅덩이 밖에만.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_missile_water.png (PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
