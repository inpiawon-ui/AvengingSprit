"""잔혼 파티 시험판 — 모델 B 「영혼 지휘」를 넣는다(로비에서 A · B 를 바꿔 고른다). 한 번만 돌린다."""
import os, sys
f = os.path.join(os.path.dirname(__file__), 'template.html')
t = open(f, encoding='utf-8').read()
if 'MODEL_B' in t:
    sys.exit('이미 넣었다')
R = []
def rep(a, b):
    assert a in t, a[:80]
    R.append((a, b))

# 판 상태
rep("  ultReady:false,actCd:0,lastPossess:null};",
    "  ultReady:false,actCd:0,lastPossess:null,\n  // MODEL_B — 영혼 지휘: 메인 한 몸 + 혼령(안 맞음 · 펫 평타 · 내가 누르면 차례로 스킬)\n  mode:acct.mode||'A',spirits:[],spiritCap:hasRes('hand2')?3:2,swapLeft:2,chainGap:0.35,chainMul:1};")
rep(" if(acct.companion&&stars(acct.companion)>=3){S.hand.push(mkMember(acct.companion,W/2+60,950));toast('동행: '+HOSTS[acct.companion].n+' 이(가) 손 자리에','#b98cff');}",
    " if(acct.companion&&stars(acct.companion)>=3){if(S.mode==='B'){S.spirits.push(mkMember(acct.companion,W/2+60,950));toast('동행: '+HOSTS[acct.companion].n+' 이(가) 혼령으로','#b98cff');}else{S.hand.push(mkMember(acct.companion,W/2+60,950));toast('동행: '+HOSTS[acct.companion].n+' 이(가) 손 자리에','#b98cff');}}")
rep("if(S.spirit)m-=share('spirit');return m;}", "if(S.spirit)m-=share('spirit');m-=S.spirits.length*share('hand');return m;}")
rep(" S.hand.forEach((m,j)=>{m.x=W/2+(j?-70:70);m.y=BOT-60;});", " S.hand.forEach((m,j)=>{m.x=W/2+(j?-70:70);m.y=BOT-60;});S.spirits.forEach((m,j)=>{m.x=W/2+(j-1)*60;m.y=BOT-40;});S.swapLeft=2;")
rep("if(S.spirit)a.push(S.spirit.k);return a;}", "if(S.spirit)a.push(S.spirit.k);S.spirits.forEach(m=>a.push(m.k));return a;}")
rep("function passiveMul(k){let m=0;if(S.body&&S.body.k===k)m+=1;if(S.soul&&S.soul.k===k)m+=FIT[HOSTS[k].br]==='soul'?1+S.fitBonus:1;return m;}",
    "function passiveMul(k){let m=0;if(S.body&&S.body.k===k)m+=1;if(S.soul&&S.soul.k===k)m+=FIT[HOSTS[k].br]==='soul'?1+S.fitBonus:1;for(const s of S.spirits)if(s.k===k)m+=0.5;return m;}")

# 몸이 쓰러질 때 — B 는 혼령 하나를 골라 들어간다
rep(" S.body=null;\n if(S.hand.length){",
    " S.body=null;\n if(S.mode==='B'&&S.spirits.length){clampEnergy();if(scene!=='play')return;modal={title:'메인이 쓰러졌다 — 어느 혼령으로 들어갈까?',lines:['들어간 몸은 체력이 가득 찬다 · 그 혼령 칸은 빈다'],opts:S.spirits.map((m,i)=>({t:HOSTS[m.k].n+' · '+BR[HOSTS[m.k].br],sub:'액티브 「'+HOSTS[m.k].act[0]+'」 · 필살기 「'+HOSTS[m.k].ult+'」',fn:()=>{const s=S.spirits.splice(i,1)[0];s.hp=s.max;s.x=b.x;s.y=b.y;S.body=s;S.invuln=1.5;S.ultReady=true;S.fx.push({ty:'beam',x:b.x,y:b.y-200,x2:b.x,y2:b.y,life:0.4});clampEnergy();}}))};return;}\n if(S.hand.length){")

