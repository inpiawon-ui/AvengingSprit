$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[배경] 유령 빛(빙의할 몸에 하늘에서 빛이 내려옴)을 PD가 승인한 시안과 **똑같이** 맞추는 중이다.
PD 지적 이력: 「시안과 실제가 다르다」 · 「이미지 한 장 얹은 것 같다 — 빛은 알파 · 파티클로」 · 「빛이 너무 두껍다, 얇게」(이건 시안보다 우선) ·
「안쪽이 지저분하고 가운데 선이 깨져 보이고 뿌옇다」.
지금은 그림 없이 셰이더(알파 그라데이션 · 더하기 섞기)로 광막 두 장 · 방추형 점선 빛심 · 바닥 고리를, 파티클로 십자별 알갱이를, 몸 실루엣 빛으로 테두리를 그린다.

[비교 — 3배 확대, 왼쪽 시안 · 오른쪽 게임] Projects/AVSR/_exchange/ref/gl_compare_v6.png
[거둠(빙의 순간) 장면] Projects/AVSR/_exchange/ref/gl_retract_sheet.png
[원본 시안] Projects/AVSR/_exchange/in/mock_ghostlight_v2.png

[답할 것] 한국어.
1) 판정: PD에게 보일 만함 / 손볼 것 있음
2) 시안 대비 다른 곳 — 마감 품질(안쪽 결 · 가장자리 · 번짐 · 깨짐 · 뿌연 느낌)까지, 숫자로(굵기 · 진하기 · 색)
3) 고칠 것은 셰이더 값으로: 폭 · 가장자리 부드러움 · 몸 진하기 · 심 굵기 · 심 밝기 · 위아래 흐려짐 · 점선 마디 수 · 마디 길이 · 고리 두께 · 번짐 · 색(RGB) · 알갱이 크기 · 개수 · 속도 · 테두리 색 · 두께
[저장] Projects/AVSR/_exchange/review_ghostlight_v6.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
