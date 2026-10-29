$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 연출 **시안**. 먼저 Projects/AVSR/_exchange/guide_result_fx.md 를 읽는다(목표 모습 · 박자 · 강약).
[바탕] Projects/AVSR/_exchange/ref/result_mock/base_gold_done.png — 지금 게임 화면(2배). 창 · 칸 · 글자 · 금화 · 상자 그림은 **그대로** 두고 그 위에 이펙트만 덧그린다.
[목표 느낌] 예전 그림에 박혀 있던 빛: Assets/BaseResource/ChapterScreens/reward_goldpile.png (금화 둘레 금빛 · 반짝 별) · Assets/BaseResource/LobbyMainUI/chest_silver.png (상자 둘레 파란 빛 번짐 · 뾰족한 빛살 · 반짝임).
이 빛을 지우는 게 아니라 **살아 있는 이펙트로 더 멋지게** 보여 주는 장면이다. 도트 게임에 어울리게.
[장면] mock_result_gold_peak — 1.30초 금화 완성 순간(강) — 더미 뒤 금빛이 번쩍 확 퍼진 정점, 둘레에서 반짝 별 4~6개가 팡 터진 순간
- 빛은 물건 뒤, 반짝 별만 물건 모서리에 걸쳐도 된다. 오른쪽 글자(골드 획득 · +1,240 · 은 상자)는 덮지 않는다.
- 크기는 바탕과 같은 920 x 690.
[저장] Projects/AVSR/_exchange/in/mock_result_gold_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
