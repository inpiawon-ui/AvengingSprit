$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 **4장만**: ref/skillmock/orig_amazon_sheet.png · ref/skillmock/anchor_amazon_mid.png · ref/skillmock/anchor_amazon_slam.png · in/part_ld_poses_v1.png(자세만 참고 — 지난번에 크기가 작게 나왔다). 도구 한도가 5장이라 원작 낱장을 따로 넘기지 마라(지난번 이것 때문에 실패).
도구가 크게 그려 오면 칸 격자대로 288x288 로 **최근접 축소만** 해도 된다(그림을 새로 그리거나 잘라 붙이는 것은 금지). 바탕이 분홍이 아니면 분홍으로 다시 그려라.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 플레이어 캐릭터 **아마존의 새 스킬 동작 프레임**. 스킬 「번개 무희」(적 사이를 번개처럼 미끄러져 다니며 차고 베고, 마지막에 뛰어올라 내려찍는다).
[원작 — 반드시 이 사람 그대로] Projects/AVSR/_exchange/ref/skillmock/orig_amazon_sheet.png (원작 9장 — 행 s · e · n, 열 서기 · 공격1 · 공격2) (주황 긴 머리 · 파란 눈 · 분홍 튜브탑 · **파란 짧은 치마**(반바지 아님) · **양 팔목에 회색 보호대** · 분홍 부츠 · 맨손 격투). 얼굴 · 머리 · 옷 · 색 · 도트 크기 · 외곽선이 원작과 똑같아야 한다. 새로 그리는 것은 **자세뿐**.
[크기 기준 — 가장 중요] Projects/AVSR/_exchange/ref/skillmock/orig_amazon_sheet.png = 원작 9장을 이번 시트와 **같은 칸 배치(288x288, 한 칸 96)** 로 깐 것. 원작은 서 있을 때 키가 약 75px(칸 위 13 ~ 아래 87)이다.
지난 납품(in/part_ld_poses_v1.png)은 사람이 원작의 약 0.7배로 작게 그려졌고, 팔목 보호대가 빠졌고, 치마가 반바지가 됐고, 2행 3열은 머리가 둘로 보였다 — 이 넷을 고친다.
새 자세도 **도트 한 점 크기 · 머리 크기 · 몸 비율이 원작과 1:1** 이어야 한다(머리 폭 ≈ 원작 머리 폭). 다리를 뻗은 자세는 칸을 거의 다 채워도 된다. 머리는 하나, 몸 하나.
[동작 기준] Projects/AVSR/_exchange/ref/skillmock/anchor_amazon_mid.png (시안 가운데 컷 — 아마존이 날아차기 · 돌려차기로 적 사이를 옮겨 다니는 자세들) · anchor_amazon_slam.png (오른쪽 컷 — 뛰어올라 내려찍는 자세).
만들 것: 칸 9개 시트 — 가로 3열 × 세로 3행, 한 칸 96x96(원작 스프라이트와 같은 칸 크기, 캐릭터 크기도 원작과 같게), 전체 288x288. **바탕은 분홍(#FF00FF) 단색**(나중에 뚫는다). 이펙트 · 번개 · 그림자 · 잔상 없이 캐릭터만.
- 행: 1행 = 아래(화면 쪽, south) 보기 · 2행 = 오른쪽(east) 보기 · 3행 = 위(north, 등) 보기 — 원작 방향 그림과 같은 시점
- 열: 1열 = 돌진 날아차기(몸을 앞으로 쭉 뻗은 플라잉 킥) · 2열 = 돌려차기(한 다리를 높이 휘두름) · 3열 = 뛰어올라 내려찍기(공중에서 주먹 · 무릎을 아래로)
[저장] Projects/AVSR/_exchange/in/part_ld_poses.png (v1 은 덮어쓰지 말 것)
그린 뒤 orig_amazon_sheet.png 와 같은 배율로 나란히 놓고 머리 크기 · 키가 맞는지 스스로 확인하라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
