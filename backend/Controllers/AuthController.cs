using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Text;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class AuthController : ControllerBase  // 继承 ControllerBase
    {
        // 数据库连接字符串
        private string connStr = @"Server=(localdb)\MSSQLLocalDB;Database=CampusDB;Trusted_Connection=True;";

        // 注册
        [HttpPost("register")]  // 完整路径: api/Auth/register
        public IActionResult Register([FromBody] RegisterRequest req)  // 用 IActionResult
        {
            try
            {
                // 验证两次密码
                if (req.Password != req.ConfirmPassword)
                {
                    return Ok(new ApiResponse { Success = false, Message = "两次密码不一致" });
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // 检查用户名是否存在
                    string checkSql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                    using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "用户名已存在" });
                        }
                    }

                    // 插入用户（明文密码）
                    string insertSql = @"INSERT INTO Users (Username, Password, Email, Phone, Role, CreatedAt) 
                                        VALUES (@Username, @Password, @Email, @Phone, 'User', @CreatedAt)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        cmd.Parameters.AddWithValue("@Password", req.Password);
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(req.Email) ? (object)DBNull.Value : req.Email);
                        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(req.Phone) ? (object)DBNull.Value : req.Phone);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new ApiResponse { Success = true, Message = "注册成功" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "注册失败：" + ex.Message });
            }
        }

        // 登录（普通用户）
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT Id, Username, Password, Role, Email, Phone FROM Users WHERE Username = @Username AND Password = @Password AND Role = 'User'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        cmd.Parameters.AddWithValue("@Password", req.Password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{reader["Id"]}|{reader["Username"]}|{DateTime.Now}"));

                                return Ok(new ApiResponse
                                {
                                    Success = true,
                                    Message = "登录成功",
                                    Data = new
                                    {
                                        Token = token,
                                        UserInfo = new
                                        {
                                            Id = reader["Id"],
                                            Username = reader["Username"],
                                            Email = reader["Email"],
                                            Phone = reader["Phone"],
                                            Role = reader["Role"]
                                        }
                                    }
                                });
                            }
                        }
                    }
                }

                return Ok(new ApiResponse { Success = false, Message = "用户名或密码错误" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "登录失败：" + ex.Message });
            }
        }

        // 管理员登录
        [HttpPost("admin/login")]
        public IActionResult AdminLogin([FromBody] LoginRequest req)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT Id, Username, Password, Role FROM Users WHERE Username = @Username AND Password = @Password AND Role = 'Admin'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        cmd.Parameters.AddWithValue("@Password", req.Password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{reader["Id"]}|{reader["Username"]}|admin"));

                                return Ok(new ApiResponse
                                {
                                    Success = true,
                                    Message = "管理员登录成功",
                                    Data = new
                                    {
                                        Token = token,
                                        UserInfo = new
                                        {
                                            Id = reader["Id"],
                                            Username = reader["Username"],
                                            Role = reader["Role"]
                                        }
                                    }
                                });
                            }
                        }
                    }
                }

                return Ok(new ApiResponse { Success = false, Message = "用户名或密码错误" });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "登录失败：" + ex.Message });
            }
        }
    }
}