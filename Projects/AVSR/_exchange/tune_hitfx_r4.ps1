$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 수치표 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). Claude 는 네 값을 그대로 넣고 네 실수 · 기술 제약만 알린다.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
[네 조정표] Projects/AVSR/_exchange/tune_hitfx_r1.md ~ r3.md — r3 까지 적용했다.
  r3 에서 Claude 가 고친 네 실수: 불리 면 끝 크기를 「56x9 → 28x12 · 50x5 → 24x8」로 적었는데, r2 의 66x11 을 26x11 로 잘못 옮긴 것으로 보고
  끝을 70x12 · 66x8 로 넣었다(시작을 키운 만큼). 또 _hxBeamMat 은 치명 방사광이 아니라 **유리의 몸 관통 빛기둥** 재질이다(치명 방사광은 HxDot 파티클).
[r3 결과] Projects/AVSR/_exchange/ref/hitfx_ingame/compare_r3.png (맨 위 시안, 아래 게임 0.035 · 0.075 · 0.125초, 2배) ※ 넷째 적 노란색은 시험판 탓, 판정 제외.

[Claude 가 잡은 점 — 네가 판정해라]
1) 유리 주황 불티가 **아예 안 보인다.** HxDotSolid 의 _RingWhite 0.08 로 흰 심이 빠져, 주황 원뿔 위에서 같은 주황 점이 묻혔다(r2 에선 분홍이지만 보였다).
   시안의 불티는 밝은 노랑-흰 심 + 주황 몸 + 원뿔보다 넓게 퍼짐이다. 낱알 재질을 색마다 나눌 수 있다(예: 주황 낱알 전용 재질 _RingWhite 따로).
2) 유리 원뿔이 「흐린 주황 덩어리」 — 시안처럼 가운데가 밝게 타는 불길(밝은 심 → 주황 → 바깥 진한 주황)이 아니다.
3) 치명 방사광이 0.035초에 여전히 가운데에서 십자 · 별처럼 겹친다(시작 지점이 다 같은 중심).
4) 불리는 0.035초 충돌면이 이제 보이지만 작다. 「막혀서 튕겼다」 시작은 아직 약하다(PD 요청: 튕겨낸다는 느낌이 잘 어필되게).

[써 낼 것] r3 와 같은 형식. 1) 어긋난 점(재서) 2) 바꿀 수치표(지금 값 → 새 값, 재질마다 _DstBlend · _RingWhite · _CoreWhite) 3) 점검표. 통과면 「통과」라고 분명히.
[저장] Projects/AVSR/_exchange/tune_hitfx_r4.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
