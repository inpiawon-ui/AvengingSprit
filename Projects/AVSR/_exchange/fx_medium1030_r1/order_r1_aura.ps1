$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 필살기 「강신 의식」.
[정답 시안] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_free1.png (PD 가 고른 보라 화풍 — 이 색 · 도트 질감 그대로).
35초 동안 영매 자신(광폭화)의 몸을 감싸는 보라 기운 오라 — 보조보다 확실히 세 보여야 한다.
[정답 크롭(3배)] Projects/AVSR/_exchange/fx_medium1030_r1/anchor_r1_self.png — 영매를 감싸는 보라 · 흰 빛, 위로 솟는 보라 연기 줄기와 반짝임(같은 시안 화풍).
[그릴 것] 오라만(인물 빼고 — 가운데는 비워 사람이 서 있을 자리). 아래가 넓고 위로 솟는 보라 기운 · 연기 줄기, 반짝 알갱이.
[프레임] 4프레임 가로로 — 일렁이며 위로 솟는다(반복 루프, 첫 · 끝 프레임이 이어지게).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1152 × 384. 한 프레임 약 256 × 320 칸 안에(게임에서 약 150 × 184).
[저장] Projects/AVSR/_exchange/fx_medium1030_r1/sheet_r1_aura.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
