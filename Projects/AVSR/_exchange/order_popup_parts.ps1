param([string]$Which)
$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$common = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
통과한 시안: Projects/AVSR/_exchange/in/popup_mock_v2.png — 이 시안의 공통 알림 팝업을 게임 부품으로 쪼개는 중이다. 시안과 같은 그림체 · 같은 재질 · 같은 색으로.
같은 가족 참조: Projects/AVSR/_exchange/ref/popup_ref_result_frame.png · popup_ref_chest_frame.png · popup_ref_button_yellow.png
부품만 그린다 — 글자 · 버튼 · 뒤 화면을 넣지 않는다.
배경은 **완전히 평평한 순수 마젠타 한 색**(그림자 · 빛 번짐 없이)으로 채운다 — 나중에 그 색을 빼서 투명하게 만든다. 부품 안에는 마젠타를 쓰지 않는다.
'@
switch ($Which) {
  'frame' { $body = @'
[부품] 팝업 틀 — 9-slice 로 늘려 쓴다.
- 캔버스 1200 x 600. 틀이 캔버스를 거의 꽉 채운다(바깥 여백 20 px 안쪽).
- 시안의 팝업 틀 그대로: 금속 테두리 + 안쪽 어두운 남색 판(판은 무늬 없이 평평하게, 아주 약한 질감만).
- **장식(볼트 · 사선 조립부 · 파란 빛)은 네 모서리 안에만**(각 모서리에서 가로 · 세로 160 px 안).
- 네 변의 가운데 구간은 **장식 없는 같은 금속 띠가 고르게 이어진다** — 늘려도 티가 안 나야 한다(이음선 · 광점 · 홈을 변 가운데에 두지 않는다).
- 버튼 받침은 그리지 않는다(따로 받는다). 안쪽 판은 비어 있다.
[저장] Projects/AVSR/_exchange/in/popup_frame.png
'@ }
  'tray' { $body = @'
[부품] 버튼 받침 — 시안에서 버튼 둘이 앉아 있는 얕게 들어간 받침 판. 가로로 9-slice 로 늘려 쓴다.
- 캔버스 1000 x 180. 받침이 캔버스를 거의 꽉 채운다(바깥 여백 10 px 안쪽).
- 시안처럼 판보다 한 단계 어두운 홈 + 얇은 금속 테두리 · 사선 모서리. 장식은 양 끝 모서리(각 120 px 안)에만, 가운데는 고르게 이어지는 면.
[저장] Projects/AVSR/_exchange/in/popup_tray.png
'@ }
  'groove' { $body = @'
[부품] 버튼 하나짜리 알림에서 버튼이 앉는 **얇은 홈** — 시안 popup_mock_v2.png 오른쪽(「확인」 하나) 장면의 버튼 뒤 판. 가로로 9-slice 로 늘려 쓴다.
- 캔버스 1200 x 200. 홈이 캔버스를 거의 꽉 채운다(바깥 여백 10 px 안쪽).
- 팝업 판보다 한 단계 어두운 남색 면 + **가는 선 테두리 한 줄**(옅은 청회색, 1~2 px 느낌) · 모서리는 작게 사선으로 깎는다.
- **볼트 · 금속 덩어리 · 두꺼운 테두리 · 빛나는 장식은 넣지 않는다** — 판에 살짝 파인 홈일 뿐이다. 가운데는 고르게 이어지는 면.
[저장] Projects/AVSR/_exchange/in/popup_groove.png
'@ }
  'cancel' { $body = @'
[부품] 「취소」 버튼(글자 없음) — 확인 버튼(popup_ref_button_yellow.png)과 **실루엣 · 비율이 완전히 같은** 보조 버튼.
- 캔버스 580 x 160. 버튼이 캔버스를 꽉 채운다(바깥 여백 4 px 안쪽). 노란 버튼의 팔각 실루엣 · 테두리 두께를 그대로.
- 색은 시안의 취소 버튼 — 본체 판보다 한 단계 밝은 쇠 · 청회색 면, 얇은 하이라이트. 비활성처럼 보이면 안 된다.
[저장] Projects/AVSR/_exchange/in/popup_button_cancel.png
'@ }
  'confirm' { $body = @'
[부품] 「확인」 버튼(글자 없음) — 이미 받은 취소 버튼(Projects/AVSR/_exchange/in/popup_button_cancel.png)과 **실루엣 · 테두리 · 크기가 완전히 같은** 짝 버튼.
- 캔버스 580 x 160. 버튼이 캔버스를 꽉 채운다(바깥 여백 4 px 안쪽). 취소 버튼의 팔각 실루엣 · 어두운 바깥 테두리 두께를 그대로.
- 면은 노란색 — 지금 게임의 노란 버튼(popup_ref_button_yellow.png)과 같은 노랑 · 주황 테두리 결. 시안(popup_mock_v2.png)의 확인 버튼 느낌.
[저장] Projects/AVSR/_exchange/in/popup_button_confirm.png
'@ }
  'icons' { $body = @'
[부품] 육성 화면 능력치 아이콘 두 개 — 한 장에 나란히(같은 가족으로 보이게 한 번에 그린다).
- 캔버스 800 x 400. 왼쪽 칸(0~400)에 「크리티컬 피해」 아이콘, 오른쪽 칸(400~800)에 「방어력」 아이콘. 각 아이콘은 제 칸 가운데, 칸의 80% 크기.
- 같은 화면의 기존 아이콘과 같은 결: Projects/AVSR/_exchange/ref/stat_icon_crit.png(크리티컬 확률 — 하늘색 조준경) · stat_icon_hp.png(하트). 작게 줄여도(약 50 px) 읽히는 굵고 단순한 픽셀풍.
- 크리티컬 피해: 크리티컬 확률 아이콘과 짝으로 보이게(조준경 + 터지는 느낌 등). 방어력: 방패.
[저장] Projects/AVSR/_exchange/in/stat_icons_critdmg_def.png
'@ }
}
codex exec --sandbox workspace-write --skip-git-repo-check ($common + "`n" + $body)
