using ChatServer.Repositories.Attributes;
using System;

namespace ChatServer.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public long id { get; set; }

        public string? name { get; set; }

        public string? user_name { get; set; }

        public string? password { get; set; }

        public string? email { get; set; }

        public string? avatar_url { get; set; }

        public string? phone_number { get; set; }

        public string? address { get; set; }

        public DateTime created_at { get; set; } = DateTime.UtcNow;
        public DateTime last_seen { get; set; } = DateTime.UtcNow;

        public bool is_sso { get; set; }

        public bool is_active { get; set; }

        public string? sub { get; set; }
    }
}
