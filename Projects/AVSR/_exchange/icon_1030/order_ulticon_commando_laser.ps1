$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 스킬 아이콘 발주 — 액티브 · 필살기 그림이 서로 뒤바뀌어 있어 색 체계에 맞춰 새로 그린다.
[부품] 필살기 아이콘 — 호스트 「코만도(레이저)」 · 필살기 「프리즘 레이저」: 굵은 빔이 첫 적에 꽂힌 뒤 거기서 갈래로 갈라져 둘레 적에게 꺾여 든다.
- 구도 참조(가운데에서 갈래로 뻗는 번개 구도 — 구도는 좋다, 색만 바꾼다): Assets/BaseResource/HostSelectPanel/ultimateicon_commando_laser.png
- 필살기 세트 색 기준: Assets/BaseResource/HostSelectPanel/ulticon_commando_missile.png · Assets/BaseResource/HostSelectPanel/ulticon_commando_grenade.png
- 필살기 = 어두운 붉은 바탕 · 붉은 빛. 굵은 빔 한 줄이 꽂히는 자리에서 4갈래로 꺾여 나간다.
- 정사각 타일을 바탕까지 꽉 채워 그린다(투명 · 여백 · 바깥 테두리 · 틀 없음 — UI 가 테두리를 씌운다). 핵심 요소는 타일 가운데, 가로 · 세로 70~80%.
- 64 × 64 로 줄여도 읽히게 굵은 실루엣. 글자 · 숫자 없음. 사람 · 괴물 형체를 지어내지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트). 같은 세트 기준 그림과 도트 굵기 · 빛 쓰는 법 · 바탕 톤을 똑같이.
[다시 그리기 — 1차 검수 반려 사유] 1차(Projects/AVSR/_exchange/icon_1030/v1/ulticon_commando_laser.png)는 화면 전체가 밝은 주황이라 세트의 「어두운 붉은 바탕」이 없고, 「꽂힌 뒤 갈라짐」이 X자 지그재그로만 보인다.
- 바탕은 반드시 Assets/BaseResource/HostSelectPanel/ulticon_commando_missile.png 처럼 어두운 진홍(약 RGB 125,0,15) + 붉은 방사형 빛살.
- 위에서 굵은 빔 한 줄이 내려와 가운데 한 점(프리즘 결정)에 꽂히고, 그 점에서 4갈래 가는 빔이 사방으로 꺾여 뻗는다 — 「한 줄 → 한 점 → 네 갈래」가 한눈에 읽히게.
[저장] Projects/AVSR/_exchange/icon_1030/ulticon_commando_laser.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
