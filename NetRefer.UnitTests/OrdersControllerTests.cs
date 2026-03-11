using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NetRefer.Api.Controllers;
using NetRefer.Application.Interfaces;
using NetRefer.Application.Model;
using Xunit;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _serviceMock;
    private readonly Mock<IPaymentService> _paymentMock;
    private readonly Mock<IIdempotencyService> _idempotencyMock;
    private readonly Mock<ILogger<OrdersController>> _loggerMock;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _serviceMock = new Mock<IOrderService>();
        _paymentMock = new Mock<IPaymentService>();
        _idempotencyMock = new Mock<IIdempotencyService>();
        _loggerMock = new Mock<ILogger<OrdersController>>();

        _controller = new OrdersController(
            _serviceMock.Object,
            _paymentMock.Object,
            _idempotencyMock.Object,
            _loggerMock.Object);

        var httpContext = new DefaultHttpContext();

        httpContext.Items["CorrelationId"] = "test-id";
        httpContext.Request.Headers["Idempotency-Key"] = "abc-123";

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        _idempotencyMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Submit_Should_Return_BadRequest_When_Model_Null()
    {
        var result = await _controller.Submit(null);

        result.Should().BeOfType<ObjectResult>();
    }

    [Fact]
    public async Task Submit_Should_Return_BadRequest_When_ModelState_Invalid()
    {
        var model = new OrderViewModel();

        _controller.ModelState.AddModelError("Items", "Required");

        var result = await _controller.Submit(model);

        result.Should().BeOfType<ObjectResult>();
    }

    [Fact]
    public async Task Submit_Should_Return_Conflict_When_Duplicate_Request()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _idempotencyMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _controller.Submit(model);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Submit_Should_Create_Order_When_Request_Valid()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _serviceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()))
            .ReturnsAsync(10);

        _paymentMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<int>(),
            It.IsAny<decimal>()))
            .ReturnsAsync(true);

        var result = await _controller.Submit(model);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Submit_Should_Return_500_When_Payment_Fails()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _serviceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()))
            .ReturnsAsync(5);

        _paymentMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<int>(),
            It.IsAny<decimal>()))
            .ReturnsAsync(false);

        var result = await _controller.Submit(model);

        var objectResult =
            result.Should().BeOfType<ObjectResult>().Subject;

        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Submit_Should_Return_500_When_Service_Throws()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _serviceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()))
            .ThrowsAsync(new Exception());

        var result = await _controller.Submit(model);

        var objectResult =
            result.Should().BeOfType<ObjectResult>().Subject;

        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Submit_Should_Call_Service_Once()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _serviceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()))
            .ReturnsAsync(5);

        _paymentMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<int>(),
            It.IsAny<decimal>()))
            .ReturnsAsync(true);

        await _controller.Submit(model);

        _serviceMock.Verify(
            x => x.CreateOrderAsync(
            It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_Should_Call_Payment_Once()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _serviceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()))
            .ReturnsAsync(5);

        _paymentMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<int>(),
            It.IsAny<decimal>()))
            .ReturnsAsync(true);

        await _controller.Submit(model);

        _paymentMock.Verify(
            x => x.ProcessPaymentAsync(
            It.IsAny<int>(),
            It.IsAny<decimal>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_Should_Store_Idempotency_Key()
    {
        var model = new OrderViewModel
        {
            Items = new List<OrderItemViewModel>
            {
                new OrderItemViewModel
                {
                    ProductId=1,
                    Quantity=2
                }
            }
        };

        _serviceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<int>(),
            It.IsAny<List<OrderItemViewModel>>()))
            .ReturnsAsync(5);

        _paymentMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<int>(),
            It.IsAny<decimal>()))
            .ReturnsAsync(true);

        await _controller.Submit(model);

        _idempotencyMock.Verify(
            x => x.StoreAsync(
            It.IsAny<string>(),
            It.IsAny<int>()),
            Times.Once);
    }
}