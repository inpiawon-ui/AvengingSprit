$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
[작업] 게임 캐릭터 픽셀아트 — dragon_blue(블루 드래곤) 「등장 자세」 한 장
API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 · 편집 기능만 써라. 스크립트로 그려 때우지 마라.

소환(가챠)에서 배양조 유리가 깨지고 이 캐릭터가 튀어나와 **「내가 왔다」 하고 자세를 잡는 순간**의 한 장.
힘 있게, 캐릭터 성격이 드러나게(무기 · 주먹 · 날개 등 그 캐릭터의 특징을 앞세운 결정 자세).

[정답 · 기준]
1. 원작 시트(몸 생김 · 색 · 화풍의 정답): Projects/AVSR/_exchange/ref/char_v2/orig_dragon_blue_x3.png
2. 지금 게임 큰 그림 — 크기 · 발 자리 · 외곽선 · 색 그대로 맞출 것:
   서기 Assets/BaseResource/UnitLarge/dragon_blue/unitL_dragon_blue_se.png · 공격1 Assets/BaseResource/UnitLarge/dragon_blue/unitL_dragon_blue_se_atk1.png · 공격2 Assets/BaseResource/UnitLarge/dragon_blue/unitL_dragon_blue_se_atk2.png
3. 원작 메모: - **옷 입은 날렵한 도마뱀 사람** — 통통한 아기 용 · 배 비늘판 · 동그란 눈 금지
- 청록 비늘 · **악어처럼 긴 주둥이** · 회색 뿔 두 개 · 등과 꼬리에 흰 가시 · 긴 꼬리
- **노란 셔츠 · 갈색 반바지 · 갈색 장화**(모든 방향에서 같은 옷) · 몸집 작고 약간 앞으로 숙임

[규격]
- **384×384, 마젠타 #FF00FF 단색 바탕**, 오른쪽 아래 비스듬히(지금 서기 그림과 같은 방향).
- 발 자리 · 몸 크기는 서기 그림과 같게(칸 아래에서 같은 높이에 발이 닿게). 몸이 칸 밖으로 나가지 않게.
- 원작에 없는 장신구 · 무기 · 빛 · 이펙트는 그리지 마라. 원작 캐릭터 그대로, 자세만 새로.
- 외곽선 · 도트 크기 · 명암 단계는 지금 큰 그림과 같게.
※ 크기가 조금 달라도 고치려 하지 말고 그대로 저장하라. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/appear_dragon_blue.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
