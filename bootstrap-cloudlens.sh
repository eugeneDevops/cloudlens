#!/usr/bin/env bash
#
# CloudLens — bootstrap каркаса проекта.
#
#   ./bootstrap-cloudlens.sh              # только правила, docs, CI, структура папок
#   ./bootstrap-cloudlens.sh --solution   # + dotnet sln и проекты (нужен .NET 10 SDK)
#
# Запускать в пустой или свежей папке репозитория.

set -euo pipefail

WITH_SOLUTION=0
[[ "${1:-}" == "--solution" ]] && WITH_SOLUTION=1

say() { printf '  %s\n' "$1"; }
head_() { printf '\n\033[1m%s\033[0m\n' "$1"; }

if [[ -f AGENTS.md ]]; then
  echo "AGENTS.md уже существует — скрипт перезапишет правила, которые вы правили."
  read -r -p "Продолжить? [y/N] " a
  [[ "$a" == "y" || "$a" == "Y" ]] || exit 1
fi

head_ "1/6  Структура каталогов"

mkdir -p \
  .cursor/rules \
  .claude/commands \
  .github/workflows \
  docs/{adr,ai,c4,domain} \
  deploy/k8s/base/{budgeting,anomalies,detector,ingestion,insights,web} \
  deploy/k8s/overlays/local \
  src/BuildingBlocks/{SharedKernel,Contracts.Grpc/Protos,Contracts.Integration} \
  src/Services/Budgeting/{Budgeting.Domain/{Budgets,ValueObjects,Events},Budgeting.Application/{Budgets,Abstractions},Budgeting.Infrastructure/{Persistence,Outbox,Kafka},Budgeting.Api} \
  src/Services/Anomalies/{Anomalies.Domain,Anomalies.Application,Anomalies.Infrastructure,Anomalies.Api} \
  src/Services/Detector/Detector.Api \
  src/Services/Ingestion/Ingestion.Worker \
  src/Services/Insights/{Insights.Application/Abstractions,Insights.Infrastructure/Ai/{Decorators,Prompts},Insights.Api,Insights.Mcp} \
  src/Web \
  tests

find deploy src tests -type d -empty -exec touch {}/.gitkeep \;
say "каталоги созданы"

head_ "2/6  AGENTS.md — единый источник правил"

cat > AGENTS.md <<'EOF'
# CloudLens

Контроль облачных расходов: бюджеты с порогами, детекция аномалий,
LLM-объяснения и запросы к витрине на естественном языке.

Стек: .NET 10 (LTS), Angular 18, PostgreSQL + pgvector, Kafka, gRPC, Kubernetes.

## Команды

- `make test` — юнит-тесты
- `make test-integration` — интеграционные (Testcontainers, нужен Docker)
- `make eval` — eval-тесты LLM (дорого, вручную)
- `make up` / `make down` — локальный кластер kind
- `dotnet format` — перед коммитом

## Границы слоёв (нарушение = ошибка)

- `*.Domain` — ноль внешних зависимостей. Ни EF, ни Kafka, ни Microsoft.Extensions.AI,
  ни System.Text.Json. Перед добавлением пакета проверь .csproj.
- `*.Application` — зависит только от Domain. Инфраструктура через интерфейсы в `Abstractions/`.
- `*.Infrastructure`, `*.Api` — зависят от Application.
- Между сервисами прямых ссылок нет. Только `Contracts.Grpc` и `Contracts.Integration`.

## Домен

- Агрегат: приватные сеттеры, приватный конструктор, `static Create(...) -> Result<T>`.
- Коллекции наружу только `IReadOnlyCollection<T>`.
- Инварианты проверяются внутри агрегата, не в handler'е.
- Ожидаемая ошибка -> `Result` / `Result<T>`. Исключение -> только баг программиста.
- Доменные события накапливаются в агрегате, публикуются в UnitOfWork.

## LLM

- LLM никогда не меняет состояние домена. Только чтение и генерация текста.
- Все цифры считает домен. Модель получает готовые агрегаты и описывает их словами.
- Вызовы только через `IChatClient` (Microsoft.Extensions.AI).
- Промпты — в `Prompts/*.vN.md`, не строковые константы.
- Каждый вызов: таймаут, circuit breaker, fallback, логирование токенов.

## Тесты

