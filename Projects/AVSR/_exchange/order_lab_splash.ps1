$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 배양조가 깨지는 순간의 **파티클 부품**(게임이 한 칸씩 잘라 날린다).
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 모양 · 질감 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_shards_splash.png
  — PD 가 고른 시안의 유리 조각 · 물방울 부분을 확대한 것. 전체 화면: Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png
⚠ PD: 「깨지는 이미지는?」 — 지금 조각은 속이 꽉 찬 불투명 하늘색 덩어리라 시안과 다르다. 대충 그리지 마라.

[부품] 배양액 물방울 · 물보라 6개
- 가로 3칸 × 세로 2칸, 정사각 칸.
- 정답 그림의 물 부분처럼 **청록 배양액**: 가장자리가 밝은 흰 청록, 속은 어둡게 비치는 청록(유리 조각과 같은 질감).
- 칸마다: ① 길게 튀는 물줄기(끝이 방울로 맺힘) ② 휘어진 물보라 호 ③ 물방울 3~5개 무리 ④ 큰 물방울 하나 ⑤ 작은 물방울 2개 ⑥ 바닥에서 튀어 오르는 물보라 왕관.
- 도트 외곽선, 번짐 · 후광 금지.
- **검은 바탕(#000000)** — 게임이 가산(더하기)으로 겹친다. 검은 곳 = 투명, 밝을수록 빛난다.
- 칸 가운데 하나씩 · 칸끼리 닿지 않게 · 캐릭터 · 글자 · 빛줄기는 그리지 마라.
※ 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_splash.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
