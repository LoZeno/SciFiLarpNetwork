# Platform and architecture support matrix

## Approved requirement and review status

The user approved 64-bit-only support on Windows, Linux, and macOS. The required runtime identifiers are `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. Thirty-two-bit x86 is explicitly out of scope and must not be advertised or packaged as a supported product target.

Here, **x86 means 32-bit Intel**, distinct from **x64**, which includes Intel/AMD 64-bit processors. This distinction matters: current macOS and Linux .NET distributions provide x64 and ARM64 binaries, but not a 32-bit x86 .NET 10 runtime. The .NET 10 download matrix does list a Windows x86 SDK/runtime.

| Operating system | Architecture | .NET 10 upstream availability | Avalonia desktop status | Result |
|---|---|---|---|---|
| Windows | x86 (32-bit) | .NET 10 binaries exist | Avalonia Tier 3 (commercial support only) on earlier Windows builds | Explicitly out of scope by user approval |
| Windows | x64 | Available | Tier 1 on Windows 11 24H2+, Tier 2 on Windows 11 22H2 and Windows 10 22H2 | Approved target; product release build and smoke test remain pending |
| Windows | ARM64 | Available | Tier 1 on Windows 11 24H2+, Tier 2 on Windows 11 22H2 | Approved target; product release build and smoke test remain pending |
| Linux | x86 (32-bit) | No official .NET 10 x86 Linux distribution listed; Linux ARM32 is a different architecture | Not in the listed desktop Linux architectures | Explicitly out of scope by user approval |
| Linux | x64 | Available | Tier 1 for Ubuntu 25.x and Debian 13; Fedora 43 | Approved target; distro, display-library, product build, and smoke-test evidence remain pending |
| Linux | ARM64 | Available | Tier 1 for Ubuntu 25.x and Debian 13; Tier 2 on older listed Ubuntu/Debian versions | Approved target; distro, display-library, product build, and smoke-test evidence remain pending |
| macOS | x86 (32-bit) | No official .NET 10 macOS x86 distribution; the published Intel target is x64 | Not listed | Explicitly out of scope by user approval |
| macOS | x64 | Available for Intel Macs | Tier 1 on macOS 26; Tier 2 on macOS 14/15 | Approved target; product release build and smoke test remain pending |
| macOS | ARM64 | Available for Apple silicon | Tier 1 on macOS 26; Tier 2 on macOS 14/15 | Approved target; product release build and smoke test remain pending |

Approval resolves the 32-bit scope conflict and establishes the framework-level target matrix; it does not mean all targets have already been built, published, or smoke-tested. Product-level verification for all six RIDs, including native dependencies, packaging, database setup, and runtime behavior, is a release gate in the later Avalonia, deployment, and CI steps. Do not claim a target as shipped support before that evidence exists.

## Upstream evidence reviewed

Reviewed 2026-10-31:

- Microsoft, [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0): Windows SDK downloads list x64, x86, and ARM64; Linux lists x64, ARM64, and ARM32; macOS lists x64 and ARM64. Linux ARM32 does not satisfy x86.
- Microsoft, [.NET on Windows support matrix](https://learn.microsoft.com/en-us/dotnet/core/install/windows): .NET 10 lists Windows client x64, x86, and ARM64.
- Microsoft, [.NET on macOS](https://learn.microsoft.com/en-us/dotnet/core/install/macos): installer architectures are ARM64 and x64.
- Avalonia, [supported platforms](https://docs.avaloniaui.net/docs/supported-platforms): Windows x86 on earlier Windows builds is Tier 3 (commercial support only); supported desktop Linux tables list x64/ARM64; macOS lists ARM64/x64.

These are upstream framework matrices, not proof that this product's eventual dependencies, native assets, database package, or application builds work on those targets.

## Scope decision

The user approved a 64-bit-only platform matrix on 2026-10-31. Do not claim Linux/macOS 32-bit x86 support through .NET 10 or treat Avalonia's Windows x86 Tier 3 classification as ordinary open-source support. Keep the product build/publish/smoke-test release gate open until evidence for all six approved RIDs is recorded.
