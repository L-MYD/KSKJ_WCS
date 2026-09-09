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

var builder = WebApplication.CreateBuilder(args);

// ���IIS��������
builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = false;
    options.ForwardClientCertificate = false;
});

// �ϴ��ļ�����
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP �����е���������С��Ĭ��Ϊ 8kb
    options.Limits.MaxRequestLineSize = int.MaxValue;
    // ���󻺳���������С��Ĭ��Ϊ 1M
    options.Limits.MaxRequestBufferSize = int.MaxValue;
    // �κ��������ĵ���������С�����ֽ�Ϊ��λ��,Ĭ�� 30,000,000 �ֽڣ���ԼΪ 28.6MB
    options.Limits.MaxRequestBodySize = int.MaxValue; // �������󳤶�
});

// ���ý����ļ����ȵ����ֵ��
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

// �����־����
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ����
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddEnvironmentVariables();

// ע��Swagger����
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v7", new OpenApiInfo
    {
        Title = "WCS API",
        Version = "v7.0",
        Description = "WCS Warehouse Control System API",
        Contact = new OpenApiContact
        {
            Name = "����֧��",
            Email = "support@WCS.com"
        }
    });

    // ���Bearer token֧�֣������Ҫ��
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "������Bearer token",
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

    // ��ȡXMLע���ļ�����ȷ·��
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

    // ����ö���ַ���ֵ
    c.UseAllOfToExtendReferenceSchemas();
    c.UseOneOfForPolymorphism();

    // ���ò���ID
    c.CustomOperationIds(apiDesc => apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null);
});

// ��������Ӧ����
builder.Services.AddControllers(setupAction =>
{
    // Ĭ����ӦJSON��ʽ
    setupAction.ReturnHttpNotAcceptable = true;
})
    .AddNewtonsoftJson(opt =>
    {
        opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        opt.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    })
    .AddXmlDataContractSerializerFormatters();

// ���ÿ�����
builder.Services.AddCors(options =>
{
    options.AddPolicy("cors", policy =>
        policy.WithOrigins(builder.Configuration["AllowedHosts"] ?? "*")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin()
              .WithExposedHeaders("content-disposition", "token-expired", "x-pagination"));
});

// �����֤����
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

// �����Ȩ����
builder.Services.AddAuthorization();

// ע��LoginService
builder.Services.AddTransient<WCS_Helper.Database.IDbContextFactory, WCS_Helper.Database.DbContextFactory>(); // 注册数据库上下文工厂（按 appsettings 的 DBType 在 PostgreSQL/SqlServer/MySql 间切换）
        builder.Services.AddScoped<ILoginService, LoginService>();

// �� Program.cs ������
builder.Services.AddHttpContextAccessor();

// ���ʹ�÷���һ������ע�룩
builder.Services.AddScoped<ILogService, LogService>();

builder.Services.AddScoped<IDataMonitoringService, DataMonitoringService>(); // ���ʹ��ʵ���汾
// ע��PLC���� - ʹ������ע�������ֱ��ʵ����
//builder.Services.AddSingleton<PLCService>(provider =>
//{
//    var config = provider.GetRequiredService<IConfiguration>();
//    var logger = provider.GetService<ILogger<PLCService>>();

//    // �������л�ȡIP��ַ�����û��������ʹ��Ĭ��ֵ
//    var ipAddress = config["PLC:IPAddress"] ?? "192.168.29.50";

//    logger?.LogInformation($"��ʼ��PLC����IP��ַ: {ipAddress}");

//    return new PLCService(ipAddress);
//});

new PLCService("172.18.18.20");

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// �����־
Console.WriteLine("=== WCS API ��� ===");
Console.WriteLine($"����: {app.Environment.EnvironmentName}");
Console.WriteLine($"��Ŀ¼: {app.Environment.ContentRootPath}");
Console.WriteLine($"Web��Ŀ¼: {app.Environment.WebRootPath}");

// ȷ��SwaggerĿ¼����
var swaggerDir = Path.Combine(app.Environment.ContentRootPath, "swagger");
if (!Directory.Exists(swaggerDir))
{
    Directory.CreateDirectory(swaggerDir);
    Console.WriteLine("�Ѵ���SwaggerĿ¼");
}

// ����HTTP����ܵ�
// �м��˳�����Ҫ����Ҫ������ȷ˳������

// �쳣���������㣩
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// ���������־�м��
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    Console.WriteLine($"[{startTime:HH:mm:ss}] {context.Request.Method} {context.Request.Path}");

    await next();

    var endTime = DateTime.UtcNow;
    var duration = (endTime - startTime).TotalMilliseconds;
    Console.WriteLine($"[{endTime:HH:mm:ss}] {context.Request.Method} {context.Request.Path} - {context.Response.StatusCode} ({duration}ms)");
});

// ��̬�ļ�����
app.UseStaticFiles();

// ·��
app.UseRouting();

// CORS
app.UseCors("cors");

// ��֤����Ȩ
app.UseAuthentication();
app.UseAuthorization();

// Swagger����
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        // ��̬���÷�����URL
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
    c.RoutePrefix = "swagger"; // ����·��Ϊ http://yourdomain/swagger

    // ����ѡ��
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.DisplayOperationId();
    c.DefaultModelsExpandDepth(2);
    c.DefaultModelExpandDepth(2);
    c.ShowExtensions();
    c.EnableFilter();

    // ���������ض�����
    if (!app.Environment.IsDevelopment())
    {
        c.DocExpansion(DocExpansion.None); // Ĭ���۵�����
        c.EnableValidator(); // ������֤��
    }
});

// �ն˵�����
app.MapControllers();

// �������˵�
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var info = new
    {
        Application = assembly.GetName().Name,
        Version = assembly.GetName().Version?.ToString(),
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTime.UtcNow,
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

// ���Ӧ��
try
{
    Console.WriteLine("���HTTP����...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"���ʧ��: {ex.Message}");
    Console.WriteLine($"��ջ����: {ex.StackTrace}");
    throw;
}