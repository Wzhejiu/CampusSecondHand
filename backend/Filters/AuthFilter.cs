using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CampusSecondHand.API.Helpers;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Filters
{
    // 认证过滤器：从 Authorization 请求头提取并验证 Token
    // 用法示例：
    //   [AuthFilter]                        — 需要登录
    //   [AuthFilter(RequireAdmin = true)]   — 需要管理员权限
    public class AuthFilter : ActionFilterAttribute
    {
        // 是否要求管理员权限
        public bool RequireAdmin { get; set; } = false;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. 从请求头获取 Token
            string authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                context.Result = new JsonResult(new ApiResponse
                {
                    Success = false,
                    Message = "未提供认证令牌，请先登录"
                })
                {
                    StatusCode = 401
                };
                return;
            }

            // 2. 解析 Token
            TokenInfo tokenInfo = TokenHelper.ParseToken(authHeader);
            if (tokenInfo == null)
            {
                context.Result = new JsonResult(new ApiResponse
                {
                    Success = false,
                    Message = "令牌无效或已过期，请重新登录"
                })
                {
                    StatusCode = 401
                };
                return;
            }

            // 3. 管理员权限校验
            if (RequireAdmin && tokenInfo.Role != 1)
            {
                context.Result = new JsonResult(new ApiResponse
                {
                    Success = false,
                    Message = "权限不足，需要管理员权限"
                })
                {
                    StatusCode = 403
                };
                return;
            }

            // 4. 将用户信息存入 HttpContext.Items，供后续 Controller 使用
            context.HttpContext.Items["UserId"] = tokenInfo.UserId;
            context.HttpContext.Items["Username"] = tokenInfo.Username;
            context.HttpContext.Items["Role"] = tokenInfo.Role;
        }
    }
}
