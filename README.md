## Marksman - A Windows agent for Snipe-IT

## Installation
1. Download and install the latest .msi installer from the [releases tab](https://github.com/Scope-IT/marksman/releases/).
2. By default, the program will be installed in Program Files (x86)/Scope-IT/Marksman. Edit the Marksman.exe.config file to include your API key and BaseURI from the default values to the ones given by your Snipe-IT instance.
3. Set the Company and Location parameters in Marksman.exe.config, then run the .exe.

## Features
* The agent creates an asset and fills out the fields:
  - Asset name (currently machine hostname, unless agent is run in interactive mode)
  - Asset id (Asset tag prefix + serial number)
  - Location (from a config file or the Organizational Unit)
  - Warranty (from config file)
  - Status label (from config file)
  - Mac address
  - Make, model of the machine (as reported to Windows)
* Ensures that the asses created is unique
* New locations, makes, models are created as needed


## Getting started
You will need a working [Snipe-IT](https://snipeitapp.com/) database with API access and an API key. 
*We recommend creating a separate user for the agent with minimal (read + add) permissions.*

You can run it via a GPO or Scheduled task (recommended way is to run the agent once on boot with a delay of 1+ minute)


## Bug Reports & Feature Requests
We welcome community participation in this project. Please submit an issue or pull request to participate in the development. 

## License
This project is licensed under the [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0)

## Planned features
- [ ] Component lookup (automatic tracking of connected hard drives, CPU, GPU, etc.)
- [ ] Automatic update feature (tracking of computer name and other property changes)
- [ ] Additional query types for location
- [ ] Warranty lookup APIs
- [ ] Cross-platform (Windows/MacOS/Linux) agent using Mono

## Acknowledgments
 * The project is based on [SnipeSharp API](https://github.com/cnitschkowski/SnipeSharp) by [barrycarey](https://github.com/barrycarey) and [cnitschkowski](https://github.com/cnitschkowski) with modifications by [velaar](https://github.com/velaar)
 * Snipe-IT 4.0+ is required for proper operation
