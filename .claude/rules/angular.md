---
paths:
  - "src/Web/**/*.{ts,html}"
---

- Standalone-компоненты. NgModule не создавать.
- Состояние на signals. `toSignal` вместо ручных подписок.
- Любая подписка — только с `takeUntilDestroyed`.
- `ChangeDetectionStrategy.OnPush` по умолчанию.
- `any` запрещён. Типы API генерируются из OpenAPI, руками не пишутся.
- Логика в сервисах, компонент только отображает.
- Структура feature-based: `features/<name>/{components,services,models}`.
