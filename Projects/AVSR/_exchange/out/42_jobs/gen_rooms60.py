# AVSR_Rooms60.js  ->  Assets/Scripts/Editor/RoomDef60.cs
#
# 보스 파이프라인(gen_boss.py -> BossDefTable.cs -> BossImporter)과 같은 방식이다.
# 정본 문장을 // 출처 주석으로 함께 남겨, 표만 보고도 어디서 온 값인지 알 수 있게 한다.
import io, json, os, re

ROOT = r"C:\won\UnityProject\AvengingSprit"
SRC = os.path.join(ROOT, r"Projects\AVSR\_exchange\out\42_jobs\AVSR_Rooms60.js")
DST = os.path.join(ROOT, r"Assets\Scripts\Editor\RoomDef60.cs")


def block(text, name):
    i = text.index("const %s" % name)
    j = text.index("=", i) + 1
    # 균형 잡힌 괄호까지 읽는다
    depth = 0
    k = j
    while text[k] in " \n\r\t":
        k += 1
    start = k
    opener = text[k]
    closer = {"{": "}", "[": "]"}[opener]
    while k < len(text):
        if text[k] == opener:
            depth += 1
        elif text[k] == closer:
            depth -= 1
            if depth == 0:
                break
        k += 1
    return json.loads(text[start:k + 1])


src = io.open(SRC, encoding="utf-8").read()
src = re.sub(r"^\s*//.*$", "", src, flags=re.M)      # 주석 제거
rooms = block(src, "ROOMS")
debut = block(src, "DEBUT")
trash = block(src, "TRASH_POOL")
hosts_in = block(src, "HOSTS_IN_ROOM")
elite = block(src, "ELITE_RULE")

assert len(rooms) == 60, len(rooms)


def cs(s):
    return '"%s"' % (s or "").replace('"', '\\"')


out = []
w = out.append
w("// ⚠ 이 파일은 손으로 고치지 않는다. 아래 도구가 굽는다.")
w("//   원본  Projects/AVSR/_exchange/out/42_jobs/AVSR_Rooms60.js")
w("//   생성  scratchpad/gen_rooms60.py")
w("//")
w("// 고칠 것이 있으면 **원본을 고치고 다시 굽는다.** 여기를 고치면 다음 납품에")
w("// 조용히 덮어써진다 — 어느 쪽이 정본인지 알 수 없게 되는 것이 가장 나쁘다.")
w("")
w("namespace Game.EditorTools")
w("{")
w("    /// <summary>")
w("    /// 6챕터 × 10방 = 60방 배정 (정본 v1.0 · 2026-09-01).")
w("    ///")
w("    /// 정본이 새로 만든 것은 **배정뿐**이다 — 레이아웃 12종 · 자리 10개 ·")
w("    /// 잡몹 7종 · 호스트 23명은 이미 있던 것을 그대로 쓴다.")
w("    /// 새 잡몹 0종 · 새 레이아웃 0종.")
w("    ///")
w("    /// 정본이 코드로 검증했다고 적은 규칙 넷:")
w("    ///   1. 모든 전투방에 빼앗을 몸이 최소 하나 있다")
w("    ///   2. 모든 전투방에 원거리와 근접이 둘 다 있다")
w("    ///   3. 같은 방에 같은 몸이 둘 있지 않다")
w("    ///   4. 데뷔 챕터보다 먼저 나오는 몸이 없다")
w("    /// </summary>")
w("    public static class RoomDef60")
w("    {")
w("        /// <summary>spawn 한 줄. 좌표는 발자국 중심(미터).</summary>")
w("        public sealed class Spawn")
w("        {")
w("            public float X, Y;")
w("            public string Role;      // FRONT · RANGED · FLANK · BACK")
w("            public string Unit;      // 우리 액터 키")
w("            public bool IsHost;      // H = 빼앗을 수 있다 · T = 잡몹")
w("        }")
w("")
w("        public sealed class Room")
w("        {")
w("            public int Ch;")
w("            public string No;        // \"001\" ~ \"010\"")
w("            public string Kind;      // 전투 · 이벤트 · 중간보스 · 엘리트 · 보스")
w("            public string Floor;")
w("            public string Layout;    // 레이아웃 글자. 보스방은 빈 값")
w("            public string LayoutKo;")
w("            public string Comp;")
w("            public string Pool;      // 이벤트 방일 때만 — BODY · STAKE")
w("            public string Boss;      // 보스 방일 때만 — crusher …")
w("            public string Captain;   // 중간보스 방일 때만 — 대장 호스트 키")
w("            public int Minions;      // 중간보스 방 부하 수")
w("            public string[] MinionFrom = System.Array.Empty<string>();")
w("            public bool Elite;")
w("            public Spawn[] Spawns = System.Array.Empty<Spawn>();")
w("        }")
w("")
w("        /// <summary>엘리트 규칙. 중간 보스(1.8배·빙의 불가)와 **다르다** — 엘리트는 빼앗을 수 있다.</summary>")
w("        public const float EliteScale = %sf;" % elite["scale"])
w("        public const int EliteHpMul = %d;" % elite["hpMul"])
w("        public const float EliteAtkMul = %sf;" % elite["atkMul"])
w("        public const bool ElitePossessable = %s;" % ("true" if elite["possessable"] else "false"))
w("        // 출처 — %s" % elite["note"].replace("**", ""))
w("")
w("        public static readonly Room[] All =")
w("        {")

