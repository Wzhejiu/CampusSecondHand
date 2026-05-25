using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSecondHand.API.Models
{
    [Table("user")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [Column("password")]
        public string Password { get; set; }

        [Column("phone")]
        [StringLength(20)]
        public string Phone { get; set; }

        [Column("role")]
        public int Role { get; set; } = 0; // 0 = 普通用户, 1 = 管理员

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [Column("update_time")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}