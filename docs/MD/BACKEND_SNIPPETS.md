# Backend Code Snippets - Copy & Paste

## 1. Order.cs
**Location**: Add after line 91 (after `public string? Notes { get; set; }`)

```csharp
        /// <summary>
        /// Reason for cancellation (if cancelled or refunded)
        /// </summary>
        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        /// <summary>
        /// When the order was cancelled
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Who cancelled the order (Buyer or Seller)
        /// </summary>
        [MaxLength(50)]
        public string? CancelledBy { get; set; }

        /// <summary>
        /// Paystack refund reference (if refunded)
        /// </summary>
        public string? RefundReference { get; set; }

        /// <summary>
        /// When the refund was processed
        /// </summary>
        public DateTime? RefundedAt { get; set; }
```

## 2. IPaystackService.cs
**Location**: Add before closing `}`

```csharp
        /// <summary>
        /// Refund a payment transaction
        /// </summary>
        Task<PaystackRefundResponse> RefundPaymentAsync(string transactionReference, decimal? amount = null, string? merchantNote = null);
```

## 3. PaystackService.cs
**Location**: Add before closing `}`

```csharp
        public async Task<PaystackRefundResponse> RefundPaymentAsync(string transactionReference, decimal? amount = null, string? merchantNote = null)
        {
            var url = "https://api.paystack.co/refund";

            var payload = new
            {
                transaction = transactionReference,
                amount = amount != null ? (int)(amount * 100) : (int?)null,
                merchant_note = merchantNote
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            var result = await response.Content.ReadFromJsonAsync<PaystackRefundResponse>();

            if (result == null || !result.Status)
            {
                throw new InvalidOperationException(result?.Message ?? "Refund failed");
            }

            return result;
        }
```

## 4. IOrderService.cs
**Location**: Add before closing `}`

```csharp
        Task<Order> CancelOrderAsync(int orderId, Guid userId, string cancellationReason);
        Task<bool> CanCancelOrderAsync(int orderId, Guid userId);
```

## 5. OrderService.cs
**Location**: Add before closing `}`

```csharp
        public async Task<bool> CanCancelOrderAsync(int orderId, Guid userId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;
            if (order.BuyerId != userId && order.SellerId != userId) return false;
            return order.Status == OrderStatus.Pending || order.Status == OrderStatus.Paid || order.Status == OrderStatus.AwaitingRelease;
        }

        public async Task<Order> CancelOrderAsync(int orderId, Guid userId, string cancellationReason)
        {
            var order = await _context.Orders.Include(o => o.Buyer).Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new InvalidOperationException("Order not found");
            if (!await CanCancelOrderAsync(orderId, userId)) throw new InvalidOperationException("Cannot cancel this order");

            string cancelledBy = order.BuyerId == userId ? "Buyer" : "Seller";

            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.AwaitingRelease)
            {
                if (string.IsNullOrEmpty(order.PaymentReference)) throw new InvalidOperationException("No payment reference found for refund");
                try
                {
                    var refundResult = await _paystackService.RefundPaymentAsync(order.PaymentReference, amount: order.Amount, merchantNote: $"Order #{orderId} cancelled by {cancelledBy}. Reason: {cancellationReason}");
                    order.Status = OrderStatus.Refunded;
                    order.RefundReference = refundResult.Data?.RefundReference;
                    order.RefundedAt = DateTime.UtcNow;
                }
                catch (Exception ex) { throw new InvalidOperationException($"Refund failed: {ex.Message}"); }
            }
            else { order.Status = OrderStatus.Cancelled; }

            order.CancellationReason = cancellationReason;
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledBy = cancelledBy;
            order.Product.IsSold = false;
            await _context.SaveChangesAsync();
            return order;
        }
```

## 6. OrderController.cs
**Location**: Add before closing `}`

```csharp
        [HttpPost("cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (!await _orderService.CanCancelOrderAsync(dto.OrderId, userId))
                {
                    return BadRequest(new { success = false, message = "You cannot cancel this order. It may have already been processed or completed." });
                }

                var order = await _orderService.CancelOrderAsync(dto.OrderId, userId, dto.CancellationReason);
                return Ok(new
                {
                    success = true,
                    message = order.Status == OrderStatus.Refunded ? "Order cancelled and refund processed successfully" : "Order cancelled successfully",
                    data = new { orderId = order.Id, status = order.Status.ToString(), refundReference = order.RefundReference, cancelledAt = order.CancelledAt }
                });
            }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }
```

## 7. PayoutController.cs
**Location**: Add before closing `}` (needs `using Microsoft.EntityFrameworkCore;` at top)

```csharp
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayoutsAdmin()
        {
            try
            {
                var allPayouts = await _context.PayoutQueue
                    .Include(p => p.Seller).Include(p => p.Order).ThenInclude(o => o.Product)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new { p.Id, p.OrderId, p.SellerId, SellerName = p.Seller.FullName, SellerEmail = p.Seller.Email, ProductName = p.Order.Product.Name, p.Amount, p.TransferFee, p.Status, p.ScheduledPayoutDate, p.CreatedAt, p.ProcessedAt, p.ErrorMessage })
                    .ToListAsync();
                return Ok(new { success = true, data = allPayouts, count = allPayouts.Count });
            }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }
```

## After All Changes - Run Migration:
```bash
cd backend
dotnet ef migrations add AddOrderCancellationFields
dotnet ef database update
```

---

**Files Already Created**:
- ✅ `CancelOrderDto.cs`
- ✅ `PaystackRefundResponse.cs`
- ✅ `CancelOrderModal.jsx`

Next when you return: Frontend changes!
