using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Domain.Aggregates
{
    public sealed class Money : IEquatable<Money>
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        private Money() { }

        public Money(decimal amount, string currency = "PLN")
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative", nameof(amount));
            Amount = decimal.Round(amount, 2);
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            var result = Amount - other.Amount;
            if (result < 0) throw new InvalidOperationException("Resulting money cannot be negative");
            return new Money(result, Currency);
        }

        public Money Multiply(decimal factor)
        {
            if (factor < 0) throw new ArgumentException("Factor must be non-negative", nameof(factor));
            return new Money(Amount * factor, Currency);
        }

        private void EnsureSameCurrency(Money other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Currency mismatch");
        }

        public override bool Equals(object obj) => Equals(obj as Money);
        public bool Equals(Money other) => other != null && Amount == other.Amount && Currency == other.Currency;
        public override int GetHashCode() => HashCode.Combine(Amount, Currency);
        public override string ToString() => $"{Amount} {Currency}";
    }

    public sealed class ProductId : IEquatable<ProductId>
    {
        public Guid Id { get; private set; }

        private ProductId() { }

        public ProductId(Guid id) => Id = id != Guid.Empty ? id : throw new ArgumentException("Id cannot be empty");
        public override bool Equals(object obj) => Equals(obj as ProductId);
        public bool Equals(ProductId other) => other != null && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id.ToString();
    }

    public sealed class Address
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string PostalCode { get; private set; }
        public string Country { get; private set; }

        private Address() { }

        public Address(string street, string city, string postalCode, string country = "Poland")
        {
            Street = !string.IsNullOrWhiteSpace(street) ? street : throw new ArgumentException("Street required");
            City = !string.IsNullOrWhiteSpace(city) ? city : throw new ArgumentException("City required");
            PostalCode = !string.IsNullOrWhiteSpace(postalCode) ? postalCode : throw new ArgumentException("Postal code required");
            Country = country;
        }

        public override string ToString() => $"{Street}, {PostalCode} {City}, {Country}";
    }

    public class DomainInvariantException : Exception { public DomainInvariantException(string message) : base(message) { } }

    public class OrderAggregate
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public Address ShippingAddress { get; private set; }
        public OrderStatus Status { get; private set; }

        private readonly List<OrderLine> _lines = new List<OrderLine>();
        public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

        private Money _discount = new Money(0);

        private OrderAggregate() { }

        public static OrderAggregate CreateNew(Guid orderId, Guid customerId, Address shippingAddress)
        {
            if (orderId == Guid.Empty) throw new ArgumentException("orderId required");
            if (customerId == Guid.Empty) throw new ArgumentException("customerId required");

            return new OrderAggregate
            {
                Id = orderId,
                CustomerId = customerId,
                ShippingAddress = shippingAddress,
                Status = OrderStatus.Draft
            };
        }

        public void AddItem(ProductId productId, int quantity, Money unitPrice)
        {
            EnsureNotFinalized();
            if (quantity <= 0) throw new DomainInvariantException("Quantity must be positive");
            if (unitPrice == null) throw new ArgumentNullException(nameof(unitPrice));

            var existing = _lines.FirstOrDefault(l => l.ProductId.Equals(productId));
            if (existing != null)
            {
                existing.ChangeQuantity(existing.Quantity + quantity);
            }
            else
            {
                _lines.Add(new OrderLine(productId, quantity, unitPrice));
            }

            EnsureBusinessRules();
        }

        public void RemoveItem(ProductId productId)
        {
            EnsureNotFinalized();
            var existing = _lines.FirstOrDefault(l => l.ProductId.Equals(productId));
            if (existing == null) throw new DomainInvariantException("Item not found in order");
            _lines.Remove(existing);
            EnsureBusinessRules();
        }

        public void ChangeQuantity(ProductId productId, int newQuantity)
        {
            EnsureNotFinalized();
            var existing = _lines.FirstOrDefault(l => l.ProductId.Equals(productId));
            if (existing == null) throw new DomainInvariantException("Item not found in order");
            existing.ChangeQuantity(newQuantity);
            EnsureBusinessRules();
        }

        public void ApplyDiscount(string discountCode, decimal percentage)
        {
            EnsureNotFinalized();
            if (percentage <= 0 || percentage > 100) throw new DomainInvariantException("Invalid discount percentage");
            var total = CalculateItemsTotal();
            _discount = total.Multiply(percentage / 100m);
            EnsureBusinessRules();
        }

        public void Confirm()
        {
            if (Status != OrderStatus.Draft) throw new DomainInvariantException("Only draft orders can be confirmed");
            if (!_lines.Any()) throw new DomainInvariantException("Cannot confirm an empty order");
            Status = OrderStatus.Confirmed;
        }

        public void Ship(string trackingNumber)
        {
            if (Status != OrderStatus.Confirmed) throw new DomainInvariantException("Only confirmed orders can be shipped");
            if (string.IsNullOrWhiteSpace(trackingNumber)) throw new ArgumentException("Tracking number required");
            Status = OrderStatus.Shipped;
        }

        public void Cancel(string reason)
        {
            if (Status == OrderStatus.Shipped) throw new DomainInvariantException("Shipped orders cannot be cancelled");
            Status = OrderStatus.Cancelled;
        }

        private Money CalculateItemsTotal()
        {
            Money total = new Money(0);
            foreach (var l in _lines)
            {
                total = total.Add(l.LineTotal);
            }
            return total;
        }

        public Money CalculateTotalIncludingDiscount()
        {
            var items = CalculateItemsTotal();
            if (_discount.Amount > 0)
            {
                var result = items.Subtract(_discount);
                return result;
            }
            return items;
        }

        private void EnsureNotFinalized()
        {
            if (Status == OrderStatus.Shipped || Status == OrderStatus.Cancelled)
                throw new DomainInvariantException("Order is finalized and cannot be modified");
        }

        private void EnsureBusinessRules()
        {
            if (_lines.Count > 100) throw new DomainInvariantException("Too many unique items in order");
            var total = CalculateItemsTotal();
            if (total.Amount > 1_000_000m) throw new DomainInvariantException("Order value exceeds allowed maximum");
        }
    }

    public enum OrderStatus { Draft, Confirmed, Shipped, Cancelled }

    public sealed class OrderLine
    {
        public int Id { get; private set; }
        public ProductId ProductId { get; private set; }
        public int Quantity { get; private set; }
        public Money UnitPrice { get; private set; }
        public Money LineTotal => UnitPrice.Multiply(Quantity);

        private OrderLine() { }

        public OrderLine(ProductId productId, int quantity, Money unitPrice)
        {
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
            Quantity = quantity;
            UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        }

        public void ChangeQuantity(int newQuantity)
        {
            if (newQuantity <= 0) throw new ArgumentException("Quantity must be positive");
            Quantity = newQuantity;
        }

        public override string ToString() => $"{ProductId} x{Quantity} = {LineTotal}";
    }

    public class OrdersDbContext : DbContext
    {
        public DbSet<OrderAggregate> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }

        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderAggregate>().OwnsOne(o => o.ShippingAddress);
            modelBuilder.Entity<OrderLine>().OwnsOne(l => l.UnitPrice);
            modelBuilder.Entity<OrderLine>().OwnsOne(l => l.ProductId);
            modelBuilder.Entity<OrderAggregate>().Ignore("_discount");
        }
    }
}
