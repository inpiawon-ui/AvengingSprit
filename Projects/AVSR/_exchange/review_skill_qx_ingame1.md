# 스킬 연출 퀄업 13종 인게임 1차 검수

## 판정 기준과 전제

- 정답 시안 `in/mock_skill_{몸}_v1.png`의 1 발동 / 2 최대 / 3 끝과 `ref/skill_qx_check/{몸}.png`를 대조했다.
- 캡처는 720×1280 전체 화면이며 시전 직후 약 0.1~0.3초의 한 순간이다. 아래 px 값은 캡처에서 보이는 효과 외곽의 대략적인 화면 실측값(±10 px)이다.
- 캡처 뒤 반영된 구루 `GuardScale 0.8`, 드라군 장판 지름 연동, 설녀 `isprison alpha 0.4`는 **현재 코드값**으로 판정했다. 따라서 낡은 캡처만 보고 중복 수정하지 않는다.
- 테스트 적 수가 적어서 다중 타깃 수 자체는 결함으로 보지 않았다. 화이트 위저드의 큰 노란 구슬과 옅은 근접 사거리 원도 검수 대상에서 제외했다.
- 프레임 시퀀스의 정확한 체감은 정지 캡처만으로 확정할 수 없으므로, 빠진 박자는 부품 프레임 수와 `BattleDirector.SkillFxQuality.cs`의 frameSeconds/life를 함께 보았다.

## 스킬별 판정

