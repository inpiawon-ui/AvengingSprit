$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_feat_ult_ready_mark.ps1 *> log_mock_feat_ult_ready_mark.txt
if (Test-Path in\mock_feat_ult_ready_mark.png) { "ok mock_feat_ult_ready_mark" } else { "FAIL mock_feat_ult_ready_mark" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_feat_ult_reset_card.ps1 *> log_mock_feat_ult_reset_card.txt
if (Test-Path in\mock_feat_ult_reset_card.png) { "ok mock_feat_ult_reset_card" } else { "FAIL mock_feat_ult_reset_card" }
