namespace Templates.Domain.Orders;

/// <summary>
/// Repository interface for Order aggregate root.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Adds a new order to the repository.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an order by its ID.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The order if found; otherwise null.</returns>
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves orders for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of orders.</returns>
    Task<List<Order>> ListByCustomerAsync(Guid customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of orders for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="order">The order to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an order.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(OrderId id, CancellationToken cancellationToken = default);
}
