$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 봉쇄된 저장기지 — 7~12챕터 변형 무대(1~6챕터 「미사일 기지」를 다시 쓰는 챕터). 바닥 물건 **4개**를 한 장에: ① 가시판 ② 가시판(다른 배열) ③ 낮은 울타리 ④ 낮은 울타리(다른 모양).
[모양 · 재질 · 틀 정답] 1~6챕터용 같은 부품: 아래 [바탕] 파일 — **같은 그림의 색 변형**이다. 구도 · 재질 · 칸 · 띠 자리는 그대로 두고, 벽 장식 한두 개만 이 챕터답게 바꿔도 된다.
[색 정답] Projects/AVSR/_exchange/in/mock_simple_obj_ch8.png (PD 승인 시안 — 바닥 · 벽 색) · 대표 색: 회녹(어두운 회색빛 초록)
  PD 「오브젝트 종류가 중요하지 배경은 약간만 달라도 돼」 — 크게 바꾸지 말고 색 · 분위기만 이 챕터로 옮긴다.
[같이 놓이는 것] Projects/AVSR/_exchange/bg_apply/missilev2/obj_missilev2_block_1.png ~ _5.png (이 챕터 블록 — 함께 어울려야 한다)
[바탕] Projects/AVSR/_exchange/in/bg_missile_hazards.png (1~6챕터용 — 모양 · 크기 · 배치 그대로, 색 · 재질만 이 챕터로)
- 바탕은 완전한 분홍(#FF00FF), 넷을 한 줄로 넉넉히 띄운다. 물건 하나 약 200 px 폭. 그림자 판 금지.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_missilev2_hazards.png (PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
