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
[주제] 메인이 쓰러지면 — 셋 중 하나를 고른다
가운데 아래: 쓰러진 메인 호스트(녹색 양복 갱단) · 위로 빠져나오는 하얀 유령.
유령 위에 **갈림길 화살표 셋**이 부채꼴로 뻗고, 각 끝에 그림 하나씩:
 ① 왼쪽: 다른 적에게 빙의(유령 → 새 적, 청록 소용돌이)
 ② 가운데: 보조 호스트가 금빛 고리 위로 올라섬(보조가 메인이 됨, 위쪽 화살표)
 ③ 오른쪽: 쓰러진 호스트가 금빛 빛기둥 속에서 다시 일어남(제자리 부활)
유령 옆에 작은 모래시계(시간 제한).
[저장] Projects/AVSR/_exchange/in/guide1030_main_down.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
