$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[PD 정정 — 중요] 「한 방 팍팍(등장 · 선택 순간 폭발 · 빛줄기)이 아니라, 팝업이 떠 있는 동안 은은하게 창을 꾸미는 이펙트가 필요하다.
 인터랙션이 없는데 일회성 이펙트는 의미가 없다」. 그래서 상시 루프 장식으로 다시 만들었다:
 관로를 따라 흐르는 빛 · 유리관 속 기포와 유령 · 문장 숨쉬기 · 제목 판에 가끔 지나가는 반사광 · 불씨 · 연기 · 방 오브젝트 대기 장식.

[보라]
- 움직임(끊김 없이 3.2초 루프): Projects/AVSR/_exchange/ref/fx_ambient/{levelup|altar|devil|shop|clear|room_heal|room_devil|room_shop}.gif
- 시간 순 6컷: 같은 폴더 {이름}_strip.png
- 창 디자인 원본: Projects/AVSR/_exchange/ref/popup_c_ingame/{levelup|devil}.png · audit_{altar|shop}_Korean.png
- 합성 코드(값 고칠 곳): Projects/AVSR/Tools/fx_ambient.py (Flow · Rising · TubeGhost · Breath · Sheen · LoopFx, 화면 함수별)
PD 기준: 은은하지만 「살아 있다」가 보일 것 · 글자를 가리지 않게 · 정신없지 않게(한 화면에 움직이는 계통 3~4개 이내) · 별/십자 반짝이 금지 · 화면 전체 물들이기 금지.

[써 낼 것] 한국어, 화면마다:
1) 안 보이거나 너무 약한 장식 / 과하거나 정신없는 것 / 글자를 가리는 것 / 틀 그림과 위치가 어긋난 것(관로 · 유리관 자리)
2) fx_ambient.py 기준 구체 수정값(좌표 · 크기 · 알파 · 속도 · 개수 · 색), 빼야 할 것
3) 이 틀에 더하면 좋은 상시 장식 1~2개(있으면)
마지막에 Claude 가 바로 적용할 변경 목록 정리.
[저장] Projects/AVSR/_exchange/review_fx_ambient1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
