# ================================================
# Script mở firewall cho ChatBoxWeb
# Chạy PowerShell với quyền Administrator!
# Right-click PowerShell -> Run as Administrator
# ================================================

# Mở port Backend (5206)
netsh advfirewall firewall add rule name="ChatBoxWeb Backend 5206" dir=in action=allow protocol=TCP localport=5206
Write-Host "✓ Đã mở port 5206 (Backend)" -ForegroundColor Green

# Mở port Frontend (3000)
netsh advfirewall firewall add rule name="ChatBoxWeb Frontend 3000" dir=in action=allow protocol=TCP localport=3000
Write-Host "✓ Đã mở port 3000 (Frontend)" -ForegroundColor Green

Write-Host ""
Write-Host "=== Hoàn tất! ===" -ForegroundColor Cyan
Write-Host "Máy khác có thể truy cập:" -ForegroundColor Yellow
Write-Host "  Frontend: http://<IP-CUA-BAN>:3000" -ForegroundColor Yellow
Write-Host "  Backend:  http://<IP-CUA-BAN>:5206" -ForegroundColor Yellow
Write-Host ""
Write-Host "Để xem IP Radmin VPN, mở Radmin VPN và xem IP được cấp (dạng 26.x.x.x)" -ForegroundColor Yellow
