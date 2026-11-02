# SPEC-18: Self-hosted deployment on a normal computer

## Requirement

Provide a self-hosted deployment option or package that lets an operator run the React frontend, .NET backend, and database on a normal computer acting as the host server, without requiring a separately configured or installed web server such as IIS or Apache.

## Acceptance criteria

- The package includes a documented, repeatable setup and startup path for all required services, including database initialization and configuration.
- The host does not need to install or configure IIS, Apache, or another dedicated web server. The application may use its own supported server process to serve the frontend and backend.
- Operators can configure the bind address/port, data storage location, credentials/secrets, and other deployment-specific settings without rebuilding the applications.
- Provide clear start, stop, update, and diagnostic/logging instructions, along with guidance for safe network exposure and persistent database storage.
- The deployment is tested on each OS/architecture combination claimed by SPEC-07 and is packaged through SPEC-16.
- If the selected datastore cannot be bundled or run locally in a supported manner on a target platform, document the limitation and request approval for a compatible alternative before narrowing support.
- The complete self-hosted stack starts and runs without internet access after installation, with all dependencies, static assets, and database services available locally (SPEC-21).
- Provide a deployment mode suitable for an isolated venue LAN, including local addressing, persistent data, and the secure-context requirements of browser camera access; public DNS or public certificate services must not be a runtime prerequisite.
