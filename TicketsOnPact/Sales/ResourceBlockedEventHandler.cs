using Sales.Events;

namespace Sales;

public class ResourceBlockedEventHandler
{
    public Task<bool> Handle(ResourceBlocked message)
    {
        return Task.FromResult(true);
    }
}