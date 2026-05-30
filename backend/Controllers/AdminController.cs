using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CampusSecondHand.API.Filters;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly string _connStr;

        public AdminController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Missing connection string 'Default'");
        }

        // ==================== 商品审核 ====================

        // 待审核商品列表（分页，按提交时间升序——最早提交的优先审核）
        [HttpGet("goods/pending")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult GetPendingGoods([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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

                    // 查询总数
                    string countSql = "SELECT COUNT(*) FROM `goods` WHERE audit_status = 0";
                    int totalCount;
                    using (var cmd = new MySqlCommand(countSql, conn))
                    {
                        totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 查询待审核商品列表，JOIN 用户表和分类表
                    string listSql = @"SELECT g.goods_id, g.user_id, g.category_id, g.title, g.description,
                                              g.price, g.audit_status, g.create_time, g.update_time,
                                              u.username AS publisher_name, u.phone AS publisher_phone,
                                              c.category_name,
                                              (SELECT image_url FROM `goods_image` WHERE goods_id = g.goods_id LIMIT 1) AS cover_image
                                       FROM `goods` g
                                       JOIN `user` u ON g.user_id = u.user_id
                                       JOIN `category` c ON g.category_id = c.category_id
                                       WHERE g.audit_status = 0
                                       ORDER BY g.create_time ASC
                                       LIMIT @Offset, @PageSize";

                    var list = new List<object>();
                    using (var cmd = new MySqlCommand(listSql, conn))
                    {
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
                                    description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                                    price = reader["price"],
                                    auditStatus = reader["audit_status"],
                                    createTime = reader["create_time"],
                                    updateTime = reader["update_time"],
                                    publisherName = reader["publisher_name"].ToString(),
                                    publisherPhone = reader["publisher_phone"] == DBNull.Value ? null : reader["publisher_phone"].ToString(),
                                    categoryName = reader["category_name"].ToString(),
                                    coverImage = reader["cover_image"] == DBNull.Value ? null : reader["cover_image"].ToString()
                                });
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取待审核商品列表成功",
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
                return Ok(new ApiResponse { Success = false, Message = "获取待审核列表失败：" + ex.Message });
            }
        }

        // 审核商品（通过或驳回）
        [HttpPut("goods/{id}/audit")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult AuditGoods(long id, [FromBody] AuditGoodsRequest req)
        {
            try
            {
                // 校验审核状态参数
                if (req.AuditStatus != 1 && req.AuditStatus != 2)
                {
                    return Ok(new ApiResponse { Success = false, Message = "审核状态无效，1=通过, 2=驳回" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 查询商品是否存在且为待审核状态
                    string checkSql = "SELECT goods_id, title, audit_status FROM `goods` WHERE goods_id = @Id";
                    string goodsTitle;
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品不存在" });
                            }

                            int currentStatus = Convert.ToInt32(reader["audit_status"]);
                            if (currentStatus != 0)
                            {
                                string statusText = currentStatus == 1 ? "已通过" : "已驳回";
                                return Ok(new ApiResponse { Success = false, Message = "该商品已审核过，当前状态：" + statusText });
                            }

                            goodsTitle = reader["title"].ToString();
                        }
                    }

                    // 更新审核状态
                    string updateSql = "UPDATE `goods` SET audit_status = @Status, update_time = NOW() WHERE goods_id = @Id";
                    using (var cmd = new MySqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", req.AuditStatus);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }

                    string resultMsg = req.AuditStatus == 1 ? "审核通过" : "已驳回";
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = $"商品「{goodsTitle}」{resultMsg}",
                        Data = new { goodsId = id, auditStatus = req.AuditStatus }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "审核操作失败：" + ex.Message });
            }
        }

        // ==================== 用户管理 ====================

        // 用户列表（分页，支持关键词搜索用户名，不返回密码字段）
        [HttpGet("users")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult GetUsers([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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

                    // 构建搜索条件
                    string whereClause = "";
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        whereClause = " WHERE username LIKE @Keyword";
                    }

                    // 查询总数
                    string countSql = $"SELECT COUNT(*) FROM `user`{whereClause}";
                    int totalCount;
                    using (var cmd = new MySqlCommand(countSql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            cmd.Parameters.AddWithValue("@Keyword", $"%{keyword.Trim()}%");
                        }
                        totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 查询用户列表（不返回密码字段）
                    string listSql = $@"SELECT user_id, username, phone, role, create_time, update_time
                                        FROM `user`{whereClause}
                                        ORDER BY create_time DESC
                                        LIMIT @Offset, @PageSize";

                    var list = new List<object>();
                    using (var cmd = new MySqlCommand(listSql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            cmd.Parameters.AddWithValue("@Keyword", $"%{keyword.Trim()}%");
                        }
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    userId = reader["user_id"],
                                    username = reader["username"].ToString(),
                                    phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
                                    role = reader["role"],
                                    createTime = reader["create_time"],
                                    updateTime = reader["update_time"]
                                });
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取用户列表成功",
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
                return Ok(new ApiResponse { Success = false, Message = "获取用户列表失败：" + ex.Message });
            }
        }

        // 用户详情（含发布的商品数量统计）
        [HttpGet("users/{id}")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult GetUserDetail(long id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 先查询用户基本信息
                    string userSql = "SELECT user_id, username, phone, role, create_time, update_time FROM `user` WHERE user_id = @Id";
                    string username;
                    string phone;
                    int role;
                    DateTime createTime;
                    DateTime updateTime;

                    using (var cmd = new MySqlCommand(userSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "用户不存在" });
                            }

                            username = reader["username"].ToString();
                            phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString();
                            role = Convert.ToInt32(reader["role"]);
                            createTime = Convert.ToDateTime(reader["create_time"]);
                            updateTime = Convert.ToDateTime(reader["update_time"]);
                        }
                    }

                    // 统计各状态商品数量
                    int totalGoods = 0;
                    string totalSql = "SELECT COUNT(*) FROM `goods` WHERE user_id = @UserId";
                    using (var cmd = new MySqlCommand(totalSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        totalGoods = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    int pendingGoods = 0;
                    string pendingSql = "SELECT COUNT(*) FROM `goods` WHERE user_id = @UserId AND audit_status = 0";
                    using (var cmd = new MySqlCommand(pendingSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        pendingGoods = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    int approvedGoods = 0;
                    string approvedSql = "SELECT COUNT(*) FROM `goods` WHERE user_id = @UserId AND audit_status = 1";
                    using (var cmd = new MySqlCommand(approvedSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        approvedGoods = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    int rejectedGoods = 0;
                    string rejectedSql = "SELECT COUNT(*) FROM `goods` WHERE user_id = @UserId AND audit_status = 2";
                    using (var cmd = new MySqlCommand(rejectedSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        rejectedGoods = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取用户详情成功",
                        Data = new
                        {
                            userId = id,
                            username = username,
                            phone = phone,
                            role = role,
                            createTime = createTime,
                            updateTime = updateTime,
                            goodsStats = new
                            {
                                total = totalGoods,
                                pending = pendingGoods,
                                approved = approvedGoods,
                                rejected = rejectedGoods
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "获取用户详情失败：" + ex.Message });
            }
        }

        // 修改用户角色（不能修改自己的角色）
        // role: 0=降为普通用户, 1=升为管理员
        [HttpPut("users/{id}/role")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult UpdateUserRole(long id, [FromBody] UpdateRoleRequest req)
        {
            try
            {
                // 校验角色参数
                if (req.Role != 0 && req.Role != 1)
                {
                    return Ok(new ApiResponse { Success = false, Message = "角色值无效，0=普通用户, 1=管理员" });
                }

                // 从 Token 获取当前操作者ID
                long currentAdminId = Convert.ToInt64(HttpContext.Items["UserId"]);

                // 不能修改自己的角色
                if (id == currentAdminId)
                {
                    return Ok(new ApiResponse { Success = false, Message = "不能修改自己的角色" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 检查用户是否存在
                    string checkSql = "SELECT user_id, username, role FROM `user` WHERE user_id = @Id";
                    string targetUsername;
                    int oldRole;
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "用户不存在" });
                            }

                            targetUsername = reader["username"].ToString();
                            oldRole = Convert.ToInt32(reader["role"]);
                        }
                    }

                    // 角色未变化则直接返回成功
                    if (oldRole == req.Role)
                    {
                        string roleName = req.Role == 1 ? "管理员" : "普通用户";
                        return Ok(new ApiResponse { Success = true, Message = $"用户「{targetUsername}」已是{roleName}，无需修改" });
                    }

                    // 更新角色
                    string updateSql = "UPDATE `user` SET role = @Role, update_time = NOW() WHERE user_id = @Id";
                    using (var cmd = new MySqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Role", req.Role);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }

                    string newRoleName = req.Role == 1 ? "管理员" : "普通用户";
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = $"已将用户「{targetUsername}」的角色修改为{newRoleName}",
                        Data = new { userId = id, role = req.Role }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "修改角色失败：" + ex.Message });
            }
        }

        // 删除用户（需管理员权限，不能删除自己）
        [HttpDelete("users/{id}")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult DeleteUser(long id)
        {
            try
            {
                // 从 Token 获取当前操作者ID
                long currentAdminId = Convert.ToInt64(HttpContext.Items["UserId"]);

                // 不能删除自己
                if (id == currentAdminId)
                {
                    return Ok(new ApiResponse { Success = false, Message = "不能删除自己的账号" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 检查用户是否存在
                    string checkSql = "SELECT user_id, username, role FROM `user` WHERE user_id = @Id";
                    string targetUsername;
                    int targetRole;
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "用户不存在" });
                            }

                            targetUsername = reader["username"].ToString();
                            targetRole = Convert.ToInt32(reader["role"]);
                        }
                    }

                    // 禁止删除其他管理员
                    if (targetRole == 1)
                    {
                        return Ok(new ApiResponse { Success = false, Message = "不能删除其他管理员账号" });
                    }

                    // 检查是否有进行中的交易
                    string tradeSql = "SELECT COUNT(*) FROM `trade` WHERE (buyer_id = @UserId OR seller_id = @UserId) AND status = 0";
                    using (var cmd = new MySqlCommand(tradeSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        int activeTradeCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (activeTradeCount > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "该用户有进行中的交易，请先处理后再删除" });
                        }
                    }

                    // 按外键依赖顺序级联删除
                    // ① 删除该用户所有商品的图片（goods_image 有 ON DELETE CASCADE，但显式清理更安全）
                    string delImageSql = "DELETE FROM `goods_image` WHERE goods_id IN (SELECT goods_id FROM `goods` WHERE user_id = @UserId)";
                    using (var cmd = new MySqlCommand(delImageSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        cmd.ExecuteNonQuery();
                    }

                    // ② 删除该用户相关的所有交易记录
                    string delTradeSql = "DELETE FROM `trade` WHERE buyer_id = @UserId OR seller_id = @UserId";
                    using (var cmd = new MySqlCommand(delTradeSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        cmd.ExecuteNonQuery();
                    }

                    // ③ 删除该用户的所有商品
                    string delGoodsSql = "DELETE FROM `goods` WHERE user_id = @UserId";
                    using (var cmd = new MySqlCommand(delGoodsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        cmd.ExecuteNonQuery();
                    }

                    // ④ 最后删除用户
                    string delUserSql = "DELETE FROM `user` WHERE user_id = @UserId";
                    using (var cmd = new MySqlCommand(delUserSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", id);
                        cmd.ExecuteNonQuery();
                    }

                    return Ok(new ApiResponse { Success = true, Message = $"用户「{targetUsername}」已删除" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "删除用户失败：" + ex.Message });
            }
        }

        // ==================== 商品管理 ====================

        /// <summary>
        /// 管理员编辑任意商品信息（价格、描述、图片等），不受发布者限制
        /// </summary>
        [HttpPut("goods/{id}")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult UpdateGoods(long id, [FromBody] AdminUpdateGoodsRequest req)
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // ① 校验商品是否存在
                    string checkSql = "SELECT goods_id, title, user_id FROM `goods` WHERE goods_id = @Id";
                    string goodsTitle;
                    long ownerId;
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品不存在" });
                            }
                            goodsTitle = reader["title"].ToString();
                            ownerId = Convert.ToInt64(reader["user_id"]);
                        }
                    }

                    // ② 动态构建 UPDATE 语句（只更新前端传了值的字段）
                    var setClauses = new System.Text.StringBuilder();
                    var parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrWhiteSpace(req.Title))
                    {
                        setClauses.Append("title = @Title, ");
                        parameters.Add(new MySqlParameter("@Title", req.Title.Trim()));
                    }
                    if (!string.IsNullOrWhiteSpace(req.Description))
                    {
                        setClauses.Append("description = @Description, ");
                        parameters.Add(new MySqlParameter("@Description", req.Description.Trim()));
                    }
                    if (req.Price.HasValue)
                    {
                        setClauses.Append("price = @Price, ");
                        parameters.Add(new MySqlParameter("@Price", req.Price.Value));
                    }
                    if (req.CategoryId.HasValue)
                    {
                        setClauses.Append("category_id = @CategoryId, ");
                        parameters.Add(new MySqlParameter("@CategoryId", req.CategoryId.Value));
                    }

                    if (setClauses.Length > 0)
                    {
                        setClauses.Append("update_time = NOW()");
                        string updateSql = $"UPDATE `goods` SET {setClauses} WHERE goods_id = @Id";
                        parameters.Add(new MySqlParameter("@Id", id));

                        using (var cmd = new MySqlCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddRange(parameters.ToArray());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // ③ 如果传了新图片列表，先删旧图再插新图
                    if (req.Images != null && req.Images.Count > 0)
                    {
                        string delSql = "DELETE FROM `goods_image` WHERE goods_id = @GoodsId";
                        using (var cmd = new MySqlCommand(delSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@GoodsId", id);
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var imageUrl in req.Images)
                        {
                            if (string.IsNullOrWhiteSpace(imageUrl)) continue;
                            string insSql = "INSERT INTO `goods_image` (goods_id, image_url) VALUES (@GoodsId, @Url)";
                            using (var cmd = new MySqlCommand(insSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@GoodsId", id);
                                cmd.Parameters.AddWithValue("@Url", imageUrl.Trim());
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = $"商品「{goodsTitle}」已更新（发布者ID: {ownerId}）",
                        Data = new { goodsId = id }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "编辑商品失败：" + ex.Message });
            }
        }

        // ========== 请求 DTO（放在同一文件，与项目现有风格对齐） ==========

        // 审核商品请求
        public class AuditGoodsRequest
        {
            public int AuditStatus { get; set; } // 1=通过, 2=驳回
        }

        // 修改角色请求
        public class UpdateRoleRequest
        {
            public int Role { get; set; } // 0=普通用户, 1=管理员
        }

        /// <summary>管理员编辑商品的请求体</summary>
        public class AdminUpdateGoodsRequest
        {
            /// <summary>商品标题（可选）</summary>
            public string? Title { get; set; }
            /// <summary>商品描述（可选）</summary>
            public string? Description { get; set; }
            /// <summary>价格（可选）</summary>
            public decimal? Price { get; set; }
            /// <summary>分类ID（可选）</summary>
            public long? CategoryId { get; set; }
            /// <summary>图片URL列表（可选，传了则全量替换）</summary>
            public List<string>? Images { get; set; }
        }
    }
}
