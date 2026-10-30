$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 스킬 아이콘 발주 — 스킬이 바뀌어 아이콘을 새로 그린다.
[부품] 액티브(일반 스킬) 아이콘 — 호스트 「코만도(기관총)」 · 액티브 「제압 사격」: 앞쪽 부채꼴로 2초 동안 기관총을 연사하고, 맞은 적은 30% 느려진다. 아래(가운데)에서 위쪽 부채꼴로 퍼져 나가는 굵은 총탄 줄기 여러 갈래 + 총구 섬광, 탄이 퍼지는 부채꼴 모양이 한눈에 읽히게.
- 지금 아이콘(방어막 구슬)은 옛 스킬 그림이라 틀렸다 — 구슬 · 방패 모양 금지.
- 액티브 세트 기준(바탕 톤 · 빛): Assets/BaseResource/HostSelectPanel/ultimateicon_commando_grenade.png · Assets/BaseResource/HostSelectPanel/ultimateicon_commando_missile.png
- 액티브 = 어두운 남색 바탕 · 청록(시안) 빛. 같은 코만도의 필살기(탄막 요새) 그림 Assets/BaseResource/HostSelectPanel/ulticon_commando_mg.png 와 모양이 겹치지 않게.
- 정사각 타일을 바탕까지 꽉 채워 그린다(투명 · 여백 · 바깥 테두리 · 틀 없음 — UI 가 테두리를 씌운다). 핵심 요소는 타일 가운데, 가로 · 세로 70~80%.
- 64 × 64 로 줄여도 읽히게 굵은 실루엣. 글자 · 숫자 없음. 시안에 없는 괴물 · 사람 형체를 지어내지 마라.
- 캔버스 512 × 512, 픽셀 아트(굵은 도트). 같은 세트 기준 그림과 도트 굵기 · 빛 쓰는 법 · 바탕 톤을 똑같이.
[저장] Projects/AVSR/_exchange/icon_1030/acticon_commando_mg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
