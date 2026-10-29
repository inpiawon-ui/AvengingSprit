# 빙의 파티 — 인게임 UI 시안 가이드 (PD 2026-10-29)

## PD 원문
- 「지금 스샷 보낸 것처럼 저런 식의 조합을 좀 보여 주면 좋겠고, 지금 시안의 UI 는 너무 별로야」
- 「우리 게임에 들어가 있는 저 UI 도 전체적으로 다 바뀌는 게 맞을 것 같아」
- 「레벨 표기는 저렇게 잘 보이게 두는 것 맞고, 나머지는 바뀐 시스템을 잘 표현할 수 있게」

## 참고 그림 (`Projects/AVSR/_exchange/ref/party_ui_1029/`)
| 파일 | 무엇 |
|---|---|
| `pd_ref_trait_tracker.png` | **목표 느낌** — 롤토체스식 시너지 추적. 세로로 선 육각 문장 + 이름 + 「모은 수 / 다음 단계 수」. 켜진 것은 금빛, 아직인 것은 어둡게 |
| `pd_ref_top_hud_now.png` | 지금 위쪽 HUD. **레벨 줄(Lv 2 + 경험치 막대)은 이 정도로 잘 보이게 남긴다.** 나머지는 새로 |
| `pd_ref_party_hud_now_bad.png` | 지금 임시 파티 판 — **반려.** 글자만 늘어놓아 아무것도 안 읽힌다 |
| `base_ingame_now.png` | 지금 전투 화면 전체(바탕) |

## 새 시스템 한 장 요약 (화면이 말해야 하는 것)
- 유령 HP 100 = 한 판의 목숨이자 **예산**. 빙의 · 교체 · 사망마다 깎인다(첫 빙의 −20 · 보조 넣기 −10 · 메인 교체 −25 · 보조 교체 −15 · 메인 사망 −30)
- 파티 = **메인 1**(직접 조작 · 체력 · 스킬 · 필살기 · 패시브) + **보조 2**(체력 없음 · 무적 · 6초마다 스킬 자동 · 보랏빛 혼)
- 적 호스트가 체력 60% 아래면 「빙의 가능」 — 직접 터치하면 잠깐 멈추고 선택창
- **시너지**: 속성 3(파워 · 무기 · 마법) + 조직 5(갱단 · 코만도 부대 · 무도가 · 괴물 · 마도) + 균형(속성 셋이 다 다름). 같은 태그 2명 = 약 · 3명 = 강
- 남은 교체 횟수: 메인 교체 챕터 2회 · 보조 교체 1회

## 화면 정보 순서 (위에서부터 중요한 순)
1. **유령 HP(예산)** — 가장 크게. 비용이 빠질 때 어디서 얼마가 빠졌는지 보일 자리
2. **레벨 줄** — 지금처럼 크게(Lv 숫자 + 경험치 막대)
3. **파티 M · 1 · 2** — 얼굴 · 역할(메인 왕관 / 보조 번호) · 메인 체력 · 보조 스킬 쿨 고리. 보조를 길게 누르면 메인과 자리 바꾸기
4. **시너지 추적** — 육각 문장 + 「2/3」. 켜진 것은 밝게, 하나만 더 모으면 켜지는 것은 강조
5. 스테이지 번호 · 일시정지 · 골드 — 작게
6. 아래: 이동 패드 · 스킬 · 필살기 버튼(지금 자리 유지)

## 그리는 법
- 화풍은 지금 게임(도트 픽셀 · RE:BORN 화면 틀 — 어두운 남색 판 · 청록 테두리)을 따른다. 새 장르처럼 매끈한 벡터로 가지 않는다
- 글자는 한국어. 크기 720 × 1280 세로, 위쪽 · 아래쪽 안전 여백
- 전투가 가장 잘 보여야 한다 — HUD 는 위 · 아래 띠와 왼쪽(또는 오른쪽) 가장자리 좁은 열만 쓴다. 필드 가운데를 판으로 덮지 않는다
- 시너지 문장은 태그마다 다른 모양(갱단 · 코만도 · 무도가 · 괴물 · 마도 · 파워 · 무기 · 마법 · 균형)
- 금지: 글자만 늘어놓은 판 · 필드 한가운데 큰 반투명 판 · 영어 글자

