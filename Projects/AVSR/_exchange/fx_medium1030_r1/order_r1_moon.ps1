$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 필살기 「강신 의식」.
[정답 시안] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_free1.png (PD 가 고른 보라 화풍 — 이 색 · 도트 질감 그대로).
35초 동안 영매 자신의 머리 뒤에 서 있는 초승달 문양(영매만의 표식 — 보조에게는 없다).
[정답 크롭(3배)] Projects/AVSR/_exchange/fx_medium1030_r1/anchor_r1_moon.png — 세로로 선 보라 초승달, 가운데로 내려오는 빛, 둘레 작은 반짝임.
[그릴 것] 초승달 문양만. 세로로 선 모양(정면).
[프레임] 4프레임 가로로 — 숨쉬듯 밝아졌다 어두워지고 반짝임이 둘레를 돈다(반복 루프).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1152 × 320. 한 프레임 약 240 × 280 칸 안에(게임에서 약 110 × 128).
[저장] Projects/AVSR/_exchange/fx_medium1030_r1/sheet_r1_moon.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
