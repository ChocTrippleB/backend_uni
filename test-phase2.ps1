# Phase 2 Testing Script
# Tests Admin User Creation and Bank Account Management

$baseUrl = "https://localhost:7255/api"

Write-Host "`n=== PHASE 2: ADMIN & BANK MANAGEMENT TESTING ===`n" -ForegroundColor Cyan

# Test 1: Create Bootstrap Admin User
Write-Host "Test 1: Creating bootstrap admin user..." -ForegroundColor Yellow
$adminData = @{
    username = "admin"
    email = "admin@unimarket.com"
    fullName = "System Administrator"
    password = "Admin123!@#"
} | ConvertTo-Json

try {
    $adminResponse = Invoke-RestMethod -Uri "$baseUrl/auth/bootstrap-admin" `
        -Method Post `
        -Body $adminData `
        -ContentType "application/json" `
        -SkipCertificateCheck

    Write-Host "✅ Admin user created successfully!" -ForegroundColor Green
    Write-Host "Admin ID: $($adminResponse.user.id)" -ForegroundColor Gray
    Write-Host "Admin Email: $($adminResponse.user.email)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to create admin: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# Test 2: Login as Admin
Write-Host "`nTest 2: Logging in as admin..." -ForegroundColor Yellow
$loginData = @{
    email = "admin@unimarket.com"
    password = "Admin123!@#"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" `
        -Method Post `
        -Body $loginData `
        -ContentType "application/json" `
        -SkipCertificateCheck

    $adminToken = $loginResponse.token
    Write-Host "✅ Admin logged in successfully!" -ForegroundColor Green
    Write-Host "Role: $($loginResponse.user.Role)" -ForegroundColor Gray
    Write-Host "Token: $($adminToken.Substring(0, 50))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to login as admin: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get existing seller credentials (assuming user ID 1 exists)
Write-Host "`nTest 3: Logging in as existing seller (User ID 1)..." -ForegroundColor Yellow
# Note: You'll need to use real credentials from your database
# For now, let's try to get the user info if they're already logged in

# Test 4: Add Bank Account for Seller
Write-Host "`nTest 4: Adding bank account for seller..." -ForegroundColor Yellow
Write-Host "Note: This requires a valid seller token. Skipping for now." -ForegroundColor Gray
Write-Host "You can test this manually with Postman or after logging in as a seller." -ForegroundColor Gray

# Test 5: Test Admin-Only Endpoints
Write-Host "`nTest 5: Testing admin-only payout endpoints..." -ForegroundColor Yellow

if ($adminToken) {
    $headers = @{
        "Authorization" = "Bearer $adminToken"
    }

    # Test payout stats endpoint
    try {
        Write-Host "  - Getting payout statistics..." -ForegroundColor Gray
        $stats = Invoke-RestMethod -Uri "$baseUrl/Payout/stats" `
            -Method Get `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "  ✅ Successfully accessed admin payout stats!" -ForegroundColor Green
        Write-Host "    Total Pending: $($stats.data.totalPending)" -ForegroundColor Gray
        Write-Host "    Total Processed: $($stats.data.totalProcessed)" -ForegroundColor Gray
        Write-Host "    Total Failed: $($stats.data.totalFailed)" -ForegroundColor Gray
    } catch {
        Write-Host "  ❌ Failed to access payout stats: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test pending payouts endpoint
    try {
        Write-Host "  - Getting pending payouts for today..." -ForegroundColor Gray
        $today = Get-Date -Format "yyyy-MM-dd"
        $pendingPayouts = Invoke-RestMethod -Uri "$baseUrl/Payout/pending/$today" `
            -Method Get `
            -Headers $headers `
            -SkipCertificateCheck

        Write-Host "  ✅ Successfully accessed pending payouts!" -ForegroundColor Green
        Write-Host "    Count: $($pendingPayouts.data.Count)" -ForegroundColor Gray
    } catch {
        Write-Host "  ❌ Failed to access pending payouts: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== TESTING COMPLETE ===`n" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Use Postman to test bank account creation with a seller account" -ForegroundColor White
Write-Host "2. Test the complete order flow with bank validation" -ForegroundColor White
Write-Host "3. Process batch payouts as admin" -ForegroundColor White
