# SPEC-19: Container-based hosting

## Requirement

Docker-compatible redistributable images and/or Kubernetes deployment artifacts are an acceptable additional hosting and installation option. Container support supplements the self-hosted deployment in SPEC-18; it does not replace the required no-dedicated-web-server host-computer option.

## Acceptance criteria

- Publish versioned images for the frontend, backend, and database/event store where technically and legally supported, or document a combined deployment image/package where that is the supported approach.
- Provide reproducible image build instructions and a deployment example with persistent database storage, configuration, health checks, and startup dependencies.
- Provide Kubernetes manifests/Helm packaging only if Kubernetes is supported as a deployment target; document any operational requirements.
- Image manifests and release workflows identify supported platforms/architectures and must not imply platform support where upstream images or dependencies do not provide it.
- Document version upgrades, data persistence, backup/restore, secrets handling, and how to expose the service safely.
- Container publishing is integrated with SPEC-16 and the required configuration documentation is included in the root README per SPEC-17.
- Once images are present on the host (for example, loaded from an offline archive), the deployment starts and operates without pulling images, packages, or assets from the internet; all required images and versions are enumerated in the offline installation guide (SPEC-21).
