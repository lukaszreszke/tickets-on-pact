using Sales.Events;

namespace Sales;

public class ResourceUnblockedEventHandler
{
    public Task<bool> Handle(ResourceBlocked message)
    {
        return Task.FromResult(true);
    }
}