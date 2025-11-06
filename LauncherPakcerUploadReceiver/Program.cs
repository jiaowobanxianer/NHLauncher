
using LauncherPakcerUploadReceiver.Controllers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;

namespace LauncherPakcerUploadReceiver
{
    public class Program
    {
        public static string ContentRootPath;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ContentRootPath = builder.Environment.ContentRootPath;
            // Add services to the container.
            builder.WebHost.ConfigureKestrel(options =>
            {
                // 允许最大请求体 2GB
                options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(p => p.AddDefaultPolicy(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 2_000_000_000; // 2000 MB
                options.ValueLengthLimit = 2_000_000_000;
                options.BufferBodyLengthLimit = 1_000_000_000;
             
            });
            builder.Services.Configure<UploadSettings>(
                builder.Configuration.GetSection("UploadSettings"));
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseAuthorization();


            app.MapControllers();
            app.Urls.Add("http://0.0.0.0:5000");  // 外网可访问
            app.Run();
        }
    }
}
//Nginx 配置示例：
//client_max_body_size 2000M;

//location / {
//    proxy_pass http://127.0.0.1:5000;

//        proxy_http_version 1.1;
//    proxy_set_header Upgrade $http_upgrade;
//    proxy_set_header Connection keep - alive;
//    proxy_set_header Host $host;
//    proxy_cache_bypass $http_upgrade;
//}


//Systemd 服务示例：
//[Unit]
//Description = LauncherPakcer Upload API
//After=network.target

//[Service]
//WorkingDirectory=/www/wwwroot/hotupdate.jiaowobanxianer.com/LauncherPakcerUploadReceiver
//ExecStart=/www/wwwroot/hotupdate.jiaowobanxianer.com/LauncherPakcerUploadReceiver/LauncherPakcerUploadReceiver
//Restart=always
//RestartSec=10
//Environment=DOTNET_PRINT_TELEMETRY_MESSAGE = false

//[Install]
//WantedBy=multi-user.target
