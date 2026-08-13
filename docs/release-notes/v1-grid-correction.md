# v1 patch release notes: APRS grid correction

The v1 `GET /api/ars/v1/Aprs/loc/{id}/grid` response keeps its existing
route and JSON shape. For requests that return multiple APRS stations, each
grid is now calculated from that station's own latitude and longitude.

Clients that previously received repeated copies of the first station's grid
should treat the corrected values as a patch-level defect fix. No client
request changes are required.