| 스킬 | 시안 대비 어긋난 점 | 고칠 값 | 우선순위 |
|---|---|---|---|
| 코만도 미사일 (`commando_missile`) | **발사 분산이 과함.** 캡처에서 미사일 8발이 캐릭터 둘레 약 300 px 폭의 거의 180° 반원으로 펼쳐져, 시안의 전방 약 110~120° 부채꼴보다 옆/아래 방향 비중이 크다. 최대 폭발도 시안 약 120~140 px보다 현재 계산상 약 170 px로 커서 적과 피해 숫자를 덮는다. 발동→집중→짧은 폭발 박자는 있음. | 이 항목은 Qx 파일 밖의 실제 발사값 수정 필요: `BattleDirector.SkillsNew.cs`의 `MissileFanSpreadDeg 180f → 115f`, `MissileFanOutMeters 0.6f → 0.45f`, `MissileBlastMeters 1.9f → 1.55f`. 공용 `PwBlastBoxPerDiameter`는 수류탄에도 영향하므로 건드리지 말 것. `missile_trail`은 발사점의 순간 연기만 허용하고 수명 연장 금지. | 높음 |
| 구루 (`guru`) | **통과.** 캡처의 구형 값은 외곽 약 250~270 px로 시안보다 컸지만, 현재 `GuardScale 0.8` 적용 시 `gddome` 실효 외곽이 약 163 px, `gdring` 약 123×112 px로 줄어 캐릭터를 가리지 않는 시안 범위에 들어온다. 발동 링→결계 최대→잔여 테두리 박자도 존재한다. | 변경 없음. `GuardScale 0.8f`, `gdring 0.06f`, `gddome 0.16f / life 0.7f`, `gdrim 0.2f` 유지. | 낮음 |
| 닌자 사슬 (`ninja_chain`) | **결박이 너무 큼.** 캡처의 적 결박이 약 145×95 px로, 시안의 몸통 밀착 결박 약 85×55 px보다 약 1.7배 크고 머리/팔까지 덮는다. 발사 사슬도 굵지만 이는 1프레임 링크라 크기보다 결박 가림이 핵심 문제다. `cbwrap` 3프레임 뒤 지속 `cbhold`로 넘어가는 박자는 있음. | `QxChainCast`: `cbchain`의 `PlayBeam(..., QxCell)` 크기 `QxCell → QxCell * 0.75f`; `cbsnap scale 0.7f → 0.55f`; `cbwrap scale 0.9f → 0.55f`. `RootFxSizeQ`의 `0.8f → 0.6f`. `cbwrap frame 0.08f → 0.07f`로 최대 결박의 앞단을 조금 짧게. | 높음 |
| 로봇 (`robot`) | **통과.** 포탑은 캐릭터 오른쪽 발밑에 떨어지고, 착지 먼지는 약 35~45 px로 짧아 시안의 “착지→발사”를 방해하지 않는다. 별도 궤적 안내나 긴 여운도 없다. | 변경 없음. `QxTurretDrop`의 `Puff(QxFeet(at), Dust, 0.6f)`와 `Shake(2.5f)` 유지. | 낮음 |
| 야구선수 (`baseball`) | **통과.** 캡처 반사 링 외곽 약 145 px로 시안 약 140~150 px와 맞고, 캐릭터 얼굴/몸통을 덮지 않는다. 오렌지 링→짧은 타격 섬광 박자도 부품 구성과 일치한다. | 변경 없음. `rfring scale 1f`, frame `0.14f`, 프레임 2~3 루프 및 `rfswing scale 0.7f / 0.04f` 유지. | 낮음 |
| 샐러맨더 (`salamander`) | **재발주.** 최대 연출이 시안의 몸 중심을 채우는 저밀도 초록·보라 확산 구름이 아니라, 중앙이 완전히 빈 고채도 도넛이다. 캡처 외곽은 약 240×225 px로 크기는 과하지 않지만 중심이 발밑으로 약 45~60 px 내려가 다리만 둘러싸고 상체 박자가 비어 보인다. 현재 4프레임 모두 도넛 구조라 scale/위치로 해결 불가. | **재발주:** `vncloud` 4프레임을 중앙이 빈 고리가 아닌 반투명 확산 안개로 다시 제작. 시안처럼 1 모임→2 몸 주변 구름 최대→3 작은 잔류 점으로 끝내고, 초록 면적 alpha를 낮춰 캐릭터 실루엣을 보존할 것. 재발주 후 코드값은 `vncloud scale 1.4f → 1.25f`, 위치 `QxFeet(me.Position) → me.Position`, frame `0.1f → 0.08f`; `vngather 0.6f / 0.06f` 유지. | 높음 |
| 드라군 (`dragoon`) | **통과(현재 코드 기준).** 캡처에는 수정 전 약 280 px 불바다가 보이지만, 현재 `radius * 2 / QxCell`은 실제 장판 지름에 맞추는 값이라 시안의 타격 지점 중심 원형 최대와 일치한다. 보라 룬/불꽃 색과 끝의 짧은 잔불도 적절하다. | 변경 없음. `QxFireBloom(... radius * 2f / QxCell)`, frame `0.06f` 유지. 캡처 크기로 되돌리지 말 것. | 낮음 |
| 청룡 (`dragon_blue`) | **통과.** 몸의 청백 코일 외곽 약 190~200 px, 타깃 간 번개 굵기 약 65~70 px로 시안과 유사하다. 코일이 캐릭터를 완전히 가리지 않고 번개가 실제 타깃 사이에만 생겨 사전 궤적 안내도 아니다. 시작 코일→연쇄 볼트→착탄 스파크 박자 모두 있음. | 변경 없음. `dscoil scale 1f / 0.1f`, `dsbolt scale 0.9f / 0.06f`, `dsspark scale 0.8f / 0.06f` 유지. | 낮음 |
| 화이트 위저드 (`white_wizard`) | **통과(정지 캡처 한계 있음).** 캡처의 큰 노란 구슬은 평타이므로 제외한다. 스킬 쪽 보라·백색 모임과 청색 투사체는 캐릭터 주변을 과도하게 물들이지 않으며, 8발은 같은 적이 적은 테스트 판에서 겹쳐 보여 시안보다 적어 보일 수 있다. `wfgather` 4프레임과 명중 `wfpop` 4프레임으로 발동/끝 박자는 구현돼 있다. | 변경 없음. `wfgather scale 0.7f / 0.05f`, `wfpop scale 0.6f / 0.05f` 유지. 노란 평타 크기를 이 검수로 수정하지 말 것. | 낮음 |
| 영매 (`medium`) | **통과.** 룬 외곽 약 180×120 px, 상승 파편 약 155×165 px로 시안의 소환 지점 안에 머물며 캐릭터가 아니라 소환 위치에 붙는다. 룬→파편 상승→골렘 정착의 3박자도 있다. | 변경 없음. `mgrune scale 1f / 0.07f`, `mgrise scale 1f / 0.07f`, 두 위치 모두 `QxFeet(at)` 유지. | 낮음 |
| 설녀 (`snowwoman`) | **통과(현재 코드 기준).** 캡처는 수정 전이라 얼음 결정이 캐릭터를 강하게 가리지만, 현재 `isprison alpha 0.4`이면 실루엣 보존 조건을 충족한다. 크기 약 220×232 px는 시안 결정 외곽과 맞고 모임→감옥→파쇄도 모두 있다. | 변경 없음. `isgather scale 0.8f / 0.06f`, `isprison scale 1f / 0.22f / alpha 0.4`, `isbreak scale 1f / 0.06f` 유지. alpha를 캡처 수준으로 되돌리지 말 것. | 낮음 |
| 흡혈귀 (`vampire`) | 발밑 원이 캡처 약 125×100 px로 시안 약 150×120 px보다 15~20% 작아 발동 박자가 약하다. 피 구슬 자체는 실효 약 36 px로 시안과 맞고, 테스트 적 수가 적은 것은 결함이 아니다. 색은 붉은색 계열로 허용 범위이며 캐릭터 가림도 없다. | `QxFeastOpen`의 `vmcircle scale 1f → 1.15f`; frame `0.1f`와 `life seconds + 0.2f` 유지. `BatFxSizeQ QxCell * 1.5f`, `vmglow 0.9f / 0.1f` 유지. | 중간 |
| 사신 (`death`) | **통과.** 캡처의 보라 낫 베기 외곽 약 220×180 px는 시안의 몸 둘레 한 번 베기와 맞고, 발밑 오라는 약 190×115 px라 근접 사거리 원 안에 머문다. 첫 베기→지속 오라→처치 시 머리 위 해골의 박자가 구현돼 있으며, 별/표창 모양도 없다. | 변경 없음. `rpslash scale 1f / 0.06f`, `rpaura scale 1f / 0.1f`, 처치 `rpslash 0.6f / 0.05f`, `rpskull 0.6f / 0.2f / life 0.6f` 유지. | 낮음 |

