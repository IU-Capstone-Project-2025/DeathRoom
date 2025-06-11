# DeathRoom

**Fast-paced, server-authoritative arena shooter inspired by Quake and Call of Duty**

## Overview

This project is a real-time, fast-paced multiplayer arena shooter supporting up to 12 players per match. Built on a client–server architecture. .NET backend, and Unity 6 on the client side. All services and dependencies are containerized via Docker to ensure consistency across development and production environments.

## Architecture

- **Client**: Unity 6 application handling rendering, input, and client-side prediction.
- **Server**: .NET application in server-authority mode managing game state, matchmaking, and authoritative validation.
- **Networking**: UDP-based packet exchange for minimal latency, using LiteNetLib or ENet libraries, or Photon Fusion in server-authoritative mode.
- **Persistence**:

  - **PostgreSQL** for persistent game state and leaderboards.
  - **Entity Framework Core** as the ORM for database access.
  - **Redis** for distributed state and cache synchronization when scaling across multiple server instances.

## Key Technologies

| Layer            | Technology                                         |
| ---------------- | -------------------------------------------------- |
| Client           | Unity 6, C#, Blender (models), Mixamo (animations) |
| Server           | .NET 8, C#, LiteNetLib / ENet / Photon Fusion      |
| Data Storage     | PostgreSQL, EF Core, Redis (optional)              |
| Containerization | Docker                                             |

## Features

- **Lobby & Matchmaking**: Up to 12 players per arena, instant matchmaking, and lobby management.
- **Low-Latency Networking**: UDP-based transport with client-side prediction and server reconciliation.
- **Server Authority**: Ensures fair play and cheat prevention by validating all client actions on the server.
- **Scalability**: Horizontal scaling with Redis-backed session management and caching.
- **Asset Workflow**: 3D models created in Blender; character and weapon animations sourced from Mixamo.

## Getting Started

### Prerequisites

- Docker Engine
- .NET 8 SDK
- Unity 6 Editor
- Blender (for asset editing)
