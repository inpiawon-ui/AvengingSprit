# AVSR 원작 사운드 추출 기록

원작 아케이드 **Avenging Spirit (Jaleco, 1991)** ROM에서 배경음·효과음·ADPCM 원본 샘플을 추출해 프로젝트에 넣은 기록이다.

- **사용 근거**: 원작 IP 계약 체결 완료 — 사용자 확인(2026-09-15). 원본을 가공 없이 옮기는 것이 요구사항이다.
  - `AVSR_Content_Title.md` 5절의 "원작 음원 사용 범위 IP 확인 대기(확정 #25)" TBD는 이 확인으로 해소 대상이다(문서 갱신은 별도).
- **ROM**: `D:\Projects\AvengingSpirit\AvengingSpirit-ROM` (MAME 세트 `avspirit`, `avspirit.mcu` 제외 11개 파일 CRC 일치)
- **에뮬레이터**: MAME 0.289 (`D:\Projects\RodLand\mame\mame.exe`)
- **산출물**: `Assets/BundleResource/Sounds/` — BGM 12 · SFX 23 · Samples 37 (총 128.4 MB)
- **매니페스트**: [`_sound_extract/sound_manifest.json`](_sound_extract/sound_manifest.json)

---

## 1. 포맷

| 폴더 | 내용 | 포맷 |
|---|---|---|
| `BGM/` | 배경음 (사운드 코드 1–12) | WAV 48kHz 16bit **모노** |
| `SFX/` | 효과음 (사운드 코드 16–38) | WAV 48kHz 16bit 모노 |
| `Samples/` | OKI MSM6295 원본 ADPCM 샘플 (보컬·타악기 등 원재료) | WAV 24,242Hz 16bit 모노 |

- 기판 출력은 모노 스피커 1개다. MAME 녹음의 L/R이 **샘플 단위로 완전히 같음**을 확인하고 한 채널만 저장했다(무손실).
- 파일명 숫자 = 원작 사운드 명령 번호. 게임 이벤트와의 대응은 아직 확인하지 않았다(7절).

---

## 2. 추출 방법

### 2.1 BGM · SFX — MAME 녹음

원작 사운드는 **사운드 전용 68000 CPU가 YM2151(FM) + OKI MSM6295 ×2를 실시간으로 연주**하는 구조라 ROM에 완성 음원이 없다. MAME로 연주시켜 녹음했다.

1. **MCU 우회**: `avspirit.mcu`(보호용 TMP91640) 덤프가 없어 MAME가 실행을 거부한다. 16KB를 `C8 FE`(TLCS-90 `JR -2`, 무한루프)로 채운 더미를 넣으면 체크섬 경고만 뜨고 부팅된다.
   - 메인 CPU는 부팅 직후 멈춘다 → 게임 진행은 불가하지만 **사운드 CPU는 독립적으로 정상 동작**하고, 메인 CPU가 명령을 가로채지 않는다.
2. **곡 직접 재생**: Lua로 메인 버스의 사운드 래치 `0x044308`에 곡 번호를 기록 → 지정 시간 후 `0x0000`(정지) → 1.5초 간격.
3. **녹음**: `-wavwrite`(48kHz) 한 파일로 전곡 녹음 후 스케줄 로그대로 분할, 앞뒤 디지털 무음(|x|≤48) 제거.

### 2.2 Samples — ROM 직접 디코딩

`spirit14.rom`(oki1) · `spirit13.rom`(oki2)의 샘플 테이블(8바이트 × 항목, 0xFF 항목에서 끝)을 읽어 MAME `okim6295`와 같은 ADPCM 알고리즘으로 디코딩했다. 클럭 4MHz, PIN7 HIGH → **24,242Hz**. 에뮬레이션을 거치지 않은 원본 그대로다.

### 2.3 사운드 명령 체계 (사운드 CPU 프로그램 `spirit01/02` 디스어셈블)

