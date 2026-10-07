$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 보고 판정만 한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.
피격 이펙트 시안 6차를 검수한다: Projects/AVSR/_exchange/in/mock_hitfx_v6.png
설계서: Projects/AVSR/_exchange/spec_hitfx_v3.md (4절 시안 발주문 기준)
반려된 이전 시안: Projects/AVSR/_exchange/in/mock_hitfx_v5.png

PD 반려 이유(이번에 반드시 풀려 있어야 한다):
1) 「불리랑 유리가 티가 안 난다 · 너무 비슷」 — 화살표 · 글자를 가려도 방향이 반대로 읽히는가?
2) 「불리는 튕겨내는 느낌이어야」 — 막혀서 쏜 쪽으로 되돌아오는가?
3) 「유리는 어떻게 맞는지 안 그려진다」 — 꿰뚫고 등 뒤로 나가는 게 보이는가?
4) 「일반 투사체에 표창 · 이상한 칼 표식 금지」 — 무기를 떠올리는 모양이 하나라도 있는가?
5) 「색과 연출이 매칭」 — 보통 흰 · 치명 금 · 유리 주황 · 불리 회색이 효과 자체 색과 맞는가? 속성색이 섞였나?
6) 공통으로 쓸 수 있는가 — 총 · 레이저 · 근접 어디에 붙여도 어색하지 않은가?

판정: 각 항목 통과/미흡 + 근거(어느 적 · 어느 부분).
그리고 게임에 셰이더 · 파티클로 옮길 때 이 시안에서 맞춰야 할 수치(각 적 기준 px: 코어 크기 · 빛줄기 길이 · 원뿔 폭 · 차단면 크기 · 파편 수 · 방향)를 시안을 재서 표로 적는다(이미지 720x1280 기준).
고칠 점이 있으면 「PD에게 보이기 전에 다시 그려야 하는가 / 이대로 보여도 되는가」를 결론으로.
[저장] Projects/AVSR/_exchange/review_hitfx_mock6.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
