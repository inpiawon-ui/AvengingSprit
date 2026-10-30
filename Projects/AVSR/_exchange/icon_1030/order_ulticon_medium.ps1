$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 스킬 아이콘 발주 — 스킬이 바뀌어 아이콘을 새로 그린다.
[부품] 필살기 아이콘 — 호스트 「영매」 · 필살기 「강신 의식」: 영매 자신이 신을 내려 광폭화하고, 35초 동안 파티 전원의 공격 속도 · 치명타 피해가 오른다. 시전자 둘레로 큰 혼의 고리(마법진)가 퍼지고 하늘에서 빛기둥이 내리꽂히는 의식의 순간.
- 소환물 · 골렘 · 해골 · 원혼(괴물 얼굴) 그림 금지.
- 필살기 세트 기준(바탕 톤 · 빛): Assets/BaseResource/HostSelectPanel/ulticon_commando_missile.png · Assets/BaseResource/HostSelectPanel/ulticon_commando_grenade.png
- 필살기 = 어두운 붉은 바탕 · 붉은 빛이 기본, 혼의 고리만 보랏빛을 섞는다. 「큰 한 방」 느낌으로 크게.
- 정사각 타일을 바탕까지 꽉 채워 그린다(투명 · 여백 · 바깥 테두리 · 틀 없음 — UI 가 테두리를 씌운다). 핵심 요소는 타일 가운데, 가로 · 세로 70~80%.
- 64 × 64 로 줄여도 읽히게 굵은 실루엣. 글자 · 숫자 없음. 시안에 없는 괴물 · 사람 형체를 지어내지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트). 같은 세트 기준 그림과 도트 굵기 · 빛 쓰는 법 · 바탕 톤을 똑같이.
[저장] Projects/AVSR/_exchange/icon_1030/ulticon_medium.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
