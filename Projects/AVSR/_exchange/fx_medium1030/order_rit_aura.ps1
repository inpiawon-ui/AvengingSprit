$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
필살기 「강신 의식」 — 35초 동안 영매 자신(광폭화)의 몸을 감싸는 붉은-보라 불꽃 오라(반복).
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_guide.png 의 영매 둘레 붉은 · 보라 불꽃. 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_rit_aura.png
[그릴 것] 불꽃 오라만(인물 빼고 — 가운데는 비워 사람이 서 있을 자리). 아래가 넓고 위로 솟는 불꽃 혀, 붉은 · 주홍 + 보라 섞임.
[프레임] 4프레임 가로로 — 불꽃이 일렁이며 위로 솟는다(반복 루프, 첫 · 끝 프레임이 이어지게).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1152 × 384. 한 프레임 약 256 × 320 칸 안에(게임에서 약 150 × 180).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_rit_aura.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
