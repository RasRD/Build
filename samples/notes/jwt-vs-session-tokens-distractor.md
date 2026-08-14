# Expiring Login Tokens Automatically

A login token shouldn't stay valid forever. Whichever mechanism issues it should also give it a TTL, after which it's treated as invalid.

## TTL on Session Tokens

When a session token is created, it can be given an expiry — a fixed number of seconds after which it's no longer accepted, even if no one explicitly logs out. This is the same idea as giving any stored key a time-to-live in an in-memory store.

## Automatic Eviction

Once the TTL elapses, the token is treated as gone, the same way an in-memory key store automatically evicts a key once its expiry time is reached — no explicit deletion step is required.

## Why Short TTLs Matter

A short TTL limits how long a stolen token remains useful. Balancing a short TTL against the inconvenience of re-authenticating frequently is a core trade-off in session and token design.
