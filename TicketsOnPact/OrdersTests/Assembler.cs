namespace OrdersTests;

using System;
using Domain.Aggregates;
using System.Collections.Generic;

public class OrderAssembler
{
    private Guid _orderId = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private Guid _trackingNumber = Guid.NewGuid();
    private Address _shippingAddress = new Address("Marszałkowska 1", "Warszawa", "00-001");
    private readonly List<(Guid ProductId, int Quantity, decimal Price)> _lines = new();
    private bool _confirmed = true;
    private bool _shipped = false;
    private bool _delivered = false;

    private OrderAssembler()
    {
        _lines.Add((Guid.NewGuid(), 1, 99m));
    }

    public static OrderAssembler Default => new OrderAssembler();

    public OrderAssembler WithOrderId(Guid orderId)
    {
        _orderId = orderId;
        return this;
    }

    public OrderAssembler Confirmed()
    {
        _confirmed = true;
        return this;
    }

    public OrderAssembler Shipped()
    {
        _shipped = true;
        return this;
    }

    public OrderAssembler Delivered()
    {
        _delivered = true;
        return this;
    }

    public OrderAggregate Build()
    {
        var order = OrderAggregate.CreateNew(_orderId, _customerId, _shippingAddress);

        foreach (var line in _lines)
        {
            order.AddItem(new ProductId(line.ProductId), line.Quantity, new Money(line.Price));
        }

        if (_confirmed) order.Confirm();
        if (_shipped) order.Ship(_trackingNumber.ToString());

        return order;
    }
}