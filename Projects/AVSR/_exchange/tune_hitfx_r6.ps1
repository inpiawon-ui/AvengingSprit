$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드 파일은 고치지 마라 — 판정 문서만 쓴다.
[역할] 아트 · 연출 판단은 네가 메인(PD). 이번은 r5 적용 뒤 **최종 확인**이다.

[정답 시안] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
[네 r5 표] Projects/AVSR/_exchange/tune_hitfx_r5.md — 전부 적용했다. 숫자는 「+18 px 위로」만 적용했다.
  숫자 배율(보통 0.92→1 · 치명 1.12→1)은 보류했다: 네 표는 지금 치명 배율을 1.35 로 적었지만 실제는 1.9(보통이 1.35)이고,
  「치명은 크게 터지며 뜬다」는 기획 결정(2026-09-15)이 있어 PD 에게 묻는다.
[결과]
  정지 비교: Projects/AVSR/_exchange/ref/hitfx_ingame/r5/compare_r5.png (위에서부터 시안 · 게임 0.03 · 0.07 · 0.13초, 2배)
  프레임: Projects/AVSR/_exchange/ref/hitfx_ingame/r5/l1_00.png ~ l1_17.png (0.01초 뒤부터 0.02초 간격, 720x1280 전체 화면)
  ※ 숫자가 두 벌 보이는 것은 같은 판에서 두 번 찍어 앞 회차 숫자가 남은 것 — 판정 제외. 유리 적(갱스터) 앞을 모래주머니가 가린 것은 방 물건 — 판정 제외.
[Claude 가 잡은 점]
  - 숫자를 18 올렸더니 이제 **적 체력바 위에 숫자가 겹친다**(1·8 이 빨간 막대 위). 네 판단: 그대로 / 더 올림 / 원래(20)로 / 다른 안.
  - 유리 주황 불티가 시안보다 여전히 적고 연해 보인다(0.13초). 네 r5 종료 조건(7개 이상)을 재서 판정해라.

[써 낼 것] 1) 네 경우 최종 판정(통과/미통과, 재서) 2) 숫자 위치 결론(수치) 3) 미통과면 마지막 수치표.
[저장] Projects/AVSR/_exchange/tune_hitfx_r6.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
