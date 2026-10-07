$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 수치표 문서만 쓴다.

[역할] 이번부터 아트 · 연출 판단은 네가 메인이다(PD 지시). Claude 는 네가 정한 값을 그대로 게임에 넣고, 네 실수만 잡는다.
그러니 「시안과 똑같이(더 예쁜 건 괜찮다)」가 되도록 네가 수치를 정해라.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png (PD 승인 — 보통 · 치명 · 유리 · 불리. 불리는 은빛 강철)
  PD 추가 지시: 「불리는 상성 때문에 튕겨낸다는 느낌이 잘 어필되면 좋겠다」 · 「피격은 공통으로 쓰되 기본적으로 예뻐야 한다」.
  Claude 가 본 시안 7차의 흠(같이 고칠 것): 불리 알갱이가 화살촉 모양 → 둥근 불티로, 불리 반구가 방패 그림처럼 보임 → 얇은 빛면 번쩍으로.

[지금 게임 결과] Projects/AVSR/_exchange/ref/hitfx_ingame/compare_v1.png
  맨 위 = 시안, 아래 셋 = 게임 0.035 · 0.075 · 0.125초(같은 칸 자르기, 2배 확대). 시험판이라 적 그림이 해골 · 갱스터다(시안은 갱스터 넷).
  모양 단독 시험: Projects/AVSR/_exchange/ref/hitfx_ingame/shape_test.png — 적 아래 네 개가 왼쪽부터 원뿔 · 빛기둥 · 빛면 · 빛점 재질(120x200, 주황, 알파 1).
  Claude 의 눈: 알갱이가 7~14 px 라 거의 안 보인다(빛점 셰이더가 상자의 30% 만 밝다) · 유리 원뿔이 실처럼 가늘다(가운데 심만 보이고 몸통이 흐림) · 치명 광선 · 불똥이 안 보인다 · 불리 빛면이 유리판처럼 몸 위에 뜬다.

[구현 방식 — 이 안에서 값을 정해라]
  코드: Assets/Scripts/Module/InGame/BattleDirector.HitFx.cs (HxTap · HxCrit · HxPierce · HxDeflect · HxBuild 의 숫자)
  셰이더: Assets/BaseResource/Shaders/UIAdditiveLight.shader (_Shape 0 빛기둥/원뿔/사다리꼴: _BottomWidth _TopWidth _EdgeSoft _Body _CoreWidth _CoreBoost _FadeTop _FadeBottom _EdgeLine / _Shape 1 고리 · 점: _RingRadius _RingWidth _RingGlow _HalfMask / _DstBlend 1=더하기 10=보통 섞기)
  조각(HxPut): 재질 · 자리 · 기준점 · 각도 · 크기 시작→끝 · 색 · 알파 · 수명 · 지연. 크기는 처음에 확 퍼지고(ease-out) 알파는 고르게 빠진다.
  알갱이(HxSpray → ParticleFxPool.Spray): 부채 반각 · 개수 · 속도 범위 · 길이 x 폭 · 수명 · 색. 끌림(drag) 5 로 금방 선다. 화면 좌표 1:1, HxScale 1.15 를 곱한다.
  필요하면 셰이더 값 · 새 겹(예: 원뿔 아래 두 번째 넓은 번짐 · 알갱이 흰 심)을 더 쓰라고 해도 된다. 무기 모양(표창 · 별 · 칼자국 · 화살촉 · 방패 그림)과 속성색은 금지.

[써 낼 것] 한국어 · 표.
1) 시안 대비 지금 게임이 어긋난 점 — 경우별(보통 · 치명 · 유리 · 불리)로, 시안을 재서.
2) 고칠 수치표 — 함수 · 겹마다 바꿀 값 그대로(지금 값 → 새 값). Claude 가 그대로 옮겨 적는다. 애매하게 쓰지 마라.
3) 셰이더 · 재질 값 변경(있으면).
4) 다음 캡처에서 네가 확인할 점검표.
[저장] Projects/AVSR/_exchange/tune_hitfx_r1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