| 명령 | 의미 |
|---|---|
| `0xKKNN` | 상위 `KK` = 계열(0x00–0x05, 사용 채널 범위·작업 영역 차이), 하위 `NN` = 곡 번호 |
| `NN = 0xFF` | **정지가 아니라 페이드아웃 + 음소거 플래그**. 이후 계열 0 명령 전까지 다른 소리가 안 난다 |
| `NN = 0` | 빈 곡 = 정지 |
| 곡 테이블 | `$9000 + NN×64`, 곡마다 16채널 시퀀스 포인터 (채널 0–7 = FM, 8–15 = OKI 계열) |

- `NN 1–12` BGM, `13–15` 빈 곡, `16–38` SFX, `40–62` = `16–38`과 **포인터 완전 동일(중복)** → 추출 제외.
- 녹음은 계열 0(16채널 전체)으로 해서 곡의 모든 트랙이 울리게 했다.

---

## 3. BGM

루프곡은 **인트로 + 정확히 1루프**만 담는다. 재생기가 `loopStart`로 되돌아가면 원곡처럼 무한 반복된다.
Unity `AudioSource.loop`는 파일 전체를 반복하므로 **인트로가 있는 곡은 루프 시작 샘플 처리가 필요**하다(클라이언트 작업, 미구현).

| 코드 | 파일 | 길이 | 형태 | 루프 시작 (샘플) | 루프 길이 (샘플) |
|---|---|---|---|---|---|
| 1 | `BGM/bgm_01.wav` | 212.185s | 루프 | 867120 (18.065s) | 9317745 (194.120s) |
| 2 | `BGM/bgm_02.wav` | 210.685s | 루프 | 799968 (16.666s) | 9312900 (194.019s) |
| 3 | `BGM/bgm_03.wav` | 140.478s | 루프 | 597840 (12.455s) | 6145123 (128.023s) |
| 4 | `BGM/bgm_04.wav` | 232.726s | 루프 | 148080 (3.085s) | 11022758 (229.641s) |
| 5 | `BGM/bgm_05.wav` | 88.809s | 루프 | 1058688 (22.056s) | 3204150 (66.753s) |
| 6 | `BGM/bgm_06.wav` | 136.439s | 루프 | 1015536 (21.157s) | 5533532 (115.282s) |
| 7 | `BGM/bgm_07.wav` | 3.471s | 원샷(끝남) | — | — |
| 8 | `BGM/bgm_08.wav` | 71.786s | 루프 | 1148640 (23.930s) | 2297083 (47.856s) |
| 9 | `BGM/bgm_09.wav` | 80.230s | 루프 | 398256 (8.297s) | 3452787 (71.933s) |
| 10 | `BGM/bgm_10.wav` | 17.012s | 원샷(끝남) | — | — |
| 11 | `BGM/bgm_11.wav` | 70.758s | 원샷(끝남) | — | — |
| 12 | `BGM/bgm_12.wav` | 83.930s | 루프 | 377088 (7.856s) | 3651572 (76.074s) |

## 4. SFX

