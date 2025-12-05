# UniMarket Backend API 🚀

**Enterprise-Grade ASP.NET Core 8 Web API for Campus Marketplace**

This is the backend API for UniMarket (Campus Swap), a comprehensive student marketplace platform. Built with ASP.NET Core 8, PostgreSQL, and enterprise-level security practices, this API provides robust services for authentication, payments, escrow, batch payouts, reviews, and more.

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Latest-336791.svg)](https://www.postgresql.org/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-512BD4.svg)](https://docs.microsoft.com/en-us/ef/core/)
[![Hangfire](https://img.shields.io/badge/Hangfire-1.8.21-green.svg)](https://www.hangfire.io/)

---

## 🚀 Features

### Core API Services
- **RESTful API Design**: Clean, consistent endpoint structure
- **JWT Authentication**: Stateless authentication with role-based access control
- **Entity Framework Core**: Type-safe database operations with migrations
- **PostgreSQL Database**: Reliable, enterprise-grade RDBMS
- **CORS Support**: Configurable cross-origin resource sharing
- **Swagger Documentation**: Interactive API documentation (development mode)

### Payment & Financial
- **Paystack Integration**: Secure payment processing for ZAR currency
- **Escrow System**: 6-digit release code verification for buyer protection
- **Batch Payout Processing**: Mon/Wed/Fri scheduled transfers (70-90% fee reduction)
- **Bank Account Management**: Paystack recipient verification and management
- **Transfer Automation**: Hangfire background jobs for scheduled payouts
- **Payout Retry Logic**: Automatic retry for failed transfers

### User Management
- **User Registration & Login**: Email verification required
- **Profile Management**: Bio, faculty, course, institution tracking
- **Profile Pictures**: Firebase Storage integration for avatars
- **Role-Based Authorization**: User, Buyer, Admin roles
- **Admin Dashboard**: Statistics, user management, payout oversight

### Marketplace Features
- **Product CRUD**: Create, read, update, delete (soft delete) listings
- **Category Management**: 5 main categories, 11 subcategories
- **Image Upload**: Firebase Storage with size/type validation
- **Search & Filters**: Advanced filtering by name, category, brand, condition
- **SEO-Friendly Slugs**: URL-safe product identifiers

### Reviews & Ratings
- **Multi-Category Reviews**: 4 rating metrics (overall, quality, communication, shipping)
- **Verified Purchases**: Reviews tied to completed orders
- **Seller Rating Cache**: Aggregated statistics for performance
- **Star Distribution**: Analytics on rating breakdown
- **Review Management**: Edit within 7 days, delete capability

### Order Management
- **Order Lifecycle**: 7 status workflow (Pending → Completed)
- **Release Code System**: 6-digit alphanumeric codes for delivery confirmation
- **72-Hour Auto-Release**: Buyer protection mechanism
- **Order Tracking**: Buyer and seller order views
- **Cart Management**: Persistent shopping carts

### Background Processing
- **Hangfire Dashboard**: Monitor jobs, queues, and servers at `/hangfire`
- **Recurring Jobs**: Automated batch payout processing
- **Job Scheduling**: Cron-based scheduling for Mon/Wed/Fri 9 AM
- **Manual Triggers**: Admin override for immediate processing
- **Safety Checks**: Paystack test mode detection

### Email Services
- **Email Confirmation**: Registration verification emails
- **Transactional Emails**: Order confirmations, payout notifications
- **SMTP Integration**: MailKit for reliable email delivery

---

## 🛠️ Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | ASP.NET Core | 8.0 |
| **Language** | C# | 12 |
| **Database** | PostgreSQL | Latest |
| **ORM** | Entity Framework Core | Latest |
| **Authentication** | JWT Bearer | - |
| **Payment Gateway** | Paystack API | REST v1 |
| **File Storage** | Firebase Admin SDK | Latest |
| **Background Jobs** | Hangfire.Core | 1.8.21 |
| **Hangfire Storage** | Hangfire.PostgreSql | 1.20.12 |
| **Email** | MailKit | Latest |
| **HTTP Client** | HttpClient | Built-in |
| **JSON** | System.Text.Json | Built-in |
| **Logging** | ILogger | Built-in |

---

## 📁 Project Structure

```
backend/
├── backend/                          # Main API project
│   ├── Controllers/                  # API endpoints (15 controllers)
│   │   ├── AuthController.cs        # Registration, login, admin creation
│   │   ├── UserController.cs        # Profile management, avatar upload
│   │   ├── ItemController.cs        # Product CRUD operations
│   │   ├── OrderController.cs       # Order creation, tracking
│   │   ├── PaymentController.cs     # Paystack payment processing
│   │   ├── PayoutController.cs      # Batch payout management
│   │   ├── BankAccountController.cs # Bank account CRUD
│   │   ├── ReviewController.cs      # Review creation, updates
│   │   ├── SellerRatingController.cs # Seller statistics
│   │   ├── CartController.cs        # Shopping cart operations
│   │   ├── CartItemController.cs    # Cart item management
│   │   ├── CategoryController.cs    # Category listing
│   │   ├── ImageController.cs       # Image operations
│   │   └── UserFollowerController.cs # Following system (future)
│   │
│   ├── Services/                     # Business logic (13 services)
│   │   ├── OrderService.cs          # Order lifecycle management
│   │   ├── PayoutService.cs         # Batch payout processing
│   │   ├── PaystackService.cs       # Paystack API integration
│   │   ├── BankAccountService.cs    # Bank verification
│   │   ├── ReviewService.cs         # Review creation, validation
│   │   ├── SellerRatingService.cs   # Rating aggregation
│   │   ├── ProductService.cs        # Product management
│   │   ├── CartService.cs           # Cart operations
│   │   ├── ImageService.cs          # Firebase image upload
│   │   ├── SlugService.cs           # SEO slug generation
│   │   ├── EmailService.cs          # Email sending
│   │   ├── UserService.cs           # User management
│   │   └── HangfireAuthorizationFilter.cs # Dashboard security
│   │
│   ├── Model/                        # Database entities (18+ models)
│   │   ├── User.cs                  # User identity & profile
│   │   ├── Product.cs               # Marketplace listings
│   │   ├── Order.cs                 # Transactions
│   │   ├── PayoutQueue.cs           # Batch payout queue
│   │   ├── BankAccount.cs           # Seller payment info
│   │   ├── Review.cs                # Customer reviews
│   │   ├── SellerRating.cs          # Cached rating stats
│   │   ├── Cart.cs / CartItem.cs    # Shopping cart
│   │   ├── Category.cs / SubCategory.cs # Product organization
│   │   ├── Image.cs                 # Product images
│   │   ├── UserFollower.cs          # Social graph
│   │   ├── Role.cs                  # Authorization roles
│   │   └── ... (additional models)
│   │
│   ├── DTO/                          # Data Transfer Objects (23 classes)
│   │   ├── LoginDto.cs              # Login request/response
│   │   ├── RegisterDto.cs           # Registration data
│   │   ├── ProductDto.cs            # Product data
│   │   ├── OrderDto.cs              # Order data
│   │   ├── PaymentDto.cs            # Payment requests
│   │   ├── PayoutDto.cs             # Payout information
│   │   ├── BankAccountDto.cs        # Bank account data
│   │   ├── ReviewDto.cs             # Review data
│   │   └── ... (additional DTOs)
│   │
│   ├── Data/                         # Database context
│   │   └── AppDbContext.cs          # EF Core DbContext
│   │
│   ├── Migrations/                   # Database migrations
│   │   └── ... (20+ migration files)
│   │
│   ├── Helpers/                      # Utility classes
│   │   └── ClaimsPrincipalExtensions.cs # JWT helpers
│   │
│   ├── Attributes/                   # Custom attributes
│   │
│   ├── secrets/                      # Firebase service account
│   │   └── campusswap-*-firebase-adminsdk-*.json
│   │
│   ├── wwwroot/                      # Static files
│   │
│   ├── Program.cs                    # Application entry point
│   ├── appsettings.json              # Configuration (production)
│   ├── appsettings.Development.json  # Configuration (development)
│   ├── backend.csproj                # Project file
│   ├── Dockerfile                    # Production container
│   └── Dockerfile.dev                # Development container
│
├── .gitignore                        # Git ignore rules
├── backend.sln                       # Visual Studio solution
├── BACKEND_SNIPPETS.md               # Code snippets documentation
├── GUID_SLUG_MIGRATION_GUIDE.md      # Migration guide
├── Phase2_Bank_Management_Tests.postman_collection.json
└── README.md                         # This file
```

---

## 🚦 Getting Started

### Prerequisites

- **.NET SDK**: 8.0 or higher ([Download](https://dotnet.microsoft.com/download))
- **PostgreSQL**: 14+ ([Download](https://www.postgresql.org/download/))
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git**: For version control
- **Postman**: For API testing (optional)

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/unimarket-backend.git
cd unimarket-backend/backend
```

2. **Install dependencies**
```bash
dotnet restore
```

3. **Configure Database Connection**

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=unimarket_db;Username=postgres;Password=yourpassword"
  }
}
```

4. **Configure Environment Variables**

Update `appsettings.Development.json` with your keys:
```json
{
  "JWT": {
    "Key": "your-256-bit-secret-key-here",
    "Issuer": "studentmarketplace",
    "Audience": "studentmarket_users"
  },
  "Paystack": {
    "SecretKey": "sk_test_your_test_key",
    "PublicKey": "pk_test_your_public_key",
    "CallbackUrl": "http://localhost:5173/payment/callback"
  },
  "Firebase": {
    "ServiceAccountPath": "secrets/your-firebase-adminsdk.json",
    "StorageBucket": "your-app.appspot.com"
  },
  "Email": {
    "Email": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

5. **Set up Firebase**
- Create Firebase project at [console.firebase.google.com](https://console.firebase.google.com)
- Enable Storage
- Download service account JSON and place in `secrets/` folder
- Update `appsettings.json` with correct path

6. **Run Database Migrations**
```bash
dotnet ef database update
```

This will create all tables with seeded data (roles, categories).

7. **Run the Application**
```bash
dotnet run
```

API will be available at:
- HTTPS: `https://localhost:7255`
- HTTP: `http://localhost:5110`

8. **Access Swagger Documentation**

Navigate to: `https://localhost:7255/swagger`

### Development Commands

```bash
# Run in development mode
dotnet run

# Run with hot reload (watch mode)
dotnet watch run

# Build the project
dotnet build

# Run tests
dotnet test

# Create new migration
dotnet ef migrations add MigrationName

# Update database to latest migration
dotnet ef database update

# Revert to specific migration
dotnet ef database update MigrationName

# Remove last migration
dotnet ef migrations remove

# Generate SQL script for migration
dotnet ef migrations script
```

---

## 🔑 Configuration

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=unimarket_db;Username=postgres;Password=yourpassword"
  },
  "JWT": {
    "Key": "your-secret-key-min-256-bits",
    "Issuer": "studentmarketplace",
    "Audience": "studentmarket_users"
  },
  "Paystack": {
    "SecretKey": "sk_live_your_live_key",
    "PublicKey": "pk_live_your_public_key",
    "CallbackUrl": "https://yourdomain.com/payment/callback",
    "Currency": "ZAR"
  },
  "Firebase": {
    "ServiceAccountPath": "secrets/firebase-adminsdk.json",
    "StorageBucket": "your-app.appspot.com"
  },
  "Email": {
    "Email": "noreply@unimarket.co.za",
    "Password": "app-specific-password"
  },
  "Hangfire": {
    "EnableAutomation": false,
    "EnableDashboard": true,
    "DashboardUsername": "admin",
    "DashboardPassword": "secure-password",
    "PayoutScheduleCron": "0 9 * * 1,3,5"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment-Specific Configuration

**Development** (`appsettings.Development.json`):
- Use Paystack test keys (`sk_test_*`)
- Detailed logging enabled
- Hangfire automation disabled
- CORS allows localhost

**Production** (`appsettings.json`):
- Use Paystack live keys (`sk_live_*`)
- Minimal logging
- Hangfire automation enabled
- CORS restricted to production domains

---

## 🗄️ Database Schema

### Entity Relationship Overview

```
User (GUID)
 ├─── Products (1:Many) - Items listed for sale
 ├─── PurchaseOrders (1:Many) - Orders as buyer
 ├─── SaleOrders (1:Many) - Orders as seller
 ├─── BankAccounts (1:Many) - Payment accounts
 ├─── Reviews (1:Many) - Reviews written
 ├─── SellerRating (1:1) - Cached rating stats
 ├─── Followers (Many:Many) - Users following this user
 └─── Following (Many:Many) - Users this user follows

Order
 ├─── Product (Many:1)
 ├─── Buyer (Many:1) - User
 ├─── Seller (Many:1) - User
 ├─── PayoutQueue (1:1) - Scheduled payout
 └─── Review (1:1) - Optional review

PayoutQueue
 ├─── Order (1:1)
 └─── Seller (Many:1) - User

BankAccount
 └─── User (Many:1)

Review
 ├─── Order (1:1)
 ├─── Buyer (Many:1) - User
 ├─── Seller (Many:1) - User
 └─── Product (Many:1)
```

### Key Tables

**Users** - `Id: Guid (PK)`
- Username, Email, FullName, PasswordHash, Salt
- Bio, ProfilePictureUrl, Faculty, Course, InstitutionId
- PhoneNumber, EmailConfirmed, CreatedAt
- PaystackCustomerId, PaystackRecipientCode

**Products** - `Id: int (PK)`
- Name, Description, Price, Condition, Brand
- CategoryId, SubCategoryId, SellerId (Guid FK)
- Slug, IsSold, SoldAt, IsDeleted
- CreatedAt, UpdatedAt

**Orders** - `Id: int (PK)`
- BuyerId (Guid FK), SellerId (Guid FK), ProductId (int FK)
- Amount, OrderStatus (enum 0-6)
- PaymentReference, ReleaseCode
- ShippingAddress, BuyerPhone, Notes
- CreatedAt, PaidAt, ReleasedAt, ExpiresAt
- FailedReleaseAttempts

**PayoutQueue** - `Id: int (PK)`
- OrderId (int FK), SellerId (Guid FK)
- Amount, SellerRecipientCode
- QueuedAt, ScheduledPayoutDate, ProcessedAt
- PayoutStatus (enum: Pending/Processed/Failed)
- TransferReference, FailureReason

**BankAccounts** - `Id: int (PK)`
- UserId (Guid FK)
- AccountNumber, BankName, BankCode
- AccountHolderName, AccountType
- PaystackRecipientCode
- IsVerified, IsPrimary
- CreatedAt, UpdatedAt

**Reviews** - `Id: int (PK)`
- OrderId (int FK, unique), BuyerId (Guid FK), SellerId (Guid FK), ProductId (int FK)
- OverallRating, ProductQualityRating, CommunicationRating, ShippingSpeedRating (1-5)
- ReviewTitle (200), ReviewText (2000)
- IsVerifiedPurchase, CreatedAt, UpdatedAt

**SellerRatings** - `SellerId: Guid (PK)`
- AverageRating (decimal)
- TotalReviews, TotalSales
- FiveStarCount, FourStarCount, ThreeStarCount, TwoStarCount, OneStarCount
- LastUpdated

### Enums

**OrderStatus**:
```csharp
Pending = 0,        // Order created, awaiting payment
Paid = 1,           // Payment confirmed
AwaitingRelease = 2, // Paid, waiting for buyer code
AwaitingPayout = 3,  // Code verified, queued for payout
Completed = 4,       // Payout processed
Refunded = 5,        // Order refunded
Cancelled = 6        // Order cancelled
```

**PayoutStatus**:
```csharp
Pending = 0,        // Queued for processing
Processed = 1,      // Successfully transferred
Failed = 2          // Transfer failed (can retry)
```

**Roles**:
```csharp
User = 1,
Buyer = 2,
Admin = 10
```

---

## 📡 API Endpoints

### Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/register` | User registration with email verification | Public |
| POST | `/login` | User login (returns JWT token) | Public |
| GET | `/confirm` | Email confirmation endpoint | Public |
| POST | `/bootstrap-admin` | Create first admin user | Public (one-time) |
| POST | `/create-admin` | Create additional admin users | Admin |

**Example: Login**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "student@university.ac.za",
  "password": "SecurePassword123!"
}

Response 200:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "uuid-here",
    "username": "johndoe",
    "email": "student@university.ac.za",
    "fullName": "John Doe",
    "role": 1
  }
}
```

### Users (`/api/user`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/profile` | Get current user profile | User |
| PUT | `/profile` | Update profile (bio, faculty, course) | User |
| POST | `/upload-avatar` | Upload profile picture to Firebase | User |
| GET | `/{userId}` | Get public user profile | Public |
| GET | `/username/{username}` | Get user by username | Public |

### Products (`/api/v1/items`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/items` | List all items (with filters) | Public |
| GET | `/items/filter` | Advanced filtering | Public |
| GET | `/recent` | Get recent listings | Public |
| GET | `/{slug}` | Get product by slug or ID | Public |
| GET | `/seller/{sellerId}` | Get items by seller | Public |
| POST | `/` | Create new listing | User |
| PUT | `/item/{id}` | Update listing | User (owner) |
| DELETE | `/item/{id}` | Soft delete listing | User (owner) |

**Query Parameters**:
- `searchQuery` - Search by name/description
- `category` - Filter by category name
- `condition` - Filter by condition (New/Used)
- `sort` - Sort by price, date
- `page`, `pageSize` - Pagination

### Orders (`/api/orders`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/create` | Create order from cart | User |
| GET | `/buyer` | Get buyer's orders | User |
| GET | `/seller` | Get seller's orders awaiting release | User |
| POST | `/verify-release-code` | Seller verifies 6-digit code | User |
| GET | `/{orderId}/status` | Get order status | User |
| GET | `/{orderId}` | Get order details | User (buyer/seller) |

**Example: Create Order**
```http
POST /api/orders/create
Authorization: Bearer {token}
Content-Type: application/json

{
  "productId": 5,
  "amount": 1250.00,
  "shippingAddress": "123 Campus Rd, Room 4B",
  "buyerPhone": "+27123456789"
}
```

### Payment (`/api/payment`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/initialize` | Initialize Paystack payment | User |
| POST | `/verify` | Verify payment & get release code | User |
| POST | `/webhook` | Paystack webhook endpoint | Public (Paystack) |

**Example: Initialize Payment**
```http
POST /api/payment/initialize
Content-Type: application/json

{
  "orderId": 15,
  "email": "buyer@university.ac.za",
  "amount": 1250.00
}

Response 200:
{
  "authorizationUrl": "https://checkout.paystack.com/...",
  "accessCode": "abc123xyz",
  "reference": "ORD-15-1234567890"
}
```

### Payouts (`/api/payout`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/my-payouts` | Seller payout history | User |
| GET | `/stats` | Payout statistics | Admin |
| GET | `/pending/{date}` | View pending payouts for date | Admin |
| POST | `/process` | Trigger batch payout processing | Admin |
| POST | `/{id}/retry` | Retry failed payout | Admin |

### Bank Accounts (`/api/bankaccount`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/` | Add bank account (Paystack verification) | User |
| GET | `/` | Get all user bank accounts | User |
| GET | `/primary` | Get primary bank account | User |
| GET | `/{id}` | Get specific bank account | User |
| PUT | `/{id}` | Update bank account | User |
| PATCH | `/{id}/set-primary` | Set as primary account | User |
| DELETE | `/{id}` | Delete bank account | User |

### Reviews (`/api/review`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/` | Create review for completed order | User |
| PUT | `/{reviewId}` | Update review (within 7 days) | User (author) |
| DELETE | `/{reviewId}` | Delete review | User (author) |
| GET | `/order/{orderId}` | Get review for specific order | Public |
| GET | `/seller/{sellerId}` | Get all reviews for seller | Public |
| GET | `/product/{productId}` | Get reviews for product | Public |
| GET | `/my-reviews` | Get current user's reviews | User |
| GET | `/can-review/{orderId}` | Check if user can review order | User |

### Seller Ratings (`/api/sellerrating`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/seller/{sellerId}` | Get seller rating statistics | Public |
| GET | `/my-rating` | Get authenticated seller's rating | User |

### Cart (`/api/cart` & `/api/cartitem`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/{userId}` | Get user's cart | User |
| POST | `/add` | Add item to cart | User |
| PUT | `/update-quantity` | Update cart item quantity | User |
| DELETE | `/remove/{cartItemId}` | Remove item from cart | User |
| DELETE | `/clear/{userId}` | Clear entire cart | User |

### Categories (`/api/categories`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/` | List all categories | Public |
| GET | `/{id}` | Get category with subcategories | Public |
| GET | `/{categoryId}/subcategories` | Get subcategories for category | Public |

---

## 🔐 Authentication & Authorization

### JWT Token Structure

**Header**:
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload**:
```json
{
  "sub": "user-guid-here",
  "email": "user@university.ac.za",
  "username": "johndoe",
  "role": "User",
  "exp": 1234567890,
  "iss": "studentmarketplace",
  "aud": "studentmarket_users"
}
```

### Using JWT in Requests

Include in `Authorization` header:
```http
GET /api/user/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Role-Based Access

**Controllers decorated with**:
```csharp
[Authorize] // Requires any authenticated user
[Authorize(Roles = "Admin")] // Requires Admin role
```

**Roles**:
- `User (1)` - Default role for all registered users
- `Buyer (2)` - Can make purchases (same as User currently)
- `Admin (10)` - Full access to admin endpoints

### Extracting User ID from Claims

```csharp
var userId = User.GetUserId(); // Extension method
```

---

## 💳 Payment Integration

### Paystack Flow

**1. Initialize Payment**
```csharp
POST /api/payment/initialize
{
  "orderId": 15,
  "email": "buyer@university.ac.za",
  "amount": 1250.00
}

Returns:
{
  "authorizationUrl": "https://checkout.paystack.com/abc123",
  "accessCode": "abc123",
  "reference": "ORD-15-1234567890"
}
```

**2. Frontend Opens Paystack Popup**
- User enters card details
- 3D Secure verification (if required)
- Paystack redirects to callback URL

**3. Verify Payment**
```csharp
POST /api/payment/verify
{
  "paymentReference": "ORD-15-1234567890"
}

Returns:
{
  "releaseCode": "A7B3C9",
  "amount": 1250.00,
  "status": "success"
}
```

**4. Order Status Updates**
- Order status → `Paid`
- 6-digit release code generated
- Cart cleared
- Buyer receives code

### Paystack Service Methods

```csharp
// Initialize payment
var result = await _paystackService.InitializePaymentAsync(email, amount, reference);

// Verify payment
var verification = await _paystackService.VerifyPaymentAsync(reference);

// Create transfer recipient (bank account)
var recipientCode = await _paystackService.CreateTransferRecipientAsync(
    accountNumber, bankCode, accountHolderName
);

// Initiate transfer (payout)
var transfer = await _paystackService.InitiateTransferAsync(
    recipientCode, amount, reference
);
```

---

## 🔄 Batch Payout System

### Overview

Payouts are processed in batches every **Monday, Wednesday, Friday at 9:00 AM SAST** to reduce transfer fees by 70-90%.

### Workflow

**1. Seller Verifies Release Code**
```csharp
POST /api/orders/verify-release-code
{
  "orderId": 15,
  "releaseCode": "A7B3C9"
}

→ Creates PayoutQueue entry
→ Order status: AwaitingPayout
→ Scheduled for next payout date
```

**2. Hangfire Recurring Job**
```csharp
// Program.cs
RecurringJob.AddOrUpdate<IPayoutService>(
    "batch-payout-processor",
    service => service.ProcessPendingPayoutsAsync(),
    "0 9 * * 1,3,5", // Mon/Wed/Fri 9 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time") }
);
```

**3. Batch Processing Logic**
```csharp
public async Task<(int successCount, int failureCount, List<string> errors)> ProcessPendingPayoutsAsync()
{
    // 1. Fetch pending payouts scheduled for today or earlier
    var pendingPayouts = await _context.PayoutQueue
        .Where(p => p.PayoutStatus == PayoutStatus.Pending && p.ScheduledPayoutDate <= DateTime.UtcNow)
        .ToListAsync();

    // 2. Group by seller (batch multiple orders)
    var payoutsBySeller = pendingPayouts.GroupBy(p => p.SellerId);

    // 3. Process each seller batch
    foreach (var sellerGroup in payoutsBySeller)
    {
        var totalAmount = sellerGroup.Sum(p => p.Amount);
        var firstPayout = sellerGroup.First();

        // 4. Initiate Paystack transfer
        var result = await _paystackService.InitiateTransferAsync(
            firstPayout.SellerRecipientCode,
            totalAmount,
            $"PAYOUT-{firstPayout.Id}-{DateTime.UtcNow.Ticks}"
        );

        // 5. Update payout statuses
        if (result.Success)
        {
            foreach (var payout in sellerGroup)
            {
                payout.PayoutStatus = PayoutStatus.Processed;
                payout.ProcessedAt = DateTime.UtcNow;
                payout.TransferReference = result.TransferReference;

                // Update order status
                var order = await _context.Orders.FindAsync(payout.OrderId);
                order.OrderStatus = OrderStatus.Completed;
            }
        }
        else
        {
            // Mark as failed
            foreach (var payout in sellerGroup)
            {
                payout.PayoutStatus = PayoutStatus.Failed;
                payout.FailureReason = result.ErrorMessage;
            }
        }
    }

    await _context.SaveChangesAsync();
}
```

### Payout Schedule

| Day Sale Made | Payout Date | Wait Time |
|---------------|-------------|-----------|
| Saturday      | Monday 9 AM | 2 days    |
| Sunday        | Monday 9 AM | 1 day     |
| Monday        | Wednesday 9 AM | 2 days |
| Tuesday       | Wednesday 9 AM | 1 day  |
| Wednesday     | Friday 9 AM | 2 days    |
| Thursday      | Friday 9 AM | 1 day     |
| Friday        | Monday 9 AM | 3 days    |

**Average Wait**: 1.7 days

### Manual Processing

Admins can trigger batch processing manually:
```http
POST /api/payout/process
Authorization: Bearer {admin-token}
```

### Failed Payout Retry

```http
POST /api/payout/{payoutId}/retry
Authorization: Bearer {admin-token}
```

---

## 📊 Hangfire Dashboard

### Accessing the Dashboard

Navigate to: `https://localhost:7255/hangfire`

**Authentication**:
- Username: Configured in `appsettings.json` (`DashboardUsername`)
- Password: Configured in `appsettings.json` (`DashboardPassword`)

### Dashboard Features

- **Jobs**: View all background jobs (enqueued, scheduled, processing, succeeded, failed)
- **Recurring Jobs**: Manage recurring job schedules
- **Servers**: Monitor Hangfire worker servers
- **Batches**: Track batch job execution
- **Retries**: View and retry failed jobs

### Configuration

```json
{
  "Hangfire": {
    "EnableAutomation": false,    // Set true in production
    "EnableDashboard": true,      // Set false to disable dashboard
    "DashboardUsername": "admin",
    "DashboardPassword": "SecurePass123!",
    "PayoutScheduleCron": "0 9 * * 1,3,5" // Mon/Wed/Fri 9 AM
  }
}
```

**Safety Feature**: Hangfire will skip batch processing if Paystack test keys are detected (`sk_test_*`).

---

## 🧪 Testing

### Postman Collections

Located in project root:
- `Phase2_Bank_Management_Tests.postman_collection.json` - Bank account & admin tests

Frontend repository also contains:
- `docs/Postman/UniMarket_Complete_API.postman_collection.json` - Full API suite
- `docs/Postman/Review_System_API.postman_collection.json` - Review endpoints

### Manual Testing Workflow

**1. Register & Login**
```http
POST /api/auth/register → Confirm email
POST /api/auth/login → Get JWT token
```

**2. Create Product Listing**
```http
POST /api/v1/items (with images as multipart/form-data)
```

**3. Add to Cart & Checkout**
```http
POST /api/cartitem/add
POST /api/orders/create
```

**4. Initialize Payment**
```http
POST /api/payment/initialize → Get access code
```

**5. Verify Payment** (simulate Paystack success)
```http
POST /api/payment/verify → Get release code
```

**6. Verify Release Code** (as seller)
```http
POST /api/orders/verify-release-code
```

**7. Check Payout Queue**
```http
GET /api/payout/my-payouts
```

**8. Process Payouts** (as admin)
```http
POST /api/auth/bootstrap-admin (first time only)
POST /api/payout/process
```

---

## 🚀 Deployment

### Production Deployment Checklist

- [ ] **Update Configuration**
  - [ ] Change Paystack keys from test (`sk_test_*`) to live (`sk_live_*`)
  - [ ] Update JWT secret key (256-bit minimum)
  - [ ] Set production database connection string
  - [ ] Update CORS allowed origins
  - [ ] Set `EnableAutomation: true` in Hangfire config
  - [ ] Update callback URLs to production domain

- [ ] **Database**
  - [ ] Run all migrations on production database
  - [ ] Verify seeded data (roles, categories)
  - [ ] Set up daily backups
  - [ ] Configure point-in-time recovery

- [ ] **Security**
  - [ ] Enable HTTPS only (HSTS headers)
  - [ ] Configure firewall rules
  - [ ] Set up database connection encryption
  - [ ] Secure Hangfire dashboard (strong password or disable)
  - [ ] Review CORS policy

- [ ] **Email**
  - [ ] Configure production SMTP settings
  - [ ] Test email delivery
  - [ ] Set up email logging

- [ ] **Firebase**
  - [ ] Update service account JSON for production
  - [ ] Configure Firebase Storage security rules
  - [ ] Enable Storage CORS

- [ ] **Monitoring**
  - [ ] Set up application logging (Serilog/Elasticsearch)
  - [ ] Configure error tracking (Application Insights/Sentry)
  - [ ] Monitor Hangfire dashboard regularly
  - [ ] Set up alerts for failed payouts

### Docker Deployment

**Production Dockerfile** included:
```bash
docker build -t unimarket-backend .
docker run -p 5000:8080 -e ASPNETCORE_ENVIRONMENT=Production unimarket-backend
```

**Development Dockerfile**:
```bash
docker build -f Dockerfile.dev -t unimarket-backend-dev .
docker run -p 5000:8080 unimarket-backend-dev
```

### Cloud Deployment (Azure/AWS)

**Azure App Service**:
1. Create App Service (Linux, .NET 8)
2. Configure environment variables
3. Enable Always On
4. Set up deployment from Git

**AWS Elastic Beanstalk**:
1. Create Beanstalk environment (.NET Core)
2. Upload published application
3. Configure environment properties
4. Set up RDS PostgreSQL instance

---

## 🔒 Security Considerations

### Implemented Security Features

- **JWT Authentication**: HS256 signing, 24-hour expiration
- **Password Hashing**: Salted hashing (bcrypt-level strength)
- **Role-Based Authorization**: User, Buyer, Admin roles
- **SQL Injection Prevention**: EF Core parameterized queries
- **CORS Configuration**: Restricted origins
- **HTTPS Enforcement**: HTTP → HTTPS redirect
- **Input Validation**: DTO DataAnnotations
- **File Upload Validation**: Type whitelist (JPEG, PNG, WebP), size limits (5MB)
- **Audit Logging**: Key events logged (ILogger)

### Production Security Recommendations

1. **Use Environment Variables** for secrets (not appsettings.json)
2. **Enable Rate Limiting** on endpoints (prevent brute force)
3. **Implement IP Whitelisting** for admin endpoints
4. **Set up Web Application Firewall** (Cloudflare/AWS WAF)
5. **Regular Security Audits** & penetration testing
6. **Keep Dependencies Updated** (`dotnet outdated`)
7. **Monitor Logs** for suspicious activity
8. **Encrypt Database Connection** (SSL mode)
9. **Use Azure Key Vault** or AWS Secrets Manager for secrets
10. **Enable Two-Factor Authentication** for admin accounts (future)

---

## 📚 Additional Documentation

### Code Documentation

- **BACKEND_SNIPPETS.md** - Code snippets and examples
- **GUID_SLUG_MIGRATION_GUIDE.md** - User ID migration guide

### Frontend Documentation

See frontend repository at `D:\JS Mastery\unimarket`:
- `docs/MD/INVESTOR_PITCH_DECK.md` - Comprehensive platform overview
- `docs/MD/BATCH_PAYOUT_SYSTEM.md` - Payout system details
- `docs/MD/PAYSTACK_IMPLEMENTATION_COMPLETE.md` - Payment integration
- `docs/MD/REVIEW_SYSTEM_IMPLEMENTATION_COMPLETE.md` - Review system
- `docs/MD/PHASE3_HANGFIRE_GUIDE.md` - Hangfire setup

---

## 🤝 Contributing

This is a private university project. For collaboration inquiries, contact the project maintainers.

### Development Guidelines

1. Follow C# naming conventions (PascalCase for public members)
2. Use async/await for all I/O operations
3. Add XML documentation comments for public APIs
4. Write unit tests for service layer
5. Create migrations for database schema changes
6. Update API documentation when adding endpoints
7. Use DTOs for all API requests/responses (never expose entities directly)

---

## 📄 License

Copyright © 2025 UniMarket. All rights reserved.

This project is proprietary software developed for [University Name]. Unauthorized copying, distribution, or modification is prohibited.

---

## 📞 Contact & Support

**For Technical Support**:
- Email: dev@unimarket.co.za
- GitHub Issues: [Link to issues]

**For Investment Inquiries**:
- Email: invest@unimarket.co.za

---

## 🙏 Acknowledgments

- **Microsoft** - ASP.NET Core framework
- **Npgsql** - PostgreSQL provider for .NET
- **Paystack** - Payment processing API
- **Firebase** - Cloud storage infrastructure
- **Hangfire** - Background job processing
- **MailKit** - Email delivery

---

**UniMarket Backend: Powering the Student Economy**

*Built with 🔧 for scalability, security, and performance*

---

**Version**: 1.0.0
**Last Updated**: November 27, 2025
**Status**: Production Ready 🚀
