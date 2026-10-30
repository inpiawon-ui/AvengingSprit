$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(색의 뜻 — 내 공격 = 주황 · 금, 전투 방은 내 공격만 따뜻하게 또렷하게).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주 — 코만도(기관총) 필살기 「탄막 요새」의 날아가는 기관총 탄(트레이서) 한 발.
[정답 그림 — 이 느낌 그대로] 통과 시안 Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png 의 총알 줄기. 크롭: Projects/AVSR/_exchange/fx_mgtracer_1030/anchor_fan.png (부채꼴 전체) · Projects/AVSR/_exchange/fx_mgtracer_1030/anchor_streak_x3.png (줄기 하나 3배 확대 — 하얀 심지 + 주황 · 금 번짐의 길쭉한 탄이 끊어진 점선으로 이어진다).
[그릴 것] 그 줄기에서 탄 **한 발**만 떼어 낸 모양 — 오른쪽을 향한 가로 그림. 오른쪽 끝(머리)이 가장 밝은 흰빛 · 연노랑 심지, 왼쪽으로 갈수록 주황 → 진주황으로 짧게 타며 가늘어지는 꼬리. 길이 : 굵기 = 약 5 : 1. 가장자리는 도트로 깔끔하게(번짐 얼룩 · 잔 점 · 떠도는 불티 없음 — 지금 게임 그림이 지저분하다는 지적을 받았다).
[프레임] 같은 탄 3프레임을 한 장에 가로로 나란히 — 셋이 아주 살짝만 다르다(머리 밝기 · 꼬리 길이가 조금씩 깜빡이는 정도). 프레임 사이는 검은 빈칸으로 넉넉히 띄운다(서로 닿지 않게).
[바탕] 순수한 검정(#000000) 단색 바탕 — 빛 효과라 게임이 검정을 투명으로 바꿔 쓴다. 바탕에 무늬 · 바닥 · 그림자 금지.
[크기] 캔버스 1152 × 256. 한 프레임의 탄은 약 288 × 60 칸 안에 꽉 차게(게임에서 약 96 × 20 으로 줄여 쓴다 — 줄여도 심지와 꼬리가 또렷하게 굵은 도트).
글자 · 숫자 · 총 · 사람 없음.
[저장] Projects/AVSR/_exchange/fx_mgtracer_1030/sheet_mgtracer.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
