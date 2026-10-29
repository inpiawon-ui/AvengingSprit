$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 인게임 UI 부품 — PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_fxstory_clear_v2.png (컷3) 와 **똑같은 모양 · 색 · 도트 그림체**로.
앵커(시안에서 이 부품만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_c/clear_frame.png — 이걸 그대로 따라 깨끗하게 다시 그린다.
[부품] result_frame — 챕터 클리어 결과창 틀 전체 — 흑철 기계 틀 · 금빛 관로 · 좌우 청백 유리관 · 맨 위 유령 문장(둥근 유리 안 흰 유령). 제목 판(「CHAPTER 1 CLEAR」 자리, 위에서 약 118~178) · 이름 판(약 195~255) · 큰 안쪽 판(약 280~660) · OK 자리(약 670~760)는 **글자 없이 빈 판**. 안쪽 판 안의 줄 판 · 상자 · 금화는 그리지 않는다
[크기] 게임에서 660 x 800 px 로 쓴다. 그 비율 그대로 캔버스에 크게(가로세로 비율 유지, 캔버스의 90% 정도 차게) 한 장.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림자 · 마젠타/분홍 색을 부품에 쓰지 않는다.
- 글자 · 숫자를 넣지 않는다(게임이 위에 글자를 얹는다). 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/setc_result_frame.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
