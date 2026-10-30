$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 실제 서비스할 게임의 배경 부품이다.
[무대] 미사일 기지 변형 무대(7~12챕터, 청회색 바닥) — 전투 방 아래쪽 바깥 그림 한 장.
[지금 그림 — 그대로 따를 것] Projects/AVSR/_exchange/bg_apply/apron_fix_1030/roomapron_missilev2.png
  이 그림의 **청회색** 바닥 색 · 판 결 · 흰 바닥 줄 · 좌우 물건 · 아래로 어두워지는 것을 그대로 둔다. 올리브색 · 국방색으로 바꾸지 마라.
[바꿀 것 — 위 끝 가운데 문틀 아랫부분 하나뿐]
  정답 모양(같은 기지의 다른 무대에서 맞게 고친 것): Projects/AVSR/_exchange/bg_apply/apron_fix_1030b/roomapron_missile.png 의 위 끝 가운데
  (확대: Projects/AVSR/_exchange/ref/bg_order/fix1030b_answer_missile_frame.png).
  이 정답과 **같은 모양 · 같은 위치 · 같은 크기**로 그린다:
  - 위 끝(y=0)에서 노랑-검정 줄무늬 기둥 두 개가 내려오다가 y 약 64~80 에서 금속 문턱(아랫변)으로 닫힌다. 문틀 위쪽 변은 없다(위 끝으로 열려 있다).
  - 왼쪽 기둥 x 약 263~290, 오른쪽 기둥 x 약 430~457.
  - 문틀 안쪽(x 290~430, y 0~64)은 **이 무대 띠의 문틀 안쪽 색(청회색)**: 참고 Projects/AVSR/_exchange/ref/bg_order/fix1030_band_missilev2.png 의 가운데 문틀 안쪽.
  - 문틀 **양옆에 관 · 파이프 · 볼트 판을 그리지 마라.** 문틀 바깥은 지금 그림의 청회색 바닥 그대로.
  - 새 문틀을 **또 하나 통째로 그리지 마라**(위 변이 닫힌 네모 금지).
- 크기 정확히 720 x 540. 그림에 빛을 굽지 마라. 글자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bgfix3_missilev2_apron.png (720 x 540, PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
