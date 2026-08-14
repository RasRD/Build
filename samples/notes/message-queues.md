# Message Queues

A message queue lets one part of a system (the producer) send work to another part (the consumer) asynchronously, without both being available at the same time.

## Producer/Consumer Model

Producers publish messages onto a queue. One or more consumers pull messages off the queue and process them independently of when they were published.

## At-Least-Once Delivery

Most queues guarantee a message is delivered at least once — if a consumer crashes before acknowledging a message, it is redelivered. Consumers must be able to handle receiving the same message twice.

## Dead-Letter Queues

Messages that repeatedly fail processing are moved to a separate dead-letter queue instead of being retried forever, so they can be inspected without blocking the main queue.

## Backpressure

If consumers can't keep up with the rate of incoming messages, the queue grows. Backpressure mechanisms slow down or pause producers so the queue doesn't grow unbounded.
