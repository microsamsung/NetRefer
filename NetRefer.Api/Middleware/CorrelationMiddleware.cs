namespace NetRefer.Api.Middleware;

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;

        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        await _next(context);
    }
}