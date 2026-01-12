# OMS Deployment Assistant

A Windows desktop application that automates the monthly OMS deployment workflow, replacing manual command-line steps with a user-friendly GUI.

## Features

- **Automated Build Process**: Runs SVN update and Maven builds in sequence
- **FTP Upload**: Securely uploads WAR files to the server
- **SSH Staging**: Moves files to the Tomcat directory via SSH
- **Deployment**: Handles platform-specific deployments with automatic backups
- **Profile Scanning**: Automatically detects available Maven profiles
- **Secure Credential Storage**: Encrypts and stores passwords locally
- **Real-time Logging**: Live log output with timestamped log files

## Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (or Visual Studio Code with C# extension)
- **SVN** command-line tools (must be on PATH)
- **Maven** (must be on PATH)
- **Windows 10/11**

## Installation

1. Clone or download this repository
2. Open `OmsDeployer.sln` in Visual Studio
3. Restore NuGet packages (Visual Studio will do this automatically)
4. Build the solution (F6 or Build → Build Solution)

## Configuration

### First-Time Setup

1. Launch the application
2. Click **Settings...** button
3. Configure:
   - **FTP Host**: `ftp.rflambda.com` (default)
   - **FTP User**: `ftpuser` (default)
   - **FTP Password**: Your FTP password
   - **SSH Host**: Your server hostname/IP
   - **Root Password**: SSH root password
   - **Tomcat Password**: SSH tomcat user password
4. Click **Save**

### Repository Setup

1. Click **Browse...** next to Repo Path
2. Select your repository root directory (the one containing `lakexy` folder)
3. The application will automatically scan for available profiles

## Usage

### Step-by-Step Deployment

The application follows your original workflow:

1. **Build WAR**
   - Select a profile from the dropdown
   - Click **1. Build WAR**
   - The app will:
     - Run `svn update`
     - Build `product-finder` with `mvn install`
     - Build `omscore` with `mvn install`
     - Build `oms` with `mvn clean package -P <PROFILE>`

2. **Upload to FTP**
   - Click **2. Upload to FTP**
   - The WAR file will be uploaded to `/var/www/webadmin/data/ftpuser/`

3. **Stage to Tomcat**
   - Click **3. Stage to Tomcat**
   - The file will be moved to `/opt/tomcat7/` via SSH (as root)

4. **Deploy**
   - Select the target platform (RfLambda, RapidRf, or MillerMmic)
   - Click **4. Deploy**
   - Confirm the deployment
   - The app will:
     - Backup existing WAR: `oms/oms<PLAT>.war` → `oms/oms<PLAT>.war.YYYYMMDD`
     - Copy new WAR to `oms/oms<PLAT>.war`
     - If RfLambda: Also copy to `webapps/oms.war`
     - Clean up staged file

### Platform Selection

- **RfLambda**: No suffix (empty string)
- **RapidRf**: `.rapid` suffix
- **MillerMmic**: `.millermmic` suffix

## Project Structure

```
OmsDeployer/
├── OmsDeployer.sln              # Solution file
├── OmsDeployer.Core/            # Core library
│   ├── Models/                   # Data models
│   ├── Services/                 # Business logic
│   └── Utils/                    # Utilities
├── OmsDeployer.App/             # WPF application
│   ├── MainWindow.xaml          # Main UI
│   ├── ConfigWindow.xaml        # Settings UI
│   └── Properties/              # App settings
└── logs/                         # Log files (created at runtime)
```

## Security

- Passwords are encrypted using AES encryption with a machine-specific key
- Credentials are stored in `%APPDATA%\OmsDeployer\config.encrypted`
- Application settings are stored in user-scoped settings

## Logging

Each deployment run creates a timestamped log file in the `logs` directory:
- Format: `deploy_YYYYMMDD_HHmmss.log`
- Contains all command output, errors, and status messages

## Troubleshooting

### Build Fails
- Ensure SVN and Maven are installed and on PATH
- Verify repository path is correct
- Check that the selected profile exists in `lakexy/oms/src/main/filters/`

### FTP Upload Fails
- Verify FTP credentials in Settings
- Check network connectivity to FTP server
- Ensure FTP server is accessible

### SSH Operations Fail
- Verify SSH host and credentials
- Check that SSH port (22) is not blocked by firewall
- Ensure user has proper permissions (root for staging, tomcat for deployment)

### Profile Not Found
- Click Browse to refresh the repository path
- Verify `lakexy/oms/src/main/filters/` contains `.properties` files
- Profile names are derived from `.properties` filenames (without extension)

## Development

### Building from Command Line

```bash
dotnet restore
dotnet build
dotnet run --project OmsDeployer.App
```

### Dependencies

- **FluentFTP** (v49.0.0): FTP client library
- **SSH.NET** (v2023.0.3): SSH client library

## License

This application is for internal use only.

## Support

For issues or questions, check the log files in the `logs` directory for detailed error messages.

