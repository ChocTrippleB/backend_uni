using backend.Data;
using backend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get admin dashboard statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                // User statistics
                var totalUsers = await _context.Users.CountAsync();
                var usersLast24h = await _context.Users
                    .Where(u => u.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                    .CountAsync();

                // Order statistics
                var totalOrders = await _context.Orders.CountAsync();
                var activeOrders = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Completed)
                    .CountAsync();
                var ordersLast24h = await _context.Orders
                    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                    .CountAsync();

                // Revenue statistics
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.AwaitingPayout)
                    .SumAsync(o => (decimal?)o.Amount) ?? 0;

                var revenueToday = await _context.Orders
                    .Where(o => (o.Status == OrderStatus.Completed || o.Status == OrderStatus.AwaitingPayout)
                        && o.CreatedAt.Date == DateTime.UtcNow.Date)
                    .SumAsync(o => (decimal?)o.Amount) ?? 0;

                // Payout statistics
                var pendingPayouts = await _context.PayoutQueue
                    .Where(p => p.Status == PayoutStatus.Pending)
                    .CountAsync();

                var totalPendingAmount = await _context.PayoutQueue
                    .Where(p => p.Status == PayoutStatus.Pending)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var nextPayoutDate = await _context.PayoutQueue
                    .Where(p => p.Status == PayoutStatus.Pending)
                    .OrderBy(p => p.ScheduledPayoutDate)
                    .Select(p => p.ScheduledPayoutDate)
                    .FirstOrDefaultAsync();

                // Product statistics
                var totalProducts = await _context.Products
                    .Where(p => !p.IsDeleted)
                    .CountAsync();

                var activeProducts = await _context.Products
                    .Where(p => !p.IsDeleted && !p.IsSold)
                    .CountAsync();

                // Review statistics
                var totalReviews = await _context.Reviews.CountAsync();
                var averageRating = await _context.Reviews
                    .AverageAsync(r => (decimal?)r.OverallRating) ?? 0;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        users = new
                        {
                            total = totalUsers,
                            newLast24h = usersLast24h
                        },
                        orders = new
                        {
                            total = totalOrders,
                            active = activeOrders,
                            newLast24h = ordersLast24h
                        },
                        revenue = new
                        {
                            total = totalRevenue,
                            today = revenueToday
                        },
                        payouts = new
                        {
                            pending = pendingPayouts,
                            pendingAmount = totalPendingAmount,
                            nextPayoutDate
                        },
                        products = new
                        {
                            total = totalProducts,
                            active = activeProducts
                        },
                        reviews = new
                        {
                            total = totalReviews,
                            averageRating = Math.Round(averageRating, 2)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard stats");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving dashboard statistics",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get recent activity feed
        /// </summary>
        [HttpGet("activity")]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10)
        {
            try
            {
                var recentOrders = await _context.Orders
                    .Include(o => o.Buyer)
                    .Include(o => o.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(limit)
                    .Select(o => new
                    {
                        type = "order",
                        id = o.Id,
                        description = $"{o.Buyer.Username} purchased {o.Product.Name}",
                        amount = o.Amount,
                        status = o.Status.ToString(),
                        timestamp = o.CreatedAt
                    })
                    .ToListAsync();

                var recentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(limit)
                    .Select(u => new
                    {
                        type = "user",
                        id = u.Id,
                        description = $"New user registered: {u.Username}",
                        amount = (decimal?)null,
                        status = "registered",
                        timestamp = u.CreatedAt
                    })
                    .ToListAsync();

                var recentReviews = await _context.Reviews
                    .Include(r => r.Buyer)
                    .Include(r => r.Product)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .Select(r => new
                    {
                        type = "review",
                        id = r.Id,
                        description = $"{r.Buyer.Username} reviewed {r.Product.Name}",
                        amount = (decimal?)null,
                        status = $"{r.OverallRating} stars",
                        timestamp = r.CreatedAt
                    })
                    .ToListAsync();

                // Combine and sort all activities
                var allActivities = recentOrders
                    .Concat<object>(recentUsers)
                    .Concat(recentReviews)
                    .OrderByDescending(a =>
                    {
                        var prop = a.GetType().GetProperty("timestamp");
                        return (DateTime)(prop?.GetValue(a) ?? DateTime.MinValue);
                    })
                    .Take(limit)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = allActivities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving recent activity",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all users with pagination and search
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Users.Include(u => u.Role).AsQueryable();

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u =>
                        u.Username.Contains(search) ||
                        u.Email.Contains(search) ||
                        u.FullName.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.FullName,
                        u.CreatedAt,
                        u.EmailConfirmed,
                        u.ProfilePictureUrl,
                        Role = u.Role.Name,
                        RoleId = u.RoleId
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        users,
                        totalCount,
                        page,
                        pageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving users",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user role
        /// </summary>
        [HttpPatch("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var role = await _context.Roles.FindAsync(request.RoleId);
                if (role == null)
                {
                    return BadRequest(new { success = false, message = "Invalid role ID" });
                }

                user.RoleId = request.RoleId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} role updated to {RoleId} by admin", userId, request.RoleId);

                return Ok(new
                {
                    success = true,
                    message = $"User role updated to {role.Name}",
                    data = new
                    {
                        userId,
                        newRole = role.Name,
                        newRoleId = role.Id
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating user role",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all available roles
        /// </summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Description
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving roles",
                    error = ex.Message
                });
            }
        }
    }

    public class UpdateRoleRequest
    {
        public int RoleId { get; set; }
    }
}
