# 03. ART (아트)

> **Part Code**: ART
> **Version**: 1.4.0
> **Last Updated**: 2026-04-18
> **Document Owner**: Art Director
> **의존성**: COM-OVR-001, COM-CVT-001, GD-COR-001
> **기반 문서 (A)**: `Unity_GameDev_Template.md` — 3. ART 섹션을 상세화한 파트 문서(B)

> 📌 **핵심 원칙**: 모든 아트 에셋은 Style Guide(ART-STY-001)를 기준으로 일관성을 유지한다.
> 스타일 가이드 외 임의 변경은 Art Director 승인 후에만 허용.

---

## 📑 목차

- [ART-STY-001: 스타일 가이드](#art-sty-001-스타일-가이드)
- [ART-CHR-001: 캐릭터 아트](#art-chr-001-캐릭터-아트)
- [ART-ENV-001: 환경 아트](#art-env-001-환경-아트)
- [ART-UI-001: UI 아트](#art-ui-001-ui-아트)
- [ART-VFX-001: 비주얼 이펙트](#art-vfx-001-비주얼-이펙트)
- [ART-ANM-001: 애니메이션](#art-anm-001-애니메이션)
- [ART-OPT-001: 에셋 최적화 체크리스트](#art-opt-001-에셋-최적화-체크리스트)
- [ART-RES-001: 리소스 마스터 목록](#art-res-001-리소스-마스터-목록)
- [ART-PRM-001: 리소스 제작 프롬프트](#art-prm-001-리소스-제작-프롬프트)

---

## ART-STY-001: 스타일 가이드

### 비주얼 톤

| 항목 | 방향성 | 근거 / 레퍼런스 |
|------|--------|----------------|
| 아트 스타일 | `[예: 스타일라이즈드 3D / 픽셀아트 / 카툰 렌더링]` | `[레퍼런스 게임명]` |
| 컬러 팔레트 | 주색상: `#[HEX]` / 보조: `#[HEX]` / 포인트: `#[HEX]` | `[타겟 연령·감성 기준]` |
| 조명 톤 | `[예: 따뜻한 주광 / 차가운 네온 / 중립 환경광]` | `[레퍼런스]` |
| 실루엣 원칙 | 식별성 최우선. 3m 거리에서 캐릭터 간 구분 가능 | `[가독성 기준]` |
| 선(라인) 표현 | `[아웃라인 있음 / 없음]` / 두께: `[N]px` | `[캐릭터 가독성 기준]` |

### 금지 사항

- 레퍼런스 이미지 직접 사용 금지 (라이센스 리스크)
- 스타일 가이드 외 임의 이펙트 추가 금지
- 비승인 폰트 사용 금지 (라이센스 확인 후 사용)

> [WARNING] 주색상 변경 시 UI 아트, 캐릭터 아웃라인, 이펙트 색상 전체 재검토 필요. 알파 단계 이전에만 변경 허용.

---

## ART-CHR-001: 캐릭터 아트

### 폴리곤 예산 (Polygon Budget)

> 📌 **Polygon Budget**: 플랫폼별 렌더링 성능을 고려한 최대 삼각형(Tris) 수 기준.

| 플랫폼 | 주인공 (LOD0) | NPC / 조연 (LOD0) | 근거 |
|--------|-------------|-----------------|------|
| 모바일 (고사양) | `≤ 5,000 Tris` | `≤ 2,500 Tris` | iPhone 13 기준 60fps 유지 |
| 모바일 (보급형) | `≤ 3,000 Tris` | `≤ 1,500 Tris` | 갤럭시 A 시리즈 기준 |
| PC / Console | `≤ 15,000 Tris` | `≤ 8,000 Tris` | RTX 3060 기준 120fps |

### 텍스처 사이즈 기준

> [WARNING] 텍스처 사이즈는 반드시 **2의 제곱수**여야 합니다 (128, 256, 512, 1024, 2048…). 비정형 사이즈는 GPU 메모리 낭비 원인.

| 텍스처 용도 | 모바일 | PC | 형식 |
|-----------|--------|-----|------|
| 캐릭터 Albedo (Diffuse) | 512×512 | 1024×1024 | PNG (투명 필요 시) / JPG |
| 캐릭터 Normal Map | 512×512 | 1024×1024 | PNG (Linear 색공간 **필수**) |
| 캐릭터 Metallic / Roughness | 512×512 | 1024×1024 | PNG |
| 배경 오브젝트 | 256×256 | 512×512 | PNG / JPG |
| UI 아이콘 | 128×128 | 256×256 | PNG (테두리 바깥 투명 배경 필수) |
| 전신 UI 초상화 | 512×1024 | 1024×2048 | PNG |

> 📌 **PBR(Physically Based Rendering)**: 물리 기반 렌더링. Albedo / Normal / Metallic / Roughness 텍스처 조합. 기준 텍스처 해상도: `[2048×2048]` [근거: PBR 표준].

### 리깅 기준

| 항목 | 기준 | 근거 |
|------|------|------|
| Rig 본(Bone) 수 | 모바일 `≤ 30본` / PC `≤ 60본` | Skinning 계산 비용 최소화 |
| Unity Humanoid 호환 | 필수 (애니메이션 재사용 목적) | Unity Humanoid Avatar 시스템 |
| 손가락 리깅 | PC: 가능 / 모바일: 생략 권장 | 성능 vs. 표현력 균형 |

---

## ART-ENV-001: 환경 아트

### LOD 정책

> 📌 **LOD(Level of Detail)**: 거리 기반 메쉬 단계적 품질 조정 기법. LOD0=최고품질(근거리), LOD3=최저품질(원거리).

| LOD 단계 | 거리 | 폴리곤 비율 | 조건 |
|---------|------|-----------|------|
| LOD0 | 근거리 `[0~Nm]` | 100% | 기본 |
| LOD1 | 중거리 `[N~Nm]` | 50% 감축 | 자동 전환 |
| LOD2 | 원거리 `[N~Nm]` | 75% 감축 | 자동 전환 |
| LOD3 / Billboards | 매우 원거리 | 90% 이상 감축 | 선택 적용 |

### 환경 최적화 기준

| 항목 | 기준 | 비고 |
|------|------|------|
| 씬당 DrawCall | 모바일 `≤ 100` / PC `≤ 300` | Batching 적용 후 기준 |
| 씬당 실시간 조명 수 | 모바일 `1개` / PC `≤ 3개` | 추가 조명은 Baked 처리 |
| 시야 거리 (Far Clip) | 모바일 `100m` / PC `300m` | LOD 시스템 연동 |
| Static 오브젝트 비율 | 고정 오브젝트의 `80%` 이상 Static 설정 | Static Batching 활용 |
| Occlusion Culling | 필수 적용 (씬 빌드 시 Bake) | DrawCall 30%↑ 감소 효과 |

### 라이팅 전략

- **기본**: Baked Lighting 사용 (Lightmap)
- **예외**: 플레이어, 주요 NPC, 이펙트에만 동적 조명 허용
- **Ambient**: `[Gradient / Color / Skybox]`
- **Shadow**: 모바일 `Hard Shadow Only` / PC `Soft Shadow`

### 타일링 텍스처 활용

- 대형 오브젝트: 타일링 텍스처 + 디테일 마스크(Detail Mask) 조합
- UV 스케일: 1m² 기준 `[N]` 타일

---

## ART-UI-001: UI 아트

### 해상도 대응

| 항목 | 기준 | 비고 |
|------|------|------|
| 기준 해상도 | 1920×1080 (16:9) | UI 설계 기준 해상도 |
| 모바일 세로 기준 | 1080×1920 | 노치·펀치홀 Safe Area 고려 |
| 대응 범위 | 16:9 ~ 21:9 (노치 포함) | — |
| UI Scale Mode | Canvas Scaler: Scale With Screen Size | Unity 설정 |
| Reference Resolution | 모바일 `1080×1920` / PC `1920×1080` | Canvas Scaler 기준값 |
| Match Width/Height | 0.5 (균등 스케일) | 비율 변화 대응 |

### 접근성 (Accessibility)

- **최소 터치 타겟**: `44×44pt` [근거: Apple HIG, Material Design 공통 기준]
- **색상 의존성 금지**: 색약 대응을 위해 아이콘 + 텍스트 병기
- **대비율**: 텍스트와 배경 대비 `4.5:1` 이상 [근거: WCAG 2.1 AA 기준]
- **최소 폰트 크기**: `14px` 이하 사용 금지 [근거: 모바일 가독성 저하]

### UI 색상 팔레트

| 용도 | 색상 HEX | 사용 위치 |
|------|---------|----------|
| 주요 버튼 (Active) | `#[HEX]` | 주요 행동 유도 버튼 |
| 주요 버튼 (Disabled) | `#[HEX]` | 비활성 버튼 |
| 보조 버튼 | `#[HEX]` | 취소 / 닫기 |
| 배경 (Dark) | `#[HEX]` | 팝업 오버레이, 어두운 패널 |
| 배경 (Light) | `#[HEX]` | 밝은 패널, 카드 배경 |
| 텍스트 (Primary) | `#[HEX]` | 주요 텍스트 |
| 텍스트 (Secondary) | `#[HEX]` | 부가 설명 텍스트 |
| 성공 / 긍정 | `#[HEX]` | 보상, 성공 메시지 |
| 경고 / 주의 | `#[HEX]` | 경고 메시지 |
| 위험 / 오류 | `#[HEX]` | 에러, 삭제 버튼 |

### 폰트 기준

| 용도 | 폰트명 | 크기(px) | 굵기 | 비고 |
|------|--------|---------|------|------|
| 타이틀 / 헤더 | `[폰트명]` | 28~40px | Bold | — |
| 본문 / 설명 | `[폰트명]` | 18~22px | Regular | 줄간격 1.4 이상 |
| 버튼 텍스트 | `[폰트명]` | 20~24px | Medium | 배경 대비 4.5:1 이상 |
| 소형 라벨 | `[폰트명]` | 14~16px | Regular | **14px 이하 사용 금지** |
| 숫자 / 수치 | `[폰트명]` | 20~28px | Bold | 고정폭 폰트 권장 |

---

## ART-VFX-001: 비주얼 이펙트

### VFX 파티클 예산

> 📌 파티클 예산 초과 시 GPU 오버히트 및 프레임 드롭 발생. 모바일 기준 엄수.

| 우선순위 | 이펙트 종류 | 파티클 상한 (모바일) | 파티클 상한 (PC) |
|---------|-----------|-----------------|----------------|
| High | 플레이어 스킬 | `200` | `500` |
| Medium | 적 스킬 | `100` | `300` |
| Low | 환경 이펙트 | `50` | `150` |
| Very Low | UI 이펙트 | `30` | `100` |

> [WARNING] CL-OPT-001의 DrawCall 예산(≤100)과 충돌 가능. VFX 이펙트 기여 DrawCall 측정 후 조정 필요. → 참조: CL-OPT-001

### VFX 기술 규격

| 항목 | 기준 |
|------|------|
| 파티클 시스템 | Unity VFX Graph (모바일: Particle System) |
| 텍스처 형식 | PNG (알파 채널 필수) |
| 텍스처 사이즈 | `≤ 256×256` (모바일) / `≤ 512×512` (PC) |
| 루프 이펙트 | 반드시 원활한 루프 확인 후 납품 |

---

## ART-ANM-001: 애니메이션

### 애니메이션 기술 기준

| 항목 | 기준 | 근거 |
|------|------|------|
| 작업 프레임레이트 | 30fps | 파일 크기 절감 |
| 인게임 보간 | 60fps (Unity 보간 처리) | 시각적 부드러움 |
| 루프 애니메이션 연속성 | 첫 프레임 = 마지막 프레임 **필수** | Loop 시 튀는 현상 방지 |
| Root Motion | 이동/회전: Root Motion / 전투: 코드 제어 | 슬라이딩 현상 방지 |
| Blend Tree 최대 깊이 | `3단계 이하` | Unity Animator 성능 최적화 |
| 불필요한 키프레임 | Bake 후 커브 최적화 필수 | 데이터 크기 절감 |

### 애니메이션 레이어 구조

```
Base Layer       (전신 이동, 대기, 피격)
  └── Upper Body (상체 전투 동작)
       └── Additive (표정, 숨 쉬기 등 보조 동작)
```

### 필수 애니메이션 목록 (캐릭터당)

| 애니메이션명 | 파일명 예시 | 루프 | 비고 |
|------------|----------|------|------|
| Idle | `AN_[CharName]_Idle.anim` | ✅ | |
| Walk | `AN_[CharName]_Walk.anim` | ✅ | Root Motion |
| Run | `AN_[CharName]_Run.anim` | ✅ | Root Motion |
| Attack (Basic) | `AN_[CharName]_Attack01.anim` | ❌ | |
| Skill A | `AN_[CharName]_SkillA.anim` | ❌ | VFX 싱크 포인트 표시 |
| Hit / Damage | `AN_[CharName]_Hit.anim` | ❌ | |
| Death | `AN_[CharName]_Death.anim` | ❌ | |

---

## ART-OPT-001: 에셋 최적화 체크리스트

> 📌 에셋 납품 전 아트 담당자가 자체 확인 후 클라이언트 파트에 전달합니다.
> 미충족 항목은 즉시 반려됩니다.

| 분류 | 체크 항목 | 기준 | 확인 |
|------|----------|------|------|
| 텍스처 | 2의 제곱수 사이즈 | 128~2048 범위 내 | ☐ |
| 텍스처 | 불필요한 알파 채널 제거 | 불투명 텍스처는 JPG 사용 | ☐ |
| 텍스처 | Linear / Gamma 색공간 설정 | Normal Map은 반드시 Linear | ☐ |
| 텍스처 | Texture Atlas 적용 여부 | DrawCall 최소화 대상 확인 | ☐ |
| 메쉬 | 폴리곤 예산 준수 | 플랫폼별 기준 참조 | ☐ |
| 메쉬 | Pivot 위치 정확성 | 캐릭터: 발바닥 / 오브젝트: 중심 | ☐ |
| 메쉬 | Scale 정규화 | 모든 오브젝트 Scale = (1,1,1) | ☐ |
| 메쉬 | 불필요한 버텍스 / 면 제거 | N-Gon 없음, 뒷면 컬링 설정 | ☐ |
| 애니메이션 | 루프 연속성 확인 | 첫 프레임 = 마지막 프레임 | ☐ |
| 애니메이션 | 불필요한 키프레임 제거 | Bake 후 커브 최적화 | ☐ |
| UI | 나인슬라이스(9-slice) 설정 | 스케일 변형 UI에 적용 | ☐ |
| UI | Safe Area 대응 확인 | 노치 기기 테스트 완료 | ☐ |
| 전체 | 파일명 규칙 준수 | COM-CVT-001 네이밍 규칙 참조 | ☐ |
| 전체 | Git LFS / Perforce 커밋 완료 | 바이너리 파일 LFS 관리 필수 | ☐ |

---

## ART-RES-001: 리소스 마스터 목록

> 📌 **파트 E 문서 필수 섹션**: 아트 작업자가 단일 참조점으로 납품 파일 전체를 확인하는 목록.
> E 문서 작성 시 아래 각 테이블의 모든 행을 프로젝트 실제 스펙으로 채워야 합니다.
> `[대괄호]` 항목이 하나라도 남아 있으면 E 문서 미완성으로 간주합니다.

### 테이블 컬럼 정의

| 컬럼 | 설명 |
|------|------|
| **리소스 ID** | 내부 식별 ID (예: CHR-SPR-001) |
| **파일명 패턴** | 실제 납품 파일명. 변수는 `[대괄호]` 표기 |
| **크기 (px)** | 정확한 픽셀 크기. 반드시 2의 제곱수 |
| **형식** | `.png` / `.jpg` / `.psd` 등 |
| **알파** | 투명 배경 필요 여부 (O / X) |
| **Sprite Atlas** | 배정된 Atlas 이름. `없음`이면 단독 파일 |
| **수량** | 예: 캐릭터당 N개, 테마당 N개 |
| **비고** | 루프 여부, fps, 9-Slice 여부 등 |

---

### 파일명 변수 정의

> ⚠️ **파일명에 사용되는 `[대괄호]` 변수는 아래 표에 정의된 출처에서 값을 가져와야 합니다.**
> 임의로 값을 만들지 말고, 반드시 해당 GD 문서 또는 팀 합의 결과를 확인하세요.

| 변수 | 설명 | 값 결정 출처 | 형식 규칙 |
|------|------|------------|---------|
| `[ID]` | 캐릭터/에셋 고유 코드 | GD 문서 (캐릭터 목록) | 영문 대문자·숫자 조합. 예: `CHR01`, `HERO` |
| `[THM]` | 테마/월드 코드 | GD 문서 (스테이지 목록) | 영문 대문자. 예: `FOREST`, `CITY` |
| `[NN]` | 프레임 순서 번호 | 애니메이션 프레임 수 결정 후 | 2자리 숫자. 예: `01`, `02` |
| `[Name]` | 에셋 기능명 | 팀 합의 후 결정 | PascalCase. 예: `Start`, `Pause`, `Coin` |
| `[State]` | 상태 접미사 | — | `Active` / `Disabled` / `Idle` / `Hover` |
| `[Grade]` | 등급 코드 | GD 문서 (아이템 등급 체계) | 영문. 예: `Common`, `Rare`, `Epic` |

---

### 1. 캐릭터 스프라이트 (인게임 애니메이션)

> 📌 2D 게임용 테이블. 3D 게임은 텍스처/메쉬 목록으로 대체.

> ⚑ **이 테이블 완성 조건**: 모든 `[대괄호]` 제거. 크기는 2의 제곱수로 확정. 수량은 구체적 숫자로 기재. Sprite Atlas 이름은 섹션 7과 일치.

| 리소스 ID | 파일명 패턴 | 크기 (px) | 형식 | 알파 | Sprite Atlas | 수량 | 비고 |
|---------|-----------|----------|------|------|-------------|------|------|
| CHR-SPR-001 | `SPR_CHR_[ID]_Idle_[NN].png` | `[256×256]` | .png | O | `Atlas_CHR_[ID]` | 캐릭터당 `[N]`프레임 | `[12fps]`, Loop |
| CHR-SPR-002 | `SPR_CHR_[ID]_Run_[NN].png` | `[256×256]` | .png | O | `Atlas_CHR_[ID]` | 캐릭터당 `[N]`프레임 | `[12fps]`, Loop |
| CHR-SPR-003 | `SPR_CHR_[ID]_[State]_[NN].png` | `[256×256]` | .png | O | `Atlas_CHR_[ID]` | 캐릭터당 `[N]`프레임 | 상태별 반복 추가 |

> 📌 필수 애니메이션 상태 목록은 ART-ANM-001 참조. 상태가 추가될 때마다 행 추가.

---

### 2. 캐릭터 UI (포트레이트·아이콘)

> ⚑ **이 테이블 완성 조건**: 모든 `[대괄호]` 제거. 크기는 2의 제곱수로 확정. 수량은 구체적 숫자로 기재. Sprite Atlas 이름은 섹션 7과 일치.

| 리소스 ID | 파일명 패턴 | 크기 (px) | 형식 | 알파 | Sprite Atlas | 수량 | 비고 |
|---------|-----------|----------|------|------|-------------|------|------|
| CHR-UI-001 | `UI_CHR_[ID]_Icon.png` | `[256×256]` | .png | O | `Atlas_UI_Common` | 캐릭터당 1 | 인벤토리·뽑기 결과 |
| CHR-UI-002 | `UI_CHR_[ID]_Portrait.png` | `[512×512]` | .png | O | `Atlas_UI_Common` | 캐릭터당 1 | 선택 화면 |

---

### 3. 배경 레이어 (테마별)

> ⚑ **이 테이블 완성 조건**: 모든 `[대괄호]` 제거. 크기는 2의 제곱수로 확정. 수량은 구체적 숫자로 기재. Sprite Atlas 이름은 섹션 7과 일치.

| 리소스 ID | 파일명 패턴 | 크기 (px) | 형식 | 알파 | Sprite Atlas | 수량 | 비고 |
|---------|-----------|----------|------|------|-------------|------|------|
| ENV-BG-001 | `BG_[THM]_Sky.png` | `[1920×1080]` | .jpg | X | 없음 (단독) | 테마당 1 | 고정 배경. Parallax 0.0x |
| ENV-BG-002 | `BG_[THM]_Far_[N].png` | `[1920×540]` | .png | O | `Atlas_ENV_[THM]` | 테마당 `[N]` | Parallax `[0.Nx]`, 수평 타일링 |
| ENV-BG-003 | `BG_[THM]_Mid_[N].png` | `[1920×540]` | .png | O | `Atlas_ENV_[THM]` | 테마당 `[N]` | Parallax `[0.Nx]`, 수평 타일링 |
| ENV-BG-004 | `BG_[THM]_Near_[N].png` | `[1920×540]` | .png | O | `Atlas_ENV_[THM]` | 테마당 `[N]` | Parallax `[1.0x]` |
| ENV-BG-005 | `BG_[THM]_Fore_[N].png` | `[512×512]` 이하 | .png | O | `Atlas_ENV_[THM]` | 테마당 다수 | Parallax `[1.2x]`, 전경 오브젝트 |

---

### 4. 지형·타일셋·장애물·아이템

> ⚑ **이 테이블 완성 조건**: 모든 `[대괄호]` 제거. 크기는 2의 제곱수로 확정. 수량은 구체적 숫자로 기재. Sprite Atlas 이름은 섹션 7과 일치.

| 리소스 ID | 파일명 패턴 | 크기 (px) | 형식 | 알파 | Sprite Atlas | 수량 | 비고 |
|---------|-----------|----------|------|------|-------------|------|------|
| ENV-TILE-001 | `TILE_[THM]_[Type]_[N].png` | `[64×64~128×128]` | .png | O | `Atlas_OBS_[THM]` | 테마당 다수 | 타일맵 호환 설계 |
| ENV-OBS-001 | `OBS_[ID]_Default.png` | `[128×128~256×256]` | .png | O | `Atlas_OBS_[THM]` | 장애물 종류당 1 | 오브젝트 풀 사용 |
| ITEM-COIN | `ITEM_Coin_[NN].png` | `[64×64]` | .png | O | `Atlas_COIN_Items` | `[N]`프레임 | 회전 애니메이션 |
| ITEM-PWR | `ITEM_PWR_[ID].png` | `[128×128]` | .png | O | `Atlas_COIN_Items` | 파워업 종류당 1 | 종류별 색상 구분 필수 |

---

### 5. UI 에셋 (버튼·아이콘·프레임·HUD)

> ⚑ **이 테이블 완성 조건**: 모든 `[대괄호]` 제거. 크기는 2의 제곱수로 확정. 수량은 구체적 숫자로 기재. Sprite Atlas 이름은 섹션 7과 일치.

| 리소스 ID | 파일명 패턴 | 크기 (px) | 형식 | 알파 | 9-Slice | Sprite Atlas | 비고 |
|---------|-----------|----------|------|------|--------|-------------|------|
| UI-BTN-ACT | `UI_BTN_[Name]_Active.png` | `[가변]` | .png | O | O | `Atlas_UI_Common` | 버튼 종류당 1 |
| UI-BTN-DIS | `UI_BTN_[Name]_Disabled.png` | `[동일]` | .png | O | O | `Atlas_UI_Common` | 버튼 종류당 1 |
| UI-ICON-001 | `UI_ICON_[Name].png` | `[64×64]` | .png | O | X | `Atlas_UI_Common` | 각 아이콘 1 |
| UI-HUD-001 | `UI_HUD_[Element]_[State].png` | `[100×100]` | .png | O | X | `Atlas_UI_Common` | HUD 요소당 상태별 1 |
| UI-FRAME-001 | `UI_FRAME_[Grade]_Idle.png` | `[256×256]` | .png | O | X | `Atlas_UI_Common` | 등급당 1 |
| UI-BG-001 | `UI_BG_[Name].png` | `[9-Slice 설정]` | .png | O | O | `Atlas_UI_Common` | 팝업·패널 배경 |

---

### 6. VFX 파티클 텍스처

> ⚑ **이 테이블 완성 조건**: 모든 `[대괄호]` 제거. 크기는 2의 제곱수로 확정. 수량은 구체적 숫자로 기재. Sprite Atlas 이름은 섹션 7과 일치.

| 리소스 ID | 파일명 패턴 | 크기 (px) | 형식 | 알파 | Sprite Atlas | 비고 |
|---------|-----------|----------|------|------|-------------|------|
| VFX-TEX-001 | `VFX_TEX_[Name].png` | `≤[256×256]` | .png | O | `Atlas_VFX_Common` | 파티클 텍스처. 종류당 1 |

---

### 7. Sprite Atlas 배정 요약

> 📌 E 문서에서 모든 Atlas를 열거하고 로드 시점·최대 크기를 명시해야 합니다.

> ⚑ **이 테이블 완성 조건**: 모든 Atlas 열거 완료. 최대 크기 확정 (2의 제곱수). 로드 시점 명확히 기재. 포함 리소스 카테고리 = ART-RES-001 섹션 1~6의 Sprite Atlas 컬럼과 전수 일치.

| Atlas 이름 | 포함 리소스 카테고리 | 최대 크기 | 로드 시점 | 비고 |
|-----------|-----------------|---------|---------|------|
| `Atlas_CHR_[ID]` | 캐릭터 인게임 스프라이트 | `[2048×2048]` 최대 2장 | 캐릭터 선택 시 (Addressables) | 캐릭터별 분리 필수 |
| `Atlas_UI_Common` | 공통 UI 아이콘·버튼·프레임 | `[2048×2048]` | 앱 시작 시 상주 | — |
| `Atlas_ENV_[THM]` | 테마별 배경·환경 스프라이트 | `[2048×2048]` | 해당 테마 씬 로드 시 | 테마별 분리 |
| `Atlas_OBS_[THM]` | 테마별 장애물·타일 | `[1024×1024]` | 해당 테마 씬 로드 시 | — |
| `Atlas_COIN_Items` | 코인·파워업 아이템 | `[512×512]` | 런 씬 로드 시 | — |
| `Atlas_VFX_Common` | 공통 VFX 텍스처 | `[512×512]` | 런 씬 로드 시 | — |
| `[추가 Atlas]` | `[포함 리소스]` | `[최대 크기]` | `[로드 시점]` | — |

---

## ART-PRM-001: 리소스 제작 프롬프트

> 📌 **파트 E 문서 필수 섹션**: AI 이미지 생성 도구(Stable Diffusion, Midjourney, ComfyUI 등)를 활용해
> 리소스를 제작할 때 사용하는 프롬프트를 카테고리별로 기록합니다.
> E 문서 작성 시 아래 각 테이블의 모든 `[대괄호]` 항목을 프로젝트 실제 스펙으로 채워야 합니다.

### 프롬프트 컬럼 정의

| 컬럼 | 설명 |
|------|------|
| **리소스 ID** | ART-RES-001 테이블의 리소스 ID와 1:1 대응 |
| **생성 도구** | `Stable Diffusion` / `Midjourney` / `ComfyUI` / `DALL-E` 등 |
| **모델 / 워크플로** | 사용 모델명 또는 워크플로 파일명 (예: `dreamshaper_8.safetensors`) |
| **Positive Prompt** | 생성에 포함할 요소. 스타일·분위기·색상·구도 등을 구체적으로 기술 |
| **Negative Prompt** | 생성에서 제외할 요소. 품질 저하 요인 및 원하지 않는 특성 |
| **주요 파라미터** | Steps, CFG Scale, Sampler, Seed(고정 시), 해상도 등 |
| **비고** | 레퍼런스 이미지 경로, ControlNet 사용 여부, 후처리 지침 등 |

---

### 프롬프트 필수 구성 요소

> ⚠️ **E 문서 작성자 필독**: 아래 요소들은 모든 프롬프트 행에서 필수입니다. 하나라도 누락된 행은 반려됩니다.
> 스타일 토큰은 반드시 **ART-STY-001에서 확정된 값**을 그대로 사용해야 합니다.
> ART-STY-001이 미확정 상태에서 임의로 스타일 단어를 작성하는 것은 금지입니다.

#### Positive Prompt 필수 포함 요소 (5가지)

| # | 필수 요소 | 설명 및 주의 사항 |
|---|----------|----------------|
| 1 | **스타일 토큰** | `ART-STY-001`에서 확정된 값 그대로 사용. 미확정 시 `[ART-STY-001 아트스타일 토큰]`으로 표기 유지 |
| 2 | **리소스 용도 및 뷰 타입** | `full body` / `icon` / `background layer` 등 용도 명확히 기재 |
| 3 | **배경 처리 명시** | `transparent background` 또는 `solid [색상] background` 중 하나를 반드시 선택 |
| 4 | **크기 대응** | `ART-RES-001` 해당 행의 크기 값과 반드시 일치. 불일치 시 반려 |
| 5 | **품질 태그** | `game asset`, `high quality`, `clean edges` 세 가지 고정 태그를 항상 포함 |

#### Negative Prompt 필수 포함 요소 (3가지)

| # | 필수 요소 | 예시 토큰 |
|---|----------|---------|
| 1 | **품질 저하 방지** | `blurry, low quality, jpeg artifacts, noisy` |
| 2 | **용도 외 요소 제거** | 캐릭터: `background, scenery` / 배경: `characters, people, person` |
| 3 | **텍스트·워터마크 제거** | `text, watermark, signature, logo` |

#### 주요 파라미터 필수 기재 항목

| 파라미터 | 필수 여부 | 권장 범위 | 기재 방법 |
|---------|---------|---------|---------|
| **Steps** | 필수 | 20~50 | 구체적 숫자. 예: `Steps: 30` |
| **CFG Scale** | 필수 | 7~12 | 구체적 숫자. 예: `CFG: 7.5` |
| **Sampler** | 필수 | — | 사용 샘플러명 명시. 예: `Sampler: DPM++ 2M Karras` |
| **Size** | 필수 | ART-RES-001 기준 | ART-RES-001 해당 행의 크기와 **반드시 일치**. 불일치 시 즉시 반려 |
| Seed | 선택 | — | 재현성 필요 시만. 예: `Seed: 1234567` |

---

### 1. 캐릭터 프롬프트

| 리소스 ID | 생성 도구 | 모델 / 워크플로 | Positive Prompt | Negative Prompt | 주요 파라미터 | 비고 |
|---------|---------|--------------|----------------|----------------|------------|------|
| CHR-SPR-001 | `[도구명]` | `[모델명]` | `[예: [ART-STY-001 아트스타일 토큰] game character, idle pose, full body, [ART-STY-001 컬러 팔레트 토큰], [ART-STY-001 아웃라인 토큰], transparent background, game asset, high quality, clean edges]` | `[예: background, scenery, blurry, low quality, jpeg artifacts, noisy, text, watermark, signature, logo, extra limbs]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 256×256]` | `[ControlNet Pose 사용 여부 등]` |
| CHR-UI-001 | `[도구명]` | `[모델명]` | `[예: character portrait icon, bust shot, [ART-STY-001 아트스타일 토큰], [ART-STY-001 컬러 팔레트 토큰], transparent background, game asset, high quality, clean edges]` | `[예: full body, scenery, background, blurry, low quality, jpeg artifacts, noisy, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 256×256]` | — |
| CHR-UI-002 | `[도구명]` | `[모델명]` | `[예: character portrait, half body, detailed expression, [ART-STY-001 아트스타일 토큰], transparent background, game asset, high quality, clean edges]` | `[예: full body, scenery, background, blurry, low quality, jpeg artifacts, noisy, extra fingers, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 512×512]` | — |

> 📌 캐릭터 상태(Run, Attack, Hit, Death 등)별 프롬프트는 CHR-SPR-001 행을 복사해 상태마다 추가.

---

### 2. 환경·배경 프롬프트

| 리소스 ID | 생성 도구 | 모델 / 워크플로 | Positive Prompt | Negative Prompt | 주요 파라미터 | 비고 |
|---------|---------|--------------|----------------|----------------|------------|------|
| ENV-BG-001 | `[도구명]` | `[모델명]` | `[예: game background, [THM] theme, sky layer, [ART-STY-001 컬러 팔레트 토큰], wide panorama, no characters, game asset, high quality, clean edges]` | `[예: characters, people, person, UI elements, blurry, low quality, jpeg artifacts, noisy, oversaturated, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 1920×1080]` | 고정 배경. Parallax 0.0x |
| ENV-BG-002 | `[도구명]` | `[모델명]` | `[예: game background far layer, [THM] theme, midground scenery, seamless horizontal tile, [ART-STY-001 컬러 팔레트 토큰], game asset, high quality, clean edges]` | `[예: characters, people, person, foreground objects, blurry, low quality, jpeg artifacts, noisy, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 1920×540]` | 수평 타일링 대응 필수 |
| ENV-TILE-001 | `[도구명]` | `[모델명]` | `[예: game tile, [THM] style, top-down view, seamless texture, [ART-STY-001 컬러 팔레트 토큰], game asset, high quality, clean edges]` | `[예: characters, people, person, asymmetric, blurry, low quality, jpeg artifacts, noisy, realistic photo, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 64×64~128×128]` | 타일맵 호환 여부 확인 |

---

### 3. UI 에셋 프롬프트

| 리소스 ID | 생성 도구 | 모델 / 워크플로 | Positive Prompt | Negative Prompt | 주요 파라미터 | 비고 |
|---------|---------|--------------|----------------|----------------|------------|------|
| UI-BTN-ACT | `[도구명]` | `[모델명]` | `[예: game UI button, [ART-STY-001 아트스타일 토큰], [ART-STY-001 컬러 팔레트 토큰] color, glowing active state, transparent background, game asset, high quality, clean edges, 9-slice compatible border]` | `[예: text on button, complex details, characters, blurry, low quality, jpeg artifacts, noisy, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras]` | 9-Slice 여백 확보 필수 |
| UI-ICON-001 | `[도구명]` | `[모델명]` | `[예: game icon, [아이콘명], [ART-STY-001 아트스타일 토큰], [ART-STY-001 컬러 팔레트 토큰], transparent background, game asset, high quality, clean edges, simple silhouette, high contrast]` | `[예: complex background, characters, multiple objects, blurry, low quality, jpeg artifacts, noisy, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 64×64]` | 종류별 행 추가 |

---

### 4. VFX 파티클 텍스처 프롬프트

| 리소스 ID | 생성 도구 | 모델 / 워크플로 | Positive Prompt | Negative Prompt | 주요 파라미터 | 비고 |
|---------|---------|--------------|----------------|----------------|------------|------|
| VFX-TEX-001 | `[도구명]` | `[모델명]` | `[예: particle texture, [이펙트 종류: fire/smoke/spark/magic], seamless loop frame, black background, bright center, transparent edges, game asset, high quality, clean edges]` | `[예: characters, people, UI elements, blurry, low quality, jpeg artifacts, noisy, photorealistic, text, watermark, signature, logo]` | `[Steps: 30, CFG: 7.5, Sampler: DPM++ 2M Karras, Size: 256×256]` | 알파 채널 필수. 종류별 행 추가 |

---

### 5. 스타일 기준 프롬프트 (공통 토큰)

> 📌 모든 리소스에 공통으로 붙이는 스타일 토큰을 사전 정의합니다.
> E 문서 작성 시 아래 값을 확정하고 각 프롬프트에 일관되게 적용하세요.
>
> ⚠️ **중요**: 이 토큰들은 반드시 **ART-STY-001의 확정 값**에서 파생해야 합니다.
> ART-STY-001과 무관하게 독립적으로 스타일 토큰을 창작하는 것은 금지입니다.
> ART-STY-001이 미확정인 경우, 이 섹션 전체를 `[ART-STY-001 확정 후 작성]` 상태로 유지하세요.

| 구분 | 공통 Positive 토큰 | 공통 Negative 토큰 |
|------|-----------------|-----------------|
| 전체 공통 | `[예: game asset, [ART-STY-001 아트스타일 토큰], [ART-STY-001 아웃라인 토큰], [ART-STY-001 쉐이딩 방식 토큰]]` | `[예: realistic, photo, blurry, low quality, jpeg artifacts, noisy, watermark, text, signature, logo, extra limbs, deformed anatomy]` |
| 캐릭터 한정 | `[예: full body, dynamic pose, expressive face]` | `[예: cropped, out of frame, multiple characters]` |
| 배경 한정 | `[예: no characters, wide shot, atmospheric]` | `[예: characters, UI overlay, overexposed]` |

---

## 장르 확장 포인트 (ART)

> 아트 문서에서 장르·특성에 따라 추가 또는 변경이 필요한 항목:

| 장르 / 특성 | ART 추가 고려 항목 |
|-----------|-----------------|
| 2D 게임 | ART-CHR-001의 3D 폴리곤 예산 → 2D 스프라이트 아틀라스 기준(1024px / 2048px)으로 전면 대체. Packing Tag 규칙 추가 |
| 픽셀아트 | ART-STY-001에 픽셀 그리드 크기(16×16 / 32×32 / 64×64) 명시. 안티앨리어싱 금지 설정 추가 |
| 카툰 렌더링 | ART-STY-001에 Toon Shader 파라미터(Rim Light 강도, Outline 굵기 기준) 추가 |
| VFX 집중 (액션/슈팅) | ART-VFX-001 파티클 예산 상향 조정 + 히트스톱 연동 VFX 싱크 포인트 명시 |
| UI 중심 (퍼즐/전략) | ART-UI-001 비중 증가. 복잡한 HUD 설계, 그리드 UI 가이드라인 추가 |
| 콘솔 | ART-CHR-001 폴리곤 예산 PC/Console 기준 사용. 4K HDR 텍스처 지원 여부 명시 |
| 캐릭터 수집 (가챠) | ART-CHR-001에 등급별 비주얼 차별화 기준(실루엣, 색상, 이펙트 규모) 추가 |

---

## E 문서 작성 가이드 (ART)

> 이 파트의 E 문서(`[ProjCode]_Art.md`)를 작성할 때 아래 기준을 따르세요.

### 필수 포함 섹션

| 섹션 ID | 섹션명 | 완성 기준 |
|--------|--------|---------|
| ART-STY-001 | 스타일 가이드 | 컬러 팔레트 HEX 확정, 아웃라인 두께 수치 확정 |
| ART-CHR-001 | 캐릭터 아트 | 스프라이트 크기·아틀라스 규칙·등급별 차별화 기준 확정 |
| ART-ENV-001 | 환경 아트 | 배경 레이어 수·Parallax 속도·테마 목록 확정 |
| ART-UI-001 | UI 아트 | HUD 레이아웃, UI 팔레트, 폰트 크기 기준 확정 |
| ART-VFX-001 | 비주얼 이펙트 | 파티클 예산 수치, VFX 이벤트 싱크 포인트 정의 |
| ART-ANM-001 | 애니메이션 | 필수 애니메이션 상태 목록, fps 기준, 납품 규격 확정 |
| ART-OPT-001 | 에셋 최적화 체크리스트 | 전 항목 ☑ 처리 완료 |
| ART-RES-001 | 리소스 마스터 목록 | 모든 카테고리 테이블 행 채움. `[대괄호]` 없음 |
| ART-PRM-001 | 리소스 제작 프롬프트 | 모든 리소스 카테고리의 Positive/Negative Prompt 및 파라미터 확정. 공통 스타일 토큰 정의 완료 |

### 리소스-프롬프트 정합성 검증

> ⚠️ E 문서를 제출하기 전에 아래 4가지 정합성 검증을 반드시 수행해야 합니다.
> 검증을 통과하지 못한 문서는 "완성"으로 인정하지 않습니다.

| # | 검증 항목 | 방법 |
|---|----------|------|
| 1 | **리소스 ID 전수 대응** | ART-RES-001의 모든 리소스 ID 행이 ART-PRM-001에 동일 ID 행으로 존재하는지 확인. 빠진 ID는 프롬프트 행 추가 |
| 2 | **크기 파라미터 일치** | ART-PRM-001 각 행의 `Size` 파라미터 = ART-RES-001 해당 행의 `크기 (px)`. 단 한 행이라도 불일치 시 반려 |
| 3 | **스타일 토큰 일치** | ART-PRM-001 Positive Prompt 내 스타일 토큰 = ART-STY-001 확정 값과 완전 일치. `[토큰]` 플레이스홀더 잔존 시 미완성 |
| 4 | **공통 토큰 전체 적용** | §5에서 확정된 공통 스타일 토큰이 모든 프롬프트 행에 반영됐는지 전수 확인 |

### 최소 완성 기준 (Definition of Done)

- [ ] 모든 `[대괄호]` 플레이스홀더 제거됨
- [ ] `[TBD]` 항목에 이유 및 결정 예상 시점 명시
- [ ] **ART-RES-001 리소스 마스터 목록 — 모든 카테고리 행 채움**
- [ ] Sprite Atlas 배정 요약 표 — 전체 Atlas 열거 및 로드 시점 명시
- [ ] 크로스 파트 의존 항목 `[WARNING]` 태그 확인 (CL-OPT-001 DrawCall과 연동)
- [ ] **ART-PRM-001 리소스 제작 프롬프트 — 모든 카테고리 Positive/Negative Prompt 확정**
- [ ] ART-PRM-001 공통 스타일 토큰 정의 완료 (ART-STY-001 컬러 팔레트·스타일과 일치 확인)
- [ ] **ART-RES-001 전체 리소스 ID ↔ ART-PRM-001 행 1:1 대응 완료** (누락 ID 없음)
- [ ] **ART-PRM-001 Size 파라미터 = ART-RES-001 크기 값 전수 일치 확인** (불일치 행 없음)
- [ ] **ART-PRM-001 스타일 토큰 = ART-STY-001 확정값 일치 확인** (`[토큰]` 플레이스홀더 없음)

---

## 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0.0 | 2026-04-16 | [작성자] | 초기 템플릿 작성 |
| 1.1.0 | 2026-04-17 | [작성자] | 기반 문서 표시, 장르 확장 포인트 추가 |
| 1.2.0 | 2026-04-17 | [작성자] | ART-RES-001 리소스 마스터 목록 섹션 신설 |
| 1.3.0 | 2026-04-18 | [작성자] | ART-PRM-001 리소스 제작 프롬프트 섹션 신설; E 문서 작성 가이드에 프롬프트 필수 항목 추가 |
| 1.4.0 | 2026-04-18 | [작성자] | ART-PRM-001 프롬프트 필수 구성 요소 서브섹션 신설; 예시 프롬프트 탈장르화(스타일 토큰 일반화); ART-RES-001 파일명 변수 정의 테이블 추가; 서브테이블별 행 완성 기준 callout 추가; E 문서 가이드에 리소스-프롬프트 정합성 검증 서브섹션 및 DoD 항목 3개 추가 |
| 1.x.x | [날짜] | [작성자] | [변경 내용] |

---

*← 이전: [02_GameDesign.md](./02_GameDesign.md) | → 다음: [04_Client.md](./04_Client.md)*
