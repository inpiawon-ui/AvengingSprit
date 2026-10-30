$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경의 **게임에 넣을 부품**이다. 실제 서비스할 게임이라 마감 퀄리티가 중요하다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다.
[무대] 미사일 기지 (2챕터 무대) — 전투 방 **아래쪽 바깥**(입구 밑) 그림 한 장. **지금 그림을 고쳐 다시 그리는 것**이다.
[지금 그림 — 화풍 · 색 · 물건 정답] Projects/AVSR/_exchange/bg_apply/apron_fix_1030/roomapron_missile.png
  이 그림의 화풍 · 색 · 도트 굵기 · 좌우 물건 · 바닥 판 결 · 노란 바닥 줄 · 아래로 갈수록 어두워지는 것을 **그대로** 따른다. 바꾸는 것은 위 끝 가운데 하나뿐이다.
[고칠 문제 — 문틀이 이음매에서 끊긴다]
  이 그림 바로 위에는 방 바닥 그림의 아래 벽 띠가 붙는다(참고 크롭: Projects/AVSR/_exchange/ref/bg_order/fix1030_band_missile.png — 720 x 168, 이 크롭의 맨 아랫줄 바로 밑이 이 그림의 y=0).
  띠 가운데의 노랑-검정 줄무늬 문틀(입구 문틀)은 **양쪽 기둥이 아래로 열린 채 띠 끝에서 잘린다.** 지금 방 밖 그림에는 그 아랫부분이 없어서 게임에서 문틀이 뚝 끊겨 보인다.
  지금 모습: Projects/AVSR/_exchange/ref/bg_order/fix1030b_seam_now_missile.png (빨간 네모가 끊긴 곳)
  예전에 문틀이 닫혀 있던 모습(그리는 법 참고): Projects/AVSR/_exchange/ref/bg_order/fix1030b_oldframe_missile.png
[그릴 것]
  - 이 그림 위 끝(y=0)부터 **문틀 양쪽 기둥을 그대로 이어 아래로 내리고**, y 약 40~60 에서 **문틀 아랫변(문턱)으로 닫는다** — 예전 그림처럼 노랑-검정 줄무늬 · 금속 테두리 · 모서리 볼트.
  - 기둥 가로 위치를 띠와 **픽셀 단위로 맞춘다**: 왼쪽 기둥 바깥 테두리 x 약 262 ~ 안쪽 x 약 290, 오른쪽 기둥 안쪽 x 약 430 ~ 바깥 x 약 458. 줄무늬 각도 · 두께 · 색이 띠 안 기둥과 끊김 없이 이어져야 한다.
  - 문틀 안쪽(x 290~430)은 띠 안 문틀 안쪽과 같은 바닥 판 색으로 이어서 채운다.
  - 문틀 **양옆의 관 · 파이프 띠는 그리지 마라** — 띠 안에 이미 있다(두 겹이 되면 안 된다). 문틀 바깥은 그대로 방 밖 바닥 판이다.
  - 문틀 아래는 지금 그림과 같은 바닥 판 · 노란 바닥 줄로 이어진다.
- 크기 정확히 **720 x 540**. 가운데 문틀 아래는 입구 길이라 물건을 놓지 않는다.
- 그림에 빛을 굽지 마라(번지는 광채 · 아우라 · 반짝이 금지). 도트로 그린 작은 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bgfix2_missile_apron.png (720 x 540, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
