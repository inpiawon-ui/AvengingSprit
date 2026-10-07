$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 설계한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[배경] 세로 모바일 액션(720x1280). 원작 도트 캐릭터(작다, 몸 약 90px), 고해상 일러스트 배경.
「연쇄 방전」 퀄업이 통과됐다 — 같은 결로 **무기 속성 7종**의 스킬 이펙트를 퀄업한다.
- 통과본 기준: Projects/AVSR/_exchange/review_skillfx_now.md (방향 7원칙 · 공통 부품) · Projects/AVSR/_exchange/in/skillfx_sample_laser_v2.png (시안)
  · Projects/AVSR/_exchange/skillfx_laser_ingame_before_after.png (게임 결과) · 검수 Projects/AVSR/_exchange/review_skillfx_lz_ingame2.md
- 무기 속성 언어: 청록/황금, 직선 · 절삭선 · 동심 조준형, 빠른 이동과 짧은 소멸. 크기를 키우지 말고 4박자 대비 · 인과 · 밝기 왕 하나 · 적을 덮지 않기.
- 화면 전체 틴트 · 멈춘 듯한 연출 금지. 캐릭터 자세는 원작 그대로(그리지 않는다).

[지금 장면 — 0.3초 간격, 스킬 시전 직후] Projects/AVSR/_exchange/ref/skillfx_now/now_{amazon,thug,hopper_smg,commando_mg,gangster,hopper,ninja}.png
[스킬 — 실제 동작(바꾸지 않는다)]
- amazon 아마존 「도약 강타」: 가장 가까운 적에게 뛰어들어 착지 둘레(반경 2.5m)를 친다.
- thug 폭력배 「난사」: 1초간 3방향으로 연사(Lv5 부터 갈래가 늘고 관통).
- hopper_smg 호퍼(기관단총) 「도약 강습」: 가장 먼 적에게 뛰어들고 2초간 무적(스킬 무적이라 몸 윤곽이 빛나도 된다).
- commando_mg 코만도(기관총) 「방벽 전개」: 5초간 최대 체력만큼 쉴드를 두른다(깎이면 끝).
- gangster 갱스터 「일제 표식」: 방 안 모든 적에게 3초간 표식(표식 적은 더 아프고, 패시브로 20% 즉사).
- hopper 호퍼 「정조준」: 5초간 치명타 확률 90% 고정(지속 버프 — 내 몸에 상태 표시).
- ninja 닌자 「그림자 분신」: 방 한가운데 분신을 세운다, 5초간 적이 분신을 노린다(분신은 반투명 내 몸 그림).

[엔진이 할 수 있는 것 — 이 안에서만 설계한다]
- 그림 여러 장(최대 8)을 차례로 넘기는 「장면」 (한 장 시간 · 크기 · 색 · 진하기 · 회전 · 수명/흐려짐 조절 가능)
- 가로로 그린 그림을 두 점 사이로 늘이고 돌리는 「줄기」(빔 · 조준선 · 궤적)
- 바닥에 눕힌 고리(세로로 눌러 타원), 몸을 따라다니는 반복 표시(상태), 화면 반동(한 방향 1~2px)
- 이미 있는 부품(재사용 가능): lzaim 조준선(가는 분절) · lzring 끊긴 조준 고리 · lzhit 청백 타격 코어 · lzdebris 직선 스파크+사각 파편 · lzmuzzle 총구 응축 · lzarc 짧은 각진 전기

[답할 것] 한국어, 아래 형식 그대로(내가 그대로 발주 · 구현한다).
## 공통 부품(무기) — 7종이 같이 쓸 것. 기존 부품으로 되면 「재사용: 이름」, 새로 필요하면:
- 이름(영문 소문자 3~10자, 접두어 wp) · 무엇 · 캔버스(가로x세로, 칸 수 · 칸 크기) · 칸별 묘사 · 색
## 스킬별 (7종 각각)
### 몸키 — 스킬 이름
- 4박자: ① 예비(초) ② 발동(초) ③ 타격(초) ④ 여운(초) — 각 박자에 나오는 부품 · 자리(내 몸/총구/적/바닥/두 점 사이) · 크기(px) · 한 장 시간
- 고유 부품(최대 2개): 이름(접두어 몸키 약자 2~3자) · 캔버스 · 칸별 묘사 · 색 — 공통/기존으로 되면 「없음」
- 지금 장면에서 걷어낼 것
마지막에 「발주 목록」: 새로 그릴 부품 이름만 한 줄씩.
[저장] Projects/AVSR/_exchange/spec_skillfx_weapon.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
