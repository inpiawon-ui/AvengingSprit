$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 수치표 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). Claude 는 네 값을 그대로 넣고 네 실수 · 기술 제약만 알린다.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
[네 조정표] tune_hitfx_r1.md · tune_hitfx_r2.md (Projects/AVSR/_exchange/) — r2 까지 그대로 적용했다.
  적용 방식 메모: 색 낱알(치명 금빛 불티 · 유리 주황 불티 · 불리 은빛 불티)은 보통 섞기 점(HxDotSolid), 흰 빛 · 방사광 · 긴 빛꼬리는 더하기 점(HxDot).
  「개당 0.004초 늦게」는 파티클에 늦춰 쏘기가 없어 「그 시간만큼 궤적 뒤에서 태어나기」로 흉내 냈다.
[r2 결과] Projects/AVSR/_exchange/ref/hitfx_ingame/compare_r2.png (맨 위 시안, 아래 게임 0.035 · 0.075 · 0.125초, 2배)
  ※ 넷째 적이 노란 것은 시험판에서 해골 그림을 빌려 붙인 탓 — 판정 제외.

[Claude 가 잡은 점 — 네가 판정해라]
1) 기술: 셰이더가 점 · 심의 가운데를 하얗게 섞는다(고리 · 점은 선의 70%, 빛기둥은 심의 80%). 보통 섞기 낱알에서 이게 **주황을 분홍 파스텔로** 바래게 한다(0.125초 유리 불티).
   → 재질 값을 새로 열었다: _RingWhite(점 · 고리 가운데가 하얘지는 정도, 기본 0.7) · _CoreWhite(빛기둥 · 원뿔 심이 하얘지는 정도, 기본 0.8). 재질마다 정해라.
2) 치명 방사광 8개가 0.075초에 서로 이어져 **마름모 · 별 모양**으로 보인다(네 금지 목록).
3) 유리 원뿔이 연기처럼 흐릿하다 — 시안은 가운데가 밝게 타는 진한 주황 불길 + 둥근 주황 불티 9~11개.
4) 불리 충돌면이 거의 안 보인다(0.035초) — 「막혀서 튕겼다」의 시작이 약하다. PD 요청: 「상성 때문에 튕겨낸다는 느낌이 잘 어필되면 좋겠다」.
   (1회차에 네가 맞은 탄이 실제로 튕겨 나가는 연출을 금지 형태로 뺐다 — 여전히 그 판단이면 그대로, 아니면 다시 넣는 안을 적어라.)

[써 낼 것] r2 와 같은 형식. 1) 어긋난 점(재서) 2) 바꿀 수치표(지금 값 → 새 값, 재질마다 _DstBlend · _RingWhite · _CoreWhite 포함) 3) 다음 캡처 점검표. 통과면 「통과」라고 분명히.
[저장] Projects/AVSR/_exchange/tune_hitfx_r3.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
