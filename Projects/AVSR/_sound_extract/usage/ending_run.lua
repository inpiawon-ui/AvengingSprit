-- 입력 보호 대체 + 자동 플레이 + 사운드 요청(명령·호출 위치) 기록 + 첫 등장 스냅샷
local main = manager.machine.devices[":maincpu"].spaces["program"]
local cpu = manager.machine.devices[":maincpu"]
local ports = manager.machine.ioport.ports
local SELECT_TO_PORT = { [0x37] = ":SYSTEM", [0x35] = ":P1", [0x36] = ":P2", [0x33] = ":DSW1", [0x34] = ":DSW2" }
local selected = 0
sel_w = main:install_write_tap(0xe0000, 0xe0001, "ipsel_w", function(o, d, m) selected = d & 0xff end)
sel_r = main:install_read_tap(0xe0000, 0xe0001, "ipsel_r", function(o, d, m)
    local tag = SELECT_TO_PORT[selected]
    if tag and ports[tag] then return ports[tag]:read() & 0xff end
    return d
end)

-- 스테이지 강제: 게임이 스테이지 번호($79582)를 쓸 때마다 지정 값으로 바꾼다. 구역($79583)은 0을 쓸 때만 바꾼다.
local FORCE_STAGE = tonumber(os.getenv("AVS_STAGE") or "-1")
local FORCE_AREA = tonumber(os.getenv("AVS_AREA") or "-1")
stage_tap = main:install_write_tap(0x79582, 0x79583, "stage", function(offset, data, mask)
    local out = data
    if FORCE_STAGE >= 0 and (mask & 0xff00) ~= 0 then out = (out & 0x00ff) | (FORCE_STAGE << 8) end
    if FORCE_AREA >= 0 and (mask & 0x00ff) ~= 0 and (data & 0x00ff) == 0 then
        out = (out & 0xff00) | FORCE_AREA
        -- 게임 시작 뒤 첫 진입만 강제하고 끈다. 계속 강제하면 격파 뒤 엔딩 대신 이름 입력으로 빠진다
        if os.getenv("AVS_FORCE_ONCE") == "1" and emu.time() > 3 then FORCE_STAGE, FORCE_AREA = -1, -1 end
    end
    return out
end)

