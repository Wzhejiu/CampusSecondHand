using System;
using System.Text;

namespace CampusSecondHand.API.Helpers
{
    // Token 解析结果
    public class TokenInfo
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public int Role { get; set; } // 0 = 普通用户, 1 = 管理员
    }

    // Token 工具类：解码并解析 Base64 令牌
    public static class TokenHelper
    {
        /// <summary>
        /// 解析 Base64 令牌，格式：Base64("{userId}|{username}|{role标识}")
        /// role标识为 "admin" 时表示管理员，其他为普通用户
        /// </summary>
        /// <returns>解析成功返回 TokenInfo，失败返回 null</returns>
        public static TokenInfo ParseToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                // 兼容 "Bearer {token}" 格式
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring(7).Trim();
                }

                // Base64 解码
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));

                // 按 "|" 分割：userId|username|role标识
                string[] parts = decoded.Split('|');
                if (parts.Length < 3)
                    return null;

                if (!long.TryParse(parts[0], out long userId))
                    return null;

                string username = parts[1];
                string roleMarker = parts[2];

                // role标识为 "admin" → 管理员，否则为普通用户
                int role = roleMarker == "admin" ? 1 : 0;

                return new TokenInfo
                {
                    UserId = userId,
                    Username = username,
                    Role = role
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
