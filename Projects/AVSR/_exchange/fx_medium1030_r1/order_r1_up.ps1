$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매 필살기 「강신 의식」 — 버프가 걸린 몸 머리 위에 자연스럽게 떠 있는 작은 표시.
[꼴 · 크기 기준] 같은 영매의 액티브 표시 Assets/BaseResource/InGameMainUI/fx_mdinfup_1.png · _2 · _3 — 흰 위쪽 화살표 + 아이콘 하나를 나란히, 보라 테두리 빛, 도트 선 깔끔. 이것과 **같은 크기 · 같은 꼴**로.
[색 기준] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_free1.png (PD 가 고른 보라 화풍).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
숫자 · 글자 없음.
[그릴 것] 파티(메인 · 보조) 머리 위 표시 — 뜻은 「공격 속도 ↑ + 치명 피해 ↑」. 왼쪽에 위쪽 화살표 겹(공격 속도 — Assets/BaseResource/ReBornKit/Icons/rb_icon_stat_attackspeed_96.png 의 화살표 모양), 오른쪽에 별(치명 피해 — rb_icon_stat_critdamage_96.png 의 별 모양). 흰 심지 + 보라 테두리 빛.
[프레임] 3프레임 가로로 — 위아래로 살짝 둥실 + 빛이 반짝(반복).
[크기] 캔버스 768 × 192. 한 프레임 약 160 × 110 칸 안에(게임에서 약 52 × 32).
[저장] Projects/AVSR/_exchange/fx_medium1030_r1/sheet_r1_up.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
