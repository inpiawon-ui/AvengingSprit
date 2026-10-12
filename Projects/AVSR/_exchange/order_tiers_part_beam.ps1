$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(4절 부품 규칙).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 이펙트 부품 — **세로 빛기둥** 한 장.
[앵커 — 이 시안의 기둥 모양 · 화풍 그대로] Projects/AVSR/_exchange/ref/tiers_mock/anchor/anchor_pillar_epic.png , Projects/AVSR/_exchange/ref/tiers_mock/anchor/anchor_pillar_legendary.png , Projects/AVSR/_exchange/ref/tiers_mock/anchor/anchor_gain_beam.png
(PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_tiers_lv_appear.png 의 에픽 · 전설 카드 위 기둥, mock_tiers_gain.png 의 유령 위 가는 빛줄기)
- 위에서 아래로 곧게 내려오는 **세로 빛기둥**. 가운데 가는 흰 심이 가장 밝고, 양옆으로 부드럽게 옅어진다. 가장자리에 도트 계단이 살짝 보인다.
- 위 · 아래 끝은 부드럽게 사그라든다(위 15% · 아래 10%). 기둥 안에 작은 도트 알갱이 서너 개가 섞여도 된다.
- 색은 **흰색만**(게임에서 등급색을 곱한다 — 보라 · 금 · 하늘).
- **검은 바탕(#000000)**, 게임에서 더하기(가산)로 그린다. 바깥에 어두운 판 · 테두리 금지.
- 크기 128 x 512, 기둥 폭은 그림 폭의 60% 정도(심은 10%).
[저장] Projects/AVSR/_exchange/in/tiers_part_beam.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