- xUnit + FluentAssertions. Имя: `Should_ExpectedBehavior_When_Condition`.
- В `*.Domain.UnitTests` моков нет вообще.
- Один логический assert на тест.
- Тесты с реальной моделью — только в `*.EvalTests`.

## Чего не делать

- Не добавляй NuGet-пакеты без разрешения.
- Не создавай интерфейс с одной реализацией без нужды в инверсии зависимости.
- Не пиши комментарии, пересказывающие код.
- Никаких `#region`.
- Не трогай файлы вне запрошенной области.
- Не правь миграции EF вручную.
EOF

cat > CLAUDE.md <<'EOF'
Правила проекта: @AGENTS.md

Команды в `.claude/commands/`: `/review-layer`, `/invariants`, `/adr`.
EOF
say "AGENTS.md + CLAUDE.md"

head_ "3/6  Cursor rules"

cat > .cursor/rules/domain.mdc <<'EOF'
---
description: Доменный слой — агрегаты, инварианты, value objects
globs: ["src/Services/*/*.Domain/**/*.cs"]
alwaysApply: false
---

# Доменный слой

Ноль внешних зависимостей. Ни EF, ни Kafka, ни Microsoft.Extensions.AI,
ни System.Text.Json. Проверь .csproj перед добавлением чего-либо.

## Агрегат
- Приватные сеттеры, приватный конструктор, `static Create(...) -> Result<T>`.
- Приватный parameterless ctor только для EF, с комментарием-пояснением.
- Коллекции: приватное поле `List<T>` + публичное `IReadOnlyCollection<T>`.
- Состояние меняется только методом, который сначала проверяет инварианты.
- Значимое изменение поднимает доменное событие через `Raise(...)`.

## Ошибки
- Ожидаемый бизнес-исход -> `Result` / `Result<T>` с типизированной ошибкой
  из статического класса `XxxErrors`.
- Исключение -> только нарушение предусловия метода (баг программиста).

## Value objects
- `sealed record` или `readonly record struct`.
- Валидация в статической фабрике, не в конструкторе.
- `Money` с другой валютой -> `Result.Failure`, не исключение.

<!-- После дня 1 раскомментируй — эталон сильнее любого описания: -->
<!-- Эталонный агрегат: @src/Services/Budgeting/Budgeting.Domain/Budgets/Budget.cs -->
EOF

cat > .cursor/rules/ai-layer.mdc <<'EOF'
---
description: Интеграция LLM — границы, декораторы, промпты
globs: ["src/Services/Insights/**/*.cs"]
alwaysApply: false
---

# LLM-слой

- Только через `IChatClient` (Microsoft.Extensions.AI). Прямых SDK провайдеров нет.
- LLM не меняет состояние домена. Только чтение и генерация текста.
- Все цифры считает домен. В промпт уходят готовые агрегаты, не сырые данные.
- Промпты — в `Prompts/*.vN.md`, читаются при старте.
- Каждый вызов: таймаут, circuit breaker, fallback, учёт токенов.
- Порядок декораторов в DI: TokenAccounting -> Cached -> Fallback -> реальный клиент.
- В промпт не попадают: идентификаторы пользователей, email, сырые биллинговые строки.
- Structured output — строгая схема + валидация результата после парсинга
  (значение вне справочника или дата в будущем -> поле отбрасывается).
EOF

cat > .cursor/rules/tests.mdc <<'EOF'
---
description: Правила тестов
globs: ["tests/**/*.cs"]
alwaysApply: false
---

- xUnit + FluentAssertions. Имя: `Should_ExpectedBehavior_When_Condition`.
- Arrange/Act/Assert, пустая строка между блоками, без комментариев-заголовков.
- В `*.Domain.UnitTests` моков нет вообще. Понадобился мок — проблема в дизайне.
- Один логический assert на тест.
- Проверяем поведение и итоговое состояние, а не факт вызова метода.
- Интеграционные — только Testcontainers. Никаких shared-инстансов.
- Тесты с реальной моделью — только в `*.EvalTests`, с порогом прохождения,
  а не строгим равенством.
EOF

cat > .cursor/rules/angular.mdc <<'EOF'
---
description: Angular — компоненты, состояние, типизация
globs: ["src/Web/**/*.ts", "src/Web/**/*.html"]
alwaysApply: false
---

