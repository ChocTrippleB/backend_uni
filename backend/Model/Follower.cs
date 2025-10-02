using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Model
{
    public class Follower
    {
        public int Id { get; set; }

        public int FollowerId { get; set; }  // Who follows
        [NotMapped]
        public User? FollowerUser { get; set; }

        public int FollowedId { get; set; }  // Who is followed
        [NotMapped]
        public User? FollowedUser { get; set; }

    }
}
