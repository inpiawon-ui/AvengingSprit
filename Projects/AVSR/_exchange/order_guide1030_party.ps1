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
[주제] 메인 · 보조 — 메인 호스트 뒤를 보조 둘이 따라다니며 같이 싸운다
가운데: 메인 호스트(녹색 양복 갱단, 크게) — 발밑 금빛 고리 + 머리 위 작은 왕관 표식. 앞쪽 적에게 총을 쏜다.
메인 양옆 조금 뒤: 보조 호스트 둘(조금 작게, 서로 다른 생김새 — 예: 군인 · 깡패) — 발밑 청록 고리. 같은 적을 향해 쏜다.
메인과 보조 사이를 잇는 은은한 청록 끈(같이 다닌다는 뜻). 오른쪽에 적 하나.
[저장] Projects/AVSR/_exchange/in/guide1030_party.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
