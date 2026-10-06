$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[검수 대상] Projects/AVSR/_exchange/in/growth_merge_mock_v1.png — 모바일 게임 「호스트 육성」 화면 레이아웃 시안(720 x 1280 기준).
[지금 화면] Projects/AVSR/_exchange/ref/growth_now_host_stat.png · growth_now_host_skill.png — 능력치 / 스킬이 하위 탭으로 갈려 있었다.
[요구] 몸을 고르면 몸 정보 · 능력치 5줄(공격력 · 최대 HP · 크리티컬 확률 · 크리티컬 피해 · 방어력) · 스킬 2개 · 호스트 목록이 한 화면에 다 보일 것. 지금 화면의 그림체 · 틀 · 색을 그대로 쓸 것. 상단 줄 · 큰 탭 · 하단 HOST/PLAY/SHOP 바는 위치 그대로.

[볼 것] 아래 항목마다 「통과 / 고칠 것(구체적으로)」로 짧게.
1. 요구 내용이 빠짐없이 한 화면에 보이는가 (특히 방어력 줄, 스킬 2개 설명 전체)
2. 지금 화면과 같은 그림체 · 재질인가, 새 스타일이 섞였는가
3. 정보 위계 — 몸 정보 → 능력치 → 스킬 → 목록 순서가 읽기 쉬운가, 간격 · 정렬이 고른가
4. 호스트 목록이 너무 좁아 고르기 불편하지 않은가(한 줄 + 스크롤로 충분한가)
5. 손가락 터치 크기 — 골드 버튼 · 목록 칸 높이가 모바일에서 충분한가(약 44px 이상)
6. 글자 깨짐 · 가짜 글자
마지막 줄에 종합 판정: 「그대로 보고」 또는 「고쳐서 다시」.
[저장] 검수 결과를 Projects/AVSR/_exchange/review_growth_merge1.md 에 한국어로 쓴다.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
