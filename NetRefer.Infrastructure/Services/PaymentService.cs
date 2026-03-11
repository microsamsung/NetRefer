using System.Net;
using NetRefer.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;

namespace NetRefer.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        HttpClient httpClient,
        ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ProcessPaymentAsync(
        int orderId,
        decimal amount)
    {
        var correlationId = Guid.NewGuid();

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                3,
                retry => TimeSpan.FromSeconds(retry),
                (exception, time, retry, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {Retry} for Order {OrderId}",
                        retry,
                        orderId);
                });

        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://payment-provider/api/pay");

                request.Headers.Add(
                    "Idempotency-Key",
                    orderId.ToString());

                request.Headers.Add(
                    "Correlation-Id",
                    correlationId.ToString());

                request.Content =
                    new StringContent(
                        $"{{\"orderId\":{orderId},\"amount\":{amount}}}",
                        System.Text.Encoding.UTF8,
                        "application/json");

                var response =
                    await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Payment failed OrderId {OrderId} Status {Status}",
                        orderId,
                        response.StatusCode);

                    return false;
                }

                _logger.LogInformation(
                    "Payment success OrderId {OrderId}",
                    orderId);

                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Payment error OrderId {OrderId}",
                orderId);

            return false;
        }
    }
}