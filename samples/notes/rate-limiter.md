# Rate Limiting

Rate limiting controls how many requests a client can make in a time window to protect services from overload and abuse.

## Token Bucket

A bucket holds tokens up to a maximum capacity. Tokens are added at a fixed rate. Each request consumes one token. If the bucket is empty, the request is rejected or queued.

Token bucket allows short bursts: a client that has been idle accumulates tokens and can spend them all at once.

## Fixed Window Counter

A counter increments for each request within a fixed time window (e.g., one minute). When the window resets, the counter starts from zero.

The boundary problem: a client can send requests at the end of one window and the start of the next, doubling the effective rate.

## Sliding Window Log

Every request timestamp is stored. On each request, timestamps older than the window are removed. The count of remaining timestamps determines whether the request is allowed.

More accurate than fixed window but requires more memory proportional to the number of requests.

## Leaky Bucket

Requests enter a queue (the bucket) and are processed at a constant rate. If the queue is full, new requests are dropped.

Leaky bucket smooths traffic into a steady output rate regardless of burst patterns.