| 코드 | 파일 | 길이 | 형태 | 루프 시작 (샘플) | 루프 길이 (샘플) |
|---|---|---|---|---|---|
| 16 | `SFX/sfx_16.wav` | 0.095s | 원샷(끝남) | — | — |
| 17 | `SFX/sfx_17.wav` | 1.673s | 원샷(끝남) | — | — |
| 18 | `SFX/sfx_18.wav` | 17.282s | 루프 | 582384 (12.133s) | 247166 (5.149s) |
| 19 | `SFX/sfx_19.wav` | 0.719s | 원샷(끝남) | — | — |
| 20 | `SFX/sfx_20.wav` | 5.910s | 루프 | 27504 (0.573s) | 256152 (5.337s) |
| 21 | `SFX/sfx_21.wav` | 0.649s | 원샷(끝남) | — | — |
| 22 | `SFX/sfx_22.wav` | 0.200s | 원샷(끝남) | — | — |
| 23 | `SFX/sfx_23.wav` | 0.978s | 원샷(끝남) | — | — |
| 24 | `SFX/sfx_24.wav` | 0.894s | 원샷(끝남) | — | — |
| 25 | `SFX/sfx_25.wav` | 2.112s | 원샷(끝남) | — | — |
| 26 | `SFX/sfx_26.wav` | 0.794s | 원샷(끝남) | — | — |
| 27 | `SFX/sfx_27.wav` | 0.559s | 원샷(끝남) | — | — |
| 28 | `SFX/sfx_28.wav` | 0.982s | 원샷(끝남) | — | — |
| 29 | `SFX/sfx_29.wav` | 0.847s | 원샷(끝남) | — | — |
| 30 | `SFX/sfx_30.wav` | 1.066s | 원샷(끝남) | — | — |
| 31 | `SFX/sfx_31.wav` | 0.971s | 원샷(끝남) | — | — |
| 32 | `SFX/sfx_32.wav` | 0.186s | 원샷(끝남) | — | — |
| 33 | `SFX/sfx_33.wav` | 0.265s | 원샷(끝남) | — | — |
| 34 | `SFX/sfx_34.wav` | 0.304s | 원샷(끝남) | — | — |
| 35 | `SFX/sfx_35.wav` | 0.338s | 원샷(끝남) | — | — |
| 36 | `SFX/sfx_36.wav` | 0.321s | 원샷(끝남) | — | — |
| 37 | `SFX/sfx_37.wav` | 0.439s | 원샷(끝남) | — | — |
| 38 | `SFX/sfx_38.wav` | 0.507s | 원샷(끝남) | — | — |

## 5. Samples (OKI ADPCM 원본)

| 파일 | 길이 |
|---|---|
| `Samples/oki1_001.wav` | 0.327s |
| `Samples/oki1_002.wav` | 0.535s |
| `Samples/oki1_003.wav` | 0.652s |
| `Samples/oki1_004.wav` | 0.663s |
| `Samples/oki1_005.wav` | 0.610s |
| `Samples/oki1_006.wav` | 0.421s |
| `Samples/oki1_007.wav` | 0.459s |
| `Samples/oki1_008.wav` | 0.483s |
| `Samples/oki1_009.wav` | 1.113s |
| `Samples/oki1_010.wav` | 1.296s |
| `Samples/oki1_011.wav` | 0.425s |
| `Samples/oki1_012.wav` | 2.068s |
| `Samples/oki1_013.wav` | 0.404s |
| `Samples/oki1_014.wav` | 0.309s |
| `Samples/oki1_015.wav` | 2.091s |
| `Samples/oki2_001.wav` | 0.119s |
| `Samples/oki2_002.wav` | 1.204s |
| `Samples/oki2_003.wav` | 0.900s |
| `Samples/oki2_004.wav` | 2.703s |
| `Samples/oki2_005.wav` | 0.457s |
| `Samples/oki2_006.wav` | 0.251s |
| `Samples/oki2_007.wav` | 1.224s |
| `Samples/oki2_008.wav` | 1.118s |
| `Samples/oki2_009.wav` | 2.640s |
| `Samples/oki2_010.wav` | 0.993s |
| `Samples/oki2_011.wav` | 0.698s |
| `Samples/oki2_012.wav` | 1.229s |
| `Samples/oki2_013.wav` | 1.059s |
| `Samples/oki2_014.wav` | 1.333s |
| `Samples/oki2_015.wav` | 1.214s |
| `Samples/oki2_016.wav` | 0.232s |
| `Samples/oki2_017.wav` | 0.338s |
| `Samples/oki2_018.wav` | 0.380s |
| `Samples/oki2_019.wav` | 0.422s |
| `Samples/oki2_020.wav` | 0.401s |
| `Samples/oki2_021.wav` | 0.549s |
| `Samples/oki2_022.wav` | 0.634s |