# 액티브 — B 는 메인 → 혼령 차례로
CHAIN = """// 순서 짝 — 앞 스킬 갈래 → 뒤 스킬 갈래가 맞으면 뒤 스킬 ×1.5
const PAIR={control:{trap:'모아 태우기',barrage:'모아 쓸기'},trap:{single:'얼려 깨기',rush:'덫 위 돌격'},single:{barrage:'표식 난사',summon:'표식 사냥'},guard:{barrage:'받아 쏘기'},summon:{rush:'미끼 돌격'},rush:{control:'몰이',trap:'몰아 넣기'},barrage:{single:'쓸고 마무리'}};
function useActive(){
 if(!S.body||S.body.actCd>0)return;
 const k=S.body.k,h=HOSTS[k];castActive(k,S.body,1);S.body.actCd=h.act[1];
 if(S.mode==='B'){ // 혼령이 차례로 이어서 — 각자 쿨, 안 찬 혼령은 건너뜀
  let prev=h.br,step=0;
  S.spirits.forEach(m=>{if(m.actCd>0)return;step++;const br=HOSTS[m.k].br,pair=PAIR[prev]&&PAIR[prev][br];prev=br;m.actCd=HOSTS[m.k].act[1];
   setTimeout(()=>{if(!S||!S.body)return;m.x=S.body.x+rnd(-40,40);m.y=S.body.y-30;castActive(m.k,m,0.6*(pair?1.5:1)*S.chainMul*(S.handMul||1));if(pair)fxText(S.body.x,S.body.y-96,'연계 「'+pair+'」','#ffd56a');},step*S.chainGap*1000);});
  return;}
 // 공명: 파티에 같은 갈래가 있으면 40% 로 한 번 더
 const same=[S.soul,...S.hand,S.spirit].filter(m=>m&&HOSTS[m.k].br===h.br).length;
 if(same>0)setTimeout(()=>{if(S&&S.body){castActive(k,S.body,0.4,true);fxText(S.body.x,S.body.y-76,'공명','#b98cff');}},450);
}"""
a0 = t.index("function useActive(){")
a1 = t.index("function castUlt(")
R.append((t[a0:a1], CHAIN + "\n"))

rep(" castUlt(S.body.k,S.body,1);\n if(S.spirit){",
    " if(S.mode==='B'){const n=S.spirits.length;castUlt(S.body.k,S.body,1+0.15*n);S.spirits.forEach((m,i)=>setTimeout(()=>{if(S&&S.body){m.x=S.body.x+rnd(-60,60);m.y=S.body.y-40;castActive(m.k,m,1,true);}},300+i*200));if(n)toast('혼령 '+n+' 합세 — 필살기 +'+(15*n)+'%','#b98cff');return;}\n castUlt(S.body.k,S.body,1);\n if(S.spirit){")

# 빙의 — B 는 「들어가기 / 붙들기」
rep(" S.body=nb;S.ultReady=true;S.invuln=1.25;S.body.actCd=0; // 빙의하면 액티브 · 필살기 가득",
    " if(S.mode==='B'&&old){openEnterOrHold(old,nb,e);return;}\n S.body=nb;S.ultReady=true;S.invuln=1.25;S.body.actCd=0; // 빙의하면 액티브 · 필살기 가득")
