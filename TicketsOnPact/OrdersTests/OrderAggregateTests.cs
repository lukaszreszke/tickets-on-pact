using Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace OrdersTests
{
    public class AssemblerTest
    {
        [Fact]
        public void CanPersistOrderWithItemsInMemory()
        {
            var options = new DbContextOptionsBuilder<OrdersDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new OrdersDbContext(options);
            var orderId = Guid.NewGuid();
            var order = OrderAssembler.Default.WithOrderId(orderId).Confirmed().Build();
            context.Add(order);
            context.SaveChanges();
            
            var theOrder = context.Orders.Single(x => x.Id == orderId);
            Assert.Equal(orderId, theOrder.Id);
            Assert.Equal(OrderStatus.Confirmed, theOrder.Status);
        }
    }
}
