# Two Ways to Prove a User Is Still Logged In

After a user logs in, the app needs some way to recognize them on their next request without asking for a password again. There are two common approaches, and they differ in where the actual proof of identity lives.

## Server-Held State

One approach: give the client a random-looking identifier and keep everything that matters — who they are, what they're allowed to do — in a store on the server side, indexed by that identifier.

## Self-Contained Tokens

The other approach: hand the client a token that already contains the claims about who they are, cryptographically signed so it can't be tampered with. The server can check the signature and trust the contents without asking anyone else.

## Lookup Required or Not

The server-side-state approach means every request needs a round trip to wherever that state lives. The self-contained approach skips that round trip entirely, since the proof travels with the request.

## The Cost of Early Logout

Killing a server-held session is as easy as deleting its record. Killing a self-contained token early is harder — the token itself is still technically valid, so the server needs some separate way to remember it shouldn't be trusted anymore.
