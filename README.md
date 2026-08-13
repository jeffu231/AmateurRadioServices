# Amateur Radio  Services

A collection of api for Amateur Radio with a docker compose file to host them.

## APRS

This includes 3 api endpoints to retireve simple info like the last coordinate or grid info along with a fuller response with the full data available. The source of the data is aprs.fi and an API key is required to build and deploy the api.

## Callsign

This provides callsign lookup data. Right now it wraps calls to QRZ xml service to provide a simple wrapper around the endpoint to provide abstraction. An xml subscription and credentials from QRZ are required to use the API.

## Contact

This api provides an ability to take a a local call with a grid and a dx call with a 4 character grid and do a lookup agaist the callsign api to determine if the 4 character grid can be upgraded to a 6 character grid by matching the first 4 characters. A proper bearing is returned using the Maindenhead api with the result.

## Maidenhead

This API provides the distance between two grids, the bearing between two grids, or the grid for a given set of coordinates.

## Docker Compose

The compose file utilizes traefik for managing routes. Remove those sections if not needed. It also requires a local .env file to provide the credential for QRZ, aprs.fi, and the hostname that the container is hosted in for the traefik route.

Ex.

QrzUsername=Bob
QrzPassword=1234
AprsApiKey=xyz
HOSTNAME=myhost.com

The API remains intentionally public during the v1-to-v2 migration, so existing clients do not need a new credential. It uses a fixed-window rate limit of 60 requests per minute for each direct client IP address and a global limit of 8 concurrent requests. Configure `RateLimiting__PublicPermitLimit`, `RateLimiting__WindowSeconds`, and `RateLimiting__GlobalConcurrencyPermitLimit` in the deployment environment to reduce capacity during an incident. Set `RateLimiting__Enabled=false` only for a private deployment with equivalent network-level protection. Do not use client-supplied forwarded headers for rate-limit partitioning until trusted proxy configuration is added.

Use hierarchical configuration keys in non-Compose deployments: `Qrz__Username`, `Qrz__Password`, and `Aprs__ApiKey`. The application temporarily accepts the legacy flat keys (`QrzUsername`, `QrzPassword`, and `AprsApiKey`) to allow a safe deployment migration, but new deployments must use the hierarchical names. Keep all credential values outside source control.

Docker checks `/health/live` to establish that the process can answer HTTP.
Traefik checks `/health/ready` before it sends client traffic to the
container. Readiness validates only local, already-loaded configuration; it
does not call QRZ or APRS and therefore does not consume provider quota.
