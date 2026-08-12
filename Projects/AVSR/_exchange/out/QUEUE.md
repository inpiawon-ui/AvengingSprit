# 리소스 작업 큐 — 원작 기준 (2026-08-11 전면 개정)

**내 확인을 기다리지 말고 위에서부터 순서대로 가져가서 작업한다.**
한 항목을 `_exchange/in/` 에 넣었으면 바로 다음으로 넘어간다.
반려가 나오면 그 항목만 되돌아오고 그동안 다음 항목은 계속 간다.

규격·시점·원칙은 [`WORKORDER.md`](WORKORDER.md) **15차**를 따른다.

---

## 캐릭터마다 반드시 볼 것

```
15_original/{원작 시트명}__sheet.png     ← 인물·복장·비율·무기. 이대로 그린다
15_original/{원작 시트명}__palette.png   ← 쓸 수 있는 색 전부. 밖으로 나가면 반려
```

공통 규격 (매번 다시 적지 않는다)

```
캔버스   96×96 · 투명 · 이진 알파(반투명 0) · 안티앨리어싱 없음
정렬     발밑 y=87 · 발 중심 x=48(바닥 6줄) · 머리끝 y=12
방향     s · se · e · ne · n   (왼쪽 3방향은 코드가 반전으로 만든다)
프레임   idle / atk1 / atk2 / hit / walk1 / walk2 / die1 / die2
장수     한 캐릭터 40장 (idle 5 + 동작 35)
파일명   unit_{슬러그}_{방향}.png · unit_{슬러그}_{방향}_{프레임}.png
         ※ 방향 없는 unit_{슬러그}.png 는 필요 없다

몸 안 구멍 3% 초과 = 반려 · 팔레트 밖 색 = 반려
공격은 발을 붙인 채 · 걷기는 발이 움직이고 몸통은 제자리(머리 중심 ±2px)
사망은 원작을 따른다 — 주저앉기가 아니라 뼈만 남는다
```

---

## 큐

앞쪽 9종이 **CH1 을 완성**하는 분량이다. 여기까지만 와도 한 챕터가 그림까지 갖춰진다.

| # | 슬러그 | 원작 시트 | 장수 | 상태 |
|---|---|---|--:|---|
| ~~1~~ | ~~`gangster`~~ | Enemies - Gangster (Gun) | 40 | 재작업(원작 기준) |
| ~~2~~ | ~~`amazon`~~ | Enemies - Amazon | 40 | 폐기했던 것 복귀 |
| ~~3~~ | ~~`thug`~~ | Enemies - Gangster (Tommy Gun) | 40 | 재작업(원작 기준) |
| ~~4~~ | ~~`salamander`~~ | Enemies - Dragon (Green) | 40 | 구멍 반려분 재작업 |
| ~~5~~ | ~~`white_wizard`~~ | Enemies - Magician (Light) | 40 | 납품분 원작 대조 후 판단 |
| ~~6~~ | ~~`ninja`~~ | Enemies - Ninja (Shuriken) | 40 | **완료** |
| ~~7~~ | ~~`baseball`~~ | Enemies - Slugger | 40 | **완료** |
| ~~8~~ | ~~`commando_grenade`~~ | Enemies - Commando (Grenade) | 40 | **완료** |
| ~~9~~ | ~~`amazon_elite`~~ | Enemies - Amazon Elite | 40 | 엘리트 — 캔버스 128×128 |
| — | ↑ **여기까지 CH1 완성** | | | |
| ~~10~~ | ~~`commando_laser`~~ | Enemies - Commando (Laser) | 40 | **완료** |
| ~~11~~ | ~~`robot`~~ | Enemies - Robot | 40 | **완료** |
| ~~12~~ | ~~`guru`~~ | Enemies - Guru | 40 | **부양 · 아래 별항** |
| ~~13~~ | ~~`vampire`~~ | Enemies - Vampire | 40 | **완료** |
| ~~14~~ | ~~`hopper`~~ | Enemies - Hopper (Gun) | 40 | **완료** |
| ~~15~~ | ~~`hopper_smg`~~ | Enemies - Hopper (Sub-Machine Gun) | 40 | **완료** |
| ~~16~~ | ~~`commando_mg`~~ | Enemies - Commando (Machine Gun) | 40 | **완료** |
| ~~17~~ | ~~`snowwoman`~~ | Enemies - SnowWoman | 40 | 폐기했던 것 복귀 |
| ~~18~~ | ~~`ninja_chain`~~ | Enemies - Ninja (Chain) | 40 | 폐기했던 것 복귀 |
| ~~19~~ | ~~`medium`~~ | Enemies - Magician (Dark) | 40 | **완료** |
| ~~20~~ | ~~`dragoon`~~ | Enemies - Dragon (Red) | 40 | **완료** |
| ~~21~~ | ~~`dragon_blue`~~ | Enemies - Dragon (Blue) | 40 | **완료** |
| ~~22~~ | ~~`ghost`~~ | Playable Characters - Ghost | 40 | 우리 플레이어 본체 |
| **23~25** | **보스 3체** | Robot Snakes · Crusher · Python | 63 | [`WORKORDER_17_BOSS.md`](WORKORDER_17_BOSS.md) — 31번 다음 |
| 26~28 | 보스 3체(나머지) | Guardian · Kingpin · Sludge | — | **손대지 마라** — 정본에 챕터가 아직 없다 |
| ~~29~~ | ~~UI · 폰트 · 타이틀~~ | Miscellaneous - * | 별도 | **완료** — 우리가 시트에서 직접 뽑았다 |
| 30 | `obj_ricochet_wall_v` | — | 1 | 재작업(아래) |
| **31** | **얼티밋 아이콘 12종** | — | 12 | **지금 이것부터** — [`WORKORDER_16_ULTIMATE.md`](WORKORDER_16_ULTIMATE.md) |

