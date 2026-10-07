$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 수치표 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). Claude 는 네 값을 그대로 넣고 네 실수 · 기술 제약만 알린다.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
[네 1회차 조정표] Projects/AVSR/_exchange/tune_hitfx_r1.md — 그대로 적용했다.
[적용 결과] Projects/AVSR/_exchange/ref/hitfx_ingame/compare_r1.png (맨 위 시안, 아래 게임 0.035 · 0.075 · 0.125초, 2배)
  ※ 넷째 적이 노랗게 보이는 것은 시험판에서 그림이 안 올라온 적에 해골 그림을 빌려 붙인 탓이다 — 이펙트 문제 아님.
  ※ 숫자 위치 · 크기(머리 위에 겹침, 치명 숫자가 처음에 크게 튐)는 기존 숫자 시스템이다. 고칠 의견이 있으면 따로 적어라(수치로).

[Claude 가 잡은 점 — 네가 판정해라]
1) 기술 제약: 지금 모든 빛이 더하기 섞기(Blend SrcAlpha One)라 **밝은 회색 바닥 위에서 주황 · 은빛이 하얗게 날아간다.** 시안의 진한 주황 원뿔 · 주황 불티가 게임에선 흰 줄로 보인다.
   해결 수단(Claude 가 셰이더에 다시 열어 줄 수 있다): 재질마다 _DstBlend = 10(보통 섞기, 색이 그대로 보임) 또는 1(더하기, 밝게 빛남).
   예: 「주황 몸통은 보통 섞기 + 흰 심만 더하기」처럼 겹을 나눠라. 셰이더 frag 는 rgb = lerp(색, 흰색, 심) · a = 모양 알파 × 정점 알파.
2) 불리 은빛 불티가 한 덩어리 얼룩으로 뭉친다(18x18 둥근 점 8개가 같은 자리에서 겹침) — 낱알로 읽히게.
3) 유리 원뿔 · 관통 빛이 「흰 빛기둥」으로 읽혀 원뿔 모양이 안 보인다.

[써 낼 것] r1 과 같은 형식.
1) r1 결과의 어긋난 점(시안을 재서) 2) 바꿀 수치표(지금 값 → 새 값, 재질마다 _DstBlend 포함) 3) 다음 캡처 점검표.
통과 판정이면 「통과」라고 분명히 적어라.
[저장] Projects/AVSR/_exchange/tune_hitfx_r2.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
