using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace RentServer.Exception
{
    public class ApiExceptionMiddleware
    {
        private readonly IHostingEnvironment _env;
        private readonly IApplicationBuilder _app;
        
        public ApiExceptionMiddleware(IApplicationBuilder app, IHostingEnvironment environment)
        {
            _env = environment;
            _app = app;
        }
        
        public async Task Invoke(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex == null) return;

            object error;
            var detail = new string[]{};
            
            if (ex is ApiException exception)
            {
                error = new 
                {
                    message = exception.ErrorMsg,
                    errorCode = exception.ErrorCode,
                    success = false,
                    detail = _env.IsDevelopment() ? exception.StackTrace.Split("\n"): detail,
                };
                context.Response.StatusCode = exception.Status;
            }
            else
            {
                error = new 
                {
                    message = ex.Message,
                    errorCode = 500,
                    success = false,
                    detail = _env.IsDevelopment() ? ex.StackTrace.Split("\n"): detail,
                };

                context.Response.StatusCode = 500;
            }
            
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:8081");

            using (var writer = new StreamWriter(context.Response.Body))
            {
                new JsonSerializer().Serialize(writer, error);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
        
    }
}