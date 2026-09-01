// 컷씬 전체                                              v1.0 (2026-09-01)
//
// AVSR_Bosses.md §7 은 오프닝 둘(프롤로그·시작)만 담았다.
// 이 파일은 **원작에 있는 컷씬 전부**를 담는다 — 오프닝 2 + 스테이지 클리어 7.
//
// ⚠ 대사는 원작 영문 원문을 그대로 적었다. 한글은 우리 번역안이다.
// ⚠ 그림은 팬 립본(워터마크·게임보이 해상도)이라 **앵커로만 쓰고 새로 그린다.**
//    각 컷의 `draw` 가 그 발주 문구다.

// ── 오프닝 1 : 프롤로그 ────────────────────────────────
// 시트: Reference/Original/Miscellaneous - Prologue.png
const PROLOGUE = [
  { id:'P1', en:'One day I was walking with my girlfriend...',
    ko:'어느 날 나는 여자친구와 걷고 있었다',
    draw:'낮의 마을 거리. 남녀 둘이 나란히 걷는 상반신. 뒤로 벽돌 건물과 파란 하늘' },
  { id:'P2', en:'When suddenly I was attacked!',
    ko:'그런데 갑자기 습격당했다',
    draw:'같은 거리, 밤/어둡게. 뒤에서 코트 차림 괴한 셋이 다가온다. 둘의 뒤통수가 화면 앞에' },
  { id:'P3', en:'Ahhhhhhhhhh...',
    ko:'으아아아아악…',
    draw:'주인공 얼굴 클로즈업. 입을 벌리고 비명. 배경은 보라 단색 (공포 연출)' },
  { id:'P4', en:null,
    ko:'(대사 없음)',
    draw:'괴한이 여자친구를 안고 끌고 간다. 여자친구는 몸부림친다. 회색 어둠 배경' },
  { id:'P5', en:'Pang!',
    ko:'탕!',
    draw:'갱스터 상반신. 권총을 겨눈다 → 발포. **5프레임 애니메이션** (겨눔 / 화염1 / 화염2 / 반동 / 복귀). 배경 진파랑 단색',
    frames:5,
    note:'이 갱스터가 23명 호스트 중 하나이고 CH4 중간 보스 대장이다. 같은 얼굴로 그린다' },
  { id:'P6', en:'And then I died.',
    ko:'그리고 나는 죽었다',
    draw:'검은 화면 + 문구만' },
];

// ── 오프닝 2 : 시작 컷신 ───────────────────────────────
// 시트: Reference/Original/Miscellaneous - Start Cutscene.png
const START = [
  { id:'S1', en:'I called you here.',
    ko:'내가 자넬 불렀네',
    draw:'백발 노박사 상반신. 뒤로 파란 모니터가 늘어선 연구실 콘솔' },
  { id:'S2', en:'Those men were members of an evil secret society.',
    ko:'그자들은 악의 비밀결사였다',
    draw:'모니터 화면 가득 세계 지도. 아래에 콘솔 계기판' },
  { id:'S3', en:'They kidnapped my daughter and held her for ransom in order to get information about my research on ghost energy.',
    ko:'놈들은 내 유령 에너지 연구를 캐내려고 딸을 납치해 인질로 잡았네',
    draw:'묶여 있는 소녀. 어두운 회색 돌벽 배경. 프롤로그의 여자친구와 **같은 인물**이다 — 같은 얼굴·같은 머리색으로 그린다' },
  { id:'S4', en:'Your energy is limited.',
    ko:'자네의 에너지는 한정돼 있다',
    draw:'유리 원통관. 안에 작은 유령이 떠 있다. **유령 4프레임 애니메이션** + 관 액체 **파랑/초록 2종**',
    frames:4,
    note:'⚠ 이 관이 곧 우리 고스트 HP 게이지의 원형이다. **HUD 게이지를 이 관 모양으로 그리면 원작 연결이 산다.** 파랑=가득 · 초록=바닥' },
  { id:'S5', en:'But please help me! Save my daughter!',
    ko:'부탁하네! 내 딸을 구해줘!',
    draw:'S1 과 같은 구도. 박사가 몸을 앞으로 숙이고 표정이 절박하다 (표정만 다르게)' },
];

