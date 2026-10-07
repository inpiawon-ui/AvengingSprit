$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 판정 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). r6 적용 뒤 최종 판정이다.
[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png · [네 r6 표] Projects/AVSR/_exchange/tune_hitfx_r6.md (전부 적용: 유리 불티 22개 · 크기 · 숫자 +10 px)
[결과] 정지 비교 Projects/AVSR/_exchange/ref/hitfx_ingame/r6/compare_r6.png (시안 · 게임 0.03 · 0.07 · 0.13초)
       프레임 Projects/AVSR/_exchange/ref/hitfx_ingame/r6/f00.png ~ f21.png (720x1280 전체, 0.01초 뒤부터 0.02초 간격)
  ※ 갱스터 앞 모래주머니는 그 적의 엄폐물(몸에 붙어 다님) — 판정 제외. 넷째 적은 이번엔 해골.
[Claude 눈] 0.13초 유리 주황 불티가 머리 위에서 한 덩어리로 뭉쳐 보이는 구간이 있다. 숫자 1·8 이 체력바와 거의 붙어 있다.
[써 낼 것] 1) 네 경우 · 숫자 최종 판정(재서) 2) 미통과면 마지막 수치표(최소 변경) 3) PD 에게 보일 한 줄 요약(통과 근거 또는 남은 차이).
[저장] Projects/AVSR/_exchange/tune_hitfx_r7.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
