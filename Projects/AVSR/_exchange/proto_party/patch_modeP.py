"""잔혼 파티 시험판 — PD 정리안(2026-10-28)을 기본 모드로 넣는다. 한 번만 돌린다.

PD 「간단해 보여야 하고 버튼도 딱 봐서 어떤 건지 · 혼령은 쿨마다 스킬 자동 · 서브일 땐 능력치 30% ·
필살기 · 패시브는 메인 것만 · 조합 3개가 족보식으로 · 레벨업도 혼령 1 스킬 +1 · 액티브 스킬 개념도 없애도 돼」
"""
import os, sys
f = os.path.join(os.path.dirname(__file__), 'template.html')
t = open(f, encoding='utf-8').read()
if 'MODEL_P' in t:
    sys.exit('이미 넣었다')
R = []
def rep(a, b, n=1):
    assert t.count(a) >= 1, a[:90]
    R.append((a, b, n))

# 혼령 인프라(B)를 P 도 같이 쓴다
rep("if(S.mode==='B'){S.spirits.push(", "if(S.mode!=='A'){S.spirits.push(")
rep(" if(S.mode==='B'&&S.spirits.length){clampEnergy();", " if(S.mode!=='A'&&S.spirits.length){clampEnergy();")
rep(" if(S.mode==='B'&&old){openEnterOrHold(old,nb,e);return;}", " if(S.mode!=='A'&&old){openEnterOrHold(old,nb,e);return;}")
rep(" if(S.mode==='B'){drawPanelB();return;}", " if(S.mode==='P'){drawPanelP();return;}\n if(S.mode==='B'){drawPanelB();return;}")
rep("if(k==='1')(S.mode==='B'?", "if(k==='1')(S.mode!=='A'?")
rep("if(k==='2')(S.mode==='B'?", "if(k==='2')(S.mode!=='A'?")
rep("if(k==='3'&&S.mode==='B')", "if(k==='3'&&S.mode!=='A')")

# 판 상태
rep("mode:acct.mode||'A',", "mode:acct.mode||'P',skillLv:{},subMul:0.3,JB:null, // MODEL_P\n  ")
# 패시브 — P 는 메인 것만
rep("for(const s of S.spirits)if(s.k===k)m+=0.5;return m;}", "if(S.mode==='B')for(const s of S.spirits)if(s.k===k)m+=0.5;return m;}")
# 스킬 지속 시간은 세기로 줄이지 않는다(혼령 30% 라도 묶기 · 반사 시간은 그대로)
rep(" const tgt=nearestEnemy(caster,900);\n", " const tgt=nearestEnemy(caster,900),dp=Math.max(1,p);\n")
for a, b in [("e.mark=3*p", "e.mark=3*dp"), ("e.poison=4*p", "e.poison=4*dp"), ("S.guardT=3*p", "S.guardT=3*dp"),
             ("e.root=3*p", "e.root=3*dp"), ("S.reflectT=5*p", "S.reflectT=5*dp"), ("life:10*p", "life:10*dp"),
             ("j<Math.round(2*p)", "j<Math.max(1,Math.round(2*p))"), ("S.body.max*0.5*p", "S.body.max*0.5*Math.max(p,0.6)"),
             ("S.body.max*0.3*p", "S.body.max*0.3*Math.max(p,0.5)")]:
    rep(a, b)

# 필살기 — P 는 메인 것만(합세 없음)
rep(" if(S.mode==='B'){const n=S.spirits.length;castUlt(", " if(S.mode==='P'){castUlt(S.body.k,S.body,lvMul(S.body.k));return;}\n if(S.mode==='B'){const n=S.spirits.length;castUlt(")

