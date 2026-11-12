using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Net;
using System.Reflection;
using WCS_Services;
using WCS_IServices;
using WCS_Api.Filters; // nan_T 2026-09-09：全局异常过滤器
using WCS_Api.Extensions; // nan_T 2026-09-09：服务程序集自动注册扩展
using WCS_Common.Helper;

var builder = WebApplication.CreateBuilder(args);

// 配置IIS服务器选项
builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = false;
    options.ForwardClientCertificate = false;
});

// 上传文件大小配置
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP 请求行最大长度，默认为 8kb
    options.Limits.MaxRequestLineSize = int.MaxValue;
    // 请求缓冲区最大长度，默认为 1M
    options.Limits.MaxRequestBufferSize = int.MaxValue;
    // 任何请求体的最大允许大小（以字节为单位），默认 30,000,000 字节，约为 28.6MB
    options.Limits.MaxRequestBodySize = int.MaxValue; // 不限制请求长度
});

// 设置表单长度限制的阈值
builder.Services.Configure<FormOptions>(opt =>
{
    opt.ValueLengthLimit = int.MaxValue;
    opt.MultipartBodyLengthLimit = int.MaxValue;
    opt.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.Configure<IISServerOptions>(opt =>
{
    opt.MaxRequestBodySize = int.MaxValue;
});

// 清理日志提供者
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 配置
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddEnvironmentVariables();

// 注册Swagger配置
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v7", new OpenApiInfo
    {
        Title = "WCS API",
        Version = "v7.0",
        Description = "WCS Warehouse Control System API",
        Contact = new OpenApiContact
        {
            Name = "技术支持",
            Email = "support@WCS.com"
        }
    });

    // 添加Bearer token支持，按需启用
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "请输入Bearer token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 获取XML注释文件，确保正确路径
    try
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        Console.WriteLine($"Looking for XML file at: {xmlPath}");

        if (File.Exists(xmlPath))
        {
            Console.WriteLine($"XML file found. Including in Swagger.");
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
        else
        {
            Console.WriteLine($"XML file not found at {xmlPath}. Continuing without XML comments.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading XML comments: {ex.Message}");
    }

    // 配置枚举字符串值
    c.UseAllOfToExtendReferenceSchemas();
    c.UseOneOfForPolymorphism();

    // 配置操作ID
    c.CustomOperationIds(apiDesc => apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null);
});

// 配置控制器和响应格式
builder.Services.AddControllers(setupAction =>
{
    // 默认只响应JSON格式
    setupAction.ReturnHttpNotAcceptable = true;
    // nan_T 2026-09-09：注册全局异常过滤器（参考 DTcms.Core），
    // Service 层抛出的 ResponseException 及未预期异常统一由它转换为标准 ResponseMessage
    setupAction.Filters.Add(typeof(GlobalExceptionFilter));
})
    .AddNewtonsoftJson(opt =>
    {
        opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        opt.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    })
    .AddXmlDataContractSerializerFormatters();

// 配置跨域服务
builder.Services.AddCors(options =>
{
    // nan_T 2026-09-09：修复 CORS 配置矛盾。
    // 原写法同时调用 WithOrigins(AllowedHosts) 和 AllowAnyOrigin()，语义冲突；
    // 且 AllowedHosts 是主机过滤配置项，不是 CORS 来源列表，读出来通常是 "*"。
    // 现改为：从独立配置 "Cors:AllowedOrigins" 读取白名单（逗号分隔），
    // 未配置时回退为允许任意来源（仅建议开发/内网环境），两个 API 不再同时调用。
    options.AddPolicy("cors", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("content-disposition", "token-expired", "x-pagination");

        var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (corsOrigins != null && corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }
    });
});

// 配置认证服务
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/Login/access-denied";
        options.AccessDeniedPath = "/api/Login/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// 配置授权服务
builder.Services.AddAuthorization();

// nan_T 2026-09-09：注册数据库上下文工厂（按 appsettings 的 DBType 在 PostgreSQL/SqlServer/MySql 间切换）
builder.Services.AddTransient<WCS_Helper.Database.IDbContextFactory, WCS_Helper.Database.DbContextFactory>();

