$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — 상점 · 소환 · 패스 **장면 기획 시안**.
[반드시 먼저 읽는다] Projects/AVSR/_exchange/store_scenes.md — 이 장면(S13)의 구성이 정답이다(무엇이 · 어디에 · 어떤 상태로). 0절 판매 구성표도 본다.
[화풍 기준 — 이어서 그린다]
- 소환(보라 제단): Projects/AVSR/_exchange/in/mock_summon_a.png — 하단 바 네 칸 HOST · PLAY · SUMMON · SHOP 이 이 시안 모양.
- 상점(남색 · 하늘 네온): Projects/AVSR/_exchange/in/mock_store_currency.png · 패스: Projects/AVSR/_exchange/in/mock_store_pass.png · 로비: Projects/AVSR/_exchange/in/mock_store_lobby.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로. 호스트 얼굴은 Assets/BaseResource/Growth/Faces/face_{키}.png 를 그대로 쓴다.
[공통] 글자 한국어 · 값 원화(₩) · 젬. 이 게임 도트 화풍. 값 · 수량 · 남은 횟수 · 상태(구매 완료 · 잠김 · 받기)가 또렷하게. 그림 아래 여백에 한국어 메모 3줄 이내.
[장면 S13] 소환 결과 10회 — 5x2 카드 열 장, 대박 · 픽업 · NEW · 정수 전환 칸이 섞인 모습 + 요약 줄.
[고칠 것 — 1차 납품 Projects/AVSR/_exchange/in/scene_s13_result10_r2.png 를 보고 이것만 고쳐 다시 그린다] ① 카드 속에 우리 게임에 없는 몹(해골 병사 · 슬라임 · 플라잉 아이 · 오크 · 스켈레톤 나이트 · 리퍼 · 병사)이 들어갔다 — **우리 호스트 얼굴만**: Assets/BaseResource/Growth/Faces/face_{키}.png 그대로(예: 갱스터 · 폭력배 · 아마존 · 호퍼 · 코만도(기관총) · 구루 · 샐러맨더 · 닌자(사슬) · 하얀 마법사 · 로봇). ② 수량 앞 아이콘이 젬으로 그려졌다 — **조각 표시(조각 모양 배지)**로. ③ 조각 수는 ×2 · ×5 · ×20 셋뿐. 구성: B ×2 · B ×5 · B ×2 · B ×5 · B ×2 / A 구루 ×2 · B ×2 · **A 닌자(사슬) ×20 대박 + PICK UP** · B 「NEW」 처음 얻은 몸 ×5 · 다 키운 몸 「정수 +5」. 요약 줄 · 버튼은 1차 그대로.
[저장] Projects/AVSR/_exchange/in/scene_s13_result10_r2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