rep("function openSlotChooser(old){",
    """function addSpirit(m){let gone=null;if(S.spirits.length>=S.spiritCap){gone=S.spirits.shift();if(S.body){gone.x=S.body.x;gone.y=S.body.y;}releaseMember(gone);}m.actCd=0;S.spirits.push(mkMember(m.k,m.x,m.y,m));clampEnergy();}
function openEnterOrHold(old,nb,e){
 const full=S.spirits.length>=S.spiritCap,out=full?'  ⟶ 칸이 차서 '+HOSTS[S.spirits[0].k].n+' 을(를) 놓아준다(혼불)':'';
 modal={title:HOSTS[nb.k].n+(nb.elite?'(간부 몸)':'')+' — 들어갈까, 붙들까?',lines:['메인 '+HOSTS[old.k].n+' · 혼령 '+S.spirits.length+' / '+S.spiritCap+' · 혼령 하나마다 유령 에너지 최대 −'+share('hand')],opts:[
  {t:'들어가기 — 메인 교체',sub:HOSTS[nb.k].n+' 을(를) 직접 조작 · 지금 메인 '+HOSTS[old.k].n+' 은(는) 혼령이 된다 · 액티브 · 필살기 가득'+out,gold:true,fn:()=>{S.body=nb;S.ultReady=true;S.invuln=1.25;nb.actCd=0;addSpirit(old);toast(HOSTS[nb.k].n+' 에 들어갔다 · '+HOSTS[old.k].n+' 은(는) 혼령으로','#5cc8ff');}},
  {t:'붙들기 — 혼령으로',sub:'지금 메인 '+HOSTS[old.k].n+' 그대로 · '+HOSTS[nb.k].n+' 은(는) 혼령으로 곁에'+out,fn:()=>{S.body=old;addSpirit(nb);toast(HOSTS[nb.k].n+' 을(를) 혼령으로 붙들었다','#b98cff');}},
  {t:'놓아주기',sub:'몸을 빼앗자마자 놓아준다 — 혼불 폭발 · 에너지 몫 없음',fn:()=>{S.body=old;releaseFx(nb);}}]};
}
function swapSpirit(i){
 if(!S.body||S.swapLeft<=0||S.swapCd>0)return;const m=S.spirits[i];if(!m)return;
 const old=S.body;S.spirits[i]=mkMember(old.k,old.x,old.y,old);m.x=old.x;m.y=old.y;S.body=m;S.swapLeft--;S.swapCd=1.5;
 if(hasRes('iframe')||S.cards.iframe)S.invuln=Math.max(S.invuln,1);
 if(!S.ultRoomUsed[m.k])S.ultReady=true;
 S.fx.push({ty:'beam',x:old.x,y:old.y-60,x2:m.x,y2:m.y,life:0.3});fxText(m.x,m.y-56,'메인 교체','#f2c14e');
}
function releaseSpirit(i){const m=S.spirits.splice(i,1)[0];if(!m)return;const p=S.body||S.ghost;m.x=p.x;m.y=p.y-40;releaseMember(m);}
function openSlotChooser(old){""")

# 박사의 통신 — B 용
rep("function openDoctor(){modal={",
    "function openDoctor(){if(S.mode==='B'){modal={title:'박사의 통신',lines:['「자네의 에너지는 한정돼 있네. 하나만 고르게.」'],opts:[\n {t:'유령 에너지 회복',sub:'최대치까지 채운다',fn:()=>{S.energy=energyMax();}},\n {t:'혼령 칸 +1',sub:'이번 판 혼령이 '+(S.spiritCap+1)+'칸이 된다',fn:()=>{S.spiritCap++;}},\n {t:'연계 강화',sub:'혼령 스킬 위력 +30% · 이어지는 간격 −0.1초',fn:()=>{S.chainMul*=1.3;S.chainGap=Math.max(0.15,S.chainGap-0.1);}}]};return;}modal={")

# 혼령 — 따라다니며 펫 평타(30%) · 액티브 쿨은 따로 돈다
rep(" // 손 자리 잔혼 — 따라다니며 약한 평타 · 쿨마다 액티브",
    " S.spirits.forEach((m,i)=>{const n=S.spirits.length,ang=-Math.PI/2+(i-(n-1)/2)*0.9;const tx=me.x+Math.cos(ang)*70,ty=me.y+Math.sin(ang)*55;m.x+=(tx-m.x)*Math.min(1,dt*5);m.y+=(ty-m.y)*Math.min(1,dt*5);\n  m.actCd=Math.max(0,m.actCd-dt);m.atkT=(m.atkT||0)-dt;if(m.atkT<=0&&S.body){if(basicAttack(m,0.3,'party'))m.atkT=RATE[HOSTS[m.k].kind]*1.2;}});\n // 손 자리 잔혼 — 따라다니며 약한 평타 · 쿨마다 액티브")

# A: 손 자리 몸도 적 탄에 맞는다(쓰러지면 나눠 둔 몫을 잃는다)
rep("  if(S.body&&dist(s,S.body)<26){s.life=0;hurtMe(s.dmg,s);}}",
    "  if(S.body&&dist(s,S.body)<26){s.life=0;hurtMe(s.dmg,s);continue;}\n  for(let hi=0;hi<S.hand.length;hi++){const m=S.hand[hi];if(dist(s,m)<22){s.life=0;m.hp-=s.dmg;if(m.hp<=0){S.hand.splice(hi,1);S.lost+=share('hand');toast(HOSTS[m.k].n+' 잔혼이 쓰러졌다 — 몫 '+share('hand')+' 을(를) 잃음','#ff5c4d');S.fx.push({ty:'ring',x:m.x,y:m.y,r:60,c:'#ff5c4d',life:0.4});clampEnergy();}break;}}}")

