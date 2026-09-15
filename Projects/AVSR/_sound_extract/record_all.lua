-- 사운드 CPU에 곡 번호를 직접 넣어 전곡을 한 번에 녹음한다.
-- MCU가 없어 메인 CPU는 부팅 직후 멈추므로 래치를 가로채는 명령이 없다.
local SOUND_LATCH = 0x044308
local GAP_SECONDS = 1.5
local BOOT_SECONDS = 2.0
local BGM_SECONDS = 240
local SFX_SECONDS = 6

local main = manager.machine.devices[":maincpu"].spaces["program"]

local items = {}
for n = 1, 12 do items[#items + 1] = { n = n, dur = BGM_SECONDS } end
for n = 16, 62 do
    if n ~= 39 then items[#items + 1] = { n = n, dur = SFX_SECONDS } end
end

local log = io.open("rec_sched.txt", "w")
local state = "gap"
local index = 0
local nextTime = BOOT_SECONDS

-- 상위 바이트 0 = 16채널 전체를 쓰는 계열. 곡의 모든 트랙이 울린다. 0x0000은 정지(곡 0 = 빈 곡).
rec_sub = emu.add_machine_frame_notifier(function()
    local t = emu.time()
    if state == "done" or t < nextTime then return end
    if state == "gap" then
        index = index + 1
        if index > #items then
            log:write("DONE\n")
            log:close()
            state = "done"
            manager.machine:exit()
            return
        end
        main:write_u16(SOUND_LATCH, items[index].n)
        log:write(string.format("START %d %.6f\n", items[index].n, t))
        log:flush()
        state = "play"
        nextTime = t + items[index].dur
    else
        main:write_u16(SOUND_LATCH, 0x0000)
        log:write(string.format("STOP %d %.6f\n", items[index].n, t))
        log:flush()
        state = "gap"
        nextTime = t + GAP_SECONDS
    end
end)
