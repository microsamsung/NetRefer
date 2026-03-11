namespace NetRefer.UnitTests
{
    public class OrderTests
    {
        [Fact]
        public void Should_Create_Order()
        {
            var order = new
            {
                CustomerName = "Test",
                Amount = 100
            };

            Assert.Equal("Test", order.CustomerName);
        }
    }
}
