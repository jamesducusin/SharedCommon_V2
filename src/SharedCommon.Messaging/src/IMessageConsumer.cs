using MassTransit;

namespace SharedCommon.Messaging;

/// <summary>
/// Marker interface for SharedCommon message consumers.
/// Implementors inherit idempotency, retry, and DLQ policies registered by <c>AddSharedMessaging</c>.
/// Consumer implementations must be idempotent: the same message may be delivered more than once.
/// </summary>
/// <typeparam name="T">The message contract type.</typeparam>
public interface IMessageConsumer<T> : IConsumer<T> where T : class;
