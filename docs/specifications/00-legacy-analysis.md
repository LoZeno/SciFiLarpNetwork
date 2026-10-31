# Legacy repository analysis

## Repositories examined

- `Holonet3`: legacy implementation and related projects.
- `SciFiLarpNetwork`: destination repository, currently on branch `redesign/net10_react_eventsourced`.

## Holonet3 findings

- `Holonet3\Holonet3` is an ASP.NET Web Forms application targeting .NET Framework 4.0. Its pages and code-behind cover account/login flows, network selection, messages and contacts, news, personal files, hacking interactions, QR-code scanning, medical/laboratory tasks, and character information.
- `Holonet3\HolonetManager` is a Windows Forms management application. Its tabs administer players/characters, abilities, events, messages, news, objects/components, substances/ingredients, datapads/holodisks, infections, and event printouts. `HolonetWinControls` contains many of its reusable management controls.
- `Holonet3\Holonet4` is a separate ASP.NET MVC 4-era experiment targeting .NET Framework 4.5. It is not a modern ASP.NET Core implementation.
- `DataAccessLayer` uses a generated Entity Framework EDMX model and SQLite. `CommonBusiness` contains business modules for the same domain.
- The web configuration includes a machine-specific absolute SQLite path. The solution also carries architecture-specific SQLite native DLLs, which are deployment and portability risks.
- The source and UI use Star Wars-specific terms and Italian identifiers/text. The new product should preserve useful gameplay capabilities without carrying over franchise-specific names, assets, or assumptions.

## SciFiLarpNetwork findings

- The repository is not empty. Its `SciFiNetwork` directory contains an older Visual Studio/.NET Framework solution, an Entity Framework model, SQLite schema and sample databases, business/data-mapping projects, and an NUnit-based QR-code test project.
- Existing model/schema names already include generic concepts such as networks, species, players, characters, events, messages, datapads, items, skills, substances, and hacking. These are useful discovery inputs, not a constraint to retain the existing schema or implementation.
- The current checkout was clean when inspected. No files were removed or modified as part of this analysis.

## Migration implications

Treat the legacy applications as behavioral/domain references rather than as an implementation base. Before implementation, establish generic terminology and a capability inventory; model writes as domain events; expose current state through rebuildable projections; and replace platform-bound UI, persistence, and packaging. Define compatibility/import requirements separately if they are needed.

## Evidence sampled

- `Holonet3\Holonet3\Holonet3.csproj`, `Holonet3\Holonet3\Web.config`, `Holonet3\Holonet3\Default.aspx.cs`, and login/page inventory.
- `Holonet3\HolonetManager\Form1.cs` and `Form1.Designer.cs`.
- `Holonet3\DataAccessLayer\DataAccessLayer.csproj` and `HolonetEntityModel.edmx`.
- `Holonet3\Holonet4\Holonet4.csproj`.
- `SciFiLarpNetwork\SciFiNetwork\SciFiNetwork.sln`, `Database\databaseSchema.sql`, and the repository project/file inventory.
