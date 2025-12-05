# GUID & Slug Migration Guide

**Date**: November 13, 2025
**Status**: Backend 70% Complete - 44 Compilation Errors Remaining

---

## 📋 Executive Summary

This guide documents the migration from:
- **User IDs**: `int` → `Guid`
- **Product URLs**: `/product/:id` → `/product/:slug`
- **Profile URLs**: `/profile` → `/@username`

### ✅ What's Been Completed

#### Models (100% Complete)
- ✅ User: `Id` changed to `Guid` with auto-generation
- ✅ Product: Added `Slug` field, `SellerId` → `Guid`
- ✅ Order: `BuyerId`, `SellerId` → `Guid`
- ✅ Review: `BuyerId`, `SellerId` → `Guid`
- ✅ Cart: `UserId` → `Guid`
- ✅ BankAccount: `UserId` → `Guid`
- ✅ PayoutQueue: `SellerId` → `Guid`
- ✅ UserFollower: `FollowerId`, `FollowedId` → `Guid`
- ✅ SellerRating: `SellerId` → `Guid`

#### Services (50% Complete)
- ✅ **SlugService**: Created (ISlugService + SlugService)
  - Slugifies product names
  - Handles duplicates (appends -2, -3, etc.)
  - Auto-updates slug when product name changes
- ✅ **ProductService**: Fully updated
  - Injects ISlugService
  - `GetBySlugAsync()` method
  - `GetBySlugOrIdAsync()` method (backward compat)
  - Auto-generates slugs on create/update
  - `GetItemsBySellerAsync()` uses `Guid`
  - All projections include `slug` field

#### DTOs (100% Complete)
- ✅ ProductDto: Added `slug` field
- ✅ CreateItemDto: `SellerId` → `Guid`
- ✅ OrderResponseDto: `BuyerId`, `SellerId` → `Guid`, added `ProductSlug`
- ✅ ReviewResponseDto: `BuyerId`, `SellerId` → `Guid`
- ✅ BankAccountResponseDto: `UserId` → `Guid`
- ✅ SellerRatingDto: `SellerId` → `Guid`

#### Controllers (40% Complete)
- ✅ **ItemsController**: Fully updated
  - `GetItem(string identifier)` - accepts slug or ID
  - `GetItemsBySeller(Guid sellerId)`
  - Returns slug in all responses
- ✅ **UserController**: Fully updated
  - Uses `User.GetUserId()` helper
  - Added `GET @{username}` endpoint
  - `GetUserById(Guid id)`
- ✅ **AuthController**: Partially updated
  - `GET /me` - uses `GetUserId()` helper
  - `GET /user/{id}` - accepts `Guid`
  - JWT generation already compatible (uses `.ToString()`)

#### Helpers (100% Complete)
- ✅ **ClaimsPrincipalExtensions**: Created
  - `GetUserId()` - extracts `Guid?` from claims
  - `GetUsername()` - extracts username
  - `GetUserRole()` - extracts role

#### Configuration (100% Complete)
- ✅ SlugService registered in `Program.cs`

---

## ❌ Remaining Work (44 Compilation Errors)

### Controllers to Fix

#### 1. **OrdersController.cs** (14 errors)
**File**: `D:\JS Mastery\backend\backend\Controllers\OrdersController.cs`

**Pattern**: Replace `int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")` with `User.GetUserId()`

**Changes Needed**:
```csharp
// Add using directive
using backend.Helpers;

// Example fix pattern:
// OLD:
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

// NEW:
var userId = User.GetUserId();
if (userId == null)
    return Unauthorized(new { message = "Invalid user token" });

// Update all method signatures accepting userId
// OLD: GetOrders(int userId)
// NEW: GetOrders(Guid userId)
```

**Affected Methods**:
- `CreateOrder`
- `GetMyOrders`
- `GetOrderById`
- `GetSellerOrders`
- `VerifyReleaseCode`

---

#### 2. **PaymentController.cs** (8 errors)
**File**: `D:\JS Mastery\backend\backend\Controllers\PaymentController.cs`

