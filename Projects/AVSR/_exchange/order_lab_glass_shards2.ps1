$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 배양조가 깨지는 순간의 **파티클 부품**(게임이 한 칸씩 잘라 날린다).
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 모양 · 질감 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_shards_splash.png
  — PD 가 고른 시안의 유리 조각 · 물방울 부분을 확대한 것. 전체 화면: Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png
⚠ PD: 「깨지는 이미지는?」 — 지금 조각은 속이 꽉 찬 불투명 하늘색 덩어리라 시안과 다르다. 대충 그리지 마라.

[부품] 속이 비치는 유리 조각 8개
- 가로 4칸 × 세로 2칸, 정사각 칸.
- 정답 그림의 유리 조각처럼: **가장자리 테두리 · 깨진 면의 모서리 선만 밝은 흰 청록(#dffcff ~ #7fe9f0)으로 또렷하게**,
  **속은 아주 어둡게 비치는 청록(검은 바탕이 거의 보일 만큼)** + 면 하나에 옅은 반사 줄 한두 개. 속을 밝은 하늘색으로 꽉 채우지 마라.
- 크기 · 모양을 섞는다: 큰 판 2개(넓은 오각 · 사각 판) · 중간 3개(긴 삼각 · 쐐기) · 작은 부스러기 3개(칸의 1/4 정도).
- 도트 외곽선(1~2 px), 번짐 · 광채 후광 금지(가산이라 게임에서 빛난다).
- **검은 바탕(#000000)** — 게임이 가산(더하기)으로 겹친다. 검은 곳 = 투명, 밝을수록 빛난다.
- 칸 가운데 하나씩 · 칸끼리 닿지 않게 · 캐릭터 · 글자 · 빛줄기는 그리지 마라.
※ 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_glass_shards2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
