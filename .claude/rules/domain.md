---
paths:
  - "src/Services/*/*.Domain/**/*"
  - "src/BuildingBlocks/SharedKernel/**/*"
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

Эталонный агрегат (открывать при создании нового агрегата): src/Services/Budgeting/Budgeting.Domain/Budgets/Budget.cs
