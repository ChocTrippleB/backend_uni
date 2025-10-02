namespace backend.Model
{
    public class UserFollower
    {
        public int FollowerId { get; set; }
        public User Follower { get; set; }

        public int FollowedId { get; set; }
        public User Followed { get; set; }
    }
}
