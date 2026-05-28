using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSecondHand.API.Models
{
    // 交易实体
    [Table("trade")]
    public class Trade
    {
        [Key]
        [Column("trade_id")]
        public long TradeId { get; set; }

        [Required]
        [Column("goods_id")]
        public long GoodsId { get; set; } // 商品id

        [Required]
        [Column("buyer_id")]
        public long BuyerId { get; set; } // 买家id

        [Required]
        [Column("seller_id")]
        public long SellerId { get; set; } // 卖家id

        [Column("meet_time")]
        public DateTime? MeetTime { get; set; } // 预约时间（可空）

        [Column("meet_location")]
        [StringLength(255)]
        public string? MeetLocation { get; set; } // 预约地点（可空）

        [Column("status")]
        public int Status { get; set; } = 0; // 0 = 待确认, 1 = 已完成, 2 = 已取消

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [Column("update_time")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
