$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/play/real_clear_16x9.png — 판이 끝난 결과 화면(획득 보상에 호스트 조각 칸들). 이 결과 화면 위에 해금 연출이 뜬다.
[장면] **호스트 해금 순간** — 조각이 모여 해금 수에 닿는 순간 「드라군 해금!」 연출. 힘주는 순간 한 장.
  결과 화면이 살짝 어두워지고 가운데에 해금된 호스트(원작 그림 Assets/BaseResource/Growth/Portraits/portrait_dragoon.png 를 그대로 크게)가 영혼 빛과 함께 나타난다.
  조각들이 모여 몸이 되는 흐름, 「새 호스트 해금!」 큰 제목, 등급 딱지(SSR), 호스트 이름, 아래 「확인」 · 「성장으로」 단추.
  보상의 무게: 자주 보지 않는 큰 순간이다 — 확실히 세게(강). 그러나 글자 · 얼굴을 빛이 덮지 않게.
[저장] Projects/AVSR/_exchange/in/mock1029_host_unlock.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
