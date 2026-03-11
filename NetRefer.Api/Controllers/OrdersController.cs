using Microsoft.AspNetCore.Mvc;
using NetRefer.Application.Interfaces;
using NetRefer.Application.Model;

namespace NetRefer.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        IPaymentService paymentService,
        IIdempotencyService idempotencyService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order and processes payment safely
    /// </summary>
    [HttpPost("submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Submit([FromBody] OrderViewModel model)
    {
        var correlationId =
            HttpContext?.Items["CorrelationId"]?.ToString()
            ?? HttpContext?.TraceIdentifier
            ?? Guid.NewGuid().ToString();

        var idempotencyKey =
            Request.Headers["Idempotency-Key"]
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(
                title: "Missing Idempotency Key",
                detail: "Idempotency-Key header required",
                statusCode: 400);
        }

        if (await _idempotencyService.ExistsAsync(idempotencyKey))
        {
            _logger.LogInformation(
            "Duplicate request prevented Key {Key} Correlation {CorrelationId}",
            idempotencyKey,
            correlationId);

            return Conflict(new ProblemDetails
            {
                Title = "Duplicate request",
                Detail = "Request already processed",
                Status = 409
            });
        }

        if (model == null)
        {
            return Problem(
                title: "Invalid request",
                statusCode: 400);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (model.Items == null || !model.Items.Any())
        {
            return Problem(
                title: "Order must contain items",
                statusCode: 400);
        }

        try
        {
            var customerId = 1001;

            _logger.LogInformation(
            "Order creation started Customer {CustomerId} Correlation {CorrelationId}",
            customerId,
            correlationId);

            var orderId =
            await _orderService.CreateOrderAsync(
                customerId,
                model.Items);

            _logger.LogInformation(
            "Order created OrderId {OrderId}",
            orderId);

            var amount =
            model.Items.Sum(x => x.Quantity * 10);

            var paymentResult =
            await _paymentService.ProcessPaymentAsync(
                orderId,
                amount);

            if (!paymentResult)
            {
                _logger.LogError(
                "Payment failed Order {OrderId}",
                orderId);

                return Problem(
                    title: "Payment failed",
                    detail: "Provider rejected transaction",
                    statusCode: 500);
            }

            await _idempotencyService.StoreAsync(
                idempotencyKey,
                orderId);

            _logger.LogInformation(
            "Payment success Order {OrderId}",
            orderId);

            return Ok(new
            {
                OrderId = orderId,
                Status = "Paid",
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
            ex,
            "Order failure Correlation {CorrelationId}",
            correlationId);

            return Problem(
                title: "Internal server error",
                detail: "Unexpected failure",
                statusCode: 500);
        }
    }
}