# 족보 · 스킬 레벨
rep("function openEnterOrHold(old,nb,e){", """// ─────────────────────────────────────────
// 족보 — 파티 셋(메인 + 혼령 둘)의 조합으로 정해지는 능력치(가장 높은 족보 하나만)
// ─────────────────────────────────────────
const ORG={gangster:'갱단',thug:'갱단',commando_mg:'코만도 부대',commando_grenade:'코만도 부대',robot:'코만도 부대',amazon:'무도가',ninja_chain:'무도가',baseball:'무도가',salamander:'괴물',dragoon:'괴물',dragon_blue:'괴물',guru:'마도',medium:'마도',snowwoman:'마도'};
const JOKBO=[
 {id:'org3',n:'조직 셋',need:'같은 조직 3명',d:'모든 피해 +30% · 혼령 능력치 30% → 45%',dmg:1.3,sub:0.15,cd:1,def:1},
 {id:'br3',n:'갈래 트리플',need:'같은 갈래 3명',d:'모든 피해 +25% · 혼령 스킬 쿨 −25%',dmg:1.25,sub:0,cd:0.75,def:1},
 {id:'tri',n:'삼색',need:'파워 · 무기 · 마법 하나씩',d:'모든 피해 +15% · 받는 피해 −15%',dmg:1.15,sub:0,cd:1,def:0.85},
 {id:'mono',n:'단색',need:'같은 속성 3명',d:'모든 피해 +20%',dmg:1.2,sub:0,cd:1,def:1},
 {id:'pair',n:'원 페어',need:'같은 조직 · 갈래 · 속성 중 하나가 2명',d:'모든 피해 +8%',dmg:1.08,sub:0,cd:1,def:1},
];
function jokbo(){const ks=partyKeys();if(ks.length<2)return null;
 const cnt=f=>{const c={};ks.forEach(k=>{const v=f(k);c[v]=(c[v]||0)+1;});return Math.max(...Object.values(c));};
 const org=cnt(k=>ORG[k]),br=cnt(k=>HOSTS[k].br),at=cnt(k=>HOSTS[k].at),ats=new Set(ks.map(k=>HOSTS[k].at)).size;
 let j=null;if(ks.length>=3){if(org>=3)j=Object.assign({},JOKBO[0],{n:'조직 셋 — '+ORG[ks.find(k=>ks.filter(x=>ORG[x]===ORG[k]).length>=3)]});else if(br>=3)j=JOKBO[1];else if(ats===3)j=JOKBO[2];else if(at>=3)j=JOKBO[3];}
 if(!j&&(org>=2||br>=2||at>=2))j=JOKBO[4];return j;}
function lvMul(k){return 1+0.25*((S.skillLv[k]||1)-1);}
function lvCd(k){return 1-0.08*((S.skillLv[k]||1)-1);}
function openEnterOrHold(old,nb,e){""")
# 빙의 창 문구 — P 용
rep("'붙들기 — 혼령으로',sub:'지금 메인 '", "'붙들기 — 혼령으로',sub:(S.mode==='P'?'혼령은 스킬을 쿨마다 혼자 쓰고 평타를 쏜다(능력치 30%) · ':'')+'지금 메인 '")

# 혼령 — P: 쿨마다 자기 스킬 자동 + 평타(능력치 30%)
rep("  m.actCd=Math.max(0,m.actCd-dt);m.atkT=(m.atkT||0)-dt;if(m.atkT<=0&&S.body){if(basicAttack(m,0.3,'party'))m.atkT=RATE[HOSTS[m.k].kind]*1.2;}});",
    "  const sm=S.mode==='P'?S.subMul+(S.JB?S.JB.sub:0):0.3;\n  m.actCd=Math.max(0,m.actCd-dt);m.atkT=(m.atkT||0)-dt;if(m.atkT<=0&&S.body){if(basicAttack(m,sm,'party'))m.atkT=RATE[HOSTS[m.k].kind]*1.2;}\n  if(S.mode==='P'&&m.actCd<=0&&S.body&&nearestEnemy(m,800)){castActive(m.k,m,sm*lvMul(m.k)*2);m.actCd=HOSTS[m.k].act[1]*lvCd(m.k)*(S.JB?S.JB.cd:1);m.flashT=0.5;}\n  m.flashT=Math.max(0,(m.flashT||0)-dt);});")
# 족보 계산 · 적용
rep(" S.t+=dt;SYN=synergies();", " S.t+=dt;SYN=S.mode==='P'?[]:synergies();S.JB=S.mode==='P'?jokbo():null;")
rep(" if(syn('tri'))m*=1.1;", " if(syn('tri'))m*=1.1;if(S.JB)m*=S.JB.dmg;")
rep(" d*=m;if(S.shield>0)", " if(S.JB)d*=S.JB.def;d*=m;if(S.shield>0)")

