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
[장면] mock_summon_lab_guide_peak — 가이드의 박자표대로. 공개 순간 — 빙의가 끝나 몸이 깨어나며 배양조 유리가 산산이 깨지고, 진짜 색 몸(블루 드래곤)이 튀어나와 착지한 장면. 유리 파편 · 액체 튐 · 등급색(금) 빛살 · 배경 등급색 번짐 · 아래에 이름 띠
- 크기는 바탕과 같은 720x1280.
[저장] Projects/AVSR/_exchange/in/mock_summon_lab_guide_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