- Standalone-компоненты. NgModule не создавать.
- Состояние на signals. `toSignal` вместо ручных подписок.
- Любая подписка — только с `takeUntilDestroyed`.
- `ChangeDetectionStrategy.OnPush` по умолчанию.
- `any` запрещён. Типы API генерируются из OpenAPI, руками не пишутся.
- Логика в сервисах, компонент только отображает.
- Структура feature-based: `features/<name>/{components,services,models}`.
EOF

cat > .cursor/rules/kafka.mdc <<'EOF'
---
description: Применять при работе с Kafka, producer, consumer, outbox, идемпотентностью обработки сообщений и DLQ.
alwaysApply: false
---

- Публикация только через outbox, никогда напрямую из handler'а.
- Outbox-publisher: батч 50, `SELECT ... FOR UPDATE SKIP LOCKED`.
- Consumer: `EnableAutoCommit=false`, `StoreOffset` после успешной обработки.
- Идемпотентность по `messageId` из заголовка + таблица `processed_messages`.
- После 3 неудач — DLQ-топик с заголовками об ошибке.
- Топики именуются `<context>.<event>.v<N>`.
- Confluent.Kafka напрямую. MassTransit не добавлять.
EOF

cat > .cursor/rules/k8s.mdc <<'EOF'
---
description: Kubernetes-манифесты
globs: ["deploy/k8s/**/*.yaml"]
alwaysApply: false
---

- Kustomize: base + overlays. Helm-чарты только для Postgres и Redpanda.
- На каждый Deployment: requests/limits, securityContext (non-root, readOnlyRootFilesystem),
  пробы liveness / readiness / startup.
- gRPC-сервисы — readiness через grpc health probe, не HTTP.
- Секреты — через Secret, не в ConfigMap и не в env inline.
- Миграции БД — отдельный Job, не init-контейнер в каждом поде.
EOF

cat > .cursor/rules/review-layer.mdc <<'EOF'
---
description: Ручное ревью на нарушение архитектурных правил
alwaysApply: false
---

Проверь указанную область на нарушения AGENTS.md.

Выведи таблицу: файл | правило | нарушение | как исправить.

Отдельно отметь:
- публичные сеттеры в агрегатах;
- логику домена, уехавшую в handler;
- интерфейсы с одной реализацией без нужды в инверсии;
- комментарии, пересказывающие код;
- внешние зависимости, просочившиеся в Domain.

Ничего не меняй — только отчёт.
EOF

cat > .cursor/rules/invariants.mdc <<'EOF'
---
description: Аудит инвариантов агрегата
alwaysApply: false
---

Перечисли все инварианты указанного агрегата, найденные в коде.

Для каждого: формулировка человеческим языком | где проверяется |
есть ли тест | что сломается, если убрать.

В конце отдельным списком — инварианты без тестов и места,
где состояние меняется в обход проверок.
EOF

cat > .cursor/rules/adr.mdc <<'EOF'
---
description: Написание ADR
alwaysApply: false
---

Напиши ADR в `docs/adr/NNNN-<slug>.md` по шаблону `docs/adr/0000-template.md`.

Требования: минимум три рассмотренных варианта, у каждого честные минусы.
Одна страница. Сухо, без маркетинга и превосходных степеней.
Обязательный раздел «Что заставит пересмотреть решение».
EOF

say "8 файлов .mdc"

head_ "4/6  Claude Code commands"

cat > .claude/commands/review-layer.md <<'EOF'
---
description: Ревью области на нарушение архитектурных правил
---

Проверь $ARGUMENTS на нарушения правил из @AGENTS.md.

Выведи таблицу: файл | правило | нарушение | как исправить.

Отдельно отметь: публичные сеттеры в агрегатах, логику домена в handler'ах,
интерфейсы с одной реализацией, комментарии-пересказы, внешние зависимости в Domain.

Ничего не меняй — только отчёт.
EOF

cat > .claude/commands/invariants.md <<'EOF'
---
description: Аудит инвариантов агрегата
---

Перечисли все инварианты агрегата $ARGUMENTS, найденные в коде.

Для каждого: формулировка человеческим языком | где проверяется | есть ли тест |
что сломается, если убрать.

В конце — отдельный список инвариантов без тестов и мест, где состояние
меняется в обход проверок.
EOF

