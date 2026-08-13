# Migrate from v1 to v2

## Release and support

Version 2 is released on 2026-08-13. It is selected only by the URL segment;
do not send `x-api-version`. V1 remains supported until the production
observation and retirement gates in the [modernization plan](../plans/2026-08-12-modernization-remediation.md)
are complete. No v1 sunset or removal date is currently scheduled.

## Route map

| v1 route | v2 route                                                           | Client change |
| --- |--------------------------------------------------------------------| --- |
| `GET /api/ars/v1/Callsign?call={call}` | `GET /api/ars/v2/callsign?callsign={call}`                         | Read the matching callsign-record fields; handle non-2xx results. |
| `GET /api/ars/v1/Aprs/loc/{id}` | `GET /api/ars/v2/aprs/locations?callsigns={callsigns}`             | Use a URL-encoded, comma-separated callsign list. |
| `GET /api/ars/v1/Aprs/loc/{id}/coord` | `GET /api/ars/v2/aprs/locations/coordinates?callsigns={callsigns}` | Read `latitude` and `longitude`. |
| `GET /api/ars/v1/Aprs/loc/{id}/grid` | `GET /api/ars/v2/aprs/locations/grids?callsigns={callsigns}`       | Read each station's independently calculated grid. |
| `POST /api/ars/v1/Contact/EnhanceBearing` | `POST /api/ars/v2/contacts/enhance-bearing`                        | Send/receive immutable DTOs and handle validation. |
| `GET /api/ars/v1/Configuration/version` | `GET /api/ars/v2/configuration/version`                            | Read the documented version DTO. |
| `GET /api/ars/v1/DataService/SubscriptionExpirationTime` | `GET /api/ars/v2/configuration/qrz/subscription-expiration`        | Read the `subscriptionExpiration` object property. |
| `GET /api/ars/v1/Maidenhead/bearing?srcGrid={src}&destGrid={dest}` | `GET /api/ars/v2/maidenhead/bearing?srcGrid={src}&destGrid={dest}` | Read the JSON `bearing` property. |
| `GET /api/ars/v1/Maidenhead/distance?srcGrid={src}&destGrid={dest}` | `GET /api/ars/v2/maidenhead/distance?srcGrid={src}&destGrid={dest}` | Read JSON `miles` and `kilometers` properties. |
| `GET /api/ars/v1/Maidenhead/grid?lat={lat}&lon={lon}` | `GET /api/ars/v2/maidenhead/grid?lat={lat}&lon={lon}` | Read the JSON `grid` property. |

The legacy path callsign route remains deprecated. Use the v1 query route
during any interim period, then move directly to the v2 route.

## APRS callsign encoding and limits

`callsigns` is a query-string comma-separated list of one to 25 unique APRS
callsigns. Each callsign is one to 16 characters and the complete query value
is at most 512 characters. Callsigns with a slash must encode the slash as
`%2F`:

```text
GET /api/ars/v2/aprs/locations/grids?callsigns=C9ALL%2FP,C9ALL%2FR
```

The server decodes `%2F` before sending `C9ALL/P` or `C9ALL/R` to aprs.fi.
Query selection avoids route-segment decoding differences in clients and
proxies; do not use a callsign in the v2 route path.

## Contract changes

V2 returns a purpose-built callsign contract containing every callsign-record
field v1 returns, including address, email, license, geographic, QSL, zone,
and profile fields. The response deliberately omits the QRZ `session` object,
session key, subscription metadata, and provider messages.

`GET /api/ars/v2/configuration/qrz/subscription-expiration` deliberately exposes
only the required QRZ subscription expiration timestamp. It does not expose a
session object, token, request count, provider messages, or other session data.

The v2 APRS location response contains every station-entry field v1 returns:
`name`, `sourceCallsign`, `destinationCallsign`, `latitude`, `longitude`,
`comment`, `path`, `type`, `time`, `lastTime`, `class`, and `symbol`. It omits
only the APRS provider wrapper/status fields. Coordinate and grid routes
intentionally return their route-specific subsets. Contact responses keep the logical v1 names (`deCall`, `deGrid`,
`dxCall`, `dxGrid`, `bearing`) but are new immutable values; blank grids return
a successful response with `bearing: null`. Non-empty grids must be valid
four-, six-, or eight-character Maidenhead locators. `dxCall`, when provided,
must be non-blank and at most 16 characters.

V2 Maidenhead calculations return JSON objects: `bearing` is rounded degrees,
`distance` has rounded `miles` and `kilometers`, and `grid` contains the
calculated locator. The routes validate grid locator format and coordinate
ranges before calculating a response.

Representative success response:

```json
{
  "callsign": "C9ALL",
  "firstName": "Alex",
  "state": "IL",
  "country": "United States",
  "grid": "EN61"
}
```

Representative failure response:

```json
{
  "title": "The requested resource was not found.",
  "status": 404
}
```

## Status codes and access controls

V1 callers can receive vendor-shaped success objects even after provider
errors. V2 uses HTTP status codes instead: `400` for invalid input, `404` for
confirmed absence, `429` for quota pressure, `502` for invalid provider data,
`503` for unavailable or authentication/subscription provider failures, and
`504` for provider timeouts. Clients must handle all non-2xx responses as
failures and may use a returned `Retry-After` header for `429` responses.

The current deployment mode is public. Rate limiting is enabled by default and
is configured by the operator; deployments that set `RateLimiting__Enabled`
to `false` must use equivalent private network-level quota protection. No
pagination is provided: APRS lists are bounded by the 25-callsign request
limit.

## Cutover sequence

1. Confirm the deployed access policy and obtain a credential if one is later
   required.
2. Change the base URL to a v2 route and update the client to the stable DTO.
3. Add handling for all documented non-2xx Problem Details responses.
4. Deploy the client and compare v1 and v2 successful results for a defined
   sampling period appropriate to its traffic.
5. Remove v1 calls after the comparison succeeds; retain v1 fallback only for
   the supported migration window.
