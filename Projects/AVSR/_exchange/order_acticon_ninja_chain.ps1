$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 부품 발주.
[부품] 액티브(일반 스킬) 아이콘 — 호스트 「닌자(사슬)」 · 액티브 「사슬 결박」: 방 안 모든 적을 5초간 묶는다. 묶인 적은 더 아프게 맞는다.
PD 가 고른 필살기 아이콘 세트와 **같은 느낌**으로 액티브 아이콘 23장을 새로 그린다. 이 장은 그중 하나.
- 같은 세트(필살기, 화풍 · 도트 굵기 · 빛 쓰는 법 기준): Projects/AVSR/_exchange/in/ulticon_ninja_chain.png · 세트 전체 대조판 Projects/AVSR/_exchange/ref/ulticon/ulticon_sheet_23.png
- 이 액티브의 연출 시안(무엇이 일어나는지): Projects/AVSR/_exchange/doc_skill_concept/img/active_ninja_chain.jpg
- 이미 통과한 첫 장(액티브 세트): Projects/AVSR/_exchange/in/acticon_gangster.png — 바탕 톤 · 요소 크기 · 도트 굵기를 맞춘다.
- **필살기 아이콘과 한눈에 구분**되게: 필살기 = 어두운 붉은 바탕 · 붉은 빛, 액티브 = **어두운 남색 바탕 · 청록(시안) 빛**. 모양도 필살기와 겹치지 않게(필살기가 「큰 한 방」이면 액티브는 그 컨셉을 작게 쓰는 순간).
- 시안에 없는 괴물 · 사람 형체를 지어내지 마라 — 액티브의 핵심 요소 둘(무엇이 · 어떻게)만 굵게.
- **크기 통일(PD 「사이즈가 안 맞는 것이 있었다」)**: 정사각 타일을 바탕까지 꽉 채워 그린다(투명 · 여백 · 바깥 테두리 · 틀 없음 — UI 가 테두리를 씌운다). 핵심 요소는 **타일 가운데, 가로 · 세로 70~80% 를 차지**하게 — 23장 모두 같은 비율.
- 64 × 64 로 줄여도 읽히게 굵은 실루엣. 글자 · 숫자 없음.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트).
[저장] Projects/AVSR/_exchange/in/acticon_ninja_chain.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