# 그리기 — 혼령
rep(" // 손 자리 잔혼\n",
    " // 혼령(B) — 작고 반투명, 맞지 않는다\n S.spirits.forEach((m,i)=>{cx.save();cx.shadowColor='#b98cff';cx.shadowBlur=18;spr(hostSpr(m.k),m.x,m.y+Math.sin(S.t*4+i)*5,62,0.5,Math.cos(m.face||0)<0);cx.restore();txt(String(i+1),m.x+22,m.y-26,16,'#ffd56a','center');});\n // 손 자리 잔혼\n")
rep("if(S.lost)seg(S.lost,'#552','잃음');", "S.spirits.forEach(()=>seg(share('hand'),'#c9a8ff','혼령'));if(S.lost)seg(S.lost,'#552','잃음');")

# 아래 칸 — B
rep("function drawPanel(){\n cx.fillStyle='#14141d';cx.fillRect(0,PANEL_Y,W,H-PANEL_Y);",
    """function drawPanel(){
 cx.fillStyle='#14141d';cx.fillRect(0,PANEL_Y,W,H-PANEL_Y);
 if(S.mode==='B'){drawPanelB();return;}""")
rep("function openHelp(){",
    """function drawPanelB(){
 let x=10;const y=PANEL_Y+10;
 txt('혼령(순서 = 연계 순서) · 칸 누르기 = 메인 교체(이 방 '+S.swapLeft+'번 남음) · 길게 = 놓아주기',14,PANEL_Y+170,14,'#998');
 for(let i=0;i<S.spiritCap;i++){const m=S.spirits[i],w=150;
  btn(x,y,w,140,'',()=>{if(m)swapSpirit(i);},{bg:'#1e1e2a',stroke:m?BRCOL[HOSTS[m.k].br]:'#333',lw:2,hold:m?()=>releaseSpirit(i):null});
  txt('혼령 '+(i+1),x+w/2,y+14,14,'#c9a8ff','center');
  if(m){const h=HOSTS[m.k];spr(hostSpr(m.k),x+w/2,y+62,64,0.85);txt(h.n,x+w/2,y+104,15,'#fff','center');txt(m.actCd>0?h.act[0]+' '+m.actCd.toFixed(1):h.act[0]+' ●',x+w/2,y+124,12,m.actCd>0?'#778':'#ffd56a','center');}
  else txt('비어 있음',x+w/2,y+70,14,'#555','center');x+=w+8;}
 const bx=W-250,by=PANEL_Y+184;
 if(S.body){const h=HOSTS[S.body.k];const cd=S.body.actCd;
  btn(bx,by,120,76,'',useActive,{bg:cd>0?'#222':'#24405a',stroke:BRCOL[h.br]});txt('액티브',bx+60,by+20,15,'#9cf','center');txt(cd>0?cd.toFixed(1):h.act[0],bx+60,by+44,cd>0?24:15,'#fff','center');if(S.spirits.length)txt('+ 혼령 '+S.spirits.filter(m=>m.actCd<=0).length+' 이어서',bx+60,by+64,11,'#ffd56a','center');
  btn(bx+126,by,120,76,'',useUlt,{bg:S.ultReady?'#4a2a60':'#222',stroke:S.ultReady?'#c9a8ff':'#444'});txt('필살기',bx+186,by+20,15,'#d9c2ff','center');txt(S.ultReady?h.ult:'빙의 · 교체로 참',bx+186,by+46,S.ultReady?15:12,'#fff','center');if(S.spirits.length&&S.ultReady)txt('혼령 합세 +'+15*S.spirits.length+'%',bx+186,by+64,11,'#ffd56a','center');}
 const tg=possessTarget();
 btn(bx-130,by,124,76,'',doPossess,{bg:tg?'#1d4a66':'#1a1a22',stroke:tg?'#5cc8ff':'#333',lw:tg?4:2});txt('빙의',bx-68,by+24,20,tg?'#bff':'#556','center');txt(tg?HOSTS[tg.k].n:(S.body?'경직된 호스트 필요':'가까운 호스트'),bx-68,by+52,13,tg?'#fff':'#556','center');
 if(S.joy){cx.strokeStyle='rgba(255,255,255,.25)';cx.lineWidth=3;cx.beginPath();cx.arc(S.joy.ox,S.joy.oy,70,0,7);cx.stroke();cx.fillStyle='rgba(255,255,255,.25)';cx.beginPath();cx.arc(S.joy.ox+S.joy.dx*60,S.joy.oy+S.joy.dy*60,30,0,7);cx.fill();}
 txt('모델 B 영혼 지휘 · 방 안 끌어서 이동 · 멈추면 자동 공격',16,H-24,14,'#667');
 btn(16,by+8,90,56,'규칙',()=>openHelp(),{sz:18});
 toastList.slice(-3).forEach((t,i)=>{cx.globalAlpha=clamp(t.life,0,1)*0.9;rr(90,TOP+6+i*30,W-180,26,8,'rgba(10,10,20,.75)',null);txt(t.t,W/2,TOP+19+i*30,14,t.c,'center');cx.globalAlpha=1;});
}
function openHelp(){if((S?S.mode:acct.mode)==='B'){modal={title:'규칙 한 장 — 모델 B 영혼 지휘',lines:[
 '· 나는 유령. 적 호스트를 경직(체력 35% 몰아치기)시켜 「빙의」로 빼앗는다. 유령일 땐 가까운 호스트에 바로 빙의.',
 '· 빼앗을 때 고른다 — 「들어가기」: 새 몸을 직접 조작, 지금 메인은 혼령이 된다 / 「붙들기」: 메인 그대로, 새 몸은 혼령.',
 '· 체력은 메인만. 혼령은 맞지 않고 내 곁에 떠서 펫처럼 평타(30%)만 쏜다.',
 '· 액티브를 누르면 메인 스킬 → 혼령 1 → 혼령 2 스킬이 0.35초 간격으로 이어진다(각자 쿨 · 안 찬 혼령은 건너뜀). 순서가 짝이면(묶기 → 장판 등) 뒤 스킬 ×1.5.',
 '· 패시브는 메인 전부 + 혼령마다 절반. 필살기는 메인 것, 혼령 하나당 +15% 합세.',
 '· 혼령 칸을 누르면 그 몸으로 메인 교체(방마다 2번) · 길게 누르면 놓아주기(혼불 폭발).',
 '· 혼령 하나마다 유령 에너지 최대 −15. 메인이 쓰러지면 −20, 혼령 하나를 골라 그 몸으로(체력 가득). 0 이면 패배.',
 '· 적 유형 · 속성 · 문 위 예고는 모델 A 와 같다.'],opts:[{t:'닫기',fn:()=>{}}]};return;}""")

