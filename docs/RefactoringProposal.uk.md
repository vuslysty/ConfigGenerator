# Пропозиція рефакторингу ConfigGenerator

## Що зараз виглядає як «костиль»

### 1) Змішано надто багато відповідальностей в одному місці
- `ConfigGenerator` одночасно:
  - читає дані з різних джерел,
  - парсить таблиці,
  - валідує типи,
  - генерує C# код,
  - серіалізує JSON,
  - пише файли на диск.
- Через це клас важко тестувати, а сценарії ("лише валідація", "лише код", "лише JSON") неявні.

### 2) Великий utility-клас із бізнес-логікою
- `TableExtractionEngine` містить одразу:
  - пошук таблиць,
  - побудову моделей,
  - валідацію,
  - перевірки дублювання/перетинів,
  - форматування помилок.
- Це типова ознака God-class: будь-яка зміна в парсері чіпає «все».

### 3) Дублювання реєстрації типів
- Логіка реєстрації `AvailableTypes` повторюється в різних місцях (`CodeGenerator`, `ConfigGenerator`, `ConfigsBase`).
- Будь-який новий тип доведеться додавати мінімум у 2-3 місцях.

### 4) Неявний життєвий цикл і side-effects
- Методи `generateCode` / `generateJson` всередині роблять parse + write-to-disk.
- Асинхронний запис файлів викликається без `await`.
- В `Program.cs` захардкожені значення (`spreadsheetId`, шлях до credential, структура директорій).

### 5) Різні стилі API
- Публічні методи в lowerCamelCase, місцями "try" у назві замість стандартного `Try...`.
- Це ускладнює читання API і підвищує когнітивне навантаження.

---

## Цільова архітектура (логічний розподіл відповідальностей)

Розбити проект на 4 шари:

1. **Sources (Input adapters)**
   - `ISpreadsheetDataSource` + реалізації (`GoogleSheetDataSource`, `ExcelFileDataSource`).
   - Відповідають лише за читання сирих page data.

2. **Parsing + Validation (Domain/application services)**
   - `ITableParser` -> `List<TableData>`.
   - `ITableValidator` -> `ValidationResult` (список помилок, warning, row/col).
   - `ITypeRegistryFactory` -> єдина точка створення `AvailableTypes`.

3. **Generation (Output builders)**
   - `IConfigCodeBuilder` -> повертає string коду.
   - `ITableJsonSerializer` -> повертає json string.
   - Без I/O.

4. **Persistence / Orchestration**
   - `IArtifactWriter` (файлова система).
   - `ConfigGenerationPipeline` — оркеструє кроки: read -> parse -> validate -> build -> write.

---

## Пропонована структура папок

```text
/src
  /Application
    ConfigGenerationPipeline.cs
    Contracts/
      ITableParser.cs
      ITableValidator.cs
      ITypeRegistryFactory.cs
      IArtifactWriter.cs
  /Domain
    /Tables
      (TableData та пов'язані моделі)
    /Validation
      ValidationResult.cs
      ValidationIssue.cs
  /Infrastructure
    /Spreadsheet
      GoogleSheetDataSource.cs
      ExcelFileDataSource.cs
    /Serialization
      TableDataSerializer.cs
    /CodeGen
      CodeGenerator.cs
    /FileSystem
      ArtifactWriter.cs
  /Presentation
    Program.cs
```

> Якщо не хочете фізично рухати файли зараз — спочатку зробіть namespace-реорганізацію (логічну), а вже потім move файлів.

---

## Конкретний план рефакторингу (поетапно, без великих ризиків)

### Етап 1 — стабілізація API і побічних ефектів
- [x] Перейменувати публічні методи на PascalCase (`GenerateCode`, `GenerateJson`, `TryParseTables`).
- [x] Замінити `File.WriteAllTextAsync(...)` на безпечний запис (через writer).
- [x] Винести конфіг (`spreadsheetId`, `credentialsFile`, output path) у env/аргументи CLI.

### Етап 2 — виділення сервісів з колишнього `TableDataUtilities`
- [x] `TableDataUtilities` прибрано; extraction/validation/normalization рознесено по сервісах (`TableExtractionEngine`, `TableDataValidationService`, `TableMetadataValidationService`, `TableNameNormalizationService`, `IntIdNormalizationService`).
- [x] Валідація повертає `ValidationResult` із деталями.
- [x] Логування в домені прибрано: повідомлення збираються структуровано, друк виконується в presentation layer.
- [x] Дорізати extraction-рівень до окремих parser-компонентів для value/database/constant кандидатів таблиць.
- [x] Винести базові extraction-компоненти в окремі сервіси (`TableCandidateLocator`, `TableCellReader`, `ValueTableExtractor`, `ConstantTableExtractor`, `DatabaseTableExtractor`).

### Етап 3 — єдина фабрика типів
- [x] Введено `ITypeRegistryFactory`.
- [x] Прибрано дублювання реєстрації типів між генерацією/валідацією.
- [ ] Додати unit-тести на єдину точку реєстрації.

### Етап 4 — pipeline orchestration
- [x] Створено `ConfigGenerationPipeline` з детальними результатами виконання.
- [x] Додано `GenerationResult`/`PipelineRunResult` для прозорого статусу pipeline.
- [x] `Program.cs` використовується як composition root + керований вивід повідомлень.

### Етап 5 — тестованість
- [ ] Unit-тести для parser/validator без файлової системи.
- [ ] Integration-тест для end-to-end на тестовому XLSX fixture.
- [ ] Snapshot-тест для generated code (Roslyn output) і json.

---

## Розподіл відповідальностей (коротко)

- `ConfigGenerator` (поточний): розділити на `Pipeline` + `ArtifactBuilder` + `ArtifactWriter`.
- `CodeGenerator`: залишити тільки побудову syntax tree з чистого `TableData`.
- `TableExtractionEngine`: далі ділити на ще менші парсери/reader-компоненти до повної SRP-декомпозиції.
- `Program`: лише composition root (DI + config + запуск).

---

## Який результат отримаєте після рефакторингу

- Прозорий pipeline замість «магії» в кількох великих класах.
- Менше дублювання та side-effects.
- Швидше додавати нові типи, нові data source і нові формати артефактів.
- Значно краща тестованість і дебаг.

---

## Швидкі перемоги (можна зробити за 1 день)

- [x] Виправити async-запис файлів (без "fire-and-forget").
- [x] Уніфікувати неймінг публічних методів.
- [x] Прибрати hardcode з `Program.cs` у конфіг.
- [x] Виділити реєстрацію типів в окремий `TypeRegistryFactory`.

Це вже помітно покращить відчуття «логічності» коду без повного переписування.
