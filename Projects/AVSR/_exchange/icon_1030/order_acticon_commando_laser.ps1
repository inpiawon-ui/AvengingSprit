$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 스킬 아이콘 발주 — 액티브 · 필살기 그림이 서로 뒤바뀌어 있어 색 체계에 맞춰 새로 그린다.
[부품] 액티브(일반 스킬) 아이콘 — 호스트 「코만도(레이저)」 · 액티브 「충전 레이저」: 1초 힘을 모은 뒤 일직선으로 모든 적을 꿰뚫는 굵은 레이저.
- 구도 참조(일직선 빔 — 구도는 좋다, 색만 바꾼다): Assets/BaseResource/HostSelectPanel/ulticon_commando_laser.png
- 액티브 세트 색 기준: Assets/BaseResource/HostSelectPanel/ultimateicon_commando_grenade.png · Assets/BaseResource/HostSelectPanel/ultimateicon_commando_missile.png
- 액티브 = 어두운 남색 바탕 · 청록(시안) 빛. 갈래로 갈라지는 번개 모양은 넣지 마라(그건 필살기다).
- 정사각 타일을 바탕까지 꽉 채워 그린다(투명 · 여백 · 바깥 테두리 · 틀 없음 — UI 가 테두리를 씌운다). 핵심 요소는 타일 가운데, 가로 · 세로 70~80%.
- 64 × 64 로 줄여도 읽히게 굵은 실루엣. 글자 · 숫자 없음. 사람 · 괴물 형체를 지어내지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트). 같은 세트 기준 그림과 도트 굵기 · 빛 쓰는 법 · 바탕 톤을 똑같이.
[다시 그리기 — 1차 검수 반려 사유] 1차(Projects/AVSR/_exchange/icon_1030/v1/acticon_commando_laser.png)는 바탕이 거의 검정이고 세트의 「남색 바탕 + 가운데에서 퍼지는 청록 방사형 빛살」이 없어 옆 아이콘과 따로 논다.
- 바탕은 반드시 Assets/BaseResource/HostSelectPanel/ultimateicon_commando_grenade.png 처럼 밝은 남색(약 RGB 0,24,150) + 방사형 빛살로 꽉 채운다.
- 굵은 일직선 레이저가 아래 총구(작은 충전 고리)에서 위로 화면을 가로지른다 — 빔 둘레 청록 번쩍임 · 꿰뚫린 작은 불꽃 2~3개.
[저장] Projects/AVSR/_exchange/icon_1030/acticon_commando_laser.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
