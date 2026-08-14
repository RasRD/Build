# Distributing Traffic Across Servers

When a service runs on more than one machine, something has to decide which machine handles each incoming request — that's the load balancer's job. Its goal is to keep any single instance from becoming a bottleneck, and to keep serving traffic even if one instance goes down.

## Rotating Assignment

The simplest approach cycles through the pool of servers in order, handing each new request to the next one in line. It doesn't account for how busy each server currently is.

## Fewest Active Connections

A smarter approach picks whichever server currently has the fewest open connections, which adapts better when some requests take much longer to process than others.

## Capacity-Aware Weighting

Not all servers are equal — a bigger instance can be given a higher weight so it receives a proportionally larger share of traffic than a smaller one.

## Detecting Unhealthy Instances

The balancer regularly checks whether each server is still responding correctly. An instance that stops passing these checks is temporarily taken out of the rotation so it doesn't receive new requests until it recovers.