# 레벨업 카드 — P 는 파티 카드
rep("function openLevelUp(){S.pendingLv--;", """function partyCards(){const c=[];
 const add=(m,lab)=>{const h=HOSTS[m.k],lv=S.skillLv[m.k]||1;if(lv<5)c.push({n:lab+' 「'+h.n+' · '+h.act[0]+'」 Lv '+lv+' → '+(lv+1),d:'혼령 스킬 위력 +25% · 쿨 −8%',fn:()=>{S.skillLv[m.k]=lv+1;}});};
 S.spirits.forEach((m,i)=>add(m,'혼령 '+(i+1)));
 if(S.body){const h=HOSTS[S.body.k],lv=S.skillLv[S.body.k]||1;if(lv<5)c.push({n:'메인 「'+h.n+'」 필살기 Lv '+lv+' → '+(lv+1),d:'필살기 「'+h.ult+'」 위력 +25%',fn:()=>{S.skillLv[S.body.k]=lv+1;}});}
 c.push({n:'혼령 능력치 +10%',d:'혼령 평타 · 스킬 '+Math.round(S.subMul*100)+'% → '+Math.round(S.subMul*100+10)+'%',fn:()=>{S.subMul+=0.1;}});
 return c;}
function openLevelUp(){if(S.mode==='P'){S.pendingLv--;const pc=partyCards().sort(()=>Math.random()-0.5),gc=CARDS.filter(c=>!['orbmax','orbfast','iframe','handpow'].includes(c.id)).sort(()=>Math.random()-0.5);
  const pool=[...pc.slice(0,2),...gc].slice(0,3);modal={title:'레벨 업 — 카드 한 장',lines:['Lv '+S.lv+' · 파티 카드는 금빛'],opts:pool.map(c=>({t:c.n,sub:c.d,fn:c.fn,gold:pc.includes(c)}))};return;}
 S.pendingLv--;""")
# 박사의 통신 — P
rep("function openDoctor(){if(S.mode==='B'){", """function openDoctor(){if(S.mode==='P'){modal={title:'박사의 통신',lines:['「자네의 에너지는 한정돼 있네. 하나만 고르게.」'],opts:[
 {t:'유령 에너지 회복',sub:'최대치까지 채운다',fn:()=>{S.energy=energyMax();}},
 {t:'혼령 칸 +1',sub:'이번 판 혼령이 '+(S.spiritCap+1)+'칸이 된다(족보는 앞 셋으로)',fn:()=>{S.spiritCap++;}},
 {t:'혼령 능력치 +15%',sub:'혼령 평타 · 스킬 '+Math.round(S.subMul*100)+'% → '+Math.round(S.subMul*100+15)+'%',fn:()=>{S.subMul+=0.15;}}]};return;}
 if(S.mode==='B'){""")

# 위 칸 — P 는 족보 띠
rep(" let sx=16;SYN.forEach(", " if(S.mode==='P'){const j=S.JB;rr(16,106,W-32,38,10,j?'#3a3016':'#1a1a24',j?'#f2c14e':'#333',2);txt(j?'족보 「'+j.n+'」 · '+j.d:'족보 없음 — 같은 조직 · 갈래 · 속성을 모으거나 파워 · 무기 · 마법을 하나씩',W/2,125,j?17:14,j?'#ffd56a':'#778','center');return;}\n let sx=16;SYN.forEach(")

