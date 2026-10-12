$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(빛 = 도트 모양 + 셰이더 광원 · 파티클, 강약).
게임 「Avenging Spirit RE:BORN」 로비 — 「?(랜덤 선택)」 카드와 아래 랜덤 설명 칸 **시안**. 네가 아트 디렉터라면 어떻게 꾸미겠나.
[바탕] Projects/AVSR/_exchange/ref/hostinfo_mock/game_random.png — 지금 실제 게임 화면(720x1280). 「?」 카드(카드 줄 맨 왼쪽, 금색 테두리)와 아래 「랜덤 선택」 칸 **두 곳만** 다시 꾸미고 나머지는 그대로.
PD 말: 「?선택 아이콘도 안 이쁘고 설명창도 이상해, 만들다가 만 것 같아」.
지금 문제: 주사위 그림이 배경 사각 틀째 붙어 사신 위를 네모로 덮는다 · 가린 사신 실루엣이 작고 밋밋하다 · 설명 칸 왼쪽이 허전하다.
⚠ 원작 IP — 사신 모양은 원작과 100% 같아야 한다: 사신은 Assets/BaseResource/HostSelectPanel/hostportraitimage_death.png 를 **그대로**(모양을 다시 그리지 말 것) 어두운 남보라로만 가린 실루엣으로 쓴다. 크기 · 자리는 네가 정한다.
주사위 + 「?」 는 네가 새로 디자인해도 된다(원작 캐릭터가 아니다) — 배경 틀 없이, 도트 화풍.
꾸밈: 「전설이 숨어 있다」는 신비한 느낌(보라 · 청록 광원, 떠오르는 빛 알갱이 등). 강약 — 고른 「?」 카드는 또렷하게, 설명 칸은 은은하게. 글자는 가리지 않는다.
설명 칸 글: 「랜덤 선택 — 모든 호스트 중 하나가 랜덤으로 선택됩니다. 낮은 확률로 전설 호스트 등장!」 (원래 승인 시안처럼 「모든 호스트」 청록 · 「전설 호스트」 금색 강조 가능).
그림 옆 여백에 한국어 메모 3줄 이내(무엇이 · 어떻게 움직이나).
[저장] Projects/AVSR/_exchange/in/mock_random_v1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