local log = io.open(os.getenv("AVS_LOG") or "play_log.txt", "w")local seen = {}
local lastBgm = -1
local MAX_SHOTS = tonumber(os.getenv("AVS_MAX_SHOTS") or "400")
local shots = 0
local names = {}
for name, _ in pairs(cpu.state) do names[#names + 1] = name end
table.sort(names)
log:write("STATE " .. table.concat(names, " ") .. "\n")
local function sp_value()
    local entry = cpu.state["SP"] or cpu.state["A7"] or cpu.state["ISP"]
    return entry.value
end
queue_tap = main:install_write_tap(0x78eaa, 0x78eb3, "sndq", function(offset, data, mask)
  local ok, err = pcall(function()
    if data == 0xffff then return end
    local sp = sp_value()
    local ret = main:read_u32(sp)
    local t = emu.time()
    local function obj_type(reg)
        local p = cpu.state[reg].value
        if p >= 0x60000 and p < 0x7ff00 then return main:read_u16(p + 0x10) end
        return 0xffff
    end
    local typeA6, typeA2 = obj_type("A6"), obj_type("A2")
    local hostKind = main:read_u16(0x7a51e)
    local a6p = cpu.state["A6"].value
    local body = ""
    if a6p >= 0x60000 and a6p < 0x7ff00 then
        for k = 0x10, 0x2e, 2 do body = body .. string.format("%04x", main:read_u16(a6p + k)) end
    end
    local sub = (typeA6 == 0x1f and #body >= 12) and body:sub(5, 12) or ""
    local key = string.format("%04x@%06x@s%d@t%04x@h%d", data, ret, main:read_u8(0x79582), typeA6, hostKind)
    local shot = -1
    local isBgm = data < 0x10 or data == 0xff
    -- BGM 은 직전과 달라질 때만, 효과음은 (명령·호출 위치) 첫 등장만 찍는다. 반복 정지 루프에서 스냅샷이 폭증했다.
    local bgmChanged = isBgm and data ~= lastBgm
    if isBgm then lastBgm = data end
    if shots < MAX_SHOTS and (bgmChanged or (not isBgm and not seen[key])) then
        seen[key] = true
        manager.machine.video:snapshot()
        shot = shots
        shots = shots + 1
    end
    local a6 = cpu.state["A6"].value
    local a5 = cpu.state["A5"].value
    local a0 = cpu.state["A0"].value
    local obj = ""
    if a6 >= 0x60000 and a6 < 0x7ffe0 then
        for k = 0, 14, 2 do obj = obj .. string.format("%04x", main:read_u16(a6 + k)) end
    end
    log:write(string.format("Q %.3f cmd=%04x ret=%06x stage=%d area=%d shot=%d a0=%06x a5=%06x a6=%06x tA6=%04x tA2=%04x host=%d obj=%s body=%s\n",
        t, data, ret, main:read_u8(0x79582), main:read_u8(0x79583), shot, a0, a5, a6, typeA6, typeA2, hostKind, obj, body))
    log:flush()
  end)
  if not ok then log:write("ERR " .. tostring(err) .. "\n"); log:flush() end
end)
local progress = 15
progress_sub = emu.add_machine_frame_notifier(function()
    if emu.time() >= progress and shots < MAX_SHOTS then
        manager.machine.video:snapshot()
        log:write(string.format("P %.1f shot=%d\n", emu.time(), shots)); log:flush()
        shots = shots + 1
        progress = progress + 15
    end
end)

local function field(tag, name) return ports[tag].fields[name] end
local P = {
    right = field(":P1", "P1 Right"), left = field(":P1", "P1 Left"), up = field(":P1", "P1 Up"),
    down = field(":P1", "P1 Down"), fire = field(":P1", "P1 Button 1"), jump = field(":P1", "P1 Button 2"),
    coin = field(":SYSTEM", "Coin 1"), start = field(":SYSTEM", "1 Player Start"),
}
local function release_all() for _, f in pairs(P) do f:set_value(0) end end
math.randomseed(tonumber(os.getenv("AVS_SEED") or "7"))
local nextChange = 0
local frame = 0
local NOPLAY = os.getenv("AVS_NOPLAY") == "1"
local NOMOVE = NOPLAY or os.getenv("AVS_NOMOVE") == "1"
local NOFIRE = NOPLAY or os.getenv("AVS_NOFIRE") == "1"
play_sub = emu.add_machine_frame_notifier(function()
    frame = frame + 1
    local t = emu.time()
    -- 코인·스타트를 주기적으로 넣어 게임오버 후에도 계속 이어 간다
    local cycle = NOPLAY and -1 or (t % 20)
    if cycle > 1.0 and cycle < 1.2 then P.coin:set_value(1) elseif cycle > 1.2 and cycle < 1.4 then P.coin:set_value(0) end
    if cycle > 2.0 and cycle < 2.2 then P.start:set_value(1) elseif cycle > 2.2 and cycle < 2.4 then P.start:set_value(0) end
    if NOPLAY or (NOMOVE and main:read_u16(0x7a51e) ~= 0) or t < nextChange then
        P.fire:set_value((not NOFIRE and (emu.time() % 6) < 3 and frame % 6 < 3) and 1 or 0)
        return
    end
    nextChange = t + 0.3 + math.random() * 0.9
    P.right:set_value(0); P.left:set_value(0); P.up:set_value(0); P.down:set_value(0); P.jump:set_value(0)
    local r = math.random()
    if r < 0.55 then P.right:set_value(1) elseif r < 0.7 then P.left:set_value(1) end
    if math.random() < 0.3 then P.jump:set_value(1) end
    if math.random() < 0.15 then P.up:set_value(1) elseif math.random() < 0.1 then P.down:set_value(1) end
end)
-- 빙의 호스트 종류($7a51e)가 바뀔 때마다 기록·스냅샷 → 번호와 캐릭터를 대응시킨다
local lastHost = -1
host_sub = emu.add_machine_frame_notifier(function()
    local h = main:read_u16(0x7a51e)
    if h ~= lastHost then
        lastHost = h
        local shot = -1
        if shots < MAX_SHOTS then
            manager.machine.video:snapshot()
            shot = shots
            shots = shots + 1
        end
        log:write(string.format("H %.3f host=%d stage=%d shot=%d\n", emu.time(), h, main:read_u8(0x79582), shot))
        log:flush()
    end
end)
-- 빙의 호스트 강제: 게임이 호스트 번호($7a51e)에 0 이 아닌 값을 쓸 때 지정 번호로 바꾼다
local FORCE_HOST = tonumber(os.getenv("AVS_HOST") or "-1")
host_tap = main:install_write_tap(0x7a51e, 0x7a51f, "host", function(offset, data, mask)
    if FORCE_HOST > 0 and data ~= 0 then return FORCE_HOST end
    return data
end)
-- 무적: AVS_INVINCIBLE=1 → 유령·호스트 에너지 감소 차단, =ghost → 유령 에너지만(호스트는 죽을 수 있어 빙의가 반복된다)
local INVINCIBLE = os.getenv("AVS_INVINCIBLE") or "0"
local GHOST_ENERGY, GHOST_MAX = 0x79e54, 0xc8
local HOST_OBJ = 0x79eea
if INVINCIBLE == "1" or INVINCIBLE == "ghost" then
    ghost_energy_tap = main:install_write_tap(GHOST_ENERGY, GHOST_ENERGY + 1, "inv_ghost", function(offset, data, mask)
        if data < GHOST_MAX or data >= 0x8000 then return GHOST_MAX end
        return data
    end)
end
if INVINCIBLE == "1" then
    host_energy_tap = main:install_write_tap(HOST_OBJ + 0x66, HOST_OBJ + 0x67, "inv_host", function(offset, data, mask)
        local maxEnergy = main:read_u16(HOST_OBJ + 0x68)
        if maxEnergy > 0 and maxEnergy < 0x8000 and (data < maxEnergy or data >= 0x8000) then return maxEnergy end
        return data
    end)
end

-- 순간이동: 2초마다 빙의 호스트를 가장 가까운 적(종류 1–26) 옆으로 옮긴다. 좌우를 번갈아 붙인다.
local TELEPORT = os.getenv("AVS_TELEPORT") == "1"
-- 적은 $7a54c 부터 20칸뿐이다(폭탄 아이템 코드 $e0fa 가 이 범위만 순회). 다른 배열은 탄·장애물로 번호 체계가 다르다
local OBJECT_ARRAYS = { { 0x7a54c, 20 } }
local OBJ_SIZE, SIDE_OFFSET = 0x90, 0x180000
if TELEPORT then
    local nextTeleport, side = 8, 1
    tele_sub = emu.add_machine_frame_notifier(function()
        local t = emu.time()
        if t < nextTeleport then return end
        nextTeleport = t + 2.0
        if main:read_u16(0x7a51e) == 0 then return end
        local px, py = main:read_u32(HOST_OBJ + 0x28), main:read_u32(HOST_OBJ + 0x32)
        local best, bestDistance = nil, nil
        for _, arr in ipairs(OBJECT_ARRAYS) do
            for i = 0, arr[2] - 1 do
                local o = arr[1] + i * OBJ_SIZE
                local kind = main:read_u16(o + 0x10)
                if (main:read_u16(o) & 0x8000) ~= 0 and kind >= 1 and kind <= 26 then
                    local d = math.abs(main:read_u32(o + 0x28) - px) + math.abs(main:read_u32(o + 0x32) - py)
                    if not bestDistance or d < bestDistance then best, bestDistance = o, d end
                end
            end
        end
        if best then
            side = -side
            main:write_u32(HOST_OBJ + 0x28, (main:read_u32(best + 0x28) + side * SIDE_OFFSET) & 0xffffffff)
            main:write_u32(HOST_OBJ + 0x32, main:read_u32(best + 0x32))
            log:write(string.format("T %.1f enemy=%06x kind=%d\n", t, best, main:read_u16(best + 0x10)))
            log:flush()
        end
    end)
end
-- 격파 징글(BGM 7)이 큐에 들어가면 입력을 멈추고 3초마다 스냅샷 — 버튼이 엔딩 연출을 넘기지 않게 한다
local STOP_ON_CLEAR = os.getenv("AVS_STOP_ON_CLEAR") == "1"
if STOP_ON_CLEAR then
    local stopped, nextShot = false, 0
    clear_tap = main:install_write_tap(0x78eaa, 0x78eb3, "clear", function(offset, data, mask)
        if data == 0x0007 and not stopped then
            stopped = true
            NOPLAY, NOMOVE, NOFIRE = true, true, true
            for _, f in pairs(P) do f:set_value(0) end
            log:write(string.format("C %.2f clear jingle -> inputs stopped\n", emu.time()))
            log:flush()
        end
        return data
    end)
    clear_shot_sub = emu.add_machine_frame_notifier(function()
        local t = emu.time()
        if stopped and t >= nextShot and shots < MAX_SHOTS then
            manager.machine.video:snapshot()
            log:write(string.format("V %.2f shot=%d\n", t, shots))
            log:flush()
            shots = shots + 1
            nextShot = t + 3
        end
    end)
end