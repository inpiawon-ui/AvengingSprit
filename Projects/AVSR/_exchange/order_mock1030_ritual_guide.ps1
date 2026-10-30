$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙. 걸려 있는 동안의 표시는 은은하게 계속 도는 장식 — 숨쉬기 · 흐름 · 반짝임 루프, 시전 순간만 힘준다).
화풍 · 화면 구성 기준: Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png (방금 통과한 필살기 시안 — 이 정도의 퀄 · 도트 질감).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/mock1030_skill/now_medium_infuse.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[인물] 메인 = 영매(원작 그림 Assets/BaseResource/Unit/medium/unit_medium_e.png · _atk1 · _atk2 그대로, 새로 디자인 금지) 화면 가운데 아래. 보조 둘 = 구루(Assets/BaseResource/Unit/guru/) · 화이트 위저드(Assets/BaseResource/Unit/white_wizard/) 원작 그림을 메인보다 조금 작게, 메인 좌우 뒤에(보조는 반투명 보랏빛 · 발밑 혼령 고리가 이미 있다). HUD 보조 칸 두 개에 그 둘의 얼굴.
영매 버프 색은 **보라**.
[장면] 필살기 「강신 의식」 — 35초간 영매 자신 광폭화(피해 +50% · 공속 +60%) + 파티 전원 공속 +40% · 치명 피해 +50%, 시전 순간 둘레 적 2초 둔화. 시전 순간의 큰 의식을 그린다. 그리고 **영매(광폭화 — 더 세게)와 보조들(버프 — 약하게)이 구분돼** 보여야 한다(지금 게임은 컷인 뒤 고리 한 번뿐 — Projects/AVSR/_exchange/mock1030_skill/now_medium_ritual.png).
[연출 가이드] Projects/AVSR/_exchange/mock1030_skill/guide_mock1030_skill.md 의 4절 박자표대로 — 발밑 큰 의식 진 + 하늘 빛기둥(강), 충격 고리로 둘레 적 둔화(강), 빛줄기가 보조 둘에게 갈라짐(중), 영매는 붉은-보라 불꽃 오라(광폭화) · 보조는 작은 금빛-보라 고리(약).
[저장] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_guide.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
