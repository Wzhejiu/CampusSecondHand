using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CampusSecondHand.API.Filters;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TradeController : ControllerBase
    {
        private readonly string _connStr;

        public TradeController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Missing connection string 'Default'");
        }

        // 创建交易/下单（需登录）
        [HttpPost]
        [AuthFilter]
        public IActionResult Create([FromBody] CreateTradeRequest req)
        {
            try
            {
                // 从 Token 获取买家 ID
                long buyerId = Convert.ToInt64(HttpContext.Items["UserId"]);

                if (req.GoodsId <= 0)
                {
                    return Ok(new ApiResponse { Success = false, Message = "无效的商品ID" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 1. 查询商品信息：校验商品存在、审核通过、非本人发布
                    string goodsSql = @"SELECT goods_id, user_id, title, audit_status 
                                        FROM `goods` WHERE goods_id = @GoodsId";
                    long sellerId;
                    string goodsTitle;

                    using (var cmd = new MySqlCommand(goodsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@GoodsId", req.GoodsId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品不存在" });
                            }

                            sellerId = Convert.ToInt64(reader["user_id"]);
                            goodsTitle = reader["title"].ToString();
                            int auditStatus = Convert.ToInt32(reader["audit_status"]);

                            if (auditStatus != 1)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "商品未通过审核，无法下单" });
                            }

                            if (sellerId == buyerId)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "不能购买自己发布的商品" });
                            }
                        }
                    }

                    // 2. 检查是否已有进行中的交易（同一买家对同一商品）
                    string dupSelfSql = "SELECT COUNT(*) FROM `trade` WHERE goods_id = @GoodsId AND buyer_id = @BuyerId AND status = 0";
                    using (var cmd = new MySqlCommand(dupSelfSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@GoodsId", req.GoodsId);
                        cmd.Parameters.AddWithValue("@BuyerId", buyerId);
                        int dupCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (dupCount > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "您已对该商品发起过交易，请等待卖家确认" });
                        }
                    }

                    // 3. 创建交易记录
                    string insertSql = @"INSERT INTO `trade` (goods_id, buyer_id, seller_id, meet_time, meet_location, status, create_time)
                                         VALUES (@GoodsId, @BuyerId, @SellerId, @MeetTime, @MeetLocation, 0, @CreateTime);
                                         SELECT LAST_INSERT_ID();";

                    long tradeId;
                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@GoodsId", req.GoodsId);
                        cmd.Parameters.AddWithValue("@BuyerId", buyerId);
                        cmd.Parameters.AddWithValue("@SellerId", sellerId);
                        cmd.Parameters.AddWithValue("@MeetTime",
                            req.MeetTime.HasValue ? (object)req.MeetTime.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MeetLocation",
                            string.IsNullOrWhiteSpace(req.MeetLocation) ? (object)DBNull.Value : req.MeetLocation.Trim());
                        cmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);

                        tradeId = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "下单成功，请等待卖家确认",
                        Data = new { tradeId = tradeId }
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "下单失败：" + ex.Message });
            }
        }

        // 确认完成交易（仅卖家可操作）
        [HttpPut("{id}/complete")]
        [AuthFilter]
        public IActionResult Complete(long id)
        {
            try
            {
                long currentUserId = Convert.ToInt64(HttpContext.Items["UserId"]);

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 1. 查询交易信息：校验存在性、卖家身份、当前状态
                    long goodsId;
                    string querySql = "SELECT trade_id, goods_id, seller_id, status FROM `trade` WHERE trade_id = @TradeId";
                    using (var cmd = new MySqlCommand(querySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TradeId", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "交易不存在" });
                            }

                            long sellerId = Convert.ToInt64(reader["seller_id"]);
                            int status = Convert.ToInt32(reader["status"]);

                            if (sellerId != currentUserId)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "仅卖家可确认完成交易" });
                            }

                            if (status != 0)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "当前交易状态不允许此操作" });
                            }

                            goodsId = Convert.ToInt64(reader["goods_id"]);
                        }
                    }

                    // 2. 更新状态为已完成
                    string updateSql = "UPDATE `trade` SET status = 1, update_time = NOW() WHERE trade_id = @TradeId";
                    using (var cmd = new MySqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TradeId", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. 交易完成后将商品状态改为"订单结束"（用户不可见）
                    string updateGoodsSql = "UPDATE `goods` SET audit_status = 2, update_time = NOW() WHERE goods_id = @GoodsId";
                    using (var cmd = new MySqlCommand(updateGoodsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@GoodsId", goodsId);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new ApiResponse { Success = true, Message = "交易已完成" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "确认交易失败：" + ex.Message });
            }
        }

        // 取消交易（买家或卖家均可操作）
        [HttpPut("{id}/cancel")]
        [AuthFilter]
        public IActionResult Cancel(long id)
        {
            try
            {
                long currentUserId = Convert.ToInt64(HttpContext.Items["UserId"]);

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 查询交易信息：校验存在性、买/卖家身份、当前状态
                    string querySql = "SELECT trade_id, buyer_id, seller_id, status FROM `trade` WHERE trade_id = @TradeId";
                    using (var cmd = new MySqlCommand(querySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TradeId", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Ok(new ApiResponse { Success = false, Message = "交易不存在" });
                            }

                            long buyerId = Convert.ToInt64(reader["buyer_id"]);
                            long sellerId = Convert.ToInt64(reader["seller_id"]);
                            int status = Convert.ToInt32(reader["status"]);

                            if (currentUserId != buyerId && currentUserId != sellerId)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "您不是该交易的参与方，无法取消" });
                            }

                            if (status != 0)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "当前交易状态不允许此操作" });
                            }
                        }
                    }

                    // 更新状态为已取消
                    string updateSql = "UPDATE `trade` SET status = 2, update_time = NOW() WHERE trade_id = @TradeId";
                    using (var cmd = new MySqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TradeId", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new ApiResponse { Success = true, Message = "交易已取消" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "取消交易失败：" + ex.Message });
            }
        }

        // 我的交易列表（需登录）
        [HttpGet]
        [AuthFilter]
        public IActionResult GetMyTrades([FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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

                    // 构建筛选条件
                    string statusCondition = "";
                    if (status.HasValue && (status.Value == 0 || status.Value == 1 || status.Value == 2))
                    {
                        statusCondition = " AND t.status = @Status";
                    }

                    // 查询总数
                    string countSql = $@"SELECT COUNT(*) FROM `trade` t 
                                         WHERE (t.buyer_id = @UserId OR t.seller_id = @UserId){statusCondition}";
                    int totalCount;
                    using (var cmd = new MySqlCommand(countSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);
                        if (status.HasValue && (status.Value == 0 || status.Value == 1 || status.Value == 2))
                        {
                            cmd.Parameters.AddWithValue("@Status", status.Value);
                        }
                        totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 查询交易列表（JOIN goods 获取商品标题，JOIN user 获取买卖双方用户名）
                    string listSql = $@"SELECT t.trade_id, t.goods_id, t.buyer_id, t.seller_id, t.status,
                                               t.meet_time, t.meet_location, t.create_time, t.update_time,
                                               g.title AS goods_title,
                                               buyer.username AS buyer_name,
                                               seller.username AS seller_name
                                        FROM `trade` t
                                        LEFT JOIN `goods` g ON t.goods_id = g.goods_id
                                        JOIN `user` buyer ON t.buyer_id = buyer.user_id
                                        JOIN `user` seller ON t.seller_id = seller.user_id
                                        WHERE (t.buyer_id = @UserId OR t.seller_id = @UserId){statusCondition}
                                        ORDER BY t.create_time DESC
                                        LIMIT @Offset, @PageSize";

                    var list = new List<object>();
                    using (var cmd = new MySqlCommand(listSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);
                        if (status.HasValue && (status.Value == 0 || status.Value == 1 || status.Value == 2))
                        {
                            cmd.Parameters.AddWithValue("@Status", status.Value);
                        }
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long buyerId = Convert.ToInt64(reader["buyer_id"]);
                                long sellerId = Convert.ToInt64(reader["seller_id"]);

                                // 对方信息：如果当前用户是买家，对方是卖家；反之亦然
                                string otherPartyName = (currentUserId == buyerId)
                                    ? reader["seller_name"].ToString()
                                    : reader["buyer_name"].ToString();
                                string myRole = (currentUserId == buyerId) ? "买家" : "卖家";

                                list.Add(new
                                {
                                    tradeId = reader["trade_id"],
                                    goodsId = reader["goods_id"],
                                    goodsTitle = reader["goods_title"] == DBNull.Value ? "商品已删除" : reader["goods_title"].ToString(),
                                    buyerId = buyerId,
                                    sellerId = sellerId,
                                    status = reader["status"],
                                    myRole = myRole,
                                    otherPartyName = otherPartyName,
                                    meetTime = reader["meet_time"] == DBNull.Value ? null : reader["meet_time"],
                                    meetLocation = reader["meet_location"] == DBNull.Value ? null : reader["meet_location"].ToString(),
                                    createTime = reader["create_time"],
                                    updateTime = reader["update_time"]
                                });
                            }
                        }
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "获取交易列表成功",
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
                return Ok(new ApiResponse { Success = false, Message = "获取交易列表失败：" + ex.Message });
            }
        }
    }

    // ========== 请求 DTO ==========

    // 创建交易请求
    public class CreateTradeRequest
    {
        public long GoodsId { get; set; }
        public DateTime? MeetTime { get; set; }      // 可选：预约时间
        public string MeetLocation { get; set; }      // 可选：预约地点
    }
}
