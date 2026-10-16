$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 부품 발주.
[부품] 필살기(SPECIAL) 아이콘 — 호스트 「폭력배」 · 필살기 「회전 난사」: 2.5초 돌며 사방 연사, 멈추며 사방 16발.
앵커: 이 필살기 연출 시안 Projects/AVSR/_exchange/doc_skill_concept/img/ult_thug.jpg — 가장 상징적인 순간 하나를 아이콘으로.
화풍 기준(승인 시안에서 자른 갱스터 필살기 아이콘): Projects/AVSR/_exchange/ref/ulticon/mock_ult_icon_gangster.png — 사각 타일을 꽉 채운 도트 그림, 어두운 붉은 바탕에 강한 빛.
- 이미 통과한 첫 장(같은 세트): Projects/AVSR/_exchange/in/ulticon_gangster.png — 바탕 톤 · 도트 굵기 · 빛 쓰는 법을 맞춘다.
- 액티브 아이콘 Projects/AVSR/_exchange/doc_skill_concept/img/active_thug.jpg · 지금 액티브 아이콘 Assets/BaseResource/HostSelectPanel/ultimateicon_thug.png 와 **한눈에 달라 보여야** 한다(같은 무기 · 같은 구도 금지).
- 시안에 없는 괴물 · 사람 형체를 지어내지 마라(첫 장 1차가 그래서 반려됐다) — 필살기의 핵심 요소 둘(무엇이 · 어떻게 터지나)만 굵게.
- 정사각 타일을 **바탕까지 꽉 채워** 그린다(투명 없음, 바깥 테두리 · 틀은 그리지 마라 — UI 가 테두리를 씌운다). 64 × 64 로 줄여도 읽히게 굵은 실루엣 하나 + 빛 한 줄기.
- 23장이 한 세트다 — 바탕은 어두운 붉은 · 보랏빛 톤을 공유하고, 강조색만 캐릭터 색. 글자 · 숫자를 넣지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트).
[저장] Projects/AVSR/_exchange/in/ulticon_thug.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
