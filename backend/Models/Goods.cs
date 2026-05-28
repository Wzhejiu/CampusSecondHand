using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSecondHand.API.Models
{
    // 商品实体
    [Table("goods")]
    public class Goods
    {
        [Key]
        [Column("goods_id")]
        public long GoodsId { get; set; }

        [Required]
        [Column("user_id")]
        public long UserId { get; set; } // 发布者id

        [Required]
        [Column("category_id")]
        public long CategoryId { get; set; } // 分类id

        [Required]
        [Column("title")]
        [StringLength(100)]
        public string Title { get; set; } // 商品名称

        [Column("description")]
        public string? Description { get; set; } // 商品描述

        [Required]
        [Column("price")]
        public decimal Price { get; set; } // 价格

        [Column("audit_status")]
        public int AuditStatus { get; set; } = 0; // 0 = 待审核, 1 = 通过, 2 = 驳回

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [Column("update_time")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