// nan_T 2026-09-09：按约定自动注册 WCS_Services 程序集中所有业务服务（参考 DTcms.Core 的程序集注册方式），
// 替代原来逐个 AddScoped<ILoginService, LoginService>() 的手写注册；
// 构造函数含 string 等原始参数的 PLCService 会被自动跳过，仍由下方手动注册启动。
builder.Services.AddWcsServices();

// nan_T 2026-09-09：注册失败消息自动重发后台服务（定时扫描 retry_messages 表，重发失败的 API 请求 / PLC 指令）
builder.Services.AddHostedService<WCS_Api.Services.RetryBackgroundService>();

// 在 Program.cs 中注册
builder.Services.AddHttpContextAccessor();

// nan_T 2026-09-23：PLC 服务从配置读取 IP 并注册为 Singleton（替代原直接 new 的硬编码写法）。
// 构造函数内部会启动 Recv/Send 后台循环；启动时由 app.Services.GetRequiredService<PLCService>() 强制触发构造。
builder.Services.AddSingleton<PLCService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var ipAddress = config["PLC:IPAddress"] ?? "172.18.18.20";
    Console.WriteLine($"初始化PLC服务，IP地址: {ipAddress}");
    return new PLCService(ipAddress);
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// nan_T 2026-09-23：强制解析 PLCService，触发其构造函数中的 Task.Run(Recv/Send) 后台循环
app.Services.GetRequiredService<PLCService>();

// 启动日志
Console.WriteLine("=== WCS API 启动 ===");
Console.WriteLine($"环境: {app.Environment.EnvironmentName}");
Console.WriteLine($"项目目录: {app.Environment.ContentRootPath}");
Console.WriteLine($"Web目录: {app.Environment.WebRootPath}");

// 确保Swagger目录存在
var swaggerDir = Path.Combine(app.Environment.ContentRootPath, "swagger");
if (!Directory.Exists(swaggerDir))
{
    Directory.CreateDirectory(swaggerDir);
    Console.WriteLine("已创建Swagger目录");
}

// 配置HTTP请求管道
// 中间件顺序很重要，必须按以下正确顺序添加

// 异常处理（开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 请求监控日志中间件
app.Use(async (context, next) =>
{
    var startTime = DateTimeHelper.CnNow;
    Console.WriteLine($"[{startTime:HH:mm:ss}] {context.Request.Method} {context.Request.Path}");

    await next();

    var endTime = DateTimeHelper.CnNow;
    var duration = (endTime - startTime).TotalMilliseconds;
    Console.WriteLine($"[{endTime:HH:mm:ss}] {context.Request.Method} {context.Request.Path} - {context.Response.StatusCode} ({duration}ms)");
});

// 静态文件服务
app.UseStaticFiles();

// 路由
app.UseRouting();

// CORS
app.UseCors("cors");

// 认证和授权
app.UseAuthentication();
app.UseAuthorization();

// Swagger配置
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        // 动态设置服务器URL
        if (!app.Environment.IsDevelopment())
        {
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new OpenApiServer
                {
                    Url = $"{httpReq.Scheme}://{httpReq.Host.Value}",
                    Description = "Production Server"
                }
            };
        }
    });
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v7/swagger.json", "WCS API v7");
    c.RoutePrefix = "swagger"; // 访问路径为 http://yourdomain/swagger

    // 启用选项
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.DisplayOperationId();
    c.DefaultModelsExpandDepth(2);
    c.DefaultModelExpandDepth(2);
    c.ShowExtensions();
    c.EnableFilter();

    // 生产环境额外调整
    if (!app.Environment.IsDevelopment())
    {
        c.DocExpansion(DocExpansion.None); // 默认折叠文档
        c.EnableValidator(); // 启用验证器
    }
});

// 终端路由
app.MapControllers();

// 根路径跳转
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var info = new
    {
        Application = assembly.GetName().Name,
        Version = assembly.GetName().Version?.ToString(),
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTimeHelper.CnNow,
        Status = "Healthy",
        MachineName = Environment.MachineName,
        Uptime = Environment.TickCount
    };
    return Results.Json(info);
});

app.MapGet("/api/version", () =>
{
    return Results.Ok(new
    {
        Version = "v7.0",
        BuildDate = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location),
        Environment = app.Environment.EnvironmentName
    });
});

// 启动应用
try
{
    Console.WriteLine("启动HTTP服务...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"启动失败: {ex.Message}");
    Console.WriteLine($"堆栈信息: {ex.StackTrace}");
    throw;
}