## Claude 적용용 변경 목록

아래만 적용하고, 통과 항목 및 이미 보정된 구루/드라군/설녀 값은 건드리지 않는다.

1. `BattleDirector.SkillsNew.cs` — 코만도 미사일
   - `MissileFanSpreadDeg`: `180f → 115f`
   - `MissileFanOutMeters`: `0.6f → 0.45f`
   - `MissileBlastMeters`: `1.9f → 1.55f`
   - `PwBlastBoxPerDiameter`는 수류탄 공용이므로 변경 금지.
2. `BattleDirector.SkillFxQuality.cs` — 닌자 사슬
   - `cbchain` beam size: `QxCell → QxCell * 0.75f`
   - `cbsnap`: scale `0.7f → 0.55f`
   - `cbwrap`: scale `0.9f → 0.55f`, frame `0.08f → 0.07f`
   - `RootFxSizeQ`: `QxCell * 0.8f → QxCell * 0.6f`
3. `Assets/BaseResource/InGameMainUI/fx_vncloud_1~4.png` — 샐러맨더
   - **재발주:** 중앙이 빈 도넛을 폐기하고 시안처럼 몸 중심의 반투명 초록·보라 확산 안개 4프레임으로 교체.
   - 교체 뒤 `BattleDirector.SkillFxQuality.cs`: `vncloud scale 1.4f → 1.25f`, 위치 `QxFeet(me.Position) → me.Position`, frame `0.1f → 0.08f`.
4. `BattleDirector.SkillFxQuality.cs` — 흡혈귀
   - `vmcircle`: scale `1f → 1.15f`.

## 적용 후 재캡처 체크

- 코만도: 미사일이 전방 115° 안에서만 벌어지고 폭발 한 개가 140 px 안팎인지.
- 닌자: `cbwrap`이 적 몸통에 밀착하고 머리/팔을 덮지 않는지.
- 샐러맨더: 최대 프레임에서도 캐릭터 실루엣이 보이며 화면 전체에 초록색 막이 깔리지 않는지.
- 흡혈귀: 발밑 원만 15% 커지고 피 구슬 크기는 그대로인지.
