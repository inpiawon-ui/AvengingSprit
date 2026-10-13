$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_skill_cast.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720x1280) 인게임 — **호스트 스킬 시전(캐스팅) 연출 시안**.
[바탕] Projects/AVSR/_exchange/ref/enemycast_mock/base_now.png — 지금 실제 게임 화면. 가운데 초록 옷 · 모자 남자가 **적 호스트 갱스터**, 아래 주황 머리 여자가 **플레이어가 탄 아마존**. HUD · 버튼 · 방 · 캐릭터 그림은 그대로 두고 그 위에 연출만 덧그린다. 캐릭터 자세를 새로 그리지 않는다.
[참고] 플레이어 스킬 지금 모습(초상 + 이름 띠 컷씬): Projects/AVSR/_exchange/ref/skillfx_now/cast_1.png · 스킬 아이콘: Assets/BaseResource/HostSelectPanel/ultimateicon_gangster.png · ultimateicon_amazon.png
PD 말: 「호스트가 적일 때 스킬을 써도 연출이 따로 없어서 표시가 안 날 것 같다, 전체적으로 캐스팅 모션이 있어야 할까?」 · 「플레이어가 호스트일 때도 동일하게 적용되어야 할 것 같다」.
지금은 적 스킬 예고가 평타 예고(몸이 노랗게)와 똑같아 안 읽힌다. 게임을 멈추지 않는다(컷씬 아님) — 싸움은 흐르고 시전하는 몸 둘레에서만 일이 일어난다.
[장면] **적 갱스터가 스킬을 시전**한다. 네가 아트 디렉터라면 「적이 지금 큰 기술을 쓴다」를 어떻게 보이겠나 — 평타 예고와 확실히 다르게, 위험하다는 느낌으로. 가이드는 참고일 뿐 더 멋진 안이 있으면 그걸로. 플레이어 쪽에도 같은 모양을 쓸 것이라는 점을 염두에 둔다.
그릴 것: 화면 두 장 나란히(각 720x1280, 합쳐 1440x1280) — 왼쪽 = **차오르는 중**(시전 0.8초쯤), 오른쪽 = **발동 순간**(가장 강한 박자). 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 어떻게 움직이나).
말풍선 · 표식 안에 글자를 넣지 않는다. HUD · 체력바를 가리지 않는다.
[저장] Projects/AVSR/_exchange/in/mock_cast_enemy_free.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