---

## 확정 (PD 2026-10-29)
- 정답 틀: `in/mock_partyui_S1b.png`(위쪽 한 줄 판 · 메인 칸만 크게 + 체력 · 보조 썸네일 · 빈 자리 「+ −10」 · 판 바로 밑 왼쪽 시너지 칸 「2/3」만)
- 같은 틀 다섯 장: `in/mock_partyui_F_states.png` · `F_possess.png` · `F_syndetail.png` · `F_choice.png` · `F_death.png`
- PD 보탬: 빙의 선택창은 **화면 가운데** · 시너지 설명 창은 **칸을 눌렀을 때만**(전투 잠깐 멈춤) · 시너지 칸은 **최대한 작게**(폭 약 56px, 문장 + 「2/3」)
- 시너지 칸은 왼쪽에 **세로로** 쌓는다(한 번에 2~3칸)

## 부품 표 — 발주는 이 덩어리에서 만든다 · 납품 검수는 이것과 하나씩 맞댄다
앵커 크롭: `ref/party_ui_1029/anchor/`

```
UI-PARTY-01  partyhud_panel        위쪽 판 — 어두운 남색 반투명 + 얇은 청록 테두리 + 리벳. 9분할로 늘려 쓸 수 있게(가로 늘림). 앵커 a_top_hud.png
UI-PARTY-02  partyhud_main_frame   메인 칸 테 — 금 테 + 작은 왕관(오른쪽 위). 칸 안은 비움(얼굴 · 체력은 코드가 넣음). 약 104×84
UI-PARTY-03  partyhud_sub_frame    보조 썸네일 테 — 보라 테 정사각. 약 56×56
UI-PARTY-04  partyhud_empty_frame  빈 자리 — 점선 테 + 「+」 그림(「−10」 글자는 코드). 약 56×56
UI-PARTY-05  partyhud_bar_bg / partyhud_bar_ghost / partyhud_bar_hp   막대 바탕 · 유령 HP 채움(청록) · 메인 체력 채움(초록) — 9분할 가로
UI-PARTY-06  syn_cell_on / syn_cell_off / syn_cell_preview   시너지 칸 바탕 — 켜짐(금 테) · 꺼짐(어두운 테) · 미리보기(흐린 점선). 약 56×64
UI-PARTY-07  syn_icon_force · blade · magic · gang · commando · martial · monster · mystic · balance   시너지 육각 문장 9종(파워 · 무기 · 마법 · 갱단 · 코만도 부대 · 무도가 · 괴물 · 마도 · 균형). 약 44×44
UI-PARTY-08  possess_bubble_syn / possess_bubble_none   빙의 가능 말풍선 — 금빛(시너지 들어감) · 회색(시너지 없음). 9분할 · 꼬리 아래
UI-PARTY-09  party_popup_panel     창 판(빙의 선택 · 사망 · 시너지 설명 공용) — 9분할
UI-PARTY-10  party_row / party_row_best / party_row_off   선택 줄 — 보통 · 추천(금빛 + 「추천」 꼬리표) · 못 고름(어둡게 + 자물쇠). 9분할
UI-PARTY-11  party_timer_ring      사망창 원형 시계 테(채움은 코드가 돌린다)
금지: 글자 박기(모든 글자는 코드) · 그림에 빛 번짐 굽기 · 외곽 그림자 판
바탕: 분홍(#FF00FF) — 받으면 뚫는다
상태: 발주
```

### 창 안 부품 — 2차(PD 2026-10-29 「시안을 컨펌했는데 너무 대충 적용」)
창 판 · 줄만 받고 창 **안의 부품**을 빠뜨려 선택 · 사망 · 시너지 설명 창이 글자 목록이 됐다. 시안 요소 하나하나를 부품으로 받는다.
앵커: `ref/party_ui_1029/anchor/p4_*.png` (시안 3배 크롭 · `Tools/partyui_popup2_anchor.py`)

