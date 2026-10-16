$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임).
[부품] 필살기(SPECIAL) 아이콘 — 호스트 「갱스터」 · 필살기 「청부 처형」: 가장 체력이 높은 적(보스 포함)을 1.5초 조준한 뒤 초대형 한 발.
**다시 그린다** — 지난 장(Projects/AVSR/_exchange/in/ulticon_gangster.png)은 조준선 뒤에 괴물 실루엣을 지어내서 무엇인지 안 읽혔다. 괴물 · 사람 형체를 그리지 마라.
정답 화풍(승인 시안에서 자른 아이콘): Projects/AVSR/_exchange/ref/ulticon/mock_ult_icon_gangster.png — 어두운 붉은 바탕 가운데 **흰 붉은 조준선 원 + 십자**, 그 아래에서 위로 솟는 **굵은 흰 빛 한 줄기(초대형 탄)**, 둘레에 빛 조각 몇 개. 이것을 더 깔끔하게.
- 정사각 타일을 바탕까지 꽉 채운다(투명 없음, 바깥 테두리 · 틀은 그리지 마라). 64 × 64 로 줄여도 조준선과 빛줄기가 읽히게 — 요소는 둘뿐, 잔 무늬 금지.
- 23장 세트의 첫 장이다 — 바탕은 어두운 붉은 · 보랏빛, 강조색만 캐릭터 색. 글자 · 숫자 없음.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트).
[저장] Projects/AVSR/_exchange/in/ulticon_gangster_r2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
