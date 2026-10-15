$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — 상점 · 소환 · 패스 **장면 기획 시안**.
[반드시 먼저 읽는다] Projects/AVSR/_exchange/store_scenes.md — 이 장면(S10)의 구성이 정답이다(무엇이 · 어디에 · 어떤 상태로). 0절 판매 구성표도 본다.
[화풍 기준 — 이어서 그린다]
- 소환(보라 제단): Projects/AVSR/_exchange/in/mock_summon_a.png — 하단 바 네 칸 HOST · PLAY · SUMMON · SHOP 이 이 시안 모양.
- 상점(남색 · 하늘 네온): Projects/AVSR/_exchange/in/mock_store_currency.png · 패스: Projects/AVSR/_exchange/in/mock_store_pass.png · 로비: Projects/AVSR/_exchange/in/mock_store_lobby.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로. 호스트 얼굴은 Assets/BaseResource/Growth/Faces/face_{키}.png 를 그대로 쓴다.
[공통] 글자 한국어 · 값 원화(₩) · 젬. 이 게임 도트 화풍. 값 · 수량 · 남은 횟수 · 상태(구매 완료 · 잠김 · 받기)가 또렷하게. 그림 아래 여백에 한국어 메모 3줄 이내.
[연출 가이드] Projects/AVSR/_exchange/guide_summon_fx.md 를 읽는다 — 박자 · 강약 · 등급 예고 색 · 하지 말 것.
[장면 S10] 소환 연출 콘티 10회 — **한 장에 4컷(2x2)**: ① 0.9 예고(금 기둥 — 대박 있음) ② 1.3 열 갈래로 흩어져 뒷면 10장 ③ 2.2 차례로 뒤집히는 중(앞 6장 앞면 · 뒤 4장 뒷면, 그중 한 장은 금빛으로 떨림 — 미뤄 둔 대박) ④ 3.0 마지막 대박 카드만 따로 뒤집힘. 컷 라벨 · 박자표는 guide_summon_fx.md 그대로.
[고칠 것 — 1차 납품 Projects/AVSR/_exchange/in/scene_s10_fx10_r2.png 를 보고 이것만 고쳐 다시 그린다] ① 카드 속 얼굴에 우리 게임에 없는 것(유령 · 사막병사 · 근육맨)이 들어갔다 — **우리 호스트만**: Assets/BaseResource/Growth/Faces/face_{키}.png (gangster · thug · amazon · hopper · commando_mg · guru · salamander · ninja_chain · white_wizard 등) 그대로. 유령은 뽑기에서 안 나온다. ② 우리 게임엔 **S급이 없다** — 등급 표시는 B(하늘) · A(보라)뿐, 대박은 등급이 아니라 「조각 ×20」 금빛 테 + 「대박!」. 마지막 대박 카드는 「A · 닌자(사슬) · 조각 ×20 · 대박!」. 4컷 구성 · 라벨은 1차 그대로.
[저장] Projects/AVSR/_exchange/in/scene_s10_fx10_r2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
