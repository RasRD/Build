# Load Balancing

Load balancing distributes incoming requests across multiple backend servers so no single server is overwhelmed and the system stays available if one server fails.

## Round Robin

Requests are sent to servers in rotating order, one after another. Simple to implement, but ignores differences in server load or capacity.

## Least Connections

The load balancer tracks how many active connections each server is handling and sends the next request to the server with the fewest. Better than round robin when requests vary in cost.

## Weighted Distribution

Servers are assigned weights based on capacity (e.g., CPU, memory). A server with twice the weight receives roughly twice the traffic.

## Health Checks

The load balancer periodically probes each backend server. Servers that fail health checks are removed from rotation until they recover, preventing requests from being routed to a dead or degraded instance.
