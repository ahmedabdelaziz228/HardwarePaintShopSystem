# Seller-only licensing files

Keep `license-private-key.pem` private and never copy the `seller-tools` folder to a customer machine or public repository.

Generate a permanent license:

```powershell
dotnet run --project tools\LicenseGenerator -- "Customer Name" MACHINE-ID permanent Retail
```

Generate an expiring license:

```powershell
dotnet run --project tools\LicenseGenerator -- "Customer Name" MACHINE-ID 2027-12-31 Retail
```

Only the public key is embedded in the desktop application. Losing the private key means new licenses cannot be generated for this build.
