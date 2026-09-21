# Инварианты агрегата Budget

Номера 1–7 — те, на которые ссылается ADR-0001. Номера 8 и выше в ADR
не упоминаются, но код их держит. Номер инварианта — стабильный
идентификатор: не перенумеровывать, удалённые помечать как «снят».

Колонка «Тест»: **да** — тест упадёт, если убрать проверку;
**частично** — тест есть, но не ловит часть поведения (указано, какую);
**нет** — тест не упадёт.

## Инварианты

| # | Формулировка | Где проверяется | Тест | Что сломается без него |
|---|---|---|---|---|
| 1 | **Учтено ровно раз.** Повтор зерна `ChargeKey` с той же суммой не меняет `Spent` и не поднимает событий | `BudgetPeriod.ApplyCharge` — поиск по ключу, ранний выход при равной сумме | да: `Should_LeaveSpentUnchanged_When_SameChargeKeyAppliedTwiceWithSameAmount` (`Spent`), `Should_NotRaiseEvents_When_SameChargeKeyAppliedTwiceWithSameAmount` | Повторная доставка CUR удваивает расход и поднимает ложные алерты |
| 2 | **Исправление = дельта.** Повтор зерна с другой суммой сдвигает `Spent` на разницу и заменяет сумму зерна | `BudgetPeriod.ApplyCharge` — `delta`, `ReplaceAmount` | да: `Should_IncreaseSpentByDelta_…`, `Should_DecreaseSpentByDelta_…` (`Spent`), `Should_ReplaceAppliedChargeAmount_When_ChargeIsCorrected` | Коррекция ложится поверх старой суммы, а не вместо неё |
| 3 | **Порог срабатывает один раз.** Порог переходит `Clear → Latched` при утилизации ≥ P и поднимает ровно одно `ThresholdAlertRaised`; пока он `Latched`, событий нет | `BudgetPeriod.EvaluateThresholds` | да: `Should_RaiseSingleThresholdAlert_…`, `Should_NotRaiseEvents_When_SpendIncreasesFurther…`, `Should_TreatExactEightyPercentAsThresholdCrossing` (граница ≥). По отдельности проверен только порог 80; 50 и 100 — только в `Should_RaiseThreeThresholdAlerts…` | Алерт на каждое начисление выше порога |
| 4 | **Гистерезис.** Порог перезаряжается (`Latched → Clear`, `ThresholdReset`), только когда утилизация ≤ P − 5 п.п. | `BudgetPeriod.EvaluateThresholds`, `HysteresisPercentagePoints` | да: `Should_KeepEightyPercentLatched_When_UtilizationDropsToSeventyNinePercent` (держит), `Should_ResetThreshold_When_UtilizationDropsToSeventyFivePercent` (граница ≤ P − 5), `Should_ResetThreshold_When_UtilizationDropsToSeventyFourPercent` | Колебание суммы у порога поднимает серию алертов и сбросов |
| 5 | **Лимит закрытого периода заморожен.** `ChangeLimit` на закрытом периоде → `ClosedPeriodLimit` | `Budget.ChangeLimit` | да: `Should_ReturnFailure_When_ChangingLimitOnClosedPeriod` (`BudgetErrors.ClosedPeriodLimit`) | Меняется история: отчёт по закрытому месяцу перестаёт совпадать с `PeriodClosed` |
| 6 | **Одна валюта.** `Limit`, `Spent` и все `AppliedCharge` периода — в одной валюте | `BudgetPeriod.ApplyCharge` (начисление), `Budget.ChangeLimit` (лимит) | да: `Should_ReturnFailure_When_ChargeCurrencyDiffersFromLimit` (`BudgetErrors.CurrencyMismatch`), `Should_LeaveSpentUnchanged_When_ChargeCurrencyDiffersFromLimit`, `Should_ReturnCurrencyMismatch_When_ChangingLimitWithDifferentCurrency` | `UtilizationPercent` делит числа разных валют; после смены валюты лимита все последующие начисления отклоняются |
| 7 | **Лимит строго положителен** | `Budget.Create`, `Budget.ChangeLimit` | да: `Should_ReturnFailure_When_CreatedWithNonPositiveLimit`, `Should_ReturnFailure_When_ChangingLimitToNonPositiveAmount` | `DivideByZeroException` в `UtilizationPercent`; при отрицательном лимите утилизация меняет знак |
| 8 | **Дата потребления внутри периода** `[Start, End)` | `Budget.ApplyCharge` → `PeriodRange.Contains` | да: четыре граничных теста в `BudgetAppliedChargeTests`; отказы проверяют `BudgetErrors.UsageDateOutsidePeriod` | Начисление за чужой месяц попадает в этот бюджет |
| 9 | **Период непустой:** `End > Start` | `PeriodRange.Create` | да: `Should_ReturnFailure_When_EndIsNotAfterStart` (`End == Start` и `End < Start`) | Бюджет, который отклоняет любое начисление |
| 10 | **Ключ зерна нормализован.** `Service` непустой, без пробелов по краям, в нижнем регистре | `ChargeKey.Create` | да: `Should_ReturnEmptyService_When_ServiceIsWhitespace`, `Should_BeEqual_When_ServicesDifferByPaddingAndCase` | `AmazonEC2` и `amazonec2` становятся двумя зёрнами → двойной учёт (ломает № 1) |
| 11 | **Период закрывается один раз** | `Budget.ClosePeriod` | да: `Should_ReturnAlreadyClosed_When_ClosingAlreadyClosedPeriod` | Два `PeriodClosed` для одного месяца уходят потребителям |
| 12 | **`Spent` = сумма всех `AppliedCharge.Amount`**; при открытии — ноль в валюте лимита | Нигде явно. Держится только тем, что `Spent` и `ReplaceAmount` меняются вместе в `ApplyCharge` | да: `Should_KeepSpentEqualToSumOfAppliedCharges_When_ChargesAreAppliedAndCorrected`, `Should_HaveZeroSpentInLimitCurrency_When_CreatedWithValidLimit` | Любой новый путь изменения суммы (например, `Reconcile`) рассинхронизирует расход и зёрна |
| 13 | **Набор порогов фиксирован:** ровно 50/80/100, по одному состоянию на каждый, при открытии все `Clear` | Конструктор `BudgetPeriod` | нет: только косвенно, через `HaveCount(3)` в тесте на отрицательный `Spent` | Дубль порога даёт двойной алерт; отсутствие порога — пропущенный |
| 14 | **События порогов идут по возрастанию P** | `EvaluateThresholds` — `OrderBy(Percent)` | частично: проверены алерты (`Should_RaiseThreeThresholdAlertsInAscendingOrder_…`); порядок сбросов не проверен | Потребитель видит «100%» раньше «50%» |
| 15 | **Смена лимита пересчитывает пороги** | `Budget.ChangeLimit` → `Period.EvaluateThresholds` | частично: проверено повышение лимита (держит 80% и сбрасывает 80%); понижение, пересекающее порог вверх, не проверено | После повышения лимита порог остаётся взведённым навсегда; после понижения алерта нет |
| 16 | **Каждое событие несёт `BudgetId` и уникальный `EventId`** | Конструкторы событий, `DomainEvent` | да: `Should_AssignDistinctEventIds_…` и проверки `BudgetId` в тестах событий | Потребитель не может дедуплицировать события и понять, к какому бюджету они относятся |