**Changes Needed**:
```csharp
using backend.Helpers;

// Replace userId extraction
var userId = User.GetUserId();
if (userId == null) return Unauthorized();

// Update method signatures
// OLD: InitializePayment(int userId, ...)
// NEW: InitializePayment(Guid userId, ...)
```

**Affected Methods**:
- `InitializePayment`
- `VerifyPayment`

---

#### 3. **BankAccountController.cs** (6 errors)
**File**: `D:\JS Mastery\backend\backend\Controllers\BankAccountController.cs`

**Changes Needed**:
```csharp
using backend.Helpers;

var userId = User.GetUserId();
if (userId == null) return Unauthorized();

// All API methods use userId - update them all
```

**Affected Methods**:
- `AddBankAccount`
- `GetBankAccounts`
- `GetPrimaryBankAccount`
- `UpdateBankAccount`
- `SetPrimaryBankAccount`
- `DeleteBankAccount`

---

#### 4. **ReviewController.cs** (7 errors)
**File**: `D:\JS Mastery\backend\backend\Controllers\ReviewController.cs`

**Changes Needed**:
```csharp
using backend.Helpers;

var userId = User.GetUserId();
if (userId == null) return Unauthorized();

// Update method signatures accepting userId
```

**Affected Methods**:
- `CreateReview`
- `UpdateReview`
- `DeleteReview`
- `GetMyReviews`

---

#### 5. **FollowController.cs** (7 errors)
**File**: `D:\JS Mastery\backend\backend\Controllers\FollowController.cs`

**Changes Needed**:
```csharp
using backend.Helpers;

var userId = User.GetUserId();
if (userId == null) return Unauthorized();

// Update method signatures
// OLD: FollowUser(int followedId)
// NEW: FollowUser(Guid followedId)

// OLD: UnfollowUser(int followedId)
// NEW: UnfollowUser(Guid followedId)

// OLD: GetFollowers(int userId)
// NEW: GetFollowers(Guid userId)

// OLD: GetFollowing(int userId)
// NEW: GetFollowing(Guid userId)
```

---

### Services to Fix

#### 6. **CartService.cs** (4 errors)
**File**: `D:\JS Mastery\backend\backend\Services\CartService.cs`

**Changes Needed**:
```csharp
// Update method signatures
// OLD: Task<Cart?> GetCartByUserIdAsync(int userId);
// NEW: Task<Cart?> GetCartByUserIdAsync(Guid userId);

// Update LINQ queries
// OLD: .Where(c => c.UserId == userId)
// NEW: .Where(c => c.UserId == userId)  // already Guid

// Update assignments
// OLD: Cart { UserId = userId }
// NEW: Cart { UserId = userId }  // already Guid
```

---

#### 7. **OrderService.cs** (7 errors)
**File**: `D:\JS Mastery\backend\backend\Services\OrderService.cs`

**Changes Needed**:
```csharp
// Update interface (IOrderService.cs)
// OLD: Task<Order> CreateOrderAsync(int buyerId, CreateOrderDto dto);
// NEW: Task<Order> CreateOrderAsync(Guid buyerId, CreateOrderDto dto);

// OLD: Task<List<Order>> GetOrdersByUserIdAsync(int userId);
// NEW: Task<List<Order>> GetOrdersByUserIdAsync(Guid userId);

// OLD: Task<List<Order>> GetOrdersBySellerIdAsync(int sellerId);
// NEW: Task<List<Order>> GetOrdersBySellerIdAsync(Guid sellerId);

// Update all comparisons
// OLD: order.BuyerId == userId
// NEW: order.BuyerId == userId  // already Guid
```

---

#### 8. **BankAccountService.cs** (9 errors)
**File**: `D:\JS Mastery\backend\backend\Services\BankAccountService.cs`

