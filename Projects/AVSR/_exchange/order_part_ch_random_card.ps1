$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
API 키를 쓰는 CLI 폴백은 쓰지 마라. 한 장만 만든다.
게임 「Avenging Spirit RE:BORN」 로비 호스트 선택 — 「?(랜덤)」 카드 그림 부품.
⚠ 이 게임은 원작 IP 를 쓴다 — **캐릭터 모양은 원작 그림과 100% 같아야 한다**(PD). 캐릭터를 새로 그리거나 다시 그리지 마라.
쓸 원본(그대로 옮겨 쓴다 — 모양 · 윤곽 · 도트 하나도 바꾸지 않는다):
  ① 사신 초상: Assets/BaseResource/HostSelectPanel/hostportraitimage_death.png
  ② 주사위 + 「?」 그림: Assets/BaseResource/ChapterHost/ch_random_icon.png (지금 게임의 랜덤 그림)
만들 것: ①의 사신을 **윤곽은 그대로 둔 채 안을 검게(아주 어두운 남보라) 가린 실루엣**으로 — 눈 자리만 붉게 살짝 빛나게. 그 앞(아래쪽)에 ②의 주사위 + 「?」를 그대로 겹쳐 놓는다.
  참고 배치(모양은 따르지 말 것, 배치만): Projects/AVSR/_exchange/ref/hostinfo_mock/anchor_random_card.png
- 크기 276x364(세로 카드 칸 2배), **배경은 투명**(알파 PNG). 빛 번짐 · 그림자 · 테두리는 넣지 않는다(빛은 게임에서 따로 준다).
- 사신을 다시 그려 모양이 달라지면 불합격이다. 원본 픽셀을 복사해 색만 바꾸는 방식으로 한다.
[저장] Projects/AVSR/_exchange/in/ch_random_card.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
