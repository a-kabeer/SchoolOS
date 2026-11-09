# SchoolOS — Development Environment & Tooling Guide

**Target Audience:** SchoolOS Core Engineering Team  
**Baseline Date:** 9 November 2026  
**Status:** Approved Phase 0 Reference  

---

## 1. Required Host Toolchain

To build, test and run the SchoolOS Modular Monolith and React frontends locally, the following developer tools are required:

| Tool | Recommended Version | Purpose | Installation via Windows Package Manager (`winget`) |
|---|---|---|---|
| **.NET SDK** | .NET 10.0 (or .NET 9.0 LTS) | ASP.NET Core Backend, Entity Framework Core, Background Services | `winget install Microsoft.DotNet.SDK.9` |
| **Node.js & npm** | Node.js v22.x LTS (npm v10+) | React + TypeScript frontend build, Vite tooling | `winget install OpenJS.NodeJS.LTS` |
| **Docker Engine** | Docker Desktop (v4.30+) | Local PostgreSQL containers, Testcontainers integration testing | `winget install Docker.DockerDesktop` |
| **Git** | Git 2.45+ (Windows) | Version control | Pre-installed (`git version 2.56.0`) |
| **PostgreSQL Client** | `psql` / pgAdmin 4 (v8+) | Database inspection and migrations | `winget install PostgreSQL.PostgreSQL` |

> [!NOTE]
> In accordance with project security governance, package installations require explicit administrative authorization and are not installed automatically by AI agents.

---

## 2. Solution Structure Overview

```text
SchoolOS/
├── SchoolOS.sln                        # Root Visual Studio / Rider solution
├── global.json                         # .NET SDK version and roll-forward policy
├── Directory.Build.props               # Centralized C# build settings (.NET 10, nullable, warnings)
├── Directory.Packages.props            # Central Package Management (CPM)
├── .editorconfig                       # Formatting and style rules across C# and TypeScript
├── .gitignore                          # Repository artifact and secret exclusion rules
├── deploy/
│   └── docker-compose.yml              # Local PostgreSQL 17 service
├── src/
│   ├── Hosts/
│   │   ├── SchoolOS.Cloud.Api/         # Cloud Control Plane Web API host
│   │   └── SchoolOS.Local.Api/         # Campus Local Data Plane Web API host
│   ├── BuildingBlocks/
│   │   ├── SharedKernel/               # Entity<TId>, Result primitives, value objects
│   │   ├── Security/                   # ITenantContext, scoped authorization interfaces
│   │   └── Observability/              # Telemetry, logging, correlation primitives
│   ├── Modules/                        # Campus business modules (scaffold placeholders)
│   └── PlatformModules/                # Cloud-only platform modules (scaffold placeholders)
├── frontend/
│   └── schoolos-web/                   # React + TypeScript + Vite frontend application
├── tests/
│   ├── ArchitectureTests/              # NetArchTest modular monolith boundary verification
│   └── UnitTests/                      # Domain and building block unit tests
└── docs/                               # Blueprint v1.7 and architectural records
```

---

## 3. Local Development Workflows

### 3.1 Starting the Local PostgreSQL Database
From the repository root:
```powershell
docker compose -f deploy/docker-compose.yml up -d
```
Default connection string:
`Host=localhost;Port=5432;Database=schoolos_local_dev;Username=schoolos_dev;Password=schoolos_dev_password`

### 3.2 Building and Running the Backend
```powershell
# Restore and verify solution
dotnet restore
dotnet build --no-restore

# Run Campus Local API host (Port 5001 / HTTPS)
dotnet run --project src/Hosts/SchoolOS.Local.Api

# Run Cloud Control Plane host (Port 5002 / HTTPS)
dotnet run --project src/Hosts/SchoolOS.Cloud.Api
```

### 3.3 Running Automated Tests
```powershell
dotnet test
```

### 3.4 Running the Frontend
```powershell
cd frontend/schoolos-web
npm install
npm run dev
```
Development server operates at `http://localhost:5173`.
