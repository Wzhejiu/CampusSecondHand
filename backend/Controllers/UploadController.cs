using Microsoft.AspNetCore.Mvc;
using CampusSecondHand.API.Filters;
using CampusSecondHand.API.Models;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // 单张图片上传（需登录）
        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Ok(new ApiResponse { Success = false, Message = "请选择要上传的文件" });
                }

                // 校验文件类型
                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                string[] allowedExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
                if (!allowedExts.Contains(ext))
                {
                    return Ok(new ApiResponse { Success = false, Message = "仅支持 JPG/JPEG/PNG/GIF/WebP/BMP 格式" });
                }

                // 校验文件大小（最大 5MB）
                long maxSize = 5 * 1024 * 1024;
                if (file.Length > maxSize)
                {
                    return Ok(new ApiResponse { Success = false, Message = "文件大小不能超过5MB" });
                }

                // 确定上传目标目录
                string webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                string uploadsFolder = Path.Combine(webRoot, "uploads");
                string dateFolder = DateTime.Now.ToString("yyyyMM");
                string targetFolder = Path.Combine(uploadsFolder, dateFolder);

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                // 生成唯一文件名并保存
                string uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                string filePath = Path.Combine(targetFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string url = $"/uploads/{dateFolder}/{uniqueFileName}";

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "上传成功",
                    Data = new { url = url }
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "上传失败：" + ex.Message });
            }
        }

        // 批量上传（最多9张，需登录）
        [HttpPost("batch")]
        [AuthFilter]
        public async Task<IActionResult> UploadBatch(List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return Ok(new ApiResponse { Success = false, Message = "请选择要上传的文件" });
                }

                if (files.Count > 9)
                {
                    return Ok(new ApiResponse { Success = false, Message = "单次最多上传9张图片" });
                }

                string webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                string uploadsFolder = Path.Combine(webRoot, "uploads");
                string dateFolder = DateTime.Now.ToString("yyyyMM");
                string targetFolder = Path.Combine(uploadsFolder, dateFolder);

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                string[] allowedExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
                long maxSize = 5 * 1024 * 1024;
                var urls = new List<string>();

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExts.Contains(ext))
                    {
                        return Ok(new ApiResponse { Success = false, Message = $"文件 {file.FileName} 格式不支持" });
                    }
                    if (file.Length > maxSize)
                    {
                        return Ok(new ApiResponse { Success = false, Message = $"文件 {file.FileName} 超过5MB限制" });
                    }

                    string uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                    string filePath = Path.Combine(targetFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    urls.Add($"/uploads/{dateFolder}/{uniqueFileName}");
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"成功上传 {urls.Count} 张图片",
                    Data = new { urls = urls }
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse { Success = false, Message = "批量上传失败：" + ex.Message });
            }
        }
    }
}
