-- ============================================
-- PERFORMANCE INDEXES FOR UNIMARKET
-- ============================================
-- Run this script to add critical performance indexes
-- before production launch
-- ============================================

-- Users table indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
CREATE INDEX IF NOT EXISTS "IX_Users_CreatedAt" ON "Users" ("CreatedAt");

-- Products table indexes
CREATE INDEX IF NOT EXISTS "IX_Products_SellerId" ON "Products" ("SellerId");
CREATE INDEX IF NOT EXISTS "IX_Products_CategoryId" ON "Products" ("CategoryId");
CREATE INDEX IF NOT EXISTS "IX_Products_CreatedAt" ON "Products" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Products_IsSold" ON "Products" ("IsSold");

-- Orders table indexes
CREATE INDEX IF NOT EXISTS "IX_Orders_BuyerId" ON "Orders" ("BuyerId");
CREATE INDEX IF NOT EXISTS "IX_Orders_SellerId" ON "Orders" ("SellerId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_PaymentReference" ON "Orders" ("PaymentReference");
CREATE INDEX IF NOT EXISTS "IX_Orders_CreatedAt" ON "Orders" ("CreatedAt");

-- PayoutQueue table indexes
CREATE INDEX IF NOT EXISTS "IX_PayoutQueue_SellerId" ON "PayoutQueue" ("SellerId");
CREATE INDEX IF NOT EXISTS "IX_PayoutQueue_ScheduledPayoutDate" ON "PayoutQueue" ("ScheduledPayoutDate");

-- ============================================
-- VERIFICATION
-- ============================================
-- Run this query to see all indexes:
-- SELECT tablename, indexname FROM pg_indexes WHERE schemaname = 'public' ORDER BY tablename, indexname;
