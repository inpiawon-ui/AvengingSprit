$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 5챕터 「유령 연구소」 전투 방 **배경 · 장애물 심플화 시안**(2차 · 6챕터 한 벌).
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다.
[바탕] Projects/AVSR/_exchange/ref/step3_play/ROOM_CH5_019_b.png — 지금 게임 화면(720x1280). 장애물 자리 · HUD · 버튼 · 캐릭터 · 적은 이 바탕 그대로.
[1차에서 고른 방향 — 두 장을 섞는다]
- 바닥 · 정돈: Projects/AVSR/_exchange/in/mock_simple_C_ch1.png — 큰 타일 결 하나, 잔무늬 없음, 장애물은 윗면(밝음) + 앞면(어두움) 덩어리 블록. **완성도만 따르고 밝기 · 색은 따르지 마라.**
- 물건: Projects/AVSR/_exchange/in/mock_simple_F_ch3.png — 이 무대다운 물건(자판기 · 상자 · 드럼통 등)을 칸 하나 = 덩어리 하나로 정리한 방식. 무대 물건은 유령 연구소 에 맞게 바꾼다.
[이 챕터 대표 색] 짙은 청록. 다른 챕터와 색이 겹치면 안 된다. 바닥은 중간보다 어둡게(밝은 바닥은 캐릭터를 묻는다), 채도 낮게.
[적 탄] 6~10발 그려 넣는다. 가장 밝고 또렷한 테두리, 색은 이 챕터 바닥 색의 반대편으로 — 바닥에 절대 묻히지 않게.
- 테마 유지(유령 · 빙의 · 80~90년대 범죄 도시). 궁수의 전설 그림을 따라 그리지 마라. 장애물 밑 단색 그림자 판 금지. 크기 720 x 1280, 메모 넣지 마라.
[저장] Projects/AVSR/_exchange/in/mock_simple2_ch5.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
