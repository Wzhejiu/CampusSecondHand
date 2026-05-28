using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CampusSecondHand.API.Filters;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoodsController : ControllerBase
    {
        private readonly string _connStr;

        public GoodsController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Missing connection string 'Default'");
        }

        // ==================== 发布商品 ====================

        // 发布商品（需登录）
        [HttpPost]
        [AuthFilter]
        public IActionResult Create([FromBody] CreateGoodsRequest req)
        {
            try
            {
                long userId = Convert.ToInt64(HttpContext.Items["UserId"]);

                // 参数校验
                if (string.IsNullOrWhiteSpace(req.Title) || req.Title.Length > 100)
                {
                    return Ok(new ApiResponse { Success = false, Message = "商品标题不能为空且不超过100字符" });
                }
                if (req.Price <= 0)
                {
                    return Ok(new ApiResponse { Success = false, Message = "价格必须大于0" });
                }
                if (req.CategoryId <= 0)
                {
                    return Ok(new ApiResponse { Success = false, Message = "请选择商品分类" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 校验分类是否存在
                    string catSql = "SELECT COUNT(*) FROM `category` WHERE category_id = @CatId";
                    using (var cmd = new MySqlCommand(catSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CatId", req.CategoryId);
                        int catCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (catCount == 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "所选分类不存在" });
                        }
                    }

                    // 插入商品（audit_status 默认为 0=待审核）
                    string insertSql = @"INSERT INTO `goods` (user_id, category_id, title, description, price, audit_status, create_time)
                                         VALUES (@UserId, @CategoryId, @Title, @Description, @Price, 0, @CreateTime);
                                         SELECT LAST_INSERT_ID();";

                    long goodsId;
                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@CategoryId", req.CategoryId);
                        cmd.Parameters.AddWithValue("@Title", req.Title.Trim());
                        cmd.Parameters.AddWithValue("@Description",
                            string.IsNullOrWhiteSpace(req.Description) ? (object)DBNull.Value : req.Description.Trim());
                        cmd.Parameters.AddWithValue("@Price", req.Price);
                        cmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);

                        goodsId = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    // 插入图片关联
                    if (req.ImageUrls != null && req.ImageUrls.Count > 0)
                    {
                        foreach (var url in req.ImageUrls)
                        {
                            if (string.IsNullOrWhiteSpace(url)) continue;

                            string imgSql = "INSERT INTO `goods_image` (goods_id, image_url) VALUES (@GoodsId, @Url)";
                            using (var cmd = new MySqlCommand(imgSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@GoodsId", goodsId);
                                cmd.Parameters.AddWithValue("@Url", url.Trim());
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "发布成功，等待管理员审核",
                        Data = new { goodsId = goodsId }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "发布商品失败：" + ex.Message });
            }
        }

        // ==================== 商品列表（公开） ====================

        // 商品列表（分页 + 分类筛选 + 关键词搜索，仅展示已审核通过的商品）
        [HttpGet]
        public IActionResult GetList(
            [FromQuery] long? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;
                int offset = (page - 1) * pageSize;

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 构建筛选条件：仅展示审核通过的商品
                    string whereClause = " WHERE g.audit_status = 1";
                    if (categoryId.HasValue && categoryId.Value > 0)
                    {
                        whereClause += " AND g.category_id = @CategoryId";
                    }
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        whereClause += " AND g.title LIKE @Keyword";
                    }

                    // 查询总数
                    string countSql = $"SELECT COUNT(*) FROM `goods` g{whereClause}";
                    int totalCount;
                    using (var cmd = new MySqlCommand(countSql, conn))
                    {
                        if (categoryId.HasValue && categoryId.Value > 0)
                            cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                        if (!string.IsNullOrWhiteSpace(keyword))
                            cmd.Parameters.AddWithValue("@Keyword", $"%{keyword.Trim()}%");
                        totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 查询商品列表
                    string listSql = $@"SELECT g.goods_id, g.user_id, g.category_id, g.title, g.price, g.create_time,
                                               u.username AS publisher_name,
                                               c.category_name,
                                               (SELECT image_url FROM `goods_image` WHERE goods_id = g.goods_id LIMIT 1) AS cover_image
                                        FROM `goods` g
                                        JOIN `user` u ON g.user_id = u.user_id
                                        JOIN `category` c ON g.category_id = c.category_id
                                        {whereClause}
                                        ORDER BY g.create_time DESC
                                        LIMIT @Offset, @PageSize";

                    var list = new List<object>();
                    using (var cmd = new MySqlCommand(listSql, conn))
                    {
                        if (categoryId.HasValue && categoryId.Value > 0)
                            cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                        if (!string.IsNullOrWhiteSpace(keyword))
                            cmd.Parameters.AddWithValue("@Keyword", $"%{keyword.Trim()}%");
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    goodsId = reader["goods_id"],
                                    userId = reader["user_id"],
                                    categoryId = reader["category_id"],
                                    title = reader["title"].ToString(),
                                    price = reader["price"],
                                    publisherName = reader["publisher_name"].ToString(),
                                    categoryName = reader["category_name"].ToString(),
                                    coverImage = reader["cover_image"] == DBNull.Value ? null : reader["cover_image"].ToString(),
                                    createTime = reader["create_time"]
                                });
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取商品列表成功",
                        Data = new
                        {
                            list = list,
                            total = totalCount,
                            page = page,
                            pageSize = pageSize,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "获取商品列表失败：" + ex.Message });
            }
        }

        // 我发布的商品列表（需登录）
        [HttpGet("my")]
        [AuthFilter]
        public IActionResult GetMyGoods(
            [FromQuery] int? auditStatus,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                long currentUserId = Convert.ToInt64(HttpContext.Items["UserId"]);

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;
                int offset = (page - 1) * pageSize;

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    string whereClause = " WHERE g.user_id = @UserId";
                    if (auditStatus.HasValue && (auditStatus.Value == 0 || auditStatus.Value == 1 || auditStatus.Value == 2))
                    {
                        whereClause += " AND g.audit_status = @AuditStatus";
                    }

                    string countSql = $"SELECT COUNT(*) FROM `goods` g{whereClause}";
                    int totalCount;
                    using (var cmd = new MySqlCommand(countSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);
                        if (auditStatus.HasValue && (auditStatus.Value == 0 || auditStatus.Value == 1 || auditStatus.Value == 2))
                            cmd.Parameters.AddWithValue("@AuditStatus", auditStatus.Value);
                        totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    string listSql = $@"SELECT g.goods_id, g.title, g.price, g.audit_status, g.create_time, g.update_time,
                                               c.category_name,
                                               (SELECT image_url FROM `goods_image` WHERE goods_id = g.goods_id LIMIT 1) AS cover_image
                                        FROM `goods` g
                                        JOIN `category` c ON g.category_id = c.category_id
                                        {whereClause}
                                        ORDER BY g.create_time DESC
                                        LIMIT @Offset, @PageSize";

                    var list = new List<object>();
                    using (var cmd = new MySqlCommand(listSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);
                        if (auditStatus.HasValue && (auditStatus.Value == 0 || auditStatus.Value == 1 || auditStatus.Value == 2))
                            cmd.Parameters.AddWithValue("@AuditStatus", auditStatus.Value);
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    goodsId = reader["goods_id"],
                                    title = reader["title"].ToString(),
                                    price = reader["price"],
                                    auditStatus = reader["audit_status"],
                                    categoryName = reader["category_name"].ToString(),
                                    coverImage = reader["cover_image"] == DBNull.Value ? null : reader["cover_image"].ToString(),
                                    createTime = reader["create_time"],
                                    updateTime = reader["update_time"]
                                });
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取我的商品列表成功",
                        Data = new
                        {
                            list = list,
                            total = totalCount,
                            page = page,
                            pageSize = pageSize,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "获取我的商品列表失败：" + ex.Message });
            }
        }

        // ==================== 商品详情 ====================

        // 商品详情（公开，含图片列表和发布者信息）
        [HttpGet("{id}")]
        public IActionResult GetDetail(long id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    string goodsSql = @"SELECT g.goods_id, g.user_id, g.category_id, g.title, g.description,
                                               g.price, g.audit_status, g.create_time, g.update_time,
                                               u.username AS publisher_name, u.phone AS publisher_phone,
                                               c.category_name
                                        FROM `goods` g
                                        JOIN `user` u ON g.user_id = u.user_id
                                        JOIN `category` c ON g.category_id = c.category_id
                                        WHERE g.goods_id = @Id";

                    string title, description, publisherName, publisherPhone, categoryName;
                    long userId, categoryId;
                    decimal price;
                    int auditStatus;
                    DateTime createTime, updateTime;

                    using (var cmd = new MySqlCommand(goodsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品不存在" });
                            }

                            title = reader["title"].ToString();
                            description = reader["description"] == DBNull.Value ? null : reader["description"].ToString();
                            price = Convert.ToDecimal(reader["price"]);
                            auditStatus = Convert.ToInt32(reader["audit_status"]);
                            userId = Convert.ToInt64(reader["user_id"]);
                            categoryId = Convert.ToInt64(reader["category_id"]);
                            publisherName = reader["publisher_name"].ToString();
                            publisherPhone = reader["publisher_phone"] == DBNull.Value ? null : reader["publisher_phone"].ToString();
                            categoryName = reader["category_name"].ToString();
                            createTime = Convert.ToDateTime(reader["create_time"]);
                            updateTime = Convert.ToDateTime(reader["update_time"]);
                        }
                    }

                    // 审核未通过的商品，只有发布者和管理员能看详情
                    if (auditStatus != 1)
                    {
                        object userIdObj = HttpContext.Items["UserId"];
                        if (userIdObj == null)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "商品不存在或已下架" });
                        }
                        long currentUserId = Convert.ToInt64(userIdObj);
                        object roleObj = HttpContext.Items["Role"];
                        int currentRole = roleObj != null ? Convert.ToInt32(roleObj) : 0;

                        if (currentUserId != userId && currentRole != 1)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "商品不存在或已下架" });
                        }
                    }

                    // 查询商品图片列表
                    var images = new List<string>();
                    string imgSql = "SELECT image_url FROM `goods_image` WHERE goods_id = @GoodsId";
                    using (var cmd = new MySqlCommand(imgSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@GoodsId", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                images.Add(reader["image_url"].ToString());
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取商品详情成功",
                        Data = new
                        {
                            goodsId = id,
                            userId = userId,
                            categoryId = categoryId,
                            title = title,
                            description = description,
                            price = price,
                            auditStatus = auditStatus,
                            publisherName = publisherName,
                            publisherPhone = publisherPhone,
                            categoryName = categoryName,
                            images = images,
                            createTime = createTime,
                            updateTime = updateTime
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "获取商品详情失败：" + ex.Message });
            }
        }

        // ==================== 编辑商品 ====================

        // 编辑商品（需登录，仅发布者可操作，编辑后重新进入待审核状态）
        [HttpPut("{id}")]
        [AuthFilter]
        public IActionResult Update(long id, [FromBody] UpdateGoodsRequest req)
        {
            try
            {
                long currentUserId = Convert.ToInt64(HttpContext.Items["UserId"]);

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 校验商品存在且属于当前用户
                    string checkSql = "SELECT goods_id, user_id, audit_status FROM `goods` WHERE goods_id = @Id";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品不存在" });
                            }
                            if (Convert.ToInt64(reader["user_id"]) != currentUserId)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "只能修改自己发布的商品" });
                            }
                        }
                    }

                    // 动态构建更新 SQL（只更新传入的非空字段）
                    var setParts = new List<string>();
                    var cmd2 = new MySqlCommand();
                    cmd2.Connection = conn;

                    if (req.Title != null)
                    {
                        if (string.IsNullOrWhiteSpace(req.Title) || req.Title.Length > 100)
                            return Ok(new ApiResponse { Success = false, Message = "标题不能为空且不超过100字符" });
                        setParts.Add("title = @Title");
                        cmd2.Parameters.AddWithValue("@Title", req.Title.Trim());
                    }
                    if (req.Description != null)
                    {
                        setParts.Add("description = @Description");
                        cmd2.Parameters.AddWithValue("@Description",
                            string.IsNullOrWhiteSpace(req.Description) ? (object)DBNull.Value : req.Description.Trim());
                    }
                    if (req.Price.HasValue)
                    {
                        if (req.Price.Value <= 0)
                            return Ok(new ApiResponse { Success = false, Message = "价格必须大于0" });
                        setParts.Add("price = @Price");
                        cmd2.Parameters.AddWithValue("@Price", req.Price.Value);
                    }
                    if (req.CategoryId.HasValue)
                    {
                        if (req.CategoryId.Value <= 0)
                            return Ok(new ApiResponse { Success = false, Message = "无效的分类ID" });

                        string catSql = "SELECT COUNT(*) FROM `category` WHERE category_id = @CatId";
                        using (var catCmd = new MySqlCommand(catSql, conn))
                        {
                            catCmd.Parameters.AddWithValue("@CatId", req.CategoryId.Value);
                            if (Convert.ToInt32(catCmd.ExecuteScalar()) == 0)
                                return Ok(new ApiResponse { Success = false, Message = "所选分类不存在" });
                        }

                        setParts.Add("category_id = @CategoryId");
                        cmd2.Parameters.AddWithValue("@CategoryId", req.CategoryId.Value);
                    }

                    if (setParts.Count == 0 && (req.ImageUrls == null))
                    {
                        return Ok(new ApiResponse { Success = false, Message = "没有需要更新的内容" });
                    }

                    // 编辑后重新进入待审核状态
                    if (setParts.Count > 0)
                    {
                        setParts.Add("audit_status = 0");
                        setParts.Add("update_time = NOW()");

                        string updateSql = $"UPDATE `goods` SET {string.Join(", ", setParts)} WHERE goods_id = @Id";
                        cmd2.CommandText = updateSql;
                        cmd2.Parameters.AddWithValue("@Id", id);
                        cmd2.ExecuteNonQuery();
                    }

                    // 如果传入了新的图片列表，先删旧图再插新图
                    if (req.ImageUrls != null)
                    {
                        string delImgSql = "DELETE FROM `goods_image` WHERE goods_id = @GoodsId";
                        using (var delCmd = new MySqlCommand(delImgSql, conn))
                        {
                            delCmd.Parameters.AddWithValue("@GoodsId", id);
                            delCmd.ExecuteNonQuery();
                        }

                        foreach (var url in req.ImageUrls)
                        {
                            if (string.IsNullOrWhiteSpace(url)) continue;
                            string insImgSql = "INSERT INTO `goods_image` (goods_id, image_url) VALUES (@GoodsId, @Url)";
                            using (var insCmd = new MySqlCommand(insImgSql, conn))
                            {
                                insCmd.Parameters.AddWithValue("@GoodsId", id);
                                insCmd.Parameters.AddWithValue("@Url", url.Trim());
                                insCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new ApiResponse { Success = true, Message = "修改成功，需重新等待审核" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "修改商品失败：" + ex.Message });
            }
        }

        // ==================== 删除商品 ====================

        // 删除商品（需登录，发布者或管理员可删，goods_image 由数据库 CASCADE 自动清理）
        [HttpDelete("{id}")]
        [AuthFilter]
        public IActionResult Delete(long id)
        {
            try
            {
                long currentUserId = Convert.ToInt64(HttpContext.Items["UserId"]);
                int currentRole = Convert.ToInt32(HttpContext.Items["Role"]);

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 校验商品存在
                    string checkSql = "SELECT goods_id, user_id, title FROM `goods` WHERE goods_id = @Id";
                    long ownerId;
                    string goodsTitle = "";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品不存在" });
                            }
                            ownerId = Convert.ToInt64(reader["user_id"]);
                            goodsTitle = reader["title"].ToString();
                        }
                    }

                    // 权限校验：发布者或管理员可删
                    if (currentUserId != ownerId && currentRole != 1)
                    {
                        return Ok(new ApiResponse { Success = false, Message = "只能删除自己发布的商品" });
                    }

                    // 检查是否有进行中的交易
                    string tradeSql = "SELECT COUNT(*) FROM `trade` WHERE goods_id = @GoodsId AND status = 0";
                    using (var cmd = new MySqlCommand(tradeSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@GoodsId", id);
                        int activeTradeCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (activeTradeCount > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "该商品有进行中的交易，请先处理后再删除" });
                        }
                    }

                    // 删除商品（goods_image 表设置了 ON DELETE CASCADE，自动级联删除）
                    string deleteSql = "DELETE FROM `goods` WHERE goods_id = @Id";
                    using (var cmd = new MySqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                    return Ok(new ApiResponse { Success = true, Message = "商品「" + goodsTitle + "」已删除" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "删除商品失败：" + ex.Message });
            }
        }
    }

    // ========== 请求 DTO ==========

    // 发布商品请求
    public class CreateGoodsRequest
    {
        public string Title { get; set; }              // 商品标题（必填）
        public string Description { get; set; }         // 商品描述（可选）
        public decimal Price { get; set; }              // 价格（必填）
        public long CategoryId { get; set; }            // 分类ID（必填）
        public List<string> ImageUrls { get; set; }     // 图片URL列表（先上传再传URL）
    }

    // 编辑商品请求（所有字段可选，只更新传入的非null字段）
    public class UpdateGoodsRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public long? CategoryId { get; set; }
        public List<string> ImageUrls { get; set; }
    }
}