시트 A — `in/parts_partyui_popup2.png` (선택창 · 사망창)
UI-PARTY-12  party_tag_gold / party_tag_red     이름 아래 태그 칩 — 금 테(상성) · 붉은 테(시너지 무리). 9분할 가로 · 글자는 코드. 앵커 p4_choice_tags
UI-PARTY-13  party_box_cyan / party_box_gray    정사각 얼굴 · 스킬 칸 — 청록 테 · 회색 테(못 고름). 약 64×64. 앵커 p4_choice_skillbox · p4_death_rows
UI-PARTY-14  party_hp_pill                      「남은 유령 HP 35」 어두운 남색 칸. 9분할 가로. 앵커 p4_choice_hppill
UI-PARTY-15  party_badge_best                   「추천」 금빛 둥근 딱지(글자는 코드). 앵커 p4_choice_badge
UI-PARTY-16  party_divider_v                    머리 칸 세로 구분선(얇은 청록). 앵커 p4_choice_header
UI-PARTY-17  party_death_splat                  「메인 사망」 뒤 피 튄 판(붉은 얼룩 · 글자 없음). 앵커 p4_death_title
UI-PARTY-18  party_chain                        못 고름 줄 오른쪽을 감는 사슬 장식(자물쇠 빼고). 앵커 p4_death_chain
UI-PARTY-19  party_best_hex                     사망창 추천 줄 왼쪽 육각 꼬리표(왕관 + 아래 「추천」 글자 자리 비움). 앵커 p4_death_rows

시트 B — `in/parts_partyui_popup3.png` (시너지 설명)
UI-PARTY-20  syn_pop_panel / syn_pop_tail       말풍선 판(9분할) + 왼쪽 위 꼬리(따로). 앵커 p4_syn_popover
UI-PARTY-21  syn_tier_on / syn_tier_off         단계 줄 — 켜짐(금 테 + 호박색 안) · 꺼짐(남색 + 회청 테). 9분할 가로. 앵커 p4_syn_tiers
UI-PARTY-22  syn_more_chip                      「1명 더」 회청 테 작은 칩. 앵커 p4_syn_tiers
UI-PARTY-23  syn_member_gold / syn_member_purple / syn_member_gray   무리 몸 칸 — 메인(금) · 보조(보라) · 아직 없음(회색). 약 72×72. 앵커 p4_syn_members
UI-PARTY-24  syn_member_badge                   칸 오른쪽 위 금빛 작은 딱지(「M」 · 「1」 글자는 코드). 앵커 p4_syn_members
UI-PARTY-25  party_divider_h                    아래 규칙 줄 위 가는 가로선. 앵커 p4_syn_popover

시트 C — `in/parts_partyui_popup4.png`
UI-PARTY-10b party_row_gold                   추천 금 테 줄에서 왕관 꼬리표를 뺀 것(선택창 추천 줄 · 사망창 추천 줄 + UI-PARTY-19). 앵커 p4_choice_best
UI-PARTY-26  party_ghost_mini                 사망창 비용 앞 작은 유령 얼굴. 앵커 p4_death_rows

시트 D — `in/parts_partyui_popup5.png` (빙의 가능 표시 — 정답 S1b)
UI-PARTY-27  possess_pill                   보라 알약 꼬리표 + 유령 얼굴(「빙의 가능」 글자는 코드). 앵커 p4_possess_bubble
UI-PARTY-28  possess_bubble_light           밝은 회백 말풍선 · 왼쪽 아래 꼬리(「데려오면 ▶ / 무리 2/3 · 효과」 글자는 코드). 앵커 p4_possess_bubble

시트 E — `in/parts_partyui_popup7.png`
UI-PARTY-10c party_row_locked              못 고름 줄 — 자물쇠 뺀 것(자물쇠는 시안처럼 사슬 왼쪽 끝에 따로). 앵커 p4_death_rows C 줄
UI-PARTY-29  party_lock                    작은 은색 자물쇠. 앵커 p4_death_rows
