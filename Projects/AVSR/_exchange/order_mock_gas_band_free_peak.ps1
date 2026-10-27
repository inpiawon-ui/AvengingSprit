$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — gas_band 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/base_gas_band.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
[참고 그림] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png
PD 말: 「부분적으로 떨어지는 위험 · 스킬도 아니고 너무 과하면 안 된다(4챕터 번개 1차 반려) · 기믹은 적도 맞게 · 무대마다 모양이 달라야 한다」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
[장면] mock_gas_band_free_peak — 네가 아트 디렉터라면 어떻게 꾸미겠나. 가로 한 줄(캐릭터 1.5명 높이 띠, 방 높이의 15% 안)에서 바닥 배관 틈을 따라 노란 기 도는 연두 독가스가 한꺼번에 피어오른 순간 — 그 줄에 선 적 1명도 가스에 휩싸임, 다른 줄은 깨끗. 참고 그림은 4챕터에서 통과한 크기 · 밀도 기준일 뿐
기준 그림은 참고일 뿐, 더 멋진 안이 있으면 그걸로. 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변) + 떠오르는 빛 알갱이를 함께.
- 크기는 바탕과 같은 720 x 1280. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_gas_band_free_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