for r in rooms:
    w("            new() { Ch = %d, No = %s, Kind = %s," % (r["ch"], cs(r["no"]), cs(r["kind"])))
    w("                    Floor = %s, Layout = %s, LayoutKo = %s,"
      % (cs(r.get("floor")), cs(r.get("layout")), cs(r.get("layoutKo"))))
    line = "                    Comp = %s, Pool = %s, Boss = %s, Captain = %s," % (
        cs(r.get("comp")), cs(r.get("pool")), cs(r.get("boss")), cs(r.get("captain")))
    w(line)
    w("                    Minions = %d, Elite = %s,"
      % (r.get("minions", 0), "true" if r.get("elite") else "false"))
    mf = r.get("minionFrom") or []
    if mf:
        w("                    MinionFrom = new[] { %s }," % ", ".join(cs(x) for x in mf))
    sp = r.get("spawn") or []
    if sp:
        w("                    Spawns = new Spawn[]")
        w("                    {")
        for s in sp:
            x, y, role, unit, hk = s
            w("                        new() { X = %sf, Y = %sf, Role = %s, Unit = %s, IsHost = %s },"
              % (x, y, cs(role), cs(unit), "true" if hk == "H" else "false"))
        w("                    },")
    w("                    },")

w("        };")
w("")
w("        /// <summary>챕터별 잡몹 회전. CH4~6 은 기존 7종 재조합이다 — 새 잡몹 0종.</summary>")
w("        public static readonly string[][] TrashPool =")
w("        {")
w("            System.Array.Empty<string>(),   // 0 번은 안 쓴다 — 챕터는 1부터다")
for ch in range(1, 7):
    w("            new[] { %s }," % ", ".join(cs(x) for x in trash[str(ch)]))
w("        };")
w("")
w("        /// <summary>호스트 데뷔 챕터. 각 챕터 중간 보스 대장이 그 챕터에 처음 나오는 몸이다.</summary>")
w("        public static readonly string[][] Debut =")
w("        {")
w("            System.Array.Empty<string>(),")
for ch in range(1, 7):
    w("            new[] { %s }," % ", ".join(cs(x) for x in debut[str(ch)]))
w("        };")
w("")
w("        /// <summary>방 번호별 빼앗을 몸 개수. 적혀 있지 않은 방은 0 이다.</summary>")
w("        public static int HostsInRoom(string no) => no switch")
w("        {")
for k, v in hosts_in.items():
    w('            "%s" => %d,' % (k, v))
w("            _ => 0,")
w("        };")
w("    }")
w("}")

io.open(DST, "w", encoding="utf-8", newline="\r\n").write("\n".join(out) + "\n")
print("wrote", DST)
print("rooms", len(rooms), "· spawns", sum(len(r.get("spawn") or []) for r in rooms))
