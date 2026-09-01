// 48방 ← 레이아웃 배정   v3.0  (CH1 12 · CH2 16 · CH3 20 — 전 챕터)
//
// ⚠ v3.0 — 웨이브는 방마다 하나다.
//   증원 소환은 코드째 지웠다(PD 지시). 그래서 정본의 웨이브 2 는 서지 않는다.
//   v2.0 은 [w1, w2] 를 들고 있었지만 그러면 두 번째 숫자가 죽은 값이 되고,
//   무엇보다 **엘리트가 웨이브 2 에 있어서 통째로 사라진다** — 엘리트 방 넷이 엘리트 없는 방이 된다.
//   그래서 여기서 하나로 접었다:
//     · 적 수  = 정본 웨이브 1 의 수 (CH1 003 만 3 → 4 로 올렸다. 3기는 방이라기엔 가볍다)
//     · 엘리트 = 정본 두 웨이브의 엘리트 합 — 웨이브 2 에 있던 것을 앞으로 당긴다
//     · 조합   = 방마다 하나. 웨이브 대비가 사라졌으므로 **방과 방 사이**를 갈라 놓는다
//                (같은 조합이 연달아 나오지 않는다)
//
// · EVENT · REST · SHOP 은 폐지했다. 세 방 종류는 처음부터 다시 기획한다.
//   그때까지 COMBAT 으로 돌린다 — RoomType 은 이 표가 정본보다 우선한다.
// · enemies = 그 방에 서는 총 적 수(엘리트 포함). elite = 그중 엘리트 수.
// · 최대 8기. 자리는 10개이므로 조합이 고를 여지가 항상 2자리 이상 남는다.
//   자리를 다 쓰면 어느 조합을 넣어도 같은 방이 되므로, 남는 자리는 낭비가 아니라 여유다.
// · layout '아레나' 는 보스 전용. 레이아웃도 적 자리도 여기서 정하지 않는다.
// · 난이도 사다리:  A → B → E → J → D → C → K → G → H → L → I → F
//
// 무대(그림)는 방 번호가 정한다 — 레이아웃이 아니라.
//   CH1 001~006 연구소 · 007~012 쓰레기장
//   CH2 001~008 미사일기지 · 009~016 밤거리
//   CH3 001~010 옥상 · 011~020 정유소
const FR='FRONTLINE_PLUS_RANGED', FZ='FLANK_REINFORCEMENT_PLUS_ZONER', BH='BACKLINE_PRESSURE_PLUS_HUNTER';
const A='아레나';

