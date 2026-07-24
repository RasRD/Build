# Caching

Caching stores the result of an expensive computation or lookup so that repeated access is faster.

## Cache-Aside (Lazy Loading)

The application checks the cache first. On a miss, it fetches from the database, stores the result in the cache, and returns it. The cache only contains data that has actually been requested.

## Write-Through

Every write goes to the cache and the database simultaneously. Reads always hit warm data. The trade-off is higher write latency.

## Write-Behind (Write-Back)

Writes go to the cache immediately and are flushed to the database asynchronously. Lower write latency, but data can be lost if the cache crashes before the flush.

## Cache Invalidation

When the underlying data changes, stale cache entries must be removed or updated. Invalidation is one of the hardest problems in distributed systems.

Strategies include time-based expiry (TTL), event-driven invalidation, and versioned keys.

## Eviction Policies

When the cache is full, an eviction policy decides what to remove:

- LRU (Least Recently Used): evicts the item not accessed for the longest time.
- LFU (Least Frequently Used): evicts the item accessed least often.
- FIFO: evicts the oldest item regardless of access patterns.
