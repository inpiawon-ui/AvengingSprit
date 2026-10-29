$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 설명(튜토리얼) 창에 들어가는 **설명 그림** 한 장.
[화풍 앵커 — 반드시 열어 본다] Assets/BaseResource/ReBornKit/Art/ 의 rb_art_guide_possess.png · rb_art_guide_stand_fire.png · rb_art_guide_cards.png · rb_art_guide_boss.png.
같은 세트로 보여야 한다: 어두운 남색 바탕 · 가운데 둥근 남색 얼룩 · 굵은 도트 · 하얀 유령(통통한 몸 · 짧은 꼬리 · 큰 입) · 청록 소용돌이 빛 · 금빛 강조.
[게임 화면 참고] Projects/AVSR/_exchange/ref/party_ui_1029/base_ingame_now.png · Projects/AVSR/_exchange/ref/party_hud_1029.png (지금 인게임 모습 — 호스트 · 적 생김새)
[공통] 가로 3 : 세로 2 비율(1536 x 1024 권장) 한 장. **글자 · 숫자 · 말풍선 글을 넣지 마라**(설명 글은 게임이 아래에 따로 쓴다). 화살표 · 손가락 · 아이콘으로 뜻이 읽히게.
한눈에 「무엇을 하라는지」 읽혀야 한다 — 주인공 동작 하나를 가운데 크게.
[주제] 무리 · 인연 — 같은 무리 호스트를 모으면 힘이 켜진다
가운데: 같은 옷 색(녹색 양복 갱단) 호스트 셋이 나란히 서 있고, 머리 위 작은 무리 문장(같은 모양 세 개)이 하나씩 금빛으로 켜진다.
셋을 잇는 금빛 빛줄 + 위로 솟는 금빛 기운(힘이 켜짐).
오른쪽 작게: 서로 다른 둘(예: 군인 · 마법사)이 **금빛 사슬 고리**로 이어져 하트 · 반짝임(인연 짝).
[저장] Projects/AVSR/_exchange/in/guide1030_synergy.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
