using backend.Data;
using backend.DTO;
using backend.Helpers;
using backend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/userf")]
    public class FollowController : Controller
    {
        private readonly AppDbContext _context;
        public FollowController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("follow")]
        public async Task<IActionResult> Follow([FromBody] FollowRequest model)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == null)
                return Unauthorized("User ID not found in token.");

            if (currentUserId.Value == model.FollowedId)
                return BadRequest("You can't follow yourself.");

            // Check if already following
            var existing = await _context.UserFollowers
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId.Value && f.FollowedId == model.FollowedId);

            if (existing != null)
                return BadRequest("Already following.");

            var follow = new UserFollower
            {
                FollowerId = currentUserId.Value,
                FollowedId = model.FollowedId
            };

            _context.UserFollowers.Add(follow);
            await _context.SaveChangesAsync();

            return Ok("Followed successfully.");
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var user = await _context.Users
                .Include(u => u.Followers)
                .Include(u => u.Followed)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            bool isFollowed = await _context.UserFollowers
                .AnyAsync(f => f.FollowerId == userId.Value && f.FollowedId == id);

            var result = new
            {
                user.Id,
                user.FullName,
                user.Username,
                FollowersCount = await _context.UserFollowers.CountAsync(f => f.FollowedId == id),
                FollowingCount = await _context.UserFollowers.CountAsync(f => f.FollowerId == id),
                IsFollowed = isFollowed
            };

            return Ok(result);
        }

        //[HttpPost("unfollow")]
        //public async Task<IActionResult> Unfollow([FromBody] FollowRequest model)
        //{
        //    var follow = await _context.UserFollowers
        //        .FirstOrDefaultAsync(f => f.FollowerId == model.FollowerId && f.FollowedId == model.FollowedId);

        //    if (follow == null)
        //        return NotFound("Not following.");

        //    _context.UserFollowers.Remove(follow);
        //    await _context.SaveChangesAsync();

        //    return Ok("Unfollowed successfully.");
        //}
    }
}
