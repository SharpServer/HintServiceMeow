
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white)](https://discord.gg/H3TACT3Buh) [![GitHub Release](https://img.shields.io/github/v/release/MeowServer/HintServiceMeow)](https://github.com/MeowServer/HintServiceMeow/releases)

## Введение
**HintServiceMeow (HSM)** — это фреймворк для SCP: Secret Laboratory, позволяющий плагинам отображать текст в выбранной позиции на экране игрока.

---

## Установка

Для установки плагина выполните следующие шаги:

1. Перейдите на [страницу релизов](https://github.com/MeowServer/HintServiceMeow/releases) и скачайте последний файл `HintServiceMeow.dll`. Затем поместите его в папку плагинов.
2. Если вы используете **LabAPI** (API по умолчанию), поместите `Harmony.dll` в папку **dependencies**.
3. Перезапустите сервер.
4. Настройте конфигурацию по своему усмотрению.
5. Перезапустите сервер ещё раз для применения изменений конфигурации.

---

## Документация

Вот несколько полезных ресурсов для начала работы:

- [Начало работы](/Docs/Russian/GettingStarted.md)
- [Основные функции](/Docs/Russian/CoreFeatures.md)
- [История изменений](/Docs/Russian/CHANGELOG.md)

---

## Часто задаваемые вопросы

### 1. Почему плагин не работает?
- Убедитесь, что **HintServiceMeow** установлен корректно.
- Проверьте, нет ли других плагинов, конфликтующих с **HintServiceMeow**.
- Просмотрите ошибки, возникающие при активации плагинов.

### 2. Почему подсказки перекрываются?
- Это может происходить, когда несколько плагинов размещают подсказки в одной позиции. Вы можете изменить позицию UI в файле конфигурации каждого плагина.
- Если плагин не позволяет изменить позицию через файл конфигурации, обратитесь к автору плагина за помощью.

---

## Авторы

Спасибо всем, кто внёс вклад в HintServiceMeow!
Ваши pull request'ы, отчёты об ошибках и предложения помогают поддерживать этот проект.

- [@Someone](https://github.com/Someone-193) — За добавление проверки стиля кода.
- [XLittleLeft](https://github.com/XLittleLeft) — За добавление поддержки LabAPI.
- [Firething](https://github.com/Firething) — За добавление португальского перевода.
