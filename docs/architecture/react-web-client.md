# React web client

The browser client is a React and strict-TypeScript single-page application in `Frontend`. It is built with Vite and uses React Router for client-side resource navigation. It consumes only the same-origin, versioned Siren API; it does not contain domain persistence or authorization rules.

## Local development

Install the frontend's locked dependencies and run the checks from `Frontend`:

```powershell
npm ci
npm run check
npm run build
```

Copy `.env.example` to `.env.local` and set `VITE_BACKEND_ORIGIN` to the local backend origin. By default, Vite serves the UI at `http://localhost:5173` and proxies `/api` requests to `http://localhost:5000`; start the backend on that port or change the value. The proxy keeps browser requests same-origin and avoids requiring permissive CORS configuration. `VITE_API_BASE_PATH` must match the backend `PathBase` plus `/api` when a path base is configured.

`npm run dev` starts the development SPA. `npm run build` emits production assets under `Frontend/dist`; `npm run preview` serves that build locally and uses the same configured API proxy. For a non-root application mount, set `VITE_APP_BASE_PATH` to the deployment path ending in `/` and rebuild so Vite assets and client-side routes use the same base. All static resources are included locally in the build; the client has no CDN, remote font, telemetry, or external runtime dependency.

## Siren behavior

The application starts from the configured API root, follows its advertised latest-version link, and renders navigation only from links with the Siren navigation relation. Selecting a destination routes within the SPA while the API client fetches the advertised `href`. Resource classes and properties remain resource data; they are not interpreted as React component names. Unknown properties are rendered as text, while unknown relations are ignored.

Resource/action targets must remain on the configured same-origin API path. The client never executes an action automatically. It renders supported JSON actions as labelled forms and submits only the advertised fields; unsupported methods, media types, or field types are not executable. Server authorization remains authoritative, and stable Siren error codes are localized rather than interpreting server prose. The selected English/Italian locale is stored locally when browser storage is available and is sent to the API through `Accept-Language`.

The current backend exposes only API discovery roots, so the shell displays the connection state and an empty navigation message until feature endpoints advertise destinations. This task builds the frontend independently; integration of the production asset directory into the self-hosted application is part of the later deployment specification.
