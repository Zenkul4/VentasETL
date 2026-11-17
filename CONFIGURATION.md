# Local configuration

Do not commit environment-specific configuration or credentials. `Infrastructure/appsettings.example.json` is a safe template and is not loaded automatically. Copy it to a local `appsettings.json` only when needed; Git ignores that file.

Provide the database connection string through `ConnectionStrings__DefaultConnection`. Supply environment-specific ETL paths, API settings, and any additional connection strings through the hosting application's secret store or environment variables. Do not place real credentials in this repository.