**Changes Needed**:
```csharp
// Update interface (IBankAccountService.cs)
// OLD: Task<BankAccountResponseDto> AddBankAccountAsync(int userId, AddBankAccountDto dto);
// NEW: Task<BankAccountResponseDto> AddBankAccountAsync(Guid userId, AddBankAccountDto dto);

// OLD: Task<List<BankAccountResponseDto>> GetBankAccountsAsync(int userId);
// NEW: Task<List<BankAccountResponseDto>> GetBankAccountsAsync(Guid userId);

// Update all LINQ queries
// OLD: .Where(ba => ba.UserId == userId)
// NEW: .Where(ba => ba.UserId == userId)  // already Guid

// Update assignments
// OLD: BankAccount { UserId = userId }
// NEW: BankAccount { UserId = userId }  // already Guid
```

---

#### 9. **PayoutService.cs** (2 errors)
**File**: `D:\JS Mastery\backend\backend\Services\PayoutService.cs`

**Changes Needed**:
```csharp
// Update LINQ queries
// OLD: .Where(p => p.SellerId == userId)
// NEW: .Where(p => p.SellerId == userId)  // already Guid
```

---

## 🔧 Quick Fix Script

Here's the systematic approach to fix all 44 errors:

### Step 1: Add Helper Import to All Controllers
Add this line to the top of each controller file:
```csharp
using backend.Helpers;
```

**Files**:
- `Controllers/OrdersController.cs`
- `Controllers/PaymentController.cs`
- `Controllers/BankAccountController.cs`
- `Controllers/ReviewController.cs`
- `Controllers/FollowController.cs`

### Step 2: Replace User ID Extraction Pattern

**Find (Regex)**:
```regex
var userId = int\.Parse\(User\.FindFirst\(ClaimTypes\.NameIdentifier\)\?\.Value \?\? "0"\);
```

**Replace With**:
```csharp
var userId = User.GetUserId();
if (userId == null)
    return Unauthorized(new { message = "Invalid user token" });
```

### Step 3: Update Method Signatures

**Find Pattern**: `int userId`
**Replace With**: `Guid userId`

**Affected Parameters**:
- All controller action parameters
- All service method parameters
- All service interface methods

### Step 4: Remove Unnecessary Casts

The models already use `Guid`, so no explicit casts are needed:
```csharp
// This will work automatically once method signatures are fixed:
var order = new Order {
    BuyerId = userId,  // userId is already Guid
    SellerId = sellerId  // sellerId is already Guid
};
```

---

## 🗄️ Database Migration Steps

**IMPORTANT**: Only proceed after all 44 compilation errors are fixed!

### Step 1: Delete Old Migrations
```bash
cd "D:\JS Mastery\backend\backend"
rmdir /s /q Migrations
```

### Step 2: Drop Existing Database
```bash
dotnet ef database drop --force
```

### Step 3: Create Fresh Migration
```bash
dotnet ef migrations add InitialWithGuidAndSlug
```

### Step 4: Apply Migration
```bash
dotnet ef database update
```

### Step 5: Verify Database Schema
- User.Id → `uuid` (PostgreSQL)
- Product.Slug → `text` with unique index
- All foreign keys → `uuid`

---

## 🎯 Frontend Changes (After Backend Complete)

### React Router (App.jsx)
```javascript
// OLD:
<Route path="/product/:id" element={<ProductDetail />} />

// NEW:
<Route path="/product/:slug" element={<ProductDetail />} />
<Route path="/@:username" element={<Profile />} />
```

### API Services
```javascript
// Update all API calls to use strings for GUIDs
// GUIDs in JavaScript are just strings

// Example:
const userId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID as string
api.get(`/api/User/${userId}`);

// Slugs:
api.get(`/api/v1/item/${slug}`);  // e.g., "macbook-pro-2020"
```

### CartContext
```javascript
// localStorage stores user object with GUID ID
const user = JSON.parse(localStorage.getItem('user'));
console.log(user.id);  // "550e8400-e29b-41d4-a716-446655440000"

// Use as-is in API calls (GUIDs are strings in JS)
```

### Product Links
```javascript
// OLD:
<Link to={`/product/${product.id}`}>View Product</Link>

// NEW:
<Link to={`/product/${product.slug}`}>View Product</Link>
```

### Profile Links
```javascript
// OLD:
<Link to={`/profile/${user.id}`}>View Profile</Link>

// NEW:
<Link to={`/@${user.username}`}>View Profile</Link>
```

