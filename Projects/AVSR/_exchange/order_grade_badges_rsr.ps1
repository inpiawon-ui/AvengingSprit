$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 부품 발주.
[부품] 등급 배지 세 개 — R · SR · SSR — 를 **한 장에 가로로 나란히**(같은 크기 · 같은 모양 · 같은 굵기, 색과 글자만 다르게).
앵커(이 모양 그대로): Projects/AVSR/_exchange/ref/ulticon/mock_badges_r_sr_ssr.png (승인 시안에서 잘라 낸 배지 — 왼쪽 위 모서리 깎인 방패형 칸 + 굵은 글자)
지금 게임 배지(같은 틀 · 글자만 B·A·S): Projects/AVSR/_exchange/ref/ulticon/ch_grade_b.png · ch_grade_a.png · ch_grade_s.png
- R = 청록 · SR = 보라 · SSR = 금. 글자는 굵은 흰 글씨(SSR 글자는 금빛 흰). 세 글자 폭이 달라도 배지 바깥 크기는 같게(가로가 약간 긴 둥근 사각, 지금 배지 비율 48×50 보다 조금 넓은 64×50 정도 비율).
- 배경은 순수 마젠타(#FF00FF) 단색 — 그림자 · 빛 번짐을 바깥에 그리지 마라(뚫어서 쓴다).
- 캔버스 1536 × 512, 배지 셋을 칸 셋에 하나씩 가운데 정렬, 배지 사이 마젠타 여백 넉넉히.
[저장] Projects/AVSR/_exchange/in/grade_badges_rsr.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
