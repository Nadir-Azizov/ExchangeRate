BambooCard - Exchange Rate Service

Overview

BambooCard is a .NET-based service for currency exchange and conversion. Users can search historical rates, retrieve the latest data from a provider, and convert between 31 supported currencies. The application includes role-based authentication, in-memory caching, and background job automation for data updates.

Features

Exchange rate search and conversion

31 currencies supported

Background job to fetch daily rates from Frankfurter at 16:00

Role-based JWT authentication (Admin and User)

Refresh token support

Admin-only manual refresh from provider

Cached responses for fast access

Retry policy for background jobs on provider failures

Settings managed via appsettings.json

Unit tests with xUnit

Custom error handling and exception responses

Setup Instructions

Install prerequisites:

.NET 8 SDK

SQL Server

Clone the repository:
git clone https://github.com/Nadir-Azizov/ExchangeRate

Navigate to project directory:
cd BambooCard

Configure appsettings.json with your values:

SQL connection string

Frankfurter API base URL

JWT settings (key, issuer, audience)

Cache settings

Run the project:
dotnet run --project BambooCard.WebAPI

Access the application at:
http://localhost:5000 or https://localhost:5001

Authentication

Authentication is based on JWT. There are two roles:

Admin: can trigger manual exchange rate refresh

User: can access cached data and perform conversions

Data Update Mechanism

A background job runs every day at 16:00 to fetch new rates from the Frankfurter API. New data is stored in the SQL database and simultaneously updates the in-memory cache. Manual refresh is also available to Admin users. Retry policies are applied in case the provider is temporarily unavailable.

Caching

In-memory caching is handled through a custom ICacheManager. Cached entries expire after a configured sliding time window, which is set in appsettings.json.

Unit Testing

Unit tests are written using xUnit. To run all tests:
dotnet test

Error Handling

Custom exceptions and a global error handler are used to standardize error responses across the API.

Assumptions

Only one exchange rate provider (Frankfurter) is used

The number of supported currencies is fixed at 31

Redis or distributed caching is not used

The system is for internal or authenticated user access only

No Docker support is currently needed

Possible Future Enhancements

Add metrics and monitoring for cache and job performance

Add a simple admin panel to trigger refresh or view logs

Expose more analytics or statistics for usage tracking