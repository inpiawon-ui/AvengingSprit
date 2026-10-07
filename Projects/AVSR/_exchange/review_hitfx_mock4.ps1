$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.
[배경] 피격 이펙트 시안 4차(3차 검수 Projects/AVSR/_exchange/review_hitfx_mock3.md 의 고칠 점 반영). PD 반려 사유: 1) 일반 투사체에 표창 모양이 들어감 2) 상성 불리면 튕겨 나가는 연출이어야 함 3) 전체적으로 이상함(그림 한 장 느낌).
시안: Projects/AVSR/_exchange/in/mock_hitfx_v4.png · 확대: Projects/AVSR/_exchange/ref/hx_v4_zoom.png · 발주문: Projects/AVSR/_exchange/order_hitfx_mock4.ps1
[답할 것] 한국어. 1) 판정: PD에게 보일 만함 / 다시 그릴 것 2) 반려 사유 3가지가 해결됐는지 각각 3) 네 경우(보통 · 치명 · 유리 · 불리)가 서로 한눈에 구별되는지 4) 이것을 게임에서 「빛 셰이더(섬광 · 고리) + 파티클(불똥 · 파편 · 튕기는 탄)」로 구현할 때 겹 구성(무엇을 셰이더로, 무엇을 파티클로, 크기 px · 개수 · 속도 · 수명 · 색 RGB)을 경우마다.
[저장] Projects/AVSR/_exchange/review_hitfx_mock4.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
