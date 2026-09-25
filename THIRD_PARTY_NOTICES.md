# Стороннее ПО

Grip — самостоятельный проект, но одна функция использует чужой компонент напрямую, без изменений.

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