cat > .claude/commands/adr.md <<'EOF'
---
description: Написать ADR
---

Напиши ADR в docs/adr/ по теме: $ARGUMENTS

Шаблон — @docs/adr/0000-template.md
Минимум три варианта с честными минусами. Одна страница. Без маркетинга.
EOF

say "3 команды"

head_ "5/6  docs, CI, Makefile, конфиги"

cat > docs/adr/0000-template.md <<'EOF'
# NNNN. <Заголовок решения>

Дата: YYYY-MM-DD
Статус: Принято | Заменено на NNNN | Отклонено

## Контекст

Какую задачу решаем и какие ограничения действуют.

## Рассмотренные варианты

### Вариант A
Суть. Плюсы. Минусы — честно.

### Вариант B
### Вариант C

## Решение

Что выбрано и почему именно это, а не остальные.

## Последствия

Что стало проще, что сложнее, какую цену платим.

## Что заставит пересмотреть

Конкретные условия, при которых решение перестанет быть верным.
EOF

cat > docs/ai/workflow.md <<'EOF'
# Как в этом проекте использовался ИИ

<!-- Заполняется по ходу недели, а не в конце. Это часть портфолио. -->

## Схема правил

`AGENTS.md` — единый источник для всех агентов (Cursor, Claude Code и др.).
`CLAUDE.md` — ссылка на него, чтобы не дублировать.
`.cursor/rules/*.mdc` — узкие правила, привязанные к путям.

Распределение по режимам активации Cursor:

| Правило | Режим | Почему так |
|---|---|---|
| AGENTS.md | Always | Общие границы, нужны в каждом запросе |
| domain, ai-layer, tests, angular, k8s | Auto Attached (globs) | Нужны только при работе с этими путями |
| kafka | Agent Requested | Пути разбросаны, надёжнее по description |
| review-layer, invariants, adr | Manual | Вызываются осознанно |

## Где модель ошибалась систематически

<!-- Формат: ошибка -> как вылечено -> коммит с правилом -->

## Что писалось руками и почему

## Границы: чего ИИ не делал
EOF

cat > docs/ai/prompt-log.md <<'EOF'
# Журнал промптов

Удачные формулировки и разборы неудач. По дню на раздел.

## День 1 — домен Budgeting
EOF

cat > docs/domain/ubiquitous-language.md <<'EOF'
# Единый язык

| Термин | Значение | Не путать с |
|---|---|---|
| Budget | Лимит расходов на период для области (аккаунт/теги) | Forecast |
| BillingRecord | Строка биллинга от провайдера; может прийти повторно и с исправлением | Charge |
| Applied charge | Биллинговая запись, учтённая в конкретном бюджете | BillingRecord |
| Threshold | Порог в процентах от лимита (50/80/100) | Limit |
| Threshold alert | Факт однократного уведомления о пороге в текущем периоде | Notification |
| Hysteresis | Запас, на который сумма должна упасть, чтобы порог «перезарядился» | — |
| AnomalyCase | Случай аномалии со своим жизненным циклом | Detection |
| Detection | Единичное срабатывание детектора; присоединяется к открытому случаю | AnomalyCase |
| Suppression | Заранее объявленное окно, в котором аномалии не будят людей | Resolved |
EOF

cat > docs/c4/container.puml <<'EOF'
@startuml
' Заполняется на дне 8
title CloudLens — Container diagram
@enduml
EOF

cat > Makefile <<'EOF'
.PHONY: test test-integration eval up down logs format

test:
	dotnet test --filter "FullyQualifiedName~UnitTests"

test-integration:
	dotnet test --filter "FullyQualifiedName~IntegrationTests"

eval:
	dotnet test --filter "FullyQualifiedName~EvalTests"

format:
	dotnet format

up:
	kind create cluster --name cloudlens --config deploy/k8s/kind.yaml || true
	kubectl apply -k deploy/k8s/overlays/local
	kubectl wait --for=condition=available --timeout=300s deployment --all

down:
	kind delete cluster --name cloudlens

logs:
	kubectl logs -l app.kubernetes.io/part-of=cloudlens --tail=100 -f
EOF

