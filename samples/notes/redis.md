# Redis

Redis is an in-memory data structure store used as a database, cache, and message broker.

## Data Structures

Redis supports strings, lists, sets, sorted sets, and hashes. Each type maps to specific use cases.

Sorted sets assign a floating-point score to each member. Members are ordered by score, making them ideal for leaderboards and priority queues.

## Persistence

Redis writes data to disk using RDB snapshots or AOF logs. RDB takes periodic snapshots; AOF appends every write command.

## Pub/Sub

Redis Pub/Sub allows publishers to send messages to channels without knowing who subscribes. Subscribers receive messages in real time.

## Expiry

Keys can be given a TTL in seconds or milliseconds. Redis evicts them automatically when the time expires, which is useful for session tokens and temporary data.