// ── 스테이지 클리어 컷신 ───────────────────────────────
// 시트: Reference/Original/Miscellaneous - Stage End Cutscenes.png
// ⚠ **이전 발주서(§8)에서 「이번엔 안 넣는다」로 뺐던 것. 다시 넣는다.**
//   6챕터를 원작 6스테이지로 잡은 근거가 바로 이 시트다. 이게 없으면 6챕터가 방 60개일 뿐이다.
const STAGE_END = [
  { st:1, id:'E1', en:'I discovered the map in the pile of garbage.',
    ko:'쓰레기 더미에서 지도를 발견했다.\n소녀가 갇힌 곳은 이 시설의 가장 깊은 곳이다',
    draw:'쓰레기 더미 위에 펼쳐진 낡은 지도. 옆에 부서진 기계 잔해',
    note:'**「열쇠 셋」은 뺐다 (2026-09-01 결정).** 우리 기획에 열쇠 수집이 없다. ' +
         '다만 그 문장이 「왜 계속 내려가나」를 설명하던 자리라 그냥 자르면 목표가 빈다. ' +
         '두 번째 줄을 위 문구로 갈아 끼워 목표만 남긴다' },
  { st:2, id:'E2', en:'It appears that the girl is not in this base.',
    ko:'이 기지엔 소녀가 없는 것 같다',
    draw:'미사일이 발사되어 화염을 뿜는다. 발사대 구조물' },
  { st:3, id:'E3', en:"It looks like I can't go into this base from the ground.",
    ko:'지상으로는 이 기지에 들어갈 수 없다',
    draw:'밤거리. 굳게 닫힌 셔터 앞에 경비 둘이 마주 서 있다. 위로 초승달' },
  { st:4, id:'E4', en:"It looks like there's a base above here.",
    ko:'위쪽에 기지가 있는 것 같다',
    draw:'시설 외벽을 올려다본 구도. 사다리와 배관이 위로 뻗는다',
    note:'다음 무대가 옥상인 이유. 그리고 옥상 보스가 **날아다니는 킹핀**인 이유' },
  { st:5, id:'E5', en:'I have a strong premonition that the girl is here.',
    ko:'소녀가 여기 있다는 강한 예감이 든다',
    draw:'묶여 있는 소녀. S3 와 같은 인물, 다른 방. 회색 실험실' },
  { st:5, id:'E6', en:'There\'s something written on the wall.\nIt\'s a code... "GHOST"---?',
    ko:'벽에 뭔가 쓰여 있다.\n암호인가… 「GHOST」…?',
    draw:'벽돌 벽에 보라색으로 갈겨쓴 「GHOST」 낙서',
    note:'**CH5 는 컷신이 두 장이다** (E5 → E6). 원작 그대로다' },
  { st:6, id:'E7', en:null,
    ko:'(대사 없음)',
    draw:'정유소 야경. 굴뚝에서 화염, 붉게 물든 하늘, 원통 탱크 실루엣',
    note:'**마지막 컷은 대사가 없다.** 그림만 보여주고 최종 보스로 넘어간다' },
];

const PLACEMENT = {
  opening: 'TitleScene → [신규] OpeningScene (프롤로그 → 시작 컷신) → LobbyScene. 첫 실행에만, 이후 건너뜀',
  stageEnd:'**챕터 010(최종 보스) 클리어 직후, 결과 화면 앞.** 챕터 전환이 곧 무대 전환이라 여기가 제 자리다',
  skip:    '전체 스킵 가능. 스킵해도 문구는 로비 「기록」에 남는다 (다시 보기)',
  ch5:     'CH5 만 컷이 둘이다 (E5 → E6). 나머지는 하나',
  ch6:     'CH6 은 대사가 없다. 그림 → 바로 최종 보스',
};

