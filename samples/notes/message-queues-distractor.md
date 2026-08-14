# Reducing Load on Downstream Systems with Queues

Placing a queue in front of a downstream system stores incoming work items until the system is ready to process them, which keeps sudden spikes from hitting the downstream system directly.

## Buffering Expensive Work

Rather than every request triggering an expensive operation on the downstream system immediately, requests are stored in the queue and drained at a rate the system can sustain, reducing peak load.

## Reusing Queued Results

If the same unit of work is queued more than once before it's processed, some implementations store the first result and hand it back for the duplicate queue entries instead of doing the expensive work again.

## Warm vs Cold Consumers

A consumer that has already loaded its dependencies processes queued items faster than one starting cold — keeping a pool of already-initialized consumers avoids paying the startup cost per item.
