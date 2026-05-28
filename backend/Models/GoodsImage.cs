using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSecondHand.API.Models
{
    // 商品图片实体
    [Table("goods_image")]
    public class GoodsImage
    {
        [Key]
        [Column("image_id")]
        public long ImageId { get; set; }

        [Required]
        [Column("goods_id")]
        public long GoodsId { get; set; } // 关联商品id

        [Required]
        [Column("image_url")]
        [StringLength(255)]
        public string ImageUrl { get; set; } // 图片url
    }
}