// 그려야 하는 장수
const ART_COUNT = {
  prologue:  { cuts:4, frames:5, blank:1, total:9,  note:'P1~P4 정지컷 4 + P5 발포 5프레임. P6 은 검은 화면이라 그림 없음' },
  start:     { cuts:5, frames:4, extra:1, total:10, note:'S1~S5 정지컷 5 + S4 유령 4프레임 + 관 초록판 1' },
  stageEnd:  { cuts:7, frames:0, total:7,  note:'E1~E7' },
  grand:     26,
};

// ⚠ 컷씬은 아니지만 **원작 고유 연출인데 한 번도 전달 안 한 것**
// 시트: Reference/Original/Miscellaneous - Death.png
const REAPER = {
  ko:'사신', en:'Death',
  rule:[
    '**4분 동안 아무것도 하지 않으면** 나타난다',
    '죽고 컨티뉴를 쓸 때까지 계속 쫓아온다',
    '접촉 피해는 작지만, **낫에 맞으면 빙의 중인 호스트 몸이 즉사한다**',
    '두 번 휘두르면 고스트 에너지가 바닥난다',
  ],
  sprites:'이동 4프레임 + 공격 2프레임 (낫 휘두름). 파랑 두건 · 분홍 옷 · 해골 얼굴',
  why:'원작의 방치 방지 장치인데, **우리 게임의 「몸을 잃는다」 공포와 정확히 같은 것을 건드린다.** ' +
      '자동 전투라 방치가 실제로 가능한 구조여서 원작보다 더 필요하다. ' +
      '4분은 우리 방 길이 기준으로 조정 (한 방 평균 40초 → 6방 무행동 = 4분)',
  decide:'**결정됨 (2026-09-01) — 방치 처벌이 아니라 캐릭터로 쓴다.**',
  asCharacter:{
    role:'24번째 호스트',
    job:'격투',
    skill:'처형 — 낫에 닿은 적이 일정 HP 이하면 즉사. 보스는 즉사 대신 큰 피해',
    appear:'CH6 009 엘리트. 다른 호스트와 같이 적으로 나오고, 이기면 빼앗는다',
    sprites:'이동 4 + 공격 2 = 6프레임. 호스트 1인분에 이미 맞는다',
    why:'원작 사신은 낫 한 방에 빙의한 몸을 죽이는 자다. 정체성을 버리지 않고 **방향만 뒤집는다** — ' +
        '이제 내가 그 짓을 한다. 죽음마저 빼앗는다는 것이 이 게임 마지막 몸으로 어울린다',
    whyNotAfk:'원래 용도(4분 방치 처벌)는 우리 게임과 싸운다. **자동 전투라 손을 떼는 것이 설계된 플레이인데** ' +
              '손 뗀 것을 벌하면 앞뒤가 안 맞는다. 캐릭터로 돌리는 것은 기능을 버리는 것이 아니라 모순을 없애는 것이다',
  },
};

// 에너지 관을 HUD 로 옮길 것인가 — **옮기지 않는다 (2026-09-01 결정)**
const HUD_LINK = {
  verdict:'관 모양 HUD 는 쓰지 않는다',
  why:'**원작 HUD 자체가 가로 막대다** ' +
      '(Reference/Original/Miscellaneous - HUD.png — ENERGY 주황 · BOSS 초록). ' +
      '관은 컷신 삽화일 뿐 HUD 가 아니다. 관으로 바꾸면 원작을 살리는 것이 아니라 원작에서 멀어진다. ' +
      '세로 관은 720×1280 에 방이 936px 이라 세로 여유도 없고, ' +
      '게이지는 곁눈질로 읽는 물건이라 길이 변화가 보이는 가로가 낫다',
  instead:'**관을 옮기지 말고 유령을 옮긴다.** 가로 막대 왼쪽 끝에 컷신의 유령을 작게 붙이고, ' +
          '에너지가 줄면 그 유령이 옅어진다',
  reuse:'cut_start_6~9 를 그대로 쓴다. 오프닝에서 관 속 유령이 옅어지는 것을 본 사람은 ' +
        'HUD 의 그 유령을 바로 읽는다. **원작 HUD 형태는 그대로 두고 연결만 가져온다. 추가 발주 0장.**',
};

module.exports = { PROLOGUE, START, STAGE_END, PLACEMENT, ART_COUNT, REAPER, HUD_LINK };
