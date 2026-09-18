using Zouq.Domain.Enums;

namespace Zouq.Domain.Orders;

public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new() { OrderStatus.InProduction, OrderStatus.Cancelled },
        [OrderStatus.InProduction] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new() { OrderStatus.Delivered, OrderStatus.Cancelled },
        [OrderStatus.Delivered] = new() { OrderStatus.Refunded },
        [OrderStatus.Cancelled] = new(),
        [OrderStatus.Refunded] = new()
    };

    public static OrderStatus Normalize(OrderStatus status) =>
        status == OrderStatus.Confirmed ? OrderStatus.Processing : status;

    public static bool IsTerminal(OrderStatus status)
    {
        status = Normalize(status);
        return status is OrderStatus.Cancelled or OrderStatus.Refunded;
    }

    /// <summary>
    /// Idempotent: same status is always allowed (no-op).
    /// </summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to)
    {
        from = Normalize(from);
        to = Normalize(to);
        if (from == to) return true;
        return Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }

    public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Illegal order status transition: {Normalize(from)} → {Normalize(to)}.");
    }
}
