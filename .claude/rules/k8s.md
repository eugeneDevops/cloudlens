---
paths:
  - "deploy/**/*.{yaml,yml}"
---

- Kustomize: base + overlays. Helm-чарты только для Postgres и Redpanda.
- На каждый Deployment: requests/limits, securityContext (non-root, readOnlyRootFilesystem),
  пробы liveness / readiness / startup.
- gRPC-сервисы — readiness через grpc health probe, не HTTP.
- Секреты — через Secret, не в ConfigMap и не в env inline.
- Миграции БД — отдельный Job, не init-контейнер в каждом поде.
