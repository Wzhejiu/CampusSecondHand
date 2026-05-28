using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSecondHand.API.Models
{
    // 商品分类实体
    [Table("category")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public long CategoryId { get; set; }

        [Required]
        [Column("category_name")]
        [StringLength(50)]
        public string CategoryName { get; set; }

        [Column("parent_id")]
        public long? ParentId { get; set; } // NULL 表示顶级分类
    }
}
