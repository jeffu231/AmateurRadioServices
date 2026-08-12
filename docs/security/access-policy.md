# API access policy

The API is intentionally public during the v1-to-v2 modernization period. It does not require an API key, mTLS certificate, or identity-provider token, because introducing any of those requirements on the active v1 routes would break existing clients without a migration path.

ASP.NET Core rate limiting protects the paid QRZ and APRS upstream accounts. Every controller route has a fixed-window limit keyed by the direct remote IP address, with a separate application-wide concurrency limit. The defaults are 60 requests per 60 seconds per direct client and 8 concurrent requests overall; an exhausted limit returns HTTP 429 with a `Retry-After` header and a Problem Details body. These limits are deployment configuration, using `RateLimiting__PublicPermitLimit`, `RateLimiting__WindowSeconds`, `RateLimiting__QueueLimit`, and `RateLimiting__GlobalConcurrencyPermitLimit`.

Do not treat `X-Forwarded-For` or similar request headers as client identity yet: the current application has not configured a trusted Traefik proxy/network list, so accepting those headers would let callers choose their own rate-limit partition. The direct address is safe but may group users when Traefik is the immediate peer. Milestone 6 will configure trusted forwarded headers and then use the safely resolved client IP.

The deployment owner can lower `RateLimiting__PublicPermitLimit` or `RateLimiting__GlobalConcurrencyPermitLimit` and restart the service to control active abuse. Restore the previous configured values and restart to roll back a limit change. Record emergency changes in the deployment incident log.

Any move to authenticated access must be introduced as a v2 contract: document the credential mechanism, provide credentials and a migration period to known consumers, add OpenAPI security requirements, and retain v1 anonymous access until its published sunset criteria are satisfied.
