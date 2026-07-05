namespace Templates.Infrastructure.Persistence.Repositories;

using SharedCommon.Core;
using Templates.Domain.Orders;
using Templates.Infrastructure.Persistence.Dapper;

/// <summary>
/// Dapper-based implementation of IOrderRepository.
/// Optimized for high-performance scenarios (1000+ TPS).
/// </summary>
public sealed class OrderRepository(IDapperRepository<Order, OrderId> dapperRepository) : IOrderRepository
{
    /// <summary>
    /// Adds a new order to the repository using sp_Order_Insert.
    /// </summary>
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(order, nameof(order));

        var parameters = new
        {
            OrderId = order.Id.Value,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            CreatedAt = order.CreatedAt
        };

        await dapperRepository.ExecuteAsync("sp_Order_Insert", parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an order by its ID using sp_Order_GetById.
    /// </summary>
    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(id, nameof(id));

        return await dapperRepository.GetByIdAsync(id.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves orders for a specific customer using sp_Order_GetByCustomer.
    /// </summary>
    public async Task<List<Order>> ListByCustomerAsync(
        Guid customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Guard.AgainstLessThan(pageNumber, 1, nameof(pageNumber));
        Guard.AgainstLessThan(pageSize, 1, nameof(pageSize));

        var parameters = new
        {
            CustomerId = customerId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Offset = (pageNumber - 1) * pageSize
        };

        var orders = await dapperRepository.QueryAsync("sp_Order_GetByCustomer", parameters, cancellationToken)
            .ConfigureAwait(false);

        return orders.ToList();
    }

    /// <summary>
    /// Gets the total count of orders for a customer using sp_Order_CountByCustomer.
    /// </summary>
    public async Task<int> CountByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));

        var parameters = new { CustomerId = customerId };

        return await dapperRepository.QueryScalarAsync<int>("sp_Order_CountByCustomer", parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an existing order using sp_Order_Update.
    /// </summary>
    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(order, nameof(order));

        var parameters = new
        {
            OrderId = order.Id.Value,
            Status = order.Status.ToString(),
            UpdatedAt = DateTime.UtcNow
        };

        await dapperRepository.ExecuteAsync("sp_Order_Update", parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an order using sp_Order_Delete (soft delete).
    /// </summary>
    public async Task DeleteAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(id, nameof(id));

        var parameters = new { OrderId = id.Value };

        await dapperRepository.ExecuteAsync("sp_Order_Delete", parameters, cancellationToken)
            .ConfigureAwait(false);
    }
}
