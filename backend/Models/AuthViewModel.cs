using System.ComponentModel.DataAnnotations;

namespace CampusSecondHand.API.Models
{
    // 登录请求
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

    // 注册请求（去掉 Email，保持与数据库字段匹配）
    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public string Phone { get; set; }
    }

    // 响应结果
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}