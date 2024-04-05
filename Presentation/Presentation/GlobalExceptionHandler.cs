namespace Presentation;

///<summary>
/// The main idea is to not have a lot of try catch blocks inside the endpoints doing the same job.
/// Instead we propagate the exception up to this middleware, which will catch it and log it,
/// and let the user know that something went wrong.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            //Run next http call
            await next(context);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            ProblemDetails problem =
                new()
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Type = "Server error",
                    Title = "Server error",
                    Detail =
                        "An internal server has occurred. Sorry, we encountered an unexpected issue while processing your request. Please ensure that the data is correct. We suggest you try again later or contact support if the problem persists."
                };

            var json = JsonConvert.SerializeObject(problem);

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(json);
        }
    }
}
