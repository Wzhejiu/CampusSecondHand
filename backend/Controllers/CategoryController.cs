using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CampusSecondHand.API.Filters;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly string _connStr;

        public CategoryController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Missing connection string 'Default'");
        }

        // 获取全部分类（树形结构，公开）
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 一次性查出全部分类
                    string sql = "SELECT category_id, category_name, parent_id FROM `category` ORDER BY parent_id, category_id";
                    var list = new List<CategoryRow>();

                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CategoryRow
                            {
                                CategoryId = Convert.ToInt64(reader["category_id"]),
                                CategoryName = reader["category_name"].ToString(),
                                ParentId = reader["parent_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["parent_id"])
                            });
                        }
                    }

                    // 在内存中构建树形结构
                    var tree = BuildTree(list, null);
                    return Ok(new ApiResponse { Success = true, Message = "获取成功", Data = tree });
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "获取分类失败：" + ex.Message });
            }
        }

        // 获取单个分类详情（公开）
        [HttpGet("{id}")]
        public IActionResult GetById(long id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = "SELECT category_id, category_name, parent_id FROM `category` WHERE category_id = @Id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var data = new
                                {
                                    CategoryId = reader["category_id"],
                                    CategoryName = reader["category_name"].ToString(),
                                    ParentId = reader["parent_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["parent_id"])
                                };

                                return Ok(new ApiResponse { Success = true, Message = "获取成功", Data = data });
                            }
                        }
                    }
                }

                return Ok(new ApiResponse { Success = false, Message = "分类不存在" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "获取分类失败：" + ex.Message });
            }
        }

        // 新增分类（需管理员权限）
        [HttpPost]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult Create([FromBody] CreateCategoryRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.CategoryName))
                {
                    return Ok(new ApiResponse { Success = false, Message = "分类名称不能为空" });
                }

                if (req.CategoryName.Length > 50)
                {
                    return Ok(new ApiResponse { Success = false, Message = "分类名称不能超过50个字符" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 如果指定了父分类，校验父分类是否存在
                    if (req.ParentId.HasValue && req.ParentId.Value > 0)
                    {
                        string checkSql = "SELECT COUNT(*) FROM `category` WHERE category_id = @ParentId";
                        using (var cmd = new MySqlCommand(checkSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ParentId", req.ParentId.Value);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            if (count == 0)
                            {
                                return Ok(new ApiResponse { Success = false, Message = "父分类不存在" });
                            }
                        }
                    }

                    // 插入新分类（parent_id 为 0 或 null 均视为顶级分类）
                    string insertSql = "INSERT INTO `category` (category_name, parent_id) VALUES (@Name, @ParentId)";
                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", req.CategoryName.Trim());
                        cmd.Parameters.AddWithValue("@ParentId",
                            (req.ParentId.HasValue && req.ParentId.Value > 0)
                                ? (object)req.ParentId.Value
                                : DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new ApiResponse { Success = true, Message = "新增分类成功" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "新增分类失败：" + ex.Message });
            }
        }

        // 编辑分类名称（需管理员权限）
        [HttpPut("{id}")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult Update(long id, [FromBody] UpdateCategoryRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.CategoryName))
                {
                    return Ok(new ApiResponse { Success = false, Message = "分类名称不能为空" });
                }

                if (req.CategoryName.Length > 50)
                {
                    return Ok(new ApiResponse { Success = false, Message = "分类名称不能超过50个字符" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 检查分类是否存在
                    string checkSql = "SELECT COUNT(*) FROM `category` WHERE category_id = @Id";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count == 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "分类不存在" });
                        }
                    }

                    // 更新分类名称
                    string updateSql = "UPDATE `category` SET category_name = @Name WHERE category_id = @Id";
                    using (var cmd = new MySqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", req.CategoryName.Trim());
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new ApiResponse { Success = true, Message = "修改分类成功" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "修改分类失败：" + ex.Message });
            }
        }

        // 删除分类（需管理员权限）
        [HttpDelete("{id}")]
        [AuthFilter(RequireAdmin = true)]
        public IActionResult Delete(long id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 检查分类是否存在
                    string checkSql = "SELECT COUNT(*) FROM `category` WHERE category_id = @Id";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count == 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "分类不存在" });
                        }
                    }

                    // 检查是否有子分类
                    string childSql = "SELECT COUNT(*) FROM `category` WHERE parent_id = @Id";
                    using (var cmd = new MySqlCommand(childSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int childCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (childCount > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "该分类下存在子分类，请先删除子分类" });
                        }
                    }

                    // 检查是否有商品关联
                    string goodsSql = "SELECT COUNT(*) FROM `goods` WHERE category_id = @Id";
                    using (var cmd = new MySqlCommand(goodsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int goodsCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (goodsCount > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "该分类下有商品关联，无法删除" });
                        }
                    }

                    // 执行删除
                    string deleteSql = "DELETE FROM `category` WHERE category_id = @Id";
                    using (var cmd = new MySqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new ApiResponse { Success = true, Message = "删除分类成功" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "删除分类失败：" + ex.Message });
            }
        }

        // ========== 内部辅助方法 ==========

        // 从扁平列表递归构建树形结构
        private static List<object> BuildTree(List<CategoryRow> all, long? parentId)
        {
            var children = all.Where(c => c.ParentId == parentId).ToList();
            var result = new List<object>();

            foreach (var cat in children)
            {
                result.Add(new
                {
                    categoryId = cat.CategoryId,
                    categoryName = cat.CategoryName,
                    parentId = cat.ParentId,
                    children = BuildTree(all, cat.CategoryId)
                });
            }

            return result;
        }

        // 内部行记录（用于树形构建）
        private class CategoryRow
        {
            public long CategoryId { get; set; }
            public string CategoryName { get; set; }
            public long? ParentId { get; set; }
        }
    }

    // ========== 请求 DTO（放在同一文件，与 AuthViewModel.cs 风格对齐） ==========

    // 新增分类请求
    public class CreateCategoryRequest
    {
        public string CategoryName { get; set; }
        public long? ParentId { get; set; } // null 或 0 表示顶级分类
    }

    // 编辑分类请求
    public class UpdateCategoryRequest
    {
        public string CategoryName { get; set; }
    }
}
