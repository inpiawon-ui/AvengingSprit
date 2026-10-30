$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(색의 뜻 — 내 공격 = 주황 · 금).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주.
코만도(기관총) 액티브 「제압 사격」의 총구 섬광 — 위로 솟는 불꽃.
[정답 그림] 통과 시안 Projects/AVSR/_exchange/mock1030_skill/mock_suppress_free2.png 의 총구 위 섬광. 크롭(3배): Projects/AVSR/_exchange/fx_skill1030/anchor_sup_flare.png — 총구에서 위쪽으로 솟는 하얀 심지 + 노랑 · 주황 불꽃 기둥, 아래가 좁고 위로 퍼지는 원뿔.
[그릴 것] 총구 위 섬광만(총 · 사람 빼고). **위를 향한** 세로 그림, 아래 가운데가 총구 자리(가장 밝은 흰빛), 위로 노랑 → 주황으로 퍼지며 끝이 갈라진다. 도트 가장자리 깔끔(얼룩 · 잔 점 없음).
[프레임] 3프레임 가로로 나란히 — 섬광이 번쩍 → 크게 → 잦아듦. 프레임 사이 검은 빈칸 넉넉히.
[바탕] 순수 검정(#000000) 단색 — 빛 효과라 게임이 검정을 투명으로 바꾼다.
[크기] 캔버스 960 × 400. 한 프레임 약 288 × 384 칸 안에 꽉 차게(게임에서 약 72 × 96 으로 줄여 쓴다).
글자 · 숫자 · 사람 · 총 없음.
[저장] Projects/AVSR/_exchange/fx_skill1030/sheet_sup_flare.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
