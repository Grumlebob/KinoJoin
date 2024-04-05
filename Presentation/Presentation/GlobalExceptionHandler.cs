using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Presentation;

//The main idea is to not have a lot of try catch blocks inside the endpoints doing the same job.
//Instead we propogate the exception up to this middleware, which will catch it and log it, and let the user know that something went wrong.
public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);

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