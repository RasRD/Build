# JWT vs Session Tokens

Both JWTs and server-side sessions are ways to keep a user authenticated across requests, but they store state in different places.

## Session Tokens

A session token is a random identifier the server hands to the client after login. The server keeps the actual session data (user ID, permissions, etc.) in its own store, keyed by that identifier. The client just presents the token; the server looks up the rest.

## JSON Web Tokens (JWT)

A JWT is a signed, self-contained token with three parts: a header, a payload (claims like user ID and expiry), and a signature. The server can verify the signature without looking anything up — all the data it needs is inside the token itself.

## Stateless vs Stateful

Because a JWT carries its own claims, verifying it doesn't require a database or store lookup — this is what "stateless" means here. A session token requires the server to check its session store on every request.

## Revocation Trade-off

Session tokens can be revoked instantly by deleting the entry from the server's store. A JWT, once issued, remains valid until it expires — revoking it early requires extra machinery like a blocklist, since the server can't simply "forget" a self-contained token.
