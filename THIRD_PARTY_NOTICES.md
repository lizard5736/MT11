# Стороннее ПО

Grip — самостоятельный проект, но несколько функций используют чужие компоненты напрямую, без изменений.

## PawnIO.Modules — модуль `AMDFamily17.bin` (температура процессора AMD)

Чтение температуры процессора AMD (Zen/Zen+/Zen 2/Zen 3/Zen 4/Zen 5, семейства 0x17–0x1A) идёт через
[PawnIO](https://github.com/namazso/PawnIO) — независимый, с открытым кодом, EV-подписанный драйвер
для безопасного чтения регистров процессора из пользовательского режима. Grip не устанавливает и не
подключает сам драйвер: он ставится пользователем отдельно (`winget install namazso.PawnIO`), Grip
только проверяет, установлен ли он, и обращается к нему тем же способом, что и другие приложения.

Файл `src/Grip/Resources/PawnIO/AMDFamily17.bin` — это скомпилированный и подписанный модуль
`AMDFamily17` из [namazso/PawnIO.Modules](https://github.com/namazso/PawnIO.Modules), версия релиза
`0.2.11`, включён в Grip как есть, без изменений.

- Копирайт: © namazso <admin@namazso.eu>
- Лицензия: GNU Lesser General Public License 2.1 или более поздняя — полный текст в
  [`licenses/PawnIO.Modules.LGPL-2.1.txt`](licenses/PawnIO.Modules.LGPL-2.1.txt)
- Исходный код модуля (`AMDFamily17.p`): https://github.com/namazso/PawnIO.Modules/blob/0.2.11/AMDFamily17.p
- SHA-256 включённого файла: `dae74615761b78bdf064dfb3e136252ddcc6fc727d88f14738d0e5800d427a91`

Протокол общения с драйвером (`CreateFile`/`DeviceIoControl`, коды IOCTL, формат буферов) написан по
образцу открытого кода [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
(MPL-2.0) — сам код в Grip написан заново, не скопирован, но логика и константы (SMN-адрес датчика
температуры, битовые маски поправки в -49°C) взяты оттуда как проверенный источник.

## Black Ops One — шрифт заголовков в теме DOOM 1993

Тема DOOM 1993 (Настройки → Общие → Тема) использует для заголовков шрифт
[Black Ops One](https://github.com/google/fonts/tree/main/ofl/blackopsone) — это стилизация под
военный трафарет 1993 года, а не копия собственного шрифта игры DOOM (он проприетарный и
принадлежит id Software, в Grip не используется и не копировался).

Файл `src/Grip/Assets/Fonts/BlackOpsOne-Regular.ttf` включён в Grip как есть, без изменений.

- Автор: Sorkin Type Co. (James Grieshaber, Eben Sorkin)
- Копирайт: © 2022 The Black-Ops Project Authors
- Лицензия: SIL Open Font License 1.1 — полный текст в
  [`licenses/BlackOpsOne.OFL-1.1.txt`](licenses/BlackOpsOne.OFL-1.1.txt)
- Источник: https://github.com/google/fonts/tree/main/ofl/blackopsone
- SHA-256 включённого файла: `282a825b5f294377387e3969f765408157dbea8da0f5d0aae68c6bc704b145b3`

## Fluent UI System Icons — иконки интерфейса

`src/Grip/UI/Theme/Icons.xaml` — векторные иконки из [Fluent UI System
Icons](https://github.com/microsoft/fluentui-system-icons) (© Microsoft Corporation, лицензия MIT),
перегенерированные скриптом `tools/icons/build_icons.py`. Несколько иконок в теме DOOM 1993
(значок в трее, знак Grip в шапке панели/настроек/приветствия/окна «О программе») заменены на
собственный пиксель-арт (`tools/icons/build_doom_mark.py`, `src/Grip/Assets/Doom/mark-24.png`) —
это не модификация иконок Fluent, а отдельный рисунок в силуэте того же знака Grip, что рисуется
в остальных темах кодом (`GripMark.cs`).
