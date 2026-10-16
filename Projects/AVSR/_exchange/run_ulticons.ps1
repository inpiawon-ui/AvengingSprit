Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('ulticon_amazon','ulticon_amazon_elite','ulticon_baseball','ulticon_commando_grenade','ulticon_commando_laser','ulticon_commando_mg','ulticon_commando_missile','ulticon_death','ulticon_dragon_blue','ulticon_dragoon','ulticon_guru','ulticon_hopper','ulticon_hopper_smg','ulticon_medium','ulticon_ninja','ulticon_ninja_chain','ulticon_robot','ulticon_salamander','ulticon_snowwoman','ulticon_thug','ulticon_vampire','ulticon_white_wizard')
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
