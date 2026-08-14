# Protecting Backend Capacity with Traffic Distribution

When too many requests arrive at once, a single server can become overloaded. Spreading requests across a pool of backend servers keeps any one instance from crossing its capacity threshold.

## Overload Protection via Routing

Rather than rejecting requests once a threshold is hit, a router can instead redirect excess requests to backend servers with spare capacity, smoothing bursts across the pool.

## Threshold-Based Routing

Each backend reports how close it is to its request-handling threshold. New requests are routed away from servers nearing that threshold and toward servers with more headroom.

## Burst Handling

A sudden burst of requests is absorbed by momentarily favoring backends that have been idle, before settling back into steady-state distribution once the burst passes.
