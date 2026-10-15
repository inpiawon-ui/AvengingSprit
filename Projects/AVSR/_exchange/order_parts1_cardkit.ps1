$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라(사각형 · 원을 코드로 찍어 만든 것은 반려). 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 로비 **호스트 카드 부품 견본 시트**. 게임에 그대로 잘라 넣을 부품이다.
[먼저 볼 것]
- 정답 모양(PD 확정 시안): Projects/AVSR/_exchange/ref/store_mock/anchor_cards.png(카드 시트 — 등급별 테두리 색 · 고른 금 테 · 오늘의 몸 띠 · 자물쇠 · 별) · Projects/AVSR/_exchange/ref/store_mock/anchor_cardrow.png(로비 카드 줄)
- 지금 게임 부품(같은 결 · 같은 두께로 이어서): Projects/AVSR/_exchange/ref/store_mock/existing_card_parts.png — 왼쪽부터 지금 카드 틀(하늘 네온 · 속 비움) · 고른 틀(금) · 별 켬 · 별 끔 · 체크 · 전투력 아이콘. 원본은 Assets/BaseResource/ChapterHost/ch_card_frame.png · ch_card_frame_sel.png · ch_star_on.png · ch_star_off.png
- 기획: Projects/AVSR/_exchange/order_store_parts.md 「1차」 표
[그릴 것 — 분홍(#FF00FF) 바탕 한 장에, 부품끼리 40px 이상 띄워서, 글자 · 이름표 없이]
1줄: 카드 틀 셋 — **속이 빈 테두리**(안쪽은 분홍 그대로 = 나중에 투명) · 지금 ch_card_frame 처럼 모서리 깎인 네온 테두리 · 가장자리 두께가 고르게(가로세로로 늘려 써도 깨지지 않게) · 각 150x200 정도:
   B = 하늘빛 네온 · A = 보라 네온 · S = 금빛 네온(셋 다 같은 모양, 색만 다름)
2줄: 고른 카드 덧판 — 금빛 굵은 네온 테두리 + 오른쪽 위 체크 칸(지금 ch_check 결) · 속 비움 · 160x210 정도
3줄: 등급 배지 셋(각 44x44) — 각진 작은 판에 큰 글자 **B**(하늘) · **A**(보라) · **S**(금) — 이 세 글자만 그림에 넣는다
4줄: 「오늘의 몸」 띠 — 가로로 긴 리본 판(150x30), 보라 바탕 · 양 끝 접힌 리본, **글자 없음**
5줄: 별 반 칸(32x32) — ch_star_on 과 같은 별인데 왼쪽 반만 노랑 · 오른쪽 반은 ch_star_off 색 · 자물쇠(48x56) — 흰 회색 도트 자물쇠, 어두운 테두리
- 빛 번짐을 분홍 위로 넓게 퍼뜨리지 않는다(뚫을 때 지저분해진다) — 네온 빛은 테두리 바로 옆 2~3px 까지만.
[저장] Projects/AVSR/_exchange/in/parts1_cardkit.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
