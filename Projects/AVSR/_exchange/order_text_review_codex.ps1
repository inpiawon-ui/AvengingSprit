$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
너는 모바일 게임 「Avenging Spirit RE:BORN」(유령이 적의 몸 =「호스트」에 빙의해 싸우는 도트 액션 로그라이트)의 현지화 · 게임 UX 문구 검수자다.
**게임 파일(TSV · 코드 · 표)은 절대 고치지 않는다. 검수 파일 하나만 쓴다.** 답은 모두 한국어로 쓴다.

[검수할 것] Projects/AVSR/_exchange/text_review/changes.tsv — 다른 검토자가 낸 문구 수정안(열: key · 기존 ko · 새 ko · 새 ja · 새 en · 새 zh_hant · 새 zh_hans · 이유). 310줄.
[용어집] Projects/AVSR/_exchange/text_review/glossary.md
[원문 전체] Projects/AVSR/Localization/AVSR_Strings.tsv (열 key · ko · ja · en · zh_hant · zh_hans)

PD 요청: 「어색한 문구 · 말이 안 되는 문장을 고치고, 게임 용어는 전 세계에서 흔히 쓰는 대표 게임 용어로. 영어 단순 번역 느낌 말고 구어체 · 게임 용어 · 상황 · 기능에 맞게. 스킬 쪽을 특히 잘.」

[볼 것]
1. 새 ko 가 자연스러운 한국 게임 말인가(번역투 · 어색한 조사 · 너무 긴 문장 · 같은 뜻 다른 말).
2. 용어 통일(호스트 · 빙의 · 유령 체력 · 쿨타임 · 치명타 · 넉백 · 기절 · 속박 · 받는 피해 증가 등)이 수정안 전체에서 지켜졌는가. 수정안에 빠졌지만 원문에 남은 같은 문제도 찾아 낸다.
3. 스킬(askill · pskill · ult · card) 설명이 「무엇을 · 얼마나 · 언제」 한눈에 읽히는가. 원문 뜻이 빠지거나 바뀌지 않았는가.
4. ja · en · zh_hant · zh_hans 가 각 언어 게임에서 쓰는 자연스러운 말인가(직역 · 오역 · 용어 불일치).
5. `{0}` 같은 자리 표시 · `<color>` 태그 · `\n` 이 원문과 같은가.
6. UI 칸 넘침 — 원문보다 크게 길어진 버튼 · 제목.

[쓸 파일] Projects/AVSR/_exchange/text_review/codex_review.tsv — 열: key · 판정(통과 / 고침 / 되돌림) · 고친 ko · 고친 ja · 고친 en · 고친 zh_hant · 고친 zh_hans · 이유. **통과한 줄은 적지 않는다**(고침 · 되돌림만). 고친 열은 바꿀 언어만 채운다. 수정안에 없지만 원문에서 새로 찾은 문제 줄도 같은 형식으로 「고침」에 적는다.
마지막 줄 다음에 빈 줄 하나 두고 「# 총평」 으로 시작하는 몇 줄 요약(전체 품질 · 자주 보인 문제 · 남은 위험).
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
