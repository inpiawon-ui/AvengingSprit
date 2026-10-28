"""잔혼 파티 시험판 — 템플릿에 게임 유닛 그림(data URI)을 넣어 한 파일로 묶는다."""
import base64, json, os, glob
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', '..'))
U = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
HOSTS = ['gangster','thug','amazon','commando_mg','commando_grenade','salamander','dragoon','dragon_blue','guru','medium','ninja_chain','baseball','snowwoman','robot']
MAP = {'ghost':'ghost','e_swarm':'bat','e_shield':'armadillo','e_turret':'watcher_eye','e_dasher':'boar','e_brute':'golem','e_skel':'skeleton','boss':'robot_snakes'}
for h in HOSTS: MAP['h_'+h] = h
def pick(folder):
    fs = sorted(glob.glob(os.path.join(U, folder, 'unit_%s_e.png' % folder))) or sorted(glob.glob(os.path.join(U, folder, '*_e.png'))) or sorted(glob.glob(os.path.join(U, folder, '*.png')))
    return fs[0]
spr = {}
for k, f in MAP.items():
    p = pick(f)
    spr[k] = 'data:image/png;base64,' + base64.b64encode(open(p, 'rb').read()).decode()
here = os.path.dirname(__file__)
t = open(os.path.join(here, 'template.html'), encoding='utf-8').read().replace('/*SPRITES*/{}', json.dumps(spr))
out = os.path.join(ROOT, 'Projects', 'AVSR', 'Docs', 'AVSR_잔혼파티_시험판_2026-10-28.html')
open(out, 'w', encoding='utf-8').write(t)
print(out, len(t))
