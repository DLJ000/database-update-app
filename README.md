# OMS Deployment Assistant

A Windows desktop application that automates the monthly OMS deployment workflow, replacing manual command-line steps with a user-friendly GUI.

## Features

- **Automated Build Process**: Runs SVN update and Maven builds in sequence
- **SCP Upload**: Securely uploads WAR files directly to the target server (into the tomcat user's home directory)
- **Deployment**: Handles platform-specific deployments with automatic backups
- **Profile Scanning**: Automatically detects available Maven profiles
- **Secure Credential Storage**: Encrypts and stores passwords locally
- **Real-time Logging**: Live log output with timestamped log files

## Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (or Visual Studio Code with C# extension)
- **SVN** 1.14.5 command-line tools (must be on PATH)
- **Maven** 3.3.9 (must be on PATH)
- **Windows 10/11**

## Installation

1. Clone or download this repository
2. Open `OmsDeployer.sln` in Visual Studio
3. Restore NuGet packages (Visual Studio will do this automatically)
4. Build the solution (F6 or Build → Build Solution)

## Configuration

### First-Time Setup

1. Launch the application
2. Go to the **Settings** tab
3. Configure:
   - **Tomcat Password**: SSH tomcat user password (shared across all servers)
4. Click **Save Settings**

The target server's hostname is derived automatically from the **Platform** selected on the OMS Deployment / Frontend UI Update tabs — see [Platform Selection](#platform-selection) below. All servers share the same Tomcat user (`tomcat`) and password.

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

2. **Upload (SCP)**
   - Select the target platform
   - Click **2. Upload (SCP)**
   - The WAR file is uploaded via SCP directly into `~` (the `tomcat` user's home directory) on the matching server

3. **Deploy**
   - Select the target platform (RfLambda, RapidRf, MillerMmic, or DBWave)
   - Click **3. Deploy**
   - Confirm the deployment
   - The app will (all paths relative to the `tomcat` user's home directory, `~`, since each server's Tomcat install may differ):
     - Backup existing WAR: `~/oms/oms.war` → `~/oms/oms.war.YYYYMMDD`
     - Copy new WAR to `~/oms/oms.war`
     - If RfLambda: Also copy to `~/webapps/oms.war`
     - Clean up staged file

### Platform Selection

Each platform maps to its own server. Choosing a platform on the Upload or Deploy step sends the file to the matching server (all servers share one Tomcat installation and password):

| Platform | Server | WAR suffix |
|---|---|---|
| RfLambda | `rflambda.com` | No suffix (empty string) |
| RapidRf | `rapidrf.com` | `.rapid` suffix |
| MillerMmic | `millermmic.com` | `.millermmic` suffix |
| DBWave_Tomcat9 | `dbwave.com` | `.dbwave` suffix |

## Project Structure

```
OmsDeployer/
├── OmsDeployer.sln              # Solution file
├── OmsDeployer.Core/            # Core library
│   ├── Models/                   # Data models
│   ├── Services/                 # Business logic
│   └── Utils/                    # Utilities
├── OmsDeployer.App/             # WPF application
│   ├── MainWindow.xaml          # Main UI (deployment + settings)
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

### SCP Upload Fails
- Verify the Tomcat password in Settings
- Check network connectivity to the target platform's server
- Ensure the SSH port (22) is not blocked by firewall

### Deployment Fails
- Verify the Tomcat password in Settings
- Check that the `tomcat` user has proper permissions on the target server

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

- **SSH.NET** (v2023.0.3): SSH/SCP client library

## License

This application is for internal use only.

## Support

For issues or questions, check the log files in the `logs` directory for detailed error messages.
