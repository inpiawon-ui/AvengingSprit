$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — summon_lab 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/summon_lab/base_hold.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
[참고 그림] Assets/BaseResource/InGameMainUI/obj_lab_bulk.png
[참고 그림] Projects/AVSR/_exchange/ref/summon_lab/ref_gacha_reveal_1_ssr.jpg
PD 말: 「motion-library 의 gacha_reveal_1 참고해서 가차 만들어줘 · 우리 컨셉에 맞게 실험실에서 유령이 나오는 컨셉 등 컨셉을 잡아서」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
먼저 Projects/AVSR/_exchange/guide_summon_lab.md 를 읽는다(목표 모습 · 박자 · 강약).
[장면] mock_summon_lab_guide_idle — 가이드의 박자표대로. 버팀 — 왼쪽 아래 관에서 나온 작은 유령(금빛 몸빛 = 대박 예고)이 배양조로 날아 들어가 빙의한 직후. 액체 속 빈 몸 실루엣이 떨리고 두 눈에 금빛 불이 켜짐 · 유리에 금이 가기 시작 · 기포가 세게 오름
- 크기는 바탕과 같은 720x1280.
[저장] Projects/AVSR/_exchange/in/mock_summon_lab_guide_idle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
