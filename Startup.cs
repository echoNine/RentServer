using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RentServer.Exception;
using RentServer.Setting;

namespace RentServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            
            new AppSetting().Initial(configuration);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                options.AddPolicy("default", policy =>
                {
                    policy.WithOrigins(AppSetting.Cors.Origins)
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod();
                });
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            services.AddOptions();
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddSession();
            services.Configure<SmtpSettings>(Configuration.GetSection("Smtp"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseSession();
            app.UseCors("default");
            
            app.UseExceptionHandler(new ExceptionHandlerOptions 
            {
                ExceptionHandler = new ApiExceptionMiddleware(app, env).Invoke
            });
            
            app.UseMvc();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot/static")),
                RequestPath = new PathString("/src")
            });
            app.UseHsts();
        }
    }
}
