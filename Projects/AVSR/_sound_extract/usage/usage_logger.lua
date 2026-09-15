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
    if FORCE_AREA >= 0 and (mask & 0x00ff) ~= 0 and (data & 0x00ff) == 0 then out = (out & 0xff00) | FORCE_AREA end
    return out
end)

local log = io.open(os.getenv("AVS_LOG") or "play_log.txt", "w")local seen = {}
local lastBgm = -1
local MAX_SHOTS = 400
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
play_sub = emu.add_machine_frame_notifier(function()
    frame = frame + 1
    local t = emu.time()
    -- 코인·스타트를 주기적으로 넣어 게임오버 후에도 계속 이어 간다
    local cycle = t % 20
    if cycle > 1.0 and cycle < 1.2 then P.coin:set_value(1) elseif cycle > 1.2 and cycle < 1.4 then P.coin:set_value(0) end
    if cycle > 2.0 and cycle < 2.2 then P.start:set_value(1) elseif cycle > 2.2 and cycle < 2.4 then P.start:set_value(0) end
    if t < nextChange then
        P.fire:set_value((frame % 6 < 3) and 1 or 0)
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