---

## 6. 루프 판정 방법과 한계

1. **루프 길이**: 곡을 240–900초 녹음하고 정규화 상호상관으로 후보를 찾은 뒤, 겹치는 **전 구간을 5초 창으로 대조해 모든 창 NCC ≥ 0.97**인 후보만 채택(가장 이른 시작 → 그중 최소 길이). 원 샘플레이트 ±8샘플로 정밀 보정.
2. **이음매**: 주기 구간 안에서 루프 시작을 1ms 단위로 옮기며 "루프 끝 다음에 원래 이어지던 20ms"와 "되돌아가 재생할 20ms"가 가장 닮은 지점을 골랐다. 전 루프 이음매 NCC ≥ 0.999 (20ms 창). 별도 50ms 창 재측정 최저 0.961(8번), 나머지 ≥ 0.985.
3. **한계**
   - 루프마다 파형이 미세하게 달라 창 NCC가 1.0이 되지 않는다. YM2151 LFO가 곡 진행과 무관하게 돌기 때문으로 **추정**한다(미검증).
   - 사운드 CPU 포인터를 추적해 보니 **채널마다 반복 길이가 다른 곡이 있다**(폴리미터 추정). 시퀀서 전체 주기는 녹음 길이 안에서 확정하지 못했고, 위 루프는 **들리는 소리 기준**이다. 4번은 포인터상 45.9초 배수 후보 중 137.8·183.7초가 오디오에서 명확히 어긋나 229.64초로 확정했다.
   - MAME 16bit 믹스에서 일부 BGM 피크가 포화된다(곡당 최대 261샘플, 0.002%). 원본 출력 특성으로 보고 그대로 두었다.

---

## 7. 미해결 · 후속

| 항목 | 상태 |
|---|---|
| 곡·효과음 ↔ 게임 장면/이벤트 대응 | 조사 완료 → [`AVSR_SoundUsage.md`](../../Assets/BundleResource/Sounds/AVSR_SoundUsage.md). 남은 미확인 항목은 그 문서 4절 |
| Addressable 등록 | `sounds` 그룹 에셋이 아직 없다(`Assets/BundleResource/CLAUDE.md` 표에는 정의됨). Unity 에디터에서 그룹 생성·등록 필요 |
| Unity 임포트 설정 | 기본값. BGM은 `Streaming` + Vorbis, SFX는 `Decompress On Load` 권장 |
| git 용량 | WAV 약 128MB, 현재 LFS 대상 아님(`.gitattributes`). 커밋 전 LFS 여부 결정 필요 |
| 인트로+루프 재생 | `ISoundManager.PlayBGM`은 루프 시작점을 모른다. 필요 시 클라이언트팀 구현 |

---

## 8. 재현

도구: [`_sound_extract/`](_sound_extract/) — Python 3.11 + numpy.

| 파일 | 역할 |
|---|---|
| `oki_extract.py` | OKI ROM → 샘플 WAV |
| `record_all.lua` | MAME 자동 녹음 스크립트(곡 번호 래치 기록·스케줄 로그) |
| `split_rec.py` | 전곡 녹음 → 곡별 모노 WAV |
| `verify_loop2.py` · `loop_refine.py` | 루프 길이·시작 검증/보정 |
| `seam_opt.py` | 이음매 최적화 |
| `export_final.py` | plan JSON → 최종 WAV + 매니페스트 |

```bash
# 1) 더미 MCU: roms/avspirit/avspirit.mcu 를 16KB의 C8 FE 반복으로 만든다
# 2) 녹음
mame avspirit -rompath roms -video none -sound none -nothrottle -skip_gameinfo -wavwrite rec_all.wav -autoboot_script record_all.lua
python split_rec.py rec_all.wav rec_sched.txt rec_split
# 3) OKI 샘플
python oki_extract.py spirit14.rom oki oki1
python oki_extract.py spirit13.rom oki oki2
```