## Инварианты без тестов

Покрытие сознательно неполное. № 13 — набор порогов задан константой
в конструкторе. № 14 — порядок сбросов использует тот же `OrderBy`,
что и порядок алертов, который проверен. № 15 — понижение лимита
не встречается в текущем ingestion.
Вернуться к ним, если пороги станут конфигурируемыми или появится
сценарий понижения лимита.

## Где состояние меняется в обход проверок

Все методы ниже — `internal`. Снаружи сборки `Budgeting.Domain` их не вызвать,
но внутри доменной сборки каждый такой вызов проходит мимо инвариантов.

| Метод | Что делает без проверки | Какие инварианты под угрозой |
|---|---|---|
| `BudgetPeriod.ChangeLimit` | Ставит лимит: не проверяет знак, валюту и `IsClosed`, не пересчитывает пороги | 5, 6, 7, 15 — проверки живут только в `Budget.ChangeLimit` |
| `BudgetPeriod.EvaluateThresholds` | Корень должен вызывать его вручную после смены лимита | 15 — новый метод корня, меняющий лимит или расход, легко забудет вызов |
| `BudgetPeriod.Close` | Ставит `IsClosed`, не проверяя, что период уже закрыт | 11 |
| `AppliedCharge.ReplaceAmount` | Меняет сумму зерна, не трогая `Spent` | 2, 12 |
| `ThresholdState.Latch` / `Reset` | Меняют состояние порога, не поднимая событий | 3, 4 — состояние и события расходятся |
| `AggregateRoot.ClearDomainEvents` (public) | Стирает накопленные события | 16 — нужен UnitOfWork; вызов в любом другом месте теряет события |

## Правила, которые живут только в голове

Поведение, которое код реализует или тест фиксирует, но решение
нигде не записано. По каждому нужна строка в ADR или новый инвариант.

1. **Закрытый период принимает начисления.** Тесты
   `Should_Succeed_When_ApplyingChargeToClosedPeriod` и
   `Should_ChangeSpent_When_ApplyingChargeToClosedPeriod` это фиксируют.
   Вероятная причина — поздние корректировки CUR, но она нигде не записана.
   Следствие, которое никто не обсуждал: пороги на закрытом периоде
   продолжают поднимать алерты. Лимит закрытого периода заморожен (№ 5),
   а расход нет — асимметрию нужно объяснить.
2. **Знак суммы.** Отрицательная сумма зерна принимается, `Spent` может
   уйти в минус. `Should_ClearAllThresholds_When_SpentIsNegative` это
   закрепляет. Отрицательное зерно (кредит, возврат) правдоподобно;
   отрицательный расход за весь период — вряд ли. Нужно решить и
   записать.
3. **Порог 100% перезаряжается на 95%.** Гистерезис одинаковый для всех
   порогов, поэтому «бюджет превышен» может прийти повторно после
   падения до 95%. Намеренно ли это — нигде не сказано.
4. **Бюджет — один период.** Код выбрал модель «одно поле `Period`»;
   открыть следующий период нечем. Это решение о жизненном цикле
   `Budget`, а в глоссарии и ADR его нет.
5. **Изменение расхода не поднимает события.** `domain.mdc` требует
   `Raise(...)` на каждое значимое изменение, а `ApplyCharge` без
   пересечения порога молчит. Либо добавить `ChargeApplied`, либо
   записать, почему расход наружу не публикуется.
6. **Порядок проверок в `ChangeLimit`.** Сначала проверяются знак и
   валюта, потом `IsClosed`. На закрытом периоде с некорректным лимитом
   вернётся не `ClosedPeriodLimit`. Сейчас это ни на что не влияет, но
   порядок ошибок — часть контракта для API.
