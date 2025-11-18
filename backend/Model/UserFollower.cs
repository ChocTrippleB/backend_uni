namespace backend.Model
{
    public class UserFollower
    {
        public Guid FollowerId { get; set; }
        public User Follower { get; set; }

        public Guid FollowedId { get; set; }
        public User Followed { get; set; }
    }
}