# 로비 — 모델 고르기
rep(" btn(W/2-160,1080,320,96,'출전 — 유령으로',()=>newRun(),{bg:'#1d4a66',stroke:'#5cc8ff',lw:4,sz:28});",
    """ const md=acct.mode||'A';
 btn(40,1080,150,96,'',()=>{acct.mode='A';saveAcct();},{bg:md==='A'?'#3a3016':'#1a1a24',stroke:md==='A'?'#f2c14e':'#333',lw:md==='A'?4:2});txt('모델 A',115,1108,20,md==='A'?'#ffd56a':'#888','center');txt('한 몸 직접',115,1136,14,'#aab','center');txt('옮겨 타기 · 자리 셋',115,1156,12,'#889','center');
 btn(W-190,1080,150,96,'',()=>{acct.mode='B';saveAcct();},{bg:md==='B'?'#3a3016':'#1a1a24',stroke:md==='B'?'#f2c14e':'#333',lw:md==='B'?4:2});txt('모델 B',W-115,1108,20,md==='B'?'#ffd56a':'#888','center');txt('영혼 지휘',W-115,1136,14,'#aab','center');txt('메인 + 혼령 연계',W-115,1156,12,'#889','center');
 btn(W/2-150,1080,300,96,'출전 — 모델 '+md,()=>newRun(),{bg:'#1d4a66',stroke:'#5cc8ff',lw:4,sz:26});""")
rep("txt('잔혼 파티 시험판 · '+(S.room+1)", "txt('시험판 모델 '+S.mode+' · '+(S.room+1)")
rep("if(k==='1')swapInto(0);if(k==='2')swapInto(1);", "if(k==='1')(S.mode==='B'?swapSpirit(0):swapInto(0));if(k==='2')(S.mode==='B'?swapSpirit(1):swapInto(1));if(k==='3'&&S.mode==='B')swapSpirit(2);")
rep("window.__T={step:(dt)=>{update(dt);},", "window.__T={swapSpirit:i=>swapSpirit(i),setMode:m=>{acct.mode=m;},step:(dt)=>{update(dt);},")

for a, b in R:
    t = t.replace(a, b, 1)
open(f, 'w', encoding='utf-8').write(t)
print('ok', len(R))
