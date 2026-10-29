$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **융단 폭격 여운 부품 시트** — PD 승인 시안 Projects/AVSR/_exchange/in/mock_skill_grenade_v1.png 의 「3 여운」과 **똑같은 모양 · 색 · 도트 그림체**로.
앵커(확대): Projects/AVSR/_exchange/ref/grenade_anchor_aftermath.png (그을린 구덩이 · 연기 기둥 · 불씨 · 검은 돌 조각)

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄.
- 1줄 1칸: 그을린 구덩이 A — 탑뷰 바닥 자국, 가운데 검은 구덩이 안에 주황 불씨 몇 점, 바깥은 톱니처럼 갈라진 어두운 회갈색 테두리. 너비 약 150 px · 높이 약 80 px(바닥에 눕힌 타원).
- 1줄 2칸: 그을린 구덩이 B — A 와 같은 그림체, 모양만 조금 다르게.
- 1줄 3칸: 검은 돌 조각 3개(작은 것 12 px · 중간 18 px · 큰 것 24 px), 서로 떨어뜨려 배치.
- 1줄 4칸: 비움(마젠타).
- 2줄 1~4칸: 연기 기둥 4장(시간 순) — 아래가 구덩이 자리, 회색 도트 연기가 위로 피어오르며 1 짧음 → 2 높아짐(약 180 px) → 3 흩어지기 시작 → 4 옅게 사라짐. 연기 안에 작은 주황 불씨 2~3점.
- 각 그림은 칸 가운데(연기는 칸 아래쪽에 밑동), 칸 밖으로 넘치지 않게. 그림자 · 마젠타/분홍 색 금지. 외곽선은 시안처럼 어두운 도트.
[저장] Projects/AVSR/_exchange/in/skill_grenade_parts.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
