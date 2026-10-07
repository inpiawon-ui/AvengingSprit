$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
PD에게 보여 줄 **피격 이펙트 시안 7차**(완성된 게임 화면 한 장, 720 x 1280 세로).
6차(Projects/AVSR/_exchange/in/mock_hitfx_v6.png)를 바탕으로 고친다 — 화면 구성 · 배경 · HUD · 적 4명 배치 · 라벨은 6차 그대로 둔다.
설계서: Projects/AVSR/_exchange/spec_hitfx_v3.md 4절 · 검수서: Projects/AVSR/_exchange/review_hitfx_mock6.md (결론 · 보정 지시 표를 전부 반영)

고칠 것:
1) 유리: 적 몸 아래(플레이어 쪽)로 뻗은 약 60px 주황 꼬리를 **완전히 없앤다.** 앞면엔 16x22px 작은 주황 입구만. 몸을 지나는 가는 빛기둥(폭 8→5px) → 등 뒤(화면 위) 원뿔 길이 약 82px · 끝 폭 약 52px. 주황 파편 9개, 전부 위쪽. 끝은 뾰족한 화살촉이 아니라 둥글게 사라지는 빛.
2) 보통: 코어 안 X자 교차 선을 없앤다. 작은 둥근 흰 코어(18x14) + 얇은 고리(지름 34) + 불규칙한 작은 파편 5개.
3) 치명: 전체 크기를 약 96x88px 안으로 줄인다. 금빛 코어 · 고리 2겹 · 짧은 광선 8개 · 파편 약 12개.
4) 불리 색을 **은빛 강철**로: 밋밋한 회색은 「꺼진 버튼」처럼 보인다(PD). 차갑고 살짝 푸른 은색(대략 RGB 170,182,198) 면에 **하얀 반짝임(쇠에 빛이 튀는 하이라이트)** 과 진한 테두리. 차단면 58x34px, 반고리 지름 46px, 플레이어 쪽(화면 아래)으로 튕겨 오는 은빛 알갱이 8개 · 짧은 빛꼬리 3개(28~48px). 적 몸 안 · 등 뒤엔 빛 없음. ▼ 와 숫자도 같은 은빛(반짝임이 있는 금속 느낌, 흐릿한 회색 금지).
금지: 표창 · 별 · 칼 자국 · X 베기 · 총구 화염 · 탄피 · 탄두 · 화살촉 · 방패 그림, 속성색(빨강 · 청록 · 보라).
[저장] Projects/AVSR/_exchange/in/mock_hitfx_v7.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
