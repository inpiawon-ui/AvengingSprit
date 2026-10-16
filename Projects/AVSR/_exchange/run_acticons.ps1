Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('acticon_amazon','acticon_amazon_elite','acticon_baseball','acticon_commando_grenade','acticon_commando_laser','acticon_commando_mg','acticon_commando_missile','acticon_death','acticon_dragon_blue','acticon_dragoon','acticon_guru','acticon_hopper','acticon_hopper_smg','acticon_medium','acticon_ninja','acticon_ninja_chain','acticon_robot','acticon_salamander','acticon_snowwoman','acticon_thug','acticon_vampire','acticon_white_wizard')
# 세 장씩 — 한꺼번에 많이 던지면 스킬 문서만 뱉고 끝난다(2026-09 교훈)
for ($i = 0; $i -lt $keys.Count; $i += 3) {
    $batch = $keys[$i..([Math]::Min($i + 2, $keys.Count - 1))]
    $running = @()
    foreach ($k in $batch) {
        $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
        $running += $p
        Write-Output "started $k"
        Start-Sleep 3
    }
    $running | Wait-Process
}
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
