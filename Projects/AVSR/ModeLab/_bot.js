// 시험용 자동 조종 — 시험판 페이지에 주입해서 쓴다(게임 파일에는 안 들어간다)
window.step = s => { for (let i = 0; i < s * 60; i++) { if (!G || !G.running) break; update(1 / 60); } };
window.goTo = function (x, y) { const me = G.body || { x: G.gx, y: G.gy }; const dx = x - me.x, dy = y - me.y, l = Math.hypot(dx, dy); if (l < 8) { joy = null; return true; } joy = { id: -1, sx: 0, sy: 0, x: dx / l * 40, y: dy / l * 40 }; return false; };
window.bot = function (key, opt = {}, maxT = 300) {
  intro(key);
  if (opt.aff) document.getElementById('optAff').value = opt.aff;
  if (opt.boss) document.getElementById('optBoss').value = opt.boss;
  if (opt.host) document.getElementById('optHost').value = opt.host;
  if (opt.day != null && document.getElementById('optDay')) document.getElementById('optDay').value = String(opt.day);
  startMode(key);
  let t = 0;
  while (G.running && t < maxT) {
    if (G.choosing) { const c = document.getElementById('card0'); if (c) c.click(); continue; }
    const door = G.doors.find(d => d.open);
    if (door) { goTo(door.x, FT + 30); step(.1); t += .1; continue; }
    if (G.channel > 0) { joy = null; step(.1); t += .1; continue; }
    const main = MODES.raid && G.M === MODES.raid ? DEF[G.S.boss].aff : domAff();
    if (!G.body) {
      const cands = G.enemies.filter(e => e.possessable && (!G.M.canPossess || G.M.canPossess(G, e)));
      cands.sort((a, b) => (BEATS[b.d.aff] === main) * 2 + isTrained(b.key) - ((BEATS[a.d.aff] === main) * 2 + isTrained(a.key)));
      const m = cands[0];
      if (m) { const at = goTo(m.x, m.y + 20); if (G.target === m || (at && G.target)) { joy = null; pressAct(); } } else joy = null;
      step(.1); t += .1; continue;
    }
    const b = G.body;
    if (!G.M.noLeave && affMul(b.d.aff, main) < 1 && G.enemies.some(e => e.possessable && BEATS[e.d.aff] === main)) { joy = null; pressAct(); step(.1); t += .1; continue; }
    if (b.trained && G.skillG >= 1) pressSkill();
    const tele = G.enemies.map(e => e.tele).find(T => T && Math.hypot(b.x - T.x, b.y - T.y) < T.r + 10);
    if (tele) { const dx = b.x - tele.x, dy = b.y - tele.y, l = Math.hypot(dx, dy) || 1; goTo(b.x + dx / l * 80, b.y + dy / l * 80); step(.1); t += .1; continue; }
    // 빠졌다 쏘기 — 가까이 붙은 적이 있으면 반대로 빠진다(원거리·중거리일수록 멀리)
    if (window.kite) {
      const keep = melee(b.d) ? 0 : 55;
      const near = G.enemies.filter(e => !e.d.boss && !e.elite && !e.possessable && Math.hypot(e.x - b.x, e.y - b.y) < keep);
      if (near.length) { let dx = 0, dy = 0; for (const e of near) { dx += b.x - e.x; dy += b.y - e.y; } const l = Math.hypot(dx, dy) || 1; goTo(clamp(b.x + dx / l * 70, FL + 20, FR - 20), clamp(b.y + dy / l * 70, FT + 30, FB - 20)); step(.1); t += .1; continue; }
    }
    let tgt = null, bd = 1e9;
    for (const e of G.enemies) { const big = e.d.boss || e.elite; if (MODES.raid && G.M === MODES.raid && !big && e.possessable) continue; const d = Math.hypot(e.x - b.x, e.y - b.y) - (big ? 40 : 0); if (d < bd) { bd = d; tgt = e; } }
    if (!tgt) { joy = null; step(.1); t += .1; continue; }
    if (bd > b.d.range * .8) { const big = tgt.d.boss || tgt.elite; goTo(tgt.x, tgt.y + (big ? 30 : 0) + Math.min(b.d.range * .6, 120)); }
    else joy = null;
    step(.1); t += .1;
  }
  joy = null;
  return `${key}${opt.aff ? '(' + opt.aff + ')' : ''}${opt.host ? '(' + opt.host + ')' : ''}${opt.boss ? '(' + opt.boss + ')' : ''}${opt.day != null ? '(요일' + opt.day + ')' : ''}: ${G.running ? 'TIMEOUT' : ov.innerText.split('\n').slice(0, 3).join(' / ')} | t=${G.t.toFixed(0)} 빙의${G.possess} 잃음${G.lost} 스킬${G.skills}`;
};
window.cmp = function (runs) {
  const out = [];
  for (const [label, set] of [['전부 키움', new Set(HOSTS)], ['안 키움', new Set()]]) {
    trained = set; out.push('— ' + label);
    for (const [k, o] of runs) out.push(bot(k, o));
  }
  trained = new Set(['gangster', 'amazon', 'robot']);
  return out.join('\n');
};
'bot ready';
