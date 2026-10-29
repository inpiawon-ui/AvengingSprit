$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 최후의 옥상 — 7~12챕터 변형 무대(1~6챕터 「옥상」를 다시 쓰는 챕터). 전투 방 **아래쪽 바깥**(입구 밑) 그림 한 장.
[모양 · 재질 · 틀 정답] 1~6챕터용 같은 부품: 아래 [바탕] 파일 — **같은 그림의 색 변형**이다. 구도 · 재질 · 칸 · 띠 자리는 그대로 두고, 벽 장식 한두 개만 이 챕터답게 바꿔도 된다.
[색 정답] Projects/AVSR/_exchange/in/mock_simple_obj_ch10.png (PD 승인 시안 — 바닥 · 벽 색) · 대표 색: 한밤 남보라
  PD 「오브젝트 종류가 중요하지 배경은 약간만 달라도 돼」 — 크게 바꾸지 말고 색 · 분위기만 이 챕터로 옮긴다.
[같이 놓이는 것] Projects/AVSR/_exchange/bg_apply/rooftopv2/obj_rooftopv2_block_1.png ~ _5.png (이 챕터 블록 — 함께 어울려야 한다)
[바탕] Projects/AVSR/_exchange/bg_apply/rooftop/roomapron_rooftop.png
- 크기 정확히 **720 x 540**. 위 끝은 이 챕터 바닥의 아래 벽 띠와 같은 색으로 이어지고 아래로 갈수록 어두워진다. 가운데 입구 길은 비운다.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_rooftopv2_apron.png (720 x 540, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
