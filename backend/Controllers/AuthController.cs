using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connStr;

        public AuthController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("Default") 
                       ?? throw new InvalidOperationException("Missing connection string 'Default'");
        }

        // 注册
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            try
            {
                if (req.Password != req.ConfirmPassword)
                {
                    return Ok(new ApiResponse { Success = false, Message = "两次密码不一致" });
                }

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();

                    // 检查用户名是否存在
                    string checkSql = "SELECT COUNT(*) FROM `user` WHERE username = @Username";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        var countObj = cmd.ExecuteScalar();
                        int count = Convert.ToInt32(countObj);
                        if (count > 0)
                        {
                            return Ok(new ApiResponse { Success = false, Message = "用户名已存在" });
                        }
                    }

                    // 插入用户（明文，训练项目）
                    string insertSql = @"INSERT INTO `user` (username, password, phone, role, create_time)
                                         VALUES (@Username, @Password, @Phone, 0, @CreateTime)";

                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        cmd.Parameters.AddWithValue("@Password", req.Password);
                        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(req.Phone) ? (object)DBNull.Value : req.Phone);
                        cmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);

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
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = "SELECT user_id, username, password, role, phone FROM `user` WHERE username = @Username AND password = @Password AND role = 0";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        cmd.Parameters.AddWithValue("@Password", req.Password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{reader["user_id"]}|{reader["username"]}|{DateTime.Now}"));

                                return Ok(new ApiResponse
                                {
                                    Success = true,
                                    Message = "登录成功",
                                    Data = new
                                    {
                                        Token = token,
                                        UserInfo = new
                                        {
                                            Id = reader["user_id"],
                                            Username = reader["username"],
                                            Phone = reader["phone"],
                                            Role = reader["role"]
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
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = "SELECT user_id, username, password, role FROM `user` WHERE username = @Username AND password = @Password AND role = 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", req.Username);
                        cmd.Parameters.AddWithValue("@Password", req.Password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{reader["user_id"]}|{reader["username"]}|admin"));

                                return Ok(new ApiResponse
                                {
                                    Success = true,
                                    Message = "管理员登录成功",
                                    Data = new
                                    {
                                        Token = token,
                                        UserInfo = new
                                        {
                                            Id = reader["user_id"],
                                            Username = reader["username"],
                                            Role = reader["role"]
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