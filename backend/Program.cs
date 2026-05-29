var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
 builder.Services.AddSwaggerGen(c =>
 {
     // 定义 JWT Bearer 安全方案 → Swagger UI 右上角出现 Authorize 按钮
     c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
     {
         Name = "Authorization",
         Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
         Scheme = "bearer",
         BearerFormat = "JWT",
         In = Microsoft.OpenApi.Models.ParameterLocation.Header,
         Description = "输入 JWT Token：Bearer {你的token}"
     });
 
     // 全局应用 Bearer 安全要求
     c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
     {
         {
             new Microsoft.OpenApi.Models.OpenApiSecurityScheme
             {
                 Reference = new Microsoft.OpenApi.Models.OpenApiReference
                 {
                     Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                     Id = "Bearer"
                 }
             },
             Array.Empty<string>()
         }
     });
 });

var app = builder.Build();

// 启用静态文件服务（用于访问 wwwroot/uploads/ 下的上传图片）
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    context.Response.Headers.Append("Access-Control-Allow-Methods", "*");
    context.Response.Headers.Append("Access-Control-Allow-Headers", "*");

    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();
app.Run();