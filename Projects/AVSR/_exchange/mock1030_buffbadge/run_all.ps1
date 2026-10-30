$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/mock1030_buffbadge"
powershell -ExecutionPolicy Bypass -File order_badge_infuse_1.ps1 *> log_infuse_1.txt
powershell -ExecutionPolicy Bypass -File order_badge_infuse_2.ps1 *> log_infuse_2.txt
powershell -ExecutionPolicy Bypass -File order_badge_infuse_3.ps1 *> log_infuse_3.txt
powershell -ExecutionPolicy Bypass -File order_badge_ritual_1.ps1 *> log_ritual_1.txt
powershell -ExecutionPolicy Bypass -File order_badge_ritual_2.ps1 *> log_ritual_2.txt
powershell -ExecutionPolicy Bypass -File order_badge_ritual_3.ps1 *> log_ritual_3.txt
