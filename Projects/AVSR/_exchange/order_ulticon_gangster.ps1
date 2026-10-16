$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 부품 발주.
[부품] 필살기(SPECIAL) 아이콘 — 호스트 「갱스터」 · 필살기 「청부 처형」: 가장 체력이 높은 적(보스 포함)을 1.5초 조준한 뒤 초대형 한 발. 보스는 최대 체력의 % 피해.
앵커: 이 필살기 연출 시안 Projects/AVSR/_exchange/doc_skill_concept/img/ult_gangster.jpg — 가장 상징적인 순간 하나를 아이콘으로.
화풍 기준(승인 시안에서 자른 갱스터 필살기 아이콘): Projects/AVSR/_exchange/ref/ulticon/mock_ult_icon_gangster.png — 사각 타일을 꽉 채운 도트 그림, 어두운 붉은 바탕에 강한 빛.
- 액티브 아이콘 Projects/AVSR/_exchange/doc_skill_concept/img/active_gangster.jpg · 지금 액티브 아이콘 Assets/BaseResource/HostSelectPanel/ultimateicon_gangster.png 와 **한눈에 달라 보여야** 한다(같은 무기 · 같은 구도 금지).
- 정사각 타일을 **바탕까지 꽉 채워** 그린다(투명 없음, 바깥 테두리 · 틀은 그리지 마라 — UI 가 테두리를 씌운다). 64 × 64 로 줄여도 읽히게 굵은 실루엣 하나 + 빛 한 줄기.
- 23장이 한 세트다 — 바탕은 어두운 붉은 · 보랏빛 톤을 공유하고, 강조색만 캐릭터 색. 글자 · 숫자를 넣지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트).
[저장] Projects/AVSR/_exchange/in/ulticon_gangster.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
