$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/fx_medium1030_r1"
powershell -ExecutionPolicy Bypass -File order_r1_pillar.ps1 *> log_r1_pillar.txt
powershell -ExecutionPolicy Bypass -File order_r1_subring.ps1 *> log_r1_subring.txt
powershell -ExecutionPolicy Bypass -File order_r1_up.ps1 *> log_r1_up.txt
powershell -ExecutionPolicy Bypass -File order_r1_upself.ps1 *> log_r1_upself.txt
