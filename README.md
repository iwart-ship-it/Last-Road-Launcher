# Last Road Launcher

Official open-source launcher and installer for the Last Road game project.

## About

Last Road Launcher is a Windows launcher responsible for:

- installing and updating the game client;
- verifying game files;
- repairing damaged or missing files;
- selecting the UA or EN client;
- launching the game.

The launcher communicates with the official Last Road update service:

https://last-road.com/

## Components

- **LastRoadLauncher** — WPF launcher built with .NET 8.
- **Installer** — Windows installer built with Inno Setup.

## Build requirements

- Windows
- .NET 8 SDK
- Inno Setup 6

## Security

This repository contains only the launcher and installer source code.

It does **not** contain:

- game server source code;
- databases;
- server configuration;
- credentials or private keys;
- game client files;
- private Last Road infrastructure.

## Project

Last Road  
OLD WORLD. NEW PATH.

https://last-road.com/

## Code Signing Policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

The SignPath Foundation certificate may only be used to sign official
Last Road Launcher and Last Road Installer builds produced from the source
code contained in this repository.

Signed releases must be produced through the project's automated build
process from the public source repository.

### Team roles

Maintainer:
- iwart-ship-it

Committer:
- iwart-ship-it

Reviewer:
- iwart-ship-it

Approver:
- iwart-ship-it

### Privacy

The Last Road Launcher connects to the official Last Road infrastructure
only when requested by the user for actions such as checking, downloading,
updating or repairing game files.

The launcher does not intentionally collect or transmit personal information
to third-party services as part of its update and repair functionality.

The certificate must not be used to sign:

- the Last Road game server;
- game client files;
- third-party software;
- private development builds;
- unrelated executables or installers.

Official releases are distributed through:

https://last-road.com/

