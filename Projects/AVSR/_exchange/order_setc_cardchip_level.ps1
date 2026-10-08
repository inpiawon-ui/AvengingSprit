$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 인게임 UI 부품 — PD 가 통과시킨 시안 C 「원혼 회로」 Projects/AVSR/_exchange/in/mock_set_C_levelup.png 와 **똑같은 모양 · 색 · 도트 그림체**로.
앵커(시안에서 이 부품만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_c/levelup_chip.png — 이걸 그대로 따라 깨끗하게 다시 그린다.
[부품] cardchip_level — 카드 위 레벨 칩 — 같은 모양, 테가 금빛, **글자 없이**
[크기] 게임에서 150 x 34 px 로 쓴다. 그 비율 그대로 캔버스에 크게(가로세로 비율 유지, 캔버스의 90% 정도 차게) 한 장.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림자 · 마젠타/분홍 색을 부품에 쓰지 않는다.
- 글자 · 숫자를 넣지 않는다(게임이 위에 글자를 얹는다). 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/setc_cardchip_level.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
