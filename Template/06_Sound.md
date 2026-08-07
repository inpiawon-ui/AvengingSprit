# 06. SOUND (사운드)

> **Part Code**: SND
> **Version**: 1.4.0
> **Last Updated**: 2026-04-18
> **Document Owner**: Sound Director
> **의존성**: COM-OVR-001, GD-NAR-001, ART-STY-001
> **기반 문서 (A)**: `Unity_GameDev_Template.md` — 6. SOUND 섹션을 상세화한 파트 문서(B)

---

## 📑 목차

- [SND-STY-001: 오디오 스타일 가이드](#snd-sty-001-오디오-스타일-가이드)
- [SND-BGM-001: BGM (배경 음악)](#snd-bgm-001-bgm-배경-음악)
- [SND-SFX-001: SFX (효과음)](#snd-sfx-001-sfx-효과음)
- [SND-VO-001: Voice Over (보이스 오버)](#snd-vo-001-voice-over-보이스-오버)
- [SND-UNT-001: Unity 오디오 통합 가이드](#snd-unt-001-unity-오디오-통합-가이드)
- [SND-PRM-001: 리소스 제작 프롬프트](#snd-prm-001-리소스-제작-프롬프트)

---

## SND-STY-001: 오디오 스타일 가이드

### 방향성 정의

| 항목 | 방향 | 레퍼런스 |
|------|------|---------|
| 오디오 무드 | `[예: 서정적 오케스트라 / 전자음 + 어쿠스틱 혼합 / 민속 악기]` | `[레퍼런스 게임/영화]` |
| 전체 톤 | `[밝고 경쾌함 / 웅장하고 어두움 / 중립적]` | `[레퍼런스]` |
| 음악 장르 | `[오케스트라 / 전자음악 / 어쿠스틱 / 혼합]` | `[레퍼런스 트랙명]` |
| 믹싱 레퍼런스 | `[레퍼런스 게임명 / 영화명]` | — |
| 다이나믹 레인지 | `-14 LUFS` (Streaming 표준) | Spotify / Apple Music 기준 |

> 📌 **LUFS(Loudness Units Full Scale)**: 방송·스트리밍 표준 음량 단위. -14 LUFS는 Spotify 표준.
> 📌 **BPM(Beats Per Minute)**: 음악 템포 단위. 높을수록 빠르고 긴박한 분위기.

### BPM 방향

| 씬 유형 | 목표 BPM | 근거 |
|--------|---------|------|
| 전투 (일반) | `[N] BPM` | 긴장감 최고조 구간 기준 |
| 전투 (보스) | `[N] BPM` | 압박감 + 웅장함 |
| 로비 / 메인 화면 | `[N] BPM` | 안정감 + 집중 유지 |
| 승리 | `[N] BPM` | 해방감, 만족감 |
| 패배 | `[N] BPM` | 아쉬움, 무거움 |

### 오디오 미들웨어 선택

| 방식 | 적합 조건 |
|------|----------|
| Unity Audio Mixer (기본) | 소규모 프로젝트, 단순한 오디오 요구 |
| FMOD | 복잡한 Adaptive Music, 다양한 파라미터 제어 |
| Wwise | 대규모 콘솔·PC 게임, 고급 Audio Designer 보유 시 |

- **본 프로젝트 선택**: `[Unity 기본 / FMOD / Wwise]`

> **[WARNING]** FMOD/Wwise 선택 시 CL-ARC-001 빌드 의존성 추가 및 라이센스 비용 검토 필요.

### 오디오 압축 포맷 선택 기준

> SND-UNT-001의 Import 설정표는 "Unity에서 어떻게 구현하나"에 대한 것이며, 이 표는 "왜 이 포맷을 선택하나"에 대한 결정 기준입니다.

| 용도 | 권장 포맷 | Load Type | 이유 |
|------|---------|-----------|------|
| BGM (2분 이상) | Vorbis (OGG) | Streaming | 메모리 절약, 품질 손실 미미 |
| SFX 짧은 루프 (1초 미만) | ADPCM | Decompress On Load | 빠른 재생, 낮은 CPU |
| SFX 일반 (1~5초) | Vorbis | Compressed In Memory | 메모리·품질 균형 |
| VO (보이스) | Vorbis | Streaming | 파일 크기 최우선 |
| 고품질 SFX (PC/콘솔) | PCM | Decompress On Load | 무손실, 메모리 여유 시 |

> **모바일 주의**: PCM은 메모리를 많이 차지하므로 모바일에서는 BGM에 절대 사용 금지.

### AudioSource / Listener 설정 원칙

- **Audio Listener는 씬당 1개** (주로 Main Camera 부착). 복수 존재 시 Unity 경고 발생
- `AudioSource.PlayOneShot()` — 동시 재생이 필요한 SFX (풀 관리 불필요하나 정지 불가)
- `AudioSource.Play()` — 중단·반복 제어가 필요한 루프 오디오
- 3D 사운드 사용 시 `Spatial Blend = 1.0`, `Min/Max Distance` 반드시 설정
- Object Pool과 연동하여 AudioSource 컴포넌트 재사용 권장 (빈번한 Instantiate 금지)

---

## SND-BGM-001: BGM (배경 음악)

> 📌 **BGM(Background Music)**: 게임 내 배경 음악. 심리스 루프(Seamless Loop) 재생 기반.

### BGM 목록

| BGM ID | 용도 | 분위기 | BPM | 길이(루프) | 파일명 |
|--------|------|--------|-----|----------|--------|
| BGM_001 | 타이틀 / 로비 | 안정, 설렘 | `[N]` | 2분 이상 | `BGM_Title_Main.ogg` |
| BGM_002 | 전투 (일반) | 긴장, 흥분 | `[N]` | 3~4분 | `BGM_Battle_Normal.ogg` |
| BGM_003 | 전투 (보스) | 압박, 웅장 | `[N]` | 4~5분 | `BGM_Battle_Boss.ogg` |
| BGM_004 | 승리 | 해방, 만족 | `[N]` | 30초~1분 | `BGM_Victory.ogg` |
| BGM_005 | 패배 | 아쉬움, 무거움 | `[N]` | 15~30초 | `BGM_Defeat.ogg` |
| BGM_006 | 상점 / 메뉴 | 편안, 중립 | `[N]` | 1분 이상 | `BGM_Shop.ogg` |
| `BGM_[N]` | `[용도]` | `[분위기]` | `[N]` | `[길이]` | `BGM_[이름].ogg` |

### BGM 기술 규격

| 항목 | 기준 | 비고 |
|------|------|------|
| 파일 형식 (인게임) | **OGG Vorbis** | MP3는 루프 시 무음 간격 발생 가능 |
| 파일 형식 (마스터 원본) | **WAV (PCM 24bit)** | 아카이브 보관용 |
| 샘플레이트 | 44,100 Hz | 표준 CD 품질 |
| 비트레이트 | 128kbps (모바일) / 192kbps (PC) | 용량 vs. 품질 균형 |
| 채널 | 스테레오 (2채널) | — |
| 루프 포인트 | 정확한 In/Out 포인트 마킹 필수 | BPM 계산 기반 설정 |
| 음량 기준 | -14 LUFS | 스마트폰 스피커 기준 |

> [WARNING] BGM 루프 포인트 오류 시 루프 재생 시 '클릭' 노이즈 발생. 납품 전 Unity에서 직접 루프 확인 필수.

### BGM 전환 규칙

- **씬 전환 시**: FadeOut `0.5초` → FadeIn `0.5초` 크로스페이드
- **전투 시작 시**: 이전 BGM 즉시 중단 → 전투 BGM FadeIn `0.3초`
- **전투 종료 시**: 전투 BGM FadeOut `1.0초` → 결과 BGM 재생

### Adaptive Music (상태 연동 BGM)

게임 상태(긴장/안전/보스 등)에 따라 음악이 동적으로 변하는 경우:

| 방식 | 구현 | 적합 장르 |
|------|------|---------|
| **레이어 크로스페이드** | 동일 BPM 트랙을 AudioMixerGroup으로 분리, 상태에 따라 볼륨 전환 | 액션, RPG |
| **수직 리믹싱** | FMOD/Wwise Parameter로 인스트루먼트 레이어 추가·제거 | 고품질 콘솔/PC |
| **수평 시퀀싱** | 상태 전환 시 다음 루프 포인트에서 자연스럽게 트랙 전환 | 심플한 모바일 |

```
// 레이어 크로스페이드 예시 구조
BGM_Base.wav       → 항상 재생 (베이스 라인)
BGM_Tension.wav    → 적 감지 시 볼륨 증가 (동일 BPM, 동일 길이)
BGM_Boss.wav       → 보스 전투 시 크로스페이드
```

- **전환 시 클릭음 방지**: 볼륨 전환은 최소 0.1~0.5초 페이드 처리
- **본 프로젝트 Adaptive Music 필요 여부**: `[필요 — 방식: ___ / 불필요]`

---

## SND-SFX-001: SFX (효과음)

> 📌 **SFX(Sound Effects)**: 게임 내 효과음. 이벤트 트리거 기반 재생.
> 📌 **Ambience**: 주변 환경음. 바람, 물소리, 군중 소리 등 배경 분위기 조성 음향.

### SFX 카테고리 및 예산

| 카테고리 | 설명 | 최대 동시 재생 | 우선순위 |
|---------|------|-------------|---------|
| UI SFX | 버튼 클릭, 팝업 오픈 등 UI 인터랙션 | 3개 | 높음 |
| 캐릭터 SFX | 공격, 피격, 이동 등 캐릭터 행동음 | 5개 | 높음 |
| 스킬 / 이펙트 SFX | 스킬 발동, 폭발 등 게임플레이 효과음 | 8개 | 중간 |
| 환경 SFX | 발소리, 문소리, 환경 트리거음 | 4개 | 낮음 |
| 알림 SFX | 레벨업, 보상 획득, 알림음 | 2개 | 매우 높음 |
| Ambience | 배경 환경음 (바람, 새소리 등) | 3개 | 매우 낮음 |

### SFX 기술 규격

| 항목 | 기준 | 비고 |
|------|------|------|
| 파일 형식 (인게임) | **OGG** | — |
| 파일 형식 (원본) | **WAV (PCM 24bit)** | — |
| 샘플레이트 | 44,100 Hz (UI/캐릭터) / 22,050 Hz (환경) | — |
| 채널 | **모노** (공간음향 사용 시) | 3D 사운드에 모노 필수 |
| 길이 | UI: 0.1~0.5초 / 일반: 0.5~3초 / 환경: 3~10초 | — |
| 무음 구간 | 시작/끝 무음 0.05초 이하 | 재생 딜레이 방지 |
| 피치 변형 변수 | ±5~10% 랜덤 | 반복 재생 피로감 감소 (Unity AudioSource 설정) |

### 필수 SFX 목록 (템플릿)

| SFX ID | 용도 | 재생 트리거 | 파일명 |
|--------|------|-----------|--------|
| SFX_UI_001 | 버튼 클릭 | 모든 버튼 Tap | `SFX_UI_BtnClick.ogg` |
| SFX_UI_002 | 팝업 오픈 | 팝업 표시 | `SFX_UI_PopupOpen.ogg` |
| SFX_UI_003 | 팝업 닫기 | 팝업 닫기 | `SFX_UI_PopupClose.ogg` |
| SFX_CHR_001 | 캐릭터 기본 공격 | 공격 모션 히트 프레임 | `SFX_CHR_[이름]_Attack01.ogg` |
| SFX_CHR_002 | 캐릭터 피격 | 피격 처리 시 | `SFX_CHR_[이름]_Hit.ogg` |
| SFX_CHR_003 | 캐릭터 사망 | 사망 처리 시 | `SFX_CHR_[이름]_Death.ogg` |
| SFX_SYS_001 | 레벨업 | 레벨업 이벤트 | `SFX_SYS_LevelUp.ogg` |
| SFX_SYS_002 | 보상 획득 | 보상 수령 시 | `SFX_SYS_Reward.ogg` |

---

## SND-VO-001: Voice Over (보이스 오버)

> 📌 **VO(Voice Over)**: 캐릭터 음성 및 내레이션.

### 녹음 스펙

| 항목 | 기준 |
|------|------|
| 샘플레이트 | 48kHz |
| 비트 심도 | 24bit WAV |
| 채널 | 모노 |
| 노이즈 플로어 | -60dB 이하 |
| 피크 레벨 | -6dB 이하 |

### 지원 언어

| 언어 | 코드 | 녹음 방식 | 비고 |
|------|------|---------|------|
| 한국어 | `KO` | 전문 성우 녹음 | 기준 언어 |
| 영어 | `EN` | `[전문 성우 / AI 합성]` | — |
| 일본어 | `JP` | `[전문 성우 / AI 합성]` | — |
| 중국어 간체 | `CN-S` | `[전문 성우 / AI 합성]` | — |
| `[추가 언어]` | `[코드]` | `[방식]` | — |

### 립싱크

- **필요 여부**: `[Yes / No]`
- **도구**: `[OVRLipSync / SALSA / 수동 Blendshape]`
- **적용 범위**: `[메인 스토리 컷씬 / 전 VO]`

### 파일 명명 규칙

```
VO_[캐릭터ID]_[씬ID]_[라인번호]_[언어코드].wav

예시:
  VO_Hero_SCN001_L001_KO.wav
  VO_Hero_SCN001_L001_EN.wav
```

---

## SND-UNT-001: Unity 오디오 통합 가이드

### Audio Mixer 채널 구조

| 채널명 | 포함 Sound | 기본 볼륨 | 유저 설정 가능 |
|--------|----------|---------|-------------|
| Master | 전체 출력 | 0dB | ✅ |
| BGM | 배경 음악 전체 | -3dB | ✅ |
| SFX | 효과음 전체 | 0dB | ✅ |
| Voice | 캐릭터 음성 | 0dB | ✅ |
| Ambience | 환경음 | -6dB | ❌ (자동 제어) |

### AudioManager 설계 기준

- BGM 전환: **FadeOut 0.5초 → FadeIn 0.5초** 크로스페이드 적용
- SFX 풀: 동시 재생 최대 **16채널**. 초과 시 우선순위 낮은 순 중단
- 볼륨 설정 저장: `PlayerPrefs`에 `BGMVolume`, `SFXVolume`, `VoiceVolume` 저장
- 포커스 아웃: 앱 백그라운드 시 BGM 일시정지, 복귀 시 재개
- 3D 공간음향: SFX 카테고리에 `AudioSource` 3D 설정 적용, UI/BGM은 2D 고정

> [WARNING] Unity `AudioClip`을 **Preload Audio Data** 옵션 On 상태로 다수 사용 시 메모리 급증. 핵심 SFX만 Preload, 나머지는 **Load On Demand** 설정 권장.

### 오디오 Import 설정 기준

| 에셋 유형 | Load Type | Compression | 비고 |
|----------|----------|-------------|------|
| BGM | Streaming | Vorbis | 메모리 절약 |
| 주요 SFX (자주 사용) | Decompress On Load | Vorbis | 재생 지연 없음 |
| 일반 SFX | Compressed In Memory | Vorbis | 균형 |
| VO | Streaming | Vorbis | 용량 큰 파일 |
| 짧은 UI SFX (<1초) | Decompress On Load | PCM | 즉각 재생 |

---

## SND-PRM-001: 리소스 제작 프롬프트

> 📌 **파트 E 문서 필수 섹션**: AI 음악·효과음 생성 도구(Suno, Udio, AudioCraft, ElevenLabs 등)를 활용해
> 리소스를 제작할 때 사용하는 프롬프트를 카테고리별로 기록합니다.
> E 문서 작성 시 아래 각 테이블의 모든 `[대괄호]` 항목을 프로젝트 실제 스펙으로 채워야 합니다.

### 프롬프트 컬럼 정의

| 컬럼 | 설명 |
|------|------|
| **리소스 ID** | SND-BGM-001 / SND-SFX-001 테이블의 ID와 1:1 대응 |
| **생성 도구** | `Suno` / `Udio` / `AudioCraft` / `ElevenLabs` / `Stable Audio` 등 |
| **프롬프트** | 생성할 음악·효과음의 분위기·장르·악기·BPM·길이 등을 구체적으로 기술 |
| **Style Tag** | 도구 지원 시 장르·분위기 태그 (예: `orchestral, epic, 140bpm`) |
| **주요 파라미터** | Duration, Temperature, Seed(고정 시) 등 도구별 설정값 |
| **비고** | 루프 포인트 지정, 후처리(노이즈 제거·정규화) 지침, 레퍼런스 트랙 경로 |

---

### 1. BGM 제작 프롬프트

| BGM ID | 생성 도구 | 프롬프트 | Style Tag | 주요 파라미터 | 비고 |
|--------|---------|---------|-----------|------------|------|
| BGM_001 | `[도구명]` | `[예: uplifting main menu theme, orchestral with light electronic elements, loopable, builds energy gradually, [색상·분위기 키워드]]` | `[예: orchestral, uplifting, loopable, 100bpm]` | `[Duration: 120s, Seed: N]` | 루프 포인트: `[N]초~[N]초`. -14 LUFS 정규화 |
| BGM_002 | `[도구명]` | `[예: intense battle music, fast-paced, driving rhythm, brass stabs, [장르] influenced, loopable]` | `[예: action, intense, brass, 140bpm]` | `[Duration: 180s]` | 전투 진입 시 즉시 전환 대응 |
| BGM_003 | `[도구명]` | `[예: epic boss battle theme, massive orchestral, choir, tension building, loopable 4-5 minutes]` | `[예: epic, choir, orchestral, 150bpm]` | `[Duration: 240s]` | 보스 등장 연출과 싱크 |
| BGM_004 | `[도구명]` | `[예: victory fanfare, short triumphant burst, uplifting brass, celebratory, 30-60 seconds]` | `[예: fanfare, victory, brass, 120bpm]` | `[Duration: 45s]` | 루프 불필요 |
| BGM_005 | `[도구명]` | `[예: defeat music, somber, slow tempo, melancholic strings, 15-30 seconds]` | `[예: melancholic, strings, slow, 60bpm]` | `[Duration: 20s]` | 루프 불필요 |

> 📌 BGM 추가 시 SND-BGM-001 목록과 행 수를 일치시킵니다.

---

### 2. SFX 제작 프롬프트

| SFX ID | 생성 도구 | 프롬프트 | Style Tag | 주요 파라미터 | 비고 |
|--------|---------|---------|-----------|------------|------|
| SFX_UI_001 | `[도구명]` | `[예: short UI button click sound, clean, crisp, digital tap, 0.1-0.2 seconds, no reverb]` | `[예: UI, click, short, clean]` | `[Duration: 0.15s]` | 무음 구간 0.05초 이하 |
| SFX_UI_002 | `[도구명]` | `[예: popup open whoosh, light airy sweep, rising pitch, 0.3 seconds]` | `[예: UI, whoosh, popup]` | `[Duration: 0.3s]` | — |
| SFX_CHR_001 | `[도구명]` | `[예: character melee attack impact, punchy hit, [무기 종류] weapon sound, medium weight, 0.5 seconds]` | `[예: impact, combat, hit]` | `[Duration: 0.5s]` | 피치 변형 ±10% 적용 예정 |
| SFX_CHR_002 | `[도구명]` | `[예: character damage hit reaction, short grunt or impact, 0.3 seconds, no music]` | `[예: hit, damage, reaction]` | `[Duration: 0.3s]` | — |
| SFX_SYS_001 | `[도구명]` | `[예: level up jingle, rewarding ascending chime, sparkle effect, 1-2 seconds, uplifting]` | `[예: reward, jingle, sparkle]` | `[Duration: 1.5s]` | 알림 우선순위 매우 높음 |

> 📌 SFX 추가 시 SND-SFX-001 목록과 행 수를 일치시킵니다.

---

### 3. Voice Over 제작 프롬프트

> 📌 AI 합성 보이스 사용 시에만 작성. 전문 성우 녹음 시 해당 없음.

| VO ID | 생성 도구 | 캐릭터 설명 | 보이스 방향 프롬프트 | 언어 | 비고 |
|-------|---------|-----------|-----------------|------|------|
| `VO_[캐릭터ID]` | `[ElevenLabs / 기타]` | `[예: 20대 남성, 용감하고 열정적인 주인공]` | `[예: confident young male hero, energetic, clear pronunciation, slight echo on dramatic lines]` | `KO` | 노이즈 플로어 -60dB 이하 확인 |
| `VO_[캐릭터ID]` | `[도구명]` | `[캐릭터 설명]` | `[보이스 방향]` | `EN` | — |

> 📌 VO 미사용 시 해당 없음 — 이유: `[전문 성우 녹음 / VO 없음]` 으로 명시.

---

### 4. 공통 오디오 방향 키워드 (Style Reference)

> 📌 SND-STY-001 방향성과 일치시켜 모든 프롬프트에 일관되게 적용할 키워드를 정의합니다.

| 구분 | 공통 포함 키워드 | 공통 제외 키워드 |
|------|--------------|--------------|
| 전체 공통 | `[예: [게임 장르] game audio, [분위기] mood, clean mix, -14 LUFS normalized]` | `[예: ambient noise, distortion, clipping, heavy reverb on SFX]` |
| BGM 한정 | `[예: seamless loop, [음악 장르], [BPM 범위]bpm]` | `[예: voice, dialogue, non-loopable ending]` |
| SFX 한정 | `[예: punchy, short, no music, dry signal]` | `[예: reverb tail over 0.5s, stereo for 3D SFX]` |

---

## 장르 확장 포인트 (SND)

> 사운드 문서에서 장르·특성에 따라 추가 또는 변경이 필요한 항목:

| 장르 / 특성 | SND 추가 고려 항목 |
|-----------|-----------------|
| 액션/슈팅 | SFX 타격감 강조 목록 확장(히트스톱 연동 SFX 타이밍, 무기별 발사음 변형). SND-STY-001에 임팩트 레이어링 기준 추가 |
| RPG/어드벤처 | VO 비중 대폭 증가. Adaptive Music(상황별 BGM 레이어 전환) 설계 추가. 전투/평화 상태 전환 기준 명시 |
| 캐주얼/퍼즐 | 반복 청취에도 피로도 낮은 짧은 루프 BGM 기준 명시(1분 내외, 단순 멜로디). 보상 획득 효과음 다양성 증가 |
| 공포/서스펜스 | 3D 공간음향 SFX 비중 대폭 증가. 침묵 연출 가이드라인 추가. BGM 없는 구간 설계 규칙 |
| 리듬 게임 | SND-BGM-001에 마스터 트랙 + 노트 데이터 동기화 기술 스펙 추가. 오디오 레이턴시 보정 값(클라이언트 오프셋) 설계 |
| 오프라인/저예산 | SND-VO-001에 AI 합성 보이스 활용 기준 및 품질 검증 절차 추가. VO 언어 수 최소화 전략 |

---

## E 문서 작성 가이드 (SOUND)

> 이 파트의 E 문서(실제 프로젝트 사운드 문서)를 작성할 때 아래 기준을 따르세요.

### 필수 포함 섹션

| 섹션 ID | 섹션명 | 완성 기준 |
|--------|--------|---------|
| SND-STY-001 | 오디오 스타일 가이드 | 무드·장르·BPM 목표값 확정, 레퍼런스 명시 |
| SND-BGM-001 | BGM | 씬별 BGM 목록 + 파일명·형식·납품 사양 확정 |
| SND-SFX-001 | SFX | 전체 효과음 목록 + 파일명·형식·납품 사양 확정 |
| SND-VO-001 | Voice Over | VO 사용 여부 확정; 사용 시 언어·녹음 사양 명시 |
| SND-UNT-001 | Unity 오디오 통합 | AudioMixer 구성·이벤트 연동 방식 확정 |
| SND-PRM-001 | 리소스 제작 프롬프트 | BGM·SFX·VO 전 카테고리 프롬프트 확정. 공통 오디오 방향 키워드 정의 완료 |

### 최소 완성 기준 (Definition of Done)

- [ ] 모든 `[대괄호]` 플레이스홀더 제거됨
- [ ] `[TBD]` 항목에 이유 명시 (해결 시점 포함)
- [ ] 크로스 파트 의존 항목 `[WARNING]` 태그 확인
- [ ] SND-BGM-001 / SND-SFX-001 — 파일명·포맷·납품 경로 모두 확정
- [ ] VO 미사용 시 SND-VO-001에 "해당 없음 — 이유" 명시
- [ ] **SND-PRM-001 리소스 제작 프롬프트 — BGM·SFX 전 항목 프롬프트 확정**
- [ ] SND-PRM-001 공통 오디오 방향 키워드 정의 완료 (SND-STY-001 무드·장르와 일치 확인)

---

## 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0.0 | 2026-04-16 | [작성자] | 초기 템플릿 작성 |
| 1.1.0 | 2026-04-17 | [작성자] | 기반 문서 표시, 장르 확장 포인트 추가 |
| 1.2.0 | 2026-04-17 | [작성자] | E 문서 작성 가이드 (SOUND) 섹션 추가 |
| 1.3.0 | 2026-04-18 | [작성자] | SND-STY-001 미들웨어 선택표·압축 포맷 기준표·AudioSource 원칙 추가, SND-BGM-001 Adaptive Music 섹션 추가 |
| 1.4.0 | 2026-04-18 | [작성자] | SND-PRM-001 리소스 제작 프롬프트 섹션 신설; E 문서 작성 가이드에 프롬프트 필수 항목 추가 |
| 1.x.x | [날짜] | [작성자] | [변경 내용] |

---

*← 이전: [05_Server.md](./05_Server.md) | → 다음: [07_QA.md](./07_QA.md)*
