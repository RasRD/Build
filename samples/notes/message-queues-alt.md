# Decoupling Producers and Consumers with Queues

Instead of having one service call another directly and wait for a response, a queue lets the caller hand off a unit of work and move on, trusting that something else will pick it up later.

## Who Writes, Who Reads

The side generating work drops messages onto the queue; the side doing the work reads from it whenever it's ready, on its own schedule.

## Redelivery on Failure

If whatever was processing a message dies before confirming it finished, most queue systems will hand that same message out again rather than lose it — which means the processing logic has to tolerate seeing a duplicate.

## Setting Aside Poison Messages

A message that keeps causing failures no matter how many times it's retried gets shuffled off into a holding area instead of looping forever and jamming up everything behind it.

## Slowing the Firehose

When consumers fall behind the rate messages are arriving, the backlog builds up. Some systems respond by throttling how fast producers are allowed to add new work, keeping the backlog from growing without bound.
