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

The compose file requires a local .env file to provide the credential for QRZ, and aprs.fi api.

Ex.

QrzUsername=Bob
QrzPassword=1234
AprsApiKey=xyz
