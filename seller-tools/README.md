# Seller-only licensing files

Copy `license-private-key.pem` into this folder only on the offline seller machine. The file is ignored by Git. Keep it private and never copy it to a customer machine, source archive, cloud build, or public repository.

Generate a permanent license:

```powershell
dotnet run --project tools\LicenseGenerator -- "Customer Name" MACHINE-ID permanent Retail
```

Generate an expiring license:

```powershell
dotnet run --project tools\LicenseGenerator -- "Customer Name" MACHINE-ID 2027-12-31 Retail
```

Only the public key is embedded in the desktop application. Losing the private key means new licenses cannot be generated for this build.