# 아래 칸 — P (실제 적용할 배치 · 글자 버튼)
rep("function drawPanelB(){", """function drawPanelP(){
 const y=PANEL_Y+8;
 // 파티 셋 — 메인 · 혼령 1 · 혼령 2
 const card=(x,w,m,lab,main,i)=>{const h=m&&HOSTS[m.k];
  btn(x,y,w,150,'',()=>{if(!main&&m)swapSpirit(i);},{bg:main?'#2a2412':'#1e1e2a',stroke:m?(main?'#f2c14e':BRCOL[h.br]):'#333',lw:main?4:2,hold:(!main&&m)?()=>releaseSpirit(i):null});
  txt(lab,x+w/2,y+15,14,main?'#ffd56a':'#c9a8ff','center');
  if(!m){txt(main?'유령':'비어 있음',x+w/2,y+75,15,'#555','center');return;}
  spr(hostSpr(m.k),x+34,y+68,60,main?1:0.85);
  txt(h.n,x+66,y+44,15,'#fff');txt(ATTR[h.at]+' · '+BR[h.br].split(' ')[0]+(ORG[m.k]?' · '+ORG[m.k]:''),x+66,y+64,11,'#aab');
  if(main){txt('패시브 '+h.pas[0],x+66,y+86,12,'#73ff8c');txt('필살기 '+h.ult+' Lv'+(S.skillLv[m.k]||1),x+66,y+104,12,'#d9c2ff');bar(x+10,y+128,w-20,8,m.hp/m.max,'#73ff8c');txt('체력',x+w-12,y+142,10,'#889','right');}
  else{const cd=HOSTS[m.k].act[1]*lvCd(m.k)*(S.JB?S.JB.cd:1);txt(h.act[0]+' Lv'+(S.skillLv[m.k]||1),x+66,y+86,12,m.flashT>0?'#ffd56a':'#9fe7ff');txt('자동 · '+(m.actCd>0?m.actCd.toFixed(1)+'초':'준비'),x+66,y+104,12,'#889');bar(x+10,y+128,w-20,6,1-m.actCd/cd,'#9fe7ff');}};
 card(8,248,S.body,'메인 · 직접 조작',true,0);
 for(let i=0;i<S.spiritCap;i++)card(262+i*(S.spiritCap>2?150:226),S.spiritCap>2?144:220,S.spirits[i],'혼령 '+(i+1)+' · 자동',false,i);
 // 버튼 — 글자로
 const by=PANEL_Y+170,tg=possessTarget();
 btn(8,by,92,88,'',()=>openHelp(),{bg:'#1a1a24',stroke:'#444'});txt('규칙',54,by+44,18,'#ccd','center');
 btn(108,by,260,88,'',doPossess,{bg:tg?'#1d4a66':'#16161e',stroke:tg?'#5cc8ff':'#333',lw:tg?4:2});txt('빙의',238,by+30,24,tg?'#bff':'#556','center');
 txt(tg?HOSTS[tg.k].n+' 에게 — 들어가기 · 붙들기':(S.body?'경직된 호스트에 다가가면 켜진다':'가까운 호스트에 다가가면 켜진다'),238,by+62,13,tg?'#fff':'#556','center');
 const h=S.body&&HOSTS[S.body.k];
 btn(376,by,336,88,'',useUlt,{bg:S.ultReady?'#4a2a60':'#16161e',stroke:S.ultReady?'#c9a8ff':'#333',lw:S.ultReady?4:2});txt('필살기',544,by+30,24,S.ultReady?'#efe2ff':'#556','center');
 txt(h?(S.ultReady?h.ult+' — 지금 쓸 수 있다':'빙의하거나 혼령과 교체하면 찬다'):'몸이 없다',544,by+62,13,S.ultReady?'#fff':'#556','center');
 if(S.joy){cx.strokeStyle='rgba(255,255,255,.25)';cx.lineWidth=3;cx.beginPath();cx.arc(S.joy.ox,S.joy.oy,70,0,7);cx.stroke();cx.fillStyle='rgba(255,255,255,.25)';cx.beginPath();cx.arc(S.joy.ox+S.joy.dx*60,S.joy.oy+S.joy.dy*60,30,0,7);cx.fill();}
 txt('혼령 칸 누르기 = 메인과 교체(이 방 '+S.swapLeft+'번) · 길게 = 놓아주기 · 방 안을 끌어서 이동, 멈추면 자동 공격',W/2,H-10,12,'#667','center');
 toastList.slice(-3).forEach((t,i)=>{cx.globalAlpha=clamp(t.life,0,1)*0.9;rr(90,TOP+6+i*30,W-180,26,8,'rgba(10,10,20,.75)',null);txt(t.t,W/2,TOP+19+i*30,14,t.c,'center');cx.globalAlpha=1;});
}
function drawPanelB(){""")
# 혼령이 스킬을 쓸 때 머리 위에 반짝 — 그리기
rep("txt(String(i+1),m.x+22,m.y-26,16,'#ffd56a','center');});", "txt(String(i+1),m.x+22,m.y-26,16,'#ffd56a','center');if(m.flashT>0)txt(HOSTS[m.k].act[0],m.x,m.y-44,14,'#ffd56a','center');});")

