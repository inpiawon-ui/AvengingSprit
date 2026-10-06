$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[검수 대상] Projects/AVSR/_exchange/in/growth_starup_mock_v1.png — 1440 x 1280. 왼쪽 720 은 「호스트 육성」 화면, 오른쪽 720 은 「성급 올리기」 창이 뜬 장면.
[바탕 시안(통과본)] Projects/AVSR/_exchange/in/growth_merge_mock_v2.png · 공통 팝업 Projects/AVSR/_exchange/in/popup_mock_v2.png

[기획 의도] 몸은 조각 + 골드를 내고 성급을 올린다(10단계, 별 5개 · 한 단계 = 별 반 칸).
성급이 오르면 스킬 Lv 도 같이 오르고(성급 단계 = 스킬 Lv, Lv5 에 패시브 특수 효과), 능력치 강화 상한도 늘어난다.
사용자의 요구: 「조각을 모으고 돈을 내서 성급을 올리는 UI」 + 「성급이 오르면 스킬 레벨도 같이 오르는 유기성을 표현」.

[볼 것] 항목마다 「통과 / 고칠 것(구체적으로)」로 짧게.
1. 조각 + 골드 → 성급 UP 흐름이 첫눈에 읽히는가
2. 성급 → 스킬 Lv → 강화 상한이 함께 오른다는 연결(유기성)이 화면과 창에서 읽히는가. 더 잘 보이게 할 방법이 있으면 구체적으로
3. 숫자 · 상태가 서로 맞는가(조각 수, 별 개수, 스킬 Lv, 상한, 목록 썸네일의 별 등)
4. 통과본(v2 · 공통 팝업)과 같은 그림체 · 배치인가
5. 터치 크기(44px 이상) · 글자 깨짐
마지막 줄 종합 판정: 「그대로 보고」 또는 「고쳐서 다시」.
[저장] Projects/AVSR/_exchange/review_growth_starup1.md 에 한국어로.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
