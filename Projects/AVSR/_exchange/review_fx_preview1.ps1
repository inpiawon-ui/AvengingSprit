$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[무엇] 팝업 · 방 오브젝트 · 스테이지 클리어 연출을 게임 캡처 위에 합성한 미리보기(유니티에 넣기 전 확인용).
- 움직임: Projects/AVSR/_exchange/ref/fx_preview/{levelup|altar|devil|shop|clear|room_heal|room_devil|room_shop}.gif
- 같은 것을 시간 순 8컷으로 펼친 것: Projects/AVSR/_exchange/ref/fx_preview/{같은 이름}_strip.png (왼쪽 → 오른쪽 = 0초 → 끝)
- 정답 시안(PD 가 보고 싶어 한 연출): Projects/AVSR/_exchange/in/mock_fxstory_{levelup|altar|devil|shop|room_heal|room_devil|room_shop}_v1.png · mock_fxstory_clear_v2.png
- 박자 · 부품 계획(네가 쓴 것): Projects/AVSR/_exchange/consult_popup_fx_parts.md
- 합성 코드(값을 고칠 곳): Projects/AVSR/Tools/fx_preview.py — 화면별 함수 levelup() altar() devil() shop() clear() room()
PD 기준: 연출이 「확」 와야 한다(이미지 하나로 끝난 느낌 금지) · 시안 느낌에 가깝게 · 단 글자를 가리지 않게 · 별/십자 반짝이 금지 · 화면 전체 물들이기 금지 · 게임이 멈춘 듯 보이면 안 됨.

[써 낼 것] 한국어, 화면마다:
1) 시안 대비 약하거나 빠진 박자, 과하거나 글자를 가리는 곳(몇 번째 컷인지)
2) fx_preview.py 기준 구체적 수정값(크기 · 시작 시각 · 프레임 간격 · 색 · 알파 · 위치 · 입자 수) — 예: levelup() 의 ring_pulse 200 → 280
3) 우선순위
마지막에 Claude 가 바로 적용할 변경 목록을 한 번에 정리.
[저장] Projects/AVSR/_exchange/review_fx_preview1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
