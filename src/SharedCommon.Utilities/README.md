# SharedCommon.Utilities

Lightweight, dependency-free extension methods for strings, dates, collections, and types. No DI registration required — reference the package and use the extensions directly.

## Installation

```bash
dotnet add package SharedCommon.Utilities
```

No `AddSharedUtilities()` call needed. All methods are static extension methods.

---

## StringExtensions

```csharp
using SharedCommon.Utilities;

// URL-safe slug (strips accents, lowercases, replaces spaces with hyphens)
"Héllo Wörld! 2024".ToSlug()          // "hello-world-2024"
"Order #42".ToSlug()                  // "order-42"

// Truncate with ellipsis
"Long product description text".Truncate(15)          // "Long product..."
"Long product description text".Truncate(15, " …")    // "Long product …"

// Mask sensitive values for logging
"4111111111111111".Mask()             // "************1111"
"secret-api-key".Mask(visibleChars: 3, maskChar: '#') // "###########key"
"tok_abc123".Mask(visibleChars: 6)    // "****123"

// Null-safe checks (with nullable flow analysis support)
string? name = GetName();
if (name.IsNullOrEmpty()) return;     // name is non-null after this
if (name.IsNullOrWhiteSpace()) return;

// Title case
"order service v2".ToTitleCase()      // "Order Service V2"
```

---

## DateTimeExtensions

```csharp
using SharedCommon.Utilities;

var now = DateTimeOffset.UtcNow;

// UTC conversion
var utc = now.ToUtc();

// Day boundaries
var start = now.StartOfDay();   // 2024-03-15 00:00:00.000 +00:00
var end   = now.EndOfDay();     // 2024-03-15 23:59:59.999 +00:00

// Business day arithmetic (skips Saturday + Sunday)
var dueDate = now.AddBusinessDays(5);   // skip weekends
var prevBiz = now.AddBusinessDays(-1);  // last business day

// Weekday check
now.IsBusinessDay()   // false on Saturday/Sunday, true otherwise

// Unix timestamps
now.ToUnixTimestamp()              // seconds
now.ToUnixTimestampMilliseconds()  // milliseconds

// Temporal comparisons
order.ExpiresAt.IsInThePast()     // true if expired
token.ValidUntil.IsInTheFuture()  // true if still valid
```

---

## CollectionExtensions

```csharp
using SharedCommon.Utilities;

// Batch a large list for chunked processing (e.g. bulk DB inserts)
var records = GetThousandsOfRecords();
foreach (var batch in records.Batch(size: 100))
{
    await _repo.BulkInsertAsync(batch);
}

// Null-safe Any — no NullReferenceException
IEnumerable<Order>? orders = GetOrders();
orders.SafeAny()                      // false when null
orders.SafeAny(o => o.IsPending())    // false when null

// Filter nulls from a mixed collection
IEnumerable<string?> raw = ["a", null, "b", null, "c"];
IEnumerable<string> clean = raw.WhereNotNull();   // ["a", "b", "c"]

// Null-safe ForEach
items?.ForEach(item => Process(item));

// Null → empty (avoids null checks throughout the call chain)
IEnumerable<Tag> tags = dto.Tags.EmptyIfNull();

// Wrap a single value as IEnumerable
var single = myOrder.Yield();

// Null-or-empty check for collections
if (items.IsNullOrEmpty()) return;
```

---

## TypeExtensions

```csharp
using SharedCommon.Utilities;

// Friendly display name for generic types
typeof(List<int>).GetFriendlyName()            // "List<Int32>"
typeof(Dictionary<string, int>).GetFriendlyName() // "Dictionary<String, Int32>"

// Nullability checks
typeof(string?).IsNullable()           // true
typeof(int?).IsNullable()              // true
typeof(int).IsNullable()               // false

typeof(int?).UnwrapNullable()          // typeof(int)
typeof(string).UnwrapNullable()        // typeof(string) (unchanged)

// Interface check
typeof(OrderService).Implements<IOrderService>()   // true

// Concrete type check (not abstract, not interface)
typeof(OrderService).IsConcrete()      // true
typeof(IOrderService).IsConcrete()     // false
typeof(AbstractBase).IsConcrete()      // false

// Closed generic check
typeof(List<int>).IsClosedGenericOf(typeof(List<>))  // true
typeof(List<>).IsClosedGenericOf(typeof(List<>))     // false (open generic)
```
