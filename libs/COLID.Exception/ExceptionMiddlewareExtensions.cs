using Microsoft.AspNetCore.Builder;

namespace COLID.Exception
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
