using Sales.Events;

namespace Sales;

public class ResourceUnblockedEventHandler
{
    public Task<bool> Handle(ResourceUnblocked message)
    {
        return Task.FromResult(true);
    }
}