### Redirect Logic (ProductDetail.jsx)
```javascript
useEffect(() => {
    const { slug } = useParams();

    // If numeric (old ID format), fetch and redirect
    if (/^\d+$/.test(slug)) {
        fetchItemById(slug).then(item => {
            navigate(`/product/${item.slug}`, { replace: true });
        });
    } else {
        // Normal slug fetch
        fetchItemBySlug(slug);
    }
}, [slug]);
```

---

## 📊 Completion Checklist

### Backend
- [x] Models updated to GUID
- [x] Product model has Slug field
- [x] SlugService created
- [x] ProductService updated
- [x] ItemsController updated
- [x] UserController updated
- [x] AuthController partially updated
- [x] SlugService registered
- [x] ClaimsPrincipalExtensions helper created
- [ ] **OrdersController** (14 errors)
- [ ] **PaymentController** (8 errors)
- [ ] **BankAccountController** (6 errors)
- [ ] **ReviewController** (7 errors)
- [ ] **FollowController** (7 errors)
- [ ] **CartService** (4 errors)
- [ ] **OrderService** (7 errors)
- [ ] **BankAccountService** (9 errors)
- [ ] **PayoutService** (2 errors)
- [ ] Drop database
- [ ] Delete old migrations
- [ ] Create fresh migration
- [ ] Apply migration
- [ ] Test backend compilation

### Frontend
- [ ] Update React Router routes
- [ ] Update API services
- [ ] Update CartContext
- [ ] Update product links
- [ ] Update profile links
- [ ] Add redirect logic for old URLs
- [ ] Test all navigation flows

---

## 🚀 Estimated Time to Complete

- **Remaining Backend Fixes**: 30-45 minutes
- **Database Migration**: 5 minutes
- **Backend Testing**: 15 minutes
- **Frontend Updates**: 1-2 hours
- **Frontend Testing**: 30 minutes

**Total**: ~3-4 hours

---

## 💡 Key Insights

1. **Pattern Recognition**: All errors follow the same pattern (int → Guid)
2. **Helper Utility**: The `GetUserId()` extension method makes migrations cleaner
3. **Backward Compatibility**: `GetBySlugOrIdAsync()` allows old numeric IDs to still work
4. **Auto-Slug Generation**: Slugs are automatically created/updated in ProductService
5. **Frontend Simplicity**: GUIDs are just strings in JavaScript - no special handling needed

---

## 🔗 Related Files

**Created**:
- `Services/ISlugService.cs`
- `Services/SlugService.cs`
- `Helpers/ClaimsPrincipalExtensions.cs`

**Modified**:
- All model files in `Model/`
- `Services/IProductService.cs`
- `Services/ProductService.cs`
- `Controllers/ItemsController.cs`
- `Controllers/UserController.cs`
- `Controllers/AuthController.cs` (partial)
- `Program.cs`
- All DTO files with user/product references

**To Modify**:
- `Controllers/OrdersController.cs`
- `Controllers/PaymentController.cs`
- `Controllers/BankAccountController.cs`
- `Controllers/ReviewController.cs`
- `Controllers/FollowController.cs`
- `Services/ICartService.cs`
- `Services/CartService.cs`
- `Services/IOrderService.cs`
- `Services/OrderService.cs`
- `Services/IBankAccountService.cs`
- `Services/BankAccountService.cs`
- `Services/PayoutService.cs`

---

## 📞 Next Session Quick Start

```bash
# 1. Navigate to backend
cd "D:\JS Mastery\backend\backend"

# 2. Check current errors
dotnet build 2>&1 | findstr /C:"error"

# 3. Start fixing (use this guide)
# Begin with OrdersController.cs (14 errors)

# 4. After all fixes, compile
dotnet build

# 5. Drop DB and recreate
dotnet ef database drop --force
rmdir /s /q Migrations
dotnet ef migrations add InitialWithGuidAndSlug
dotnet ef database update

# 6. Run backend
dotnet run
```

---

**End of Migration Guide**
Last Updated: November 13, 2025