cat > .cursorignore <<'EOF'
**/bin/
**/obj/
**/node_modules/
**/dist/
**/Migrations/
**/*.Designer.cs
**/Protos/**/*.g.cs
**/*.lock
EOF

cat > .gitignore <<'EOF'
bin/
obj/
node_modules/
dist/
.vs/
.idea/
*.user
appsettings.Development.json
.env
EOF

cat > .editorconfig <<'EOF'
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_size = 4
csharp_style_namespace_declarations = file_scoped:error
dotnet_style_require_accessibility_modifiers = always:error
csharp_style_var_elsewhere = false:suggestion
dotnet_diagnostic.CA1062.severity = none

[*.{yaml,yml,json,ts,html,scss}]
indent_size = 2

[Makefile]
indent_style = tab
EOF

cat > Directory.Build.props <<'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
EOF

cat > .github/workflows/ci.yml <<'EOF'
name: ci

on:
  push:
    branches: [main]
  pull_request:

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --filter "FullyQualifiedName~UnitTests" --logger trx
      - run: dotnet test --no-build --filter "FullyQualifiedName~IntegrationTests" --logger trx
EOF

cat > .github/workflows/evals.yml <<'EOF'
# Eval-тесты недетерминированы и стоят денег — отдельно от основного CI.
name: evals

on:
  workflow_dispatch:
  schedule:
    - cron: '0 6 * * 1'

jobs:
  eval:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --filter "FullyQualifiedName~EvalTests"
        env:
          LLM__ApiKey: ${{ secrets.LLM_API_KEY }}
EOF

cat > README.md <<'EOF'
# CloudLens

Упрощённый аналог AWS Budgets и Cost Anomaly Detection: бюджеты с порогами,
детекция аномалий в облачных расходах, LLM-объяснения и запросы на естественном языке.

Каркас создан, реализация — по плану в `docs/`. README пишется на дне 8.
EOF

say "docs, CI, Makefile, .editorconfig, Directory.Build.props"

head_ "6/6  .NET solution"

if [[ $WITH_SOLUTION -eq 1 ]]; then
  if ! command -v dotnet >/dev/null 2>&1; then
    say "dotnet не найден — пропускаю. Поставь .NET 10 SDK и запусти с --solution ещё раз."
  else
    dotnet new sln -n CloudLens >/dev/null

    new_lib() { dotnet new classlib -o "$1" -n "$2" >/dev/null && rm -f "$1/Class1.cs"; }

    new_lib src/BuildingBlocks/SharedKernel SharedKernel
    new_lib src/Services/Budgeting/Budgeting.Domain Budgeting.Domain
    new_lib src/Services/Budgeting/Budgeting.Application Budgeting.Application
    new_lib src/Services/Budgeting/Budgeting.Infrastructure Budgeting.Infrastructure
    dotnet new webapi -o src/Services/Budgeting/Budgeting.Api -n Budgeting.Api >/dev/null

    dotnet new xunit -o tests/Budgeting.Domain.UnitTests -n Budgeting.Domain.UnitTests >/dev/null

    dotnet add src/Services/Budgeting/Budgeting.Domain reference src/BuildingBlocks/SharedKernel >/dev/null
    dotnet add src/Services/Budgeting/Budgeting.Application reference src/Services/Budgeting/Budgeting.Domain >/dev/null
    dotnet add src/Services/Budgeting/Budgeting.Infrastructure reference src/Services/Budgeting/Budgeting.Application >/dev/null
    dotnet add src/Services/Budgeting/Budgeting.Api reference src/Services/Budgeting/Budgeting.Infrastructure >/dev/null
    dotnet add tests/Budgeting.Domain.UnitTests reference src/Services/Budgeting/Budgeting.Domain >/dev/null
    dotnet add tests/Budgeting.Domain.UnitTests package FluentAssertions >/dev/null

    dotnet sln add $(find src tests -name '*.csproj') >/dev/null
    say "solution + контекст Budgeting созданы"
  fi
else
  say "пропущено (запусти с --solution, если .NET 10 SDK установлен)"
fi

head_ "Готово"
cat <<'EOF'
  Дальше:

    git init && git add -A
    git commit -m "chore: каркас проекта, правила для AI-агентов"

  Первый коммит должен содержать ТОЛЬКО правила и структуру — без кода.
  По истории AGENTS.md и .cursor/rules/ будут судить, управляли ли вы моделью.
EOF
