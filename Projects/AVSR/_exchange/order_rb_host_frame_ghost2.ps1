$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕으로 받는다(바탕을 지우지 말고 그대로 둔다).
게임 「Avenging Spirit RE:BORN」 로비의 **유령 카드 틀** 1장을 다시 그린다.
지금 유령 틀: Assets/BaseResource/ReBornKit/Components/rb_host_frame_ghost.png — 은백색 틀인데 좌우 · 아래에 **구름(연기) 장식**이 감겨 있다. PD 「유령 프레임에 구름 같은 거 이상해, 제거해 줘」.
모양 기준(같은 크기 · 같은 두께 · 같은 모서리): Assets/BaseResource/ReBornKit/Components/rb_host_frame_r.png (파란 R 카드 틀)
[만들 것] rb_host_frame_r 과 **똑같은 모양 · 크기 · 선 두께 · 둥근 모서리**의 세로 카드 틀. 색만 유령 틀의 **은백색 · 옅은 하늘빛 광택**(지금 유령 틀의 선 색). 구름 · 연기 · 덩굴 같은 바깥 장식은 **하나도 없다**. 틀 안쪽은 비운다(캐릭터 · 글자 · 아이콘 · 배지 없음). 바깥 테두리 둘레의 은은한 빛은 R 틀 정도만.
[크기] 266x332 한 장(틀이 그림 전체를 꽉 채운다 — 위아래 · 좌우 여백 2 px 이하).
[저장] Projects/AVSR/_exchange/in/rb_host_frame_ghost2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