module.exports={
CH1:[
 {no: 1,type:'TUTORIAL',layout:'A',enemies:4,elite:0,comp:FR, note:'대시로 엄폐를 건너뛰는 법을 배운다. 빼앗을 몸을 앞줄에도 세운 유일한 방'},
 {no: 2,type:'COMBAT',  layout:'B',enemies:5,elite:0,comp:FZ, note:'세 갈래 길. 측면에서 눌러 가운데로 몰아넣는다'},
 {no: 3,type:'COMBAT',  layout:'E',enemies:4,elite:0,comp:BH, note:'계단. 대시 세 번이면 끝까지 간다 — 배운 것을 바로 써먹는 자리'},
 {no: 4,type:'COMBAT',  layout:'J',enemies:5,elite:0,comp:FR, note:'문이 엇갈린 두 벽. 왼쪽으로 들어가 오른쪽으로 나간다 (구 EVENT)'},
 {no: 5,type:'COMBAT',  layout:'D',enemies:6,elite:0,comp:FZ, note:'십자 분단. 측면부터 나와 분면을 건너뛴다'},
 {no: 6,type:'BOSS',    layout:A,  enemies:4,elite:0,comp:FR, note:'보스 전용 아레나'},
 {no: 7,type:'COMBAT',  layout:'G',enemies:5,elite:0,comp:BH, note:'좁은 문. 쓰레기장 첫 방 — 문 뒤에서 버틴다 (구 REST)'},
 {no: 8,type:'COMBAT',  layout:'C',enemies:6,elite:0,comp:FR, note:'중앙 요새. 원거리가 뒤에 숨어 격투가 유리해진다'},
 {no: 9,type:'COMBAT',  layout:'K',enemies:6,elite:0,comp:FZ, note:'사선 분단. 대각이라 거리 감각이 어긋난다 (구 SHOP)'},
 {no:10,type:'COMBAT',  layout:'H',enemies:5,elite:0,comp:BH, note:'가시밭. 안전하게 돌까 아프게 지를까 — 시간이 자원이 된다'},
 {no:11,type:'ELITE',   layout:'L',enemies:6,elite:1,comp:FR, note:'네 귀퉁이 가시. 물러설 곳이 없다 — CH1 최난도'},
 {no:12,type:'BOSS',    layout:A,  enemies:5,elite:0,comp:FR, note:'챕터 보스 아레나'},
],
CH2:[
 {no: 1,type:'COMBAT',  layout:'B',enemies:5,elite:0,comp:FR, note:'미사일기지 첫 방. 익숙한 지형으로 무대만 바뀐 것을 알린다'},
 {no: 2,type:'COMBAT',  layout:'J',enemies:6,elite:0,comp:BH, note:'엇갈린 문. 뒤에서 밀어 두 문을 다 쓰게 만든다'},
 {no: 3,type:'COMBAT',  layout:'E',enemies:5,elite:0,comp:FZ, note:'계단. CH1 003 과 같은 지형이지만 측면부터 나온다 (구 EVENT)'},
 {no: 4,type:'ELITE',   layout:'C',enemies:6,elite:1,comp:BH, note:'중앙 요새. 엘리트가 요새 뒤에 선다 — 돌아 들어갈 수밖에 없다'},
 {no: 5,type:'COMBAT',  layout:'D',enemies:6,elite:0,comp:FR, note:'십자 분단 (구 SHOP)'},
 {no: 6,type:'COMBAT',  layout:'G',enemies:5,elite:0,comp:FZ, note:'좁은 문. 측면에서 문 옆구리를 친다'},
 {no: 7,type:'COMBAT',  layout:'K',enemies:6,elite:0,comp:BH, note:'사선 분단. 후방부터 나와 대각으로 밀어붙인다 (구 REST)'},
 {no: 8,type:'BOSS',    layout:A,  enemies:7,elite:0,comp:FR, note:'보스 전용 아레나'},
 {no: 9,type:'COMBAT',  layout:'I',enemies:6,elite:0,comp:FR, note:'회전 관문 첫 등장. 밤거리 첫 방 — 톱니를 여기서 처음 본다'},
 {no:10,type:'COMBAT',  layout:'D',enemies:6,elite:0,comp:FZ, note:'십자 분단 재등장. 톱니 방 다음의 숨돌리기 (구 EVENT)'},
 {no:11,type:'COMBAT',  layout:'H',enemies:7,elite:0,comp:BH, note:'가시밭. 7기가 서면 가시를 피할 자리가 모자란다'},
 {no:12,type:'ELITE',   layout:'F',enemies:7,elite:1,comp:FR, note:'모서리 요새 첫 등장. 가운데가 트여 사방에서 맞는다'},
 {no:13,type:'COMBAT',  layout:'G',enemies:7,elite:0,comp:FZ, note:'좁은 문. 7기가 문 앞에 뭉친다 (구 SHOP)'},
 {no:14,type:'COMBAT',  layout:'C',enemies:7,elite:0,comp:BH, note:'중앙 요새. 004 와 같은 지형에 적이 하나 더 붙는다'},
 {no:15,type:'COMBAT',  layout:'L',enemies:7,elite:0,comp:FR, note:'네 귀퉁이 가시. 보스 직전 — CH2 최난도 (구 REST)'},
 {no:16,type:'BOSS',    layout:A,  enemies:7,elite:0,comp:FR, note:'챕터 보스 아레나'},
],
CH3:[
 {no: 1,type:'COMBAT',  layout:'E',enemies:6,elite:0,comp:FR, note:'옥상 첫 방. 계단으로 시작해 무대가 바뀐 것만 알린다'},
 {no: 2,type:'ELITE',   layout:'H',enemies:7,elite:1,comp:BH, note:'가시밭 엘리트. 두 번째 방부터 엘리트가 선다'},
 {no: 3,type:'COMBAT',  layout:'J',enemies:6,elite:0,comp:FZ, note:'엇갈린 문 (구 EVENT)'},
 {no: 4,type:'COMBAT',  layout:'D',enemies:6,elite:0,comp:FR, note:'십자 분단'},
 {no: 5,type:'COMBAT',  layout:'C',enemies:7,elite:0,comp:BH, note:'중앙 요새. 요새 뒤가 두꺼워 사각이 좁아진다 (구 SHOP)'},
 {no: 6,type:'COMBAT',  layout:'K',enemies:6,elite:0,comp:FZ, note:'사선 분단'},
 {no: 7,type:'COMBAT',  layout:'G',enemies:6,elite:0,comp:FR, note:'좁은 문 (구 REST)'},
 {no: 8,type:'COMBAT',  layout:'L',enemies:8,elite:0,comp:FZ, note:'네 귀퉁이 가시. 8기 — 안전한 자리가 남지 않는다'},
 {no: 9,type:'ELITE',   layout:'I',enemies:7,elite:1,comp:BH, note:'회전 관문 엘리트. 톱니 뒤에 엘리트가 선다'},
 {no:10,type:'BOSS',    layout:A,  enemies:8,elite:0,comp:FR, note:'보스 전용 아레나'},
 {no:11,type:'COMBAT',  layout:'B',enemies:8,elite:0,comp:FR, note:'정유소 첫 방. 가장 익숙한 지형에 가장 많은 적'},
 {no:12,type:'COMBAT',  layout:'C',enemies:7,elite:0,comp:FZ, note:'중앙 요새 (구 EVENT)'},
 {no:13,type:'COMBAT',  layout:'F',enemies:7,elite:0,comp:BH, note:'모서리 요새. 트인 가운데에서 뒤로 밀린다'},
 {no:14,type:'COMBAT',  layout:'J',enemies:8,elite:0,comp:FR, note:'엇갈린 문. 8기가 두 문에 나뉘어 선다 (구 SHOP)'},
 {no:15,type:'ELITE',   layout:'L',enemies:8,elite:1,comp:BH, note:'네 귀퉁이 가시 엘리트'},
 {no:16,type:'COMBAT',  layout:'D',enemies:7,elite:0,comp:FZ, note:'십자 분단. 측면부터 나와 분면을 건너뛴다 (구 REST)'},
 {no:17,type:'COMBAT',  layout:'K',enemies:8,elite:0,comp:FR, note:'사선 분단. 8기가 대각으로 늘어선다'},
 {no:18,type:'COMBAT',  layout:'H',enemies:6,elite:0,comp:FZ, note:'가시밭. 짧은 방 — 다음 두 방을 위한 뜸'},
 {no:19,type:'ELITE',   layout:'I',enemies:8,elite:1,comp:BH, note:'회전 관문. 8기 + 엘리트 — 게임 전체 최대 밀도'},
 {no:20,type:'BOSS',    layout:A,  enemies:9,elite:0,comp:FR, note:'최종 보스 아레나'},
],
};
