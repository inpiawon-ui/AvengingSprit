$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 정유소 (7~12챕터 변형 무대) — 전투 방 **아래쪽 바깥**(입구 밑) 그림 한 장. **지금 그림을 고쳐 다시 그리는 것**이다.
[지금 그림 — 화풍 · 색 · 물건 정답] Projects/AVSR/_exchange/bg_apply/refineryv2/roomapron_refineryv2.png
  이 그림의 화풍 · 색 · 도트 굵기 · 좌우 물건 · 아래로 갈수록 어두워지는 것을 **그대로** 따른다. 바꾸는 것은 위 끝 하나뿐이다.
[가장 중요 — 위 끝에 띠 금지] 이 그림 **바로 위에는 방 바닥 그림의 아래 벽 띠(높이 108 px — 난간 · 램프 · 패널)가 이미 붙어 있다**(참고 크롭: Projects/AVSR/_exchange/ref/bg_order/fix1030_band_refineryv2.png).
  그래서 이 그림의 **맨 윗줄(y=0)부터 곧바로 방 밖 바닥**(복도 · 바깥 바닥 판)으로 시작한다. 위 끝에 난간 · 벽 띠 · 램프 줄 · 패널 띠 · 테두리 선을 **절대 그리지 마라** —
  10-29 판은 위 끝에 띠가 또 있어 게임에서 난간이 두 겹으로 보였다(캡처: Projects/AVSR/_exchange/bg_apply/game_shot/ch01.png 화면 가운데 아래의 겹친 난간 두 줄).
  위 끝 색 · 밝기는 참고 크롭 띠 아래쪽 바닥과 자연스럽게 이어지게.
- 지금 그림에서 위 끝 난간 · 벽 띠가 있던 자리는 그 아래 바닥 판 결을 위로 이어 채운다. 좌우 물건은 위 끝에 닿아도 되지만 가로로 이어지는 띠는 안 된다.
- 크기 정확히 **720 x 540**. 가운데는 입구 길이라 비워 둔다.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bgfix_refineryv2_apron.png (720 x 540, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
