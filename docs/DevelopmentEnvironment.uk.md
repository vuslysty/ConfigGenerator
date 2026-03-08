# Налаштування середовища для запуску ConfigGenerator

## Чому виникає `dotnet: command not found`
Проєкт таргетує `.NET 8` (`net8.0`), тому для локального запуску та тестів потрібен встановлений **.NET SDK 8.x**.

## Що встановити
1. Встановіть .NET SDK 8 з офіційного сайту:
   - https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. Перевірте встановлення:
   - `dotnet --info`

## Мінімальні команди для проєкту
- Збірка: `dotnet build`
- Тести: `dotnet test`
- Запуск: `dotnet run --project ConfigGenerator.csproj`

## Для CI
У CI варто явно додавати крок інсталяції SDK 8.x (наприклад, `actions/setup-dotnet`), щоб уникнути нестабільності середовища.

## Конфігурація генератора
- Основний вхідний конфіг для `Program` — файл `generator.settings.json` (можна створити на основі `generator.settings.example.json`).
- Також підтримуються CLI override-параметри у форматі `--key=value`, наприклад:
  - `--config=generator.settings.json`
  - `--spreadsheet-id=...`
  - `--generated-folder=...`
  - `--mode=parse|validate|generate|generate-code|generate-json`
  - `--json-input-file=tables.json`
- Змінні середовища лишаються тільки як backward-compatible fallback, але не як основний механізм конфігурації.

- Режими запуску: `parse` (лише парсинг), `validate` (парсинг+валідація), `generate` (повний цикл з генерацією).

- Якщо передано `JsonInputFile` (або `--json-input-file`), вхідні таблиці беруться з JSON, без читання Google Sheet.
- У цьому випадку доступні практичні сценарії: `validate` (валідація JSON), `generate-code` (генерація тільки коду), `generate-json` (перегенерація/нормалізація JSON), `generate` (обидва артефакти).
