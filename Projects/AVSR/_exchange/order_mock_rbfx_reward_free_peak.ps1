$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — rbfx_reward 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
**가이드 Projects/AVSR/_exchange/guide_reborn_fx.md 도 읽는다**(세기 · 등급 계단 · 박자) — 단 자유 시안이니 더 멋진 안이면 그쪽으로.
[바탕] Projects/AVSR/_exchange/ref/reborn_fx/base_reward.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
PD 말: 「연출 17종 코덱스 시안부터 진행해줘 · 인게임 연출은 지금 것 유지, 아웃게임 연출만 새로 컨셉(RE:BORN 화면)」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
[장면] mock_rbfx_reward_free_peak — 네가 아트 디렉터라면 어떻게 꾸미겠나. **힘주는 순간** — 보상 칸이 차례로 열리고(빛), 금화 4~6개가 위 막대 골드 칩으로 날아가는 순간(가이드 reward_reveal · coin_burst).
기준 그림은 참고일 뿐, 더 멋진 안이 있으면 그걸로. 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변) + 떠오르는 빛 알갱이를 함께.
- 크기는 바탕과 같은 720 x 1280. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_rbfx_reward_free_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
