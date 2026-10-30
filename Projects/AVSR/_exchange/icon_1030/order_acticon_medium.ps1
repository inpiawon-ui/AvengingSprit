$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 스킬 아이콘 발주 — 스킬이 바뀌어 아이콘을 새로 그린다.
[부품] 액티브(일반 스킬) 아이콘 — 호스트 「영매」(보랏빛 영혼 무녀 · 버프 마법사) · 액티브 「혼 깃들이기」: 8초 동안 파티 전원(메인 · 보조)의 공격력과 치명타가 오른다. 보랏빛 혼불 여러 개가 아군 쪽으로 흘러가 깃드는 순간(위로 솟는 강화 화살표 느낌의 혼불 · 빛나는 혼의 고리).
- 소환물 · 골렘 · 해골 · 원혼(괴물 얼굴) 그림 금지 — 이 스킬은 소환이 아니라 버프다.
- 액티브 세트 기준(바탕 톤 · 빛): Assets/BaseResource/HostSelectPanel/ultimateicon_commando_grenade.png · Assets/BaseResource/HostSelectPanel/ultimateicon_commando_missile.png
- 액티브 = 어두운 남색 바탕 · 청록(시안) 빛이 기본, 혼불만 보랏빛을 섞어 영매 느낌을 낸다.
- 정사각 타일을 바탕까지 꽉 채워 그린다(투명 · 여백 · 바깥 테두리 · 틀 없음 — UI 가 테두리를 씌운다). 핵심 요소는 타일 가운데, 가로 · 세로 70~80%.
- 64 × 64 로 줄여도 읽히게 굵은 실루엣. 글자 · 숫자 없음. 시안에 없는 괴물 · 사람 형체를 지어내지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트). 같은 세트 기준 그림과 도트 굵기 · 빛 쓰는 법 · 바탕 톤을 똑같이.
[저장] Projects/AVSR/_exchange/icon_1030/acticon_medium.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
