-- 곡마다 사운드 CPU가 OKI 두 칩에 보내는 명령 워드를 기록한다(샘플 사용처 추적용)
local SOUND_LATCH = 0x044308
local GAP_SECONDS = 1.5
local main = manager.machine.devices[":maincpu"].spaces["program"]
local snd = manager.machine.devices[":audiocpu"].spaces["program"]
local items = {}
for n = 1, 12 do items[#items + 1] = { n = n, dur = 60 } end
for n = 16, 38 do items[#items + 1] = { n = n, dur = 6 } end
local log = io.open("oki_trace.txt", "w")
local current = 0
oki1_tap = snd:install_write_tap(0x0a0000, 0x0a0003, "oki1", function(offset, data, mask)
    if current > 0 then log:write(string.format("W %d 1 %06x %04x %04x\n", current, offset, data, mask)) end
end)
oki2_tap = snd:install_write_tap(0x0c0000, 0x0c0003, "oki2", function(offset, data, mask)
    if current > 0 then log:write(string.format("W %d 2 %06x %04x %04x\n", current, offset, data, mask)) end
end)
local state, index, nextTime = "gap", 0, 2.0
sub = emu.add_machine_frame_notifier(function()
    local t = emu.time()
    if state == "done" or t < nextTime then return end
    if state == "gap" then
        index = index + 1
        if index > #items then log:write("DONE\n"); log:close(); state = "done"; manager.machine:exit(); return end
        current = items[index].n
        main:write_u16(SOUND_LATCH, current)
        state = "play"; nextTime = t + items[index].dur
    else
        main:write_u16(SOUND_LATCH, 0x0000)
        current = 0
        state = "gap"; nextTime = t + GAP_SECONDS
    end
end)