> **31번은 캐릭터 규격(96×96)이 아니다.** 64×64 UI 아이콘이고 앵커·기준이 따로 있다.
> 반드시 `WORKORDER_16_ULTIMATE.md` 를 열고 시작한다.
>
> **23~25번(보스)도 캐릭터 규격이 아니다.** 캔버스 256×256 · 닿는 선 y=232 ·
> 3방향뿐이고, 아레나 장치는 아직 그리지 않는다. `WORKORDER_17_BOSS.md` 를 따른다.

**작업 순서: 31번(얼티밋 아이콘 12) → 23~25번(보스 3체) → 30번(`obj_ricochet_wall_v`).**

**정본에만 있고 원작 시트가 없는 둘** — `shield_trooper`·`sensor_drone` 은
정본이 RE:BORN 신규로 표기한 것이라 원작 앵커가 없다. 나중에 따로 설계해 지시한다.

---

## 별항

### `guru` — 부양

원작 구루의 AI 패턴이 `Hover → Aura → Punch` 다. 바닥에 발을 딛지 않는다.

```
몸      발끝이 바닥선에서 12~16px 위
그림자  바닥선(y=87)에 가로:세로 10:3 으로 눌린 타원 · 어두운 남색 단색 · 중심 x=48
걷기    발을 내딛지 않는다. 몸이 2~3px 흔들리고
        **그림자는 바닥에 붙은 채 크기·진하기만** 변한다
        ← 그림자가 몸을 따라 오르내리면 떠 있는 것으로 안 읽힌다
사망    부양이 풀린다. 높이가 내려오며 그림자가 커지고 마지막엔 몸에 붙는다
```

### `amazon_elite` — 엘리트 규격

일반 적보다 한 등급 크다. **캔버스 128×128 · 발밑 y=118 · 발 중심 x=64.** 나머지는 같다.

### `obj_ricochet_wall_v` 재작업

가로형(`_h`)은 리벳 박힌 청색 금속판으로 잘 나왔는데 **세로형이 같은 벽으로 안 보인다.**
연보라 얇은 조각이라 금속판이 아니라 유리 파편이나 칼날처럼 읽힌다.
`_h` 를 그대로 세로로 세운 것이면 된다 — 같은 청색 금속, 같은 리벳, 같은 이음매.
캔버스 43×347, 발자국 아래 257px 그대로. 폭이 좁아 리벳은 한 줄이면 충분하다.

---

## 납품 방법

- 파일은 전부 `_exchange/in/` 에 평평하게 넣는다(하위 폴더 없음)
- 한 항목이 끝날 때마다 넣고 다음으로 넘어간다. 여러 항목을 모아 넣어도 된다
- 넣은 뒤 무엇을 넣었는지 한 줄로 알린다
