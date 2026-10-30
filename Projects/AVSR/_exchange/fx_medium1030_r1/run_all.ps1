$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/fx_medium1030_r1"
powershell -ExecutionPolicy Bypass -File order_r1_rune.ps1 *> log_r1_rune.txt
powershell -ExecutionPolicy Bypass -File order_r1_moon.ps1 *> log_r1_moon.txt
powershell -ExecutionPolicy Bypass -File order_r1_ghost.ps1 *> log_r1_ghost.txt
powershell -ExecutionPolicy Bypass -File order_r1_chain.ps1 *> log_r1_chain.txt
powershell -ExecutionPolicy Bypass -File order_r1_aura.ps1 *> log_r1_aura.txt
