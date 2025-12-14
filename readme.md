# RZD Ads
**RZD Ads** — пакет для интеграции рекламных модулей в Unity-проекты.  

### Установка Через Git URL
Добавьте пакет в Unity через **Package Manager → Add package from Git URL**:
`https://github.com/Anklbenz/Ads.git?path=Assets/Plugins/RZDAds`

## Зависимости
Для корректной работы пакета необходимы следующие зависимости:

1. **Unity TextMeshPro** (версия 3.0.6 и выше)  
   Если в проекте нет Unity подтянет автоматически

2. **UniTask (async/await интеграция)**
UniTask используется для асинхронных операций внутри RZD Ads SDK.

Установите через Git URL:

`https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

## Структура пакета
- RZDAds/
- package.json      # Описание пакета (name, version, dependencies)
- Runtime/          # Скрипты SDK
- Samples~/         # Примеры использования

## Использование
1. Импортируйте пакет через Git URL
2. Установите зависимость UniTask через Git URL
3. Откройте пример в Samples~

## Обновления
Для установки конкретной версии используйте Udpade в Package Manger