# 규칙 — P
rep("function openHelp(){if((S?S.mode:acct.mode)==='B'){", """function openHelp(){if((S?S.mode:(acct.mode||'P'))==='P'){modal={title:'규칙 한 장 — 정리안',lines:[
 '· 메인 한 몸을 직접 조작한다. 멈추면 자동 공격. 메인이 주는 것: 평타 · 패시브 · 필살기(버튼). 스킬 버튼은 없다.',
 '· 혼령(빼앗았던 몸, 최대 2): 내 곁에 떠서 자기 스킬을 쿨마다 혼자 쓰고 평타를 쏜다. 능력치는 30%. 맞지 않는다.',
 '· 빙의: 경직(체력 35% 몰아치기)된 적 호스트에 다가가 「빙의」 — 들어가기(메인 교체, 지금 메인은 혼령으로) / 붙들기(새 몸을 혼령으로).',
 '· 족보: 메인 + 혼령 둘의 조합으로 능력치가 붙는다 — 조직 셋 > 갈래 트리플 > 삼색 > 단색 > 원 페어. 가장 높은 것 하나만.',
 '· 레벨업 카드에 파티 카드(금빛): 혼령 1 · 2 스킬 Lv+1 · 메인 필살기 Lv+1 · 혼령 능력치 +10%.',
 '· 혼령 칸을 누르면 그 몸과 메인 교체(방마다 2번, 필살기 다시 참) · 길게 누르면 놓아주기(혼불 폭발).',
 '· 혼령 하나마다 유령 에너지 최대 −15. 메인이 쓰러지면 −20, 혼령 하나를 골라 그 몸으로. 0 이면 패배.',
 '· 적 유형: 떼 · 방패(정면 탄 막음) · 탄막 · 날쌘(빨간 선 뒤 돌진) · 거대. 속성 파워 → 무기 → 마법 → 파워 ±30%.'],opts:[{t:'닫기',fn:()=>{}}]};return;}
 if((S?S.mode:acct.mode)==='B'){""")

# 로비 — 정리안이 기본, A · B 는 작게
rep(" const md=acct.mode||'A';", " const md=acct.mode||'P';\n btn(W/2-150,1080,300,96,'출전 — '+(md==='P'?'정리안':'모델 '+md),()=>newRun(),{bg:'#1d4a66',stroke:'#5cc8ff',lw:4,sz:26});\n [['P','정리안(PD 10-28)'],['A','이전 A'],['B','이전 B']].forEach(([k,l],i)=>{btn(40+i*220,1190,200,60,'',()=>{acct.mode=k;saveAcct();},{bg:md===k?'#3a3016':'#1a1a24',stroke:md===k?'#f2c14e':'#333',lw:md===k?3:2});txt(l,140+i*220,1220,16,md===k?'#ffd56a':'#889','center');});\n if(false){")
rep(" btn(W/2-150,1080,300,96,'출전 — 모델 '+md,()=>newRun(),{bg:'#1d4a66',stroke:'#5cc8ff',lw:4,sz:26});", " }")
rep(" btn(W/2-160,1190,150,60,'규칙',()=>openHelp(),{sz:20});\n btn(W/2+10,1190,150,60,'기록 지우기',", " btn(W-170,40,150,48,'기록 지우기',")
rep("txt('시험판 모델 '+S.mode+' · '", "txt((S.mode==='P'?'정리안':'모델 '+S.mode)+' · '")

for a, b, n in R:
    t = t.replace(a, b, 1)
open(f, 'w', encoding='utf-8').write(t)
print('ok', len(R))
