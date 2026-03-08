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
