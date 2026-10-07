$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 판정 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). r7 적용 뒤 최종 판정 + 방향 판단이다.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
[네 r7 표] Projects/AVSR/_exchange/tune_hitfx_r7.md — 전부 적용(유리 불티 부채 +8° · 옆 흩기 ±16 px · 숫자 +6 px).
[결과] 정지 비교 Projects/AVSR/_exchange/ref/hitfx_ingame/r7/compare_r7.png (위에서부터 시안 · 게임 0.03 · 0.07 · 0.13초)
       프레임 Projects/AVSR/_exchange/ref/hitfx_ingame/r7/f00.png ~ f21.png (720x1280 전체, 0.01초 뒤부터 0.02초 간격)
       ※ 갱스터 앞 모래주머니는 그 적의 엄폐물 — 판정 제외.

[물을 것 1 — 최종 판정] 네 경우 · 숫자, 재서 통과/미통과.

[물을 것 2 — 방향] Claude 는 「시안 전체를 나란히 보면 게임 쪽이 여전히 작고 옅다 — 시안의 불티는 진한 색 + 어두운 외곽선이 있는 그린 낱알인데,
지금은 셰이더로 그린 흐린 빛 점이라 수치를 더 만져도 그 질감이 안 나온다. 불티 · 빛꼬리 낱알 그림을 시안에서 잘라 앵커로 발주해 파티클에 입히자」고 PD 에게 제안했다.
이 제안이 맞는지 네가 판단해라:
  (가) 지금 방식(셰이더 빛 점)으로 충분 — 남은 차이는 수치로 닫힌다 → 마지막 수치표
  (나) 낱알 그림이 필요 — 어떤 낱알을 몇 장(모양 · 크기 · 색 · 외곽선 · 프레임 수), 시안 어디를 앵커로, 무엇은 셰이더로 남기는지
  (다) 다른 안
PD 기준: 「시안을 잡았으면 최대한 똑같이(더 예쁘거나 멋진 건 괜찮다)」 · 「피격은 공통으로 쓰되 기본적으로 예뻐야 한다」.

[써 낼 것] 1) 최종 판정 표 2) 방향 결론과 근거(시안을 재서) 3) (가)면 수치표 · (나)면 발주 목록 4) PD 에게 보일 두세 줄 요약.
[저장] Projects/AVSR/_exchange/tune_hitfx_r8.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
