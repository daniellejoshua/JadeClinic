# Standalone Deployment Guide - JadeClinic

## Changes Made for Standalone Deployment

### 1. Configuration Changes (app.config)
- **Changed from:** SQL Server Express with network configuration
- **Changed to:** LocalDB with local file database
- **Benefits:** 
  - No need to install SQL Server separately
  - Easier deployment
  - No network configuration required
  - Database files travel with the application

### 2. Connection String Changes
- **New connection string:** Uses LocalDB with AttachDbFilename
- **Database location:** `App_Data\JadeDentalSupply.mdf` (in application folder)
- **Authentication:** Windows Integrated Security (no passwords needed)

### 3. Deployment Steps

#### For the Developer (You):
1. Build the application in Release mode
2. Copy the entire application folder including:
   - JadeClinic.exe
   - All .dll files
   - app.config
   - App_Data folder (will be created automatically)

#### For the Client:
1. Copy the application folder to their computer
2. Run JadeClinic.exe
3. The first run will automatically:
   - Create the App_Data folder
   - Initialize the LocalDB database
   - Set up all required tables

### 4. System Requirements for Client
- Windows 10/11
- .NET Framework 4.8 (usually pre-installed)
- SQL Server LocalDB (comes with Windows 10/11 or can be installed separately)

### 5. Database Location
The database file will be created at:
`[Application Path]\App_Data\JadeDentalSupply.mdf`

### 6. Backup Strategy
To backup data, simply copy the entire `App_Data` folder.

### 7. No Network Configuration Needed
- No server IP configuration
- No network setup
- No firewall configuration
- Works completely offline

## Migration Notes
If you had existing data in SQL Server Express, you'll need to:
1. Export data from the old SQL Server Express database
2. Import it into the new LocalDB database after first run

This setup is perfect for single-computer dental clinic management!