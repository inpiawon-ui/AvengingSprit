$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/play/growth_16x9.png — 지금 「호스트 성장」 화면(위 재화 · 영웅 칸 · 스킬 셋 · 스킬 레벨 칸 · 아래 탭).
[주제] 오른쪽 아래 「인연 · 청부 표식」 칸이 무엇인지 잘 안 읽힌다. **인연을 한눈에 알 수 있게** 화면을 다시 꾸민다 — 인연 부분만 고쳐도 되고, 더 낫다면 스킬 · 인연 아래쪽 전체 배치를 바꿔도 된다.
  알려야 할 것: 인연 이름 · 지금 단계(1단) · 짝 호스트 얼굴과 성급(짝을 보조로 데려가면 켜진다) · 켜지면 바뀌는 것(이 호스트의 액티브 스킬이 「표식 붙은 적을 보조가 우선 노리고 보조 피해 +50%」로 강해짐) · 단계가 오르는 조건(두 호스트 중 낮은 성급).
  지금 화면의 화풍(남색 판 · 청록 테 · 금빛 강조 · 도트 아이콘)을 그대로 따른다. 다른 칸(액티브 · 패시브 · 필살기 레벨 칸)은 그대로 두거나 함께 정돈한다.
[저장] Projects/AVSR/_exchange/in/mock1029_growth_inyeon.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
