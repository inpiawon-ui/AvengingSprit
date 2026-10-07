$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 수치표 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). Claude 는 네 값을 그대로 넣고 네 실수 · 기술 제약만 알린다.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
[네 조정표] Projects/AVSR/_exchange/tune_hitfx_r1.md ~ r4.md — r4 까지 적용했다(색 낱알 재질 셋으로 분리, 치명 방사광 중심 밖 시작, 주황 불티 흩어 태어나기).
[r4 결과] Projects/AVSR/_exchange/ref/hitfx_ingame/compare_r4.png (맨 위 시안, 아래 게임 0.035 · 0.075 · 0.125초, 2배) ※ 넷째 적 노란색은 시험판 탓.

[Claude 눈]
- 치명: 방사광이 중심에서 떨어져 시작 — 십자 · 별 매듭이 사라졌다.
- 유리: 원뿔 가운데가 밝아졌다. 주황 불티는 0.125초에 4~6개 보이지만 시안보다 흐리고 작다(시안 불티는 진한 주황 몸 + 노랑 심, 원뿔 밖으로 넓게).
- 불리: 얇은 은빛 면과 쏜 쪽 은빛 낱알은 보인다. 「막혀서 튕겼다」 첫 박자는 여전히 시안보다 약하다.
- 숫자가 적 머리를 가린다(숫자 시스템 — 별도 의견 r2 4절 그대로 적용할지 판단해라).

[써 낼 것] 1) 경우별 판정(통과/미통과 + 근거, 재서) 2) 남은 수치표(지금 값 → 새 값, 재질값 포함) 3) 점검표.
네 경우 모두 통과면 맨 위에 「전체 통과」라고 적어라. 이번이 5회차다 — 남은 차이가 작으면 「통과 + 다듬을 점」으로 끝내도 된다. 판단은 네가 한다.
[저장] Projects/AVSR/_exchange/tune_hitfx_r5.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
