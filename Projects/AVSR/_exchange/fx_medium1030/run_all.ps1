$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/fx_medium1030"
powershell -ExecutionPolicy Bypass -File order_inf_ring.ps1 *> log_inf_ring.txt
powershell -ExecutionPolicy Bypass -File order_inf_up.ps1 *> log_inf_up.txt
powershell -ExecutionPolicy Bypass -File order_inf_burst.ps1 *> log_inf_burst.txt
powershell -ExecutionPolicy Bypass -File order_rit_rune.ps1 *> log_rit_rune.txt
powershell -ExecutionPolicy Bypass -File order_rit_pillar.ps1 *> log_rit_pillar.txt
powershell -ExecutionPolicy Bypass -File order_rit_aura.ps1 *> log_rit_aura.txt
powershell -ExecutionPolicy Bypass -File order_rit_link.ps1 *> log_rit_link.txt
