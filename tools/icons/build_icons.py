#!/usr/bin/env python3
"""Builds src/Grip/UI/Theme/Icons.xaml from Fluent UI System Icons (MIT).

Usage:
    python3 tools/icons/build_icons.py [path-to-@fluentui/svg-icons/icons]

Without a path the script downloads the npm package into a temp folder.
Each SVG path is re-serialized with explicit separators so WPF's path
mini-language reads it exactly like a browser would (arc flags, ".5.5"
number runs and implicit command repeats are all spelled out).
"""
import io
import os
import re
import sys
import tarfile
import tempfile
import urllib.request
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "src", "Grip", "UI", "Theme", "Icons.xaml")
NPM = "https://registry.npmjs.org/@fluentui/svg-icons/-/svg-icons-1.1.341.tgz"

# Grip icon key -> Fluent file name (20px grid)
ICONS = {
    "Add": "add_20_regular",
    "Alert": "alert_20_regular",
    "AppGeneric": "app_generic_20_regular",
    "Apps": "apps_20_regular",
    "Archive": "archive_20_regular",
    "ArrowClockwise": "arrow_clockwise_20_regular",
    "ArrowDownload": "arrow_download_20_regular",
    "ArrowEject": "arrow_eject_20_regular",
    "ArrowEnterLeft": "arrow_enter_left_20_regular",
    "ArrowLeft": "arrow_left_20_regular",
    "ArrowReset": "arrow_reset_20_regular",
    "ArrowSort": "arrow_sort_20_regular",
    "ArrowSwap": "arrow_swap_20_regular",
    "ArrowSync": "arrow_sync_20_regular",
    "ArrowUpload": "arrow_upload_20_regular",
    "Battery": "battery_6_20_regular",
    "BinRecycle": "bin_recycle_20_regular",
    "Bluetooth": "bluetooth_20_regular",
    "Board": "board_20_regular",
    "Box": "box_20_regular",
    "BrightnessHigh": "brightness_high_20_regular",
    "Broom": "broom_20_regular",
    "Calculator": "calculator_20_regular",
    "Camera": "camera_20_regular",
    "Checkmark": "checkmark_20_regular",
    "CheckmarkCircle": "checkmark_circle_20_regular",
    "CheckmarkCircleFilled": "checkmark_circle_20_filled",
    "ChevronDown": "chevron_down_20_regular",
    "ChevronLeft": "chevron_left_20_regular",
    "ChevronRight": "chevron_right_20_regular",
    "ChevronUp": "chevron_up_20_regular",
    "CircleFilled": "circle_20_filled",
    "Clipboard": "clipboard_20_regular",
    "ClipboardPaste": "clipboard_paste_20_regular",
    "ClipboardText": "clipboard_text_ltr_20_regular",
    "Color": "color_20_regular",
    "Copy": "copy_20_regular",
    "Cursor": "cursor_20_regular",
    "DarkTheme": "dark_theme_20_regular",
    "DataPie": "data_pie_20_regular",
    "Delete": "delete_20_regular",
    "Desktop": "desktop_20_regular",
    "DesktopOff": "desktop_off_20_regular",
    "DeveloperBoard": "developer_board_20_regular",
    "Dismiss": "dismiss_20_regular",
    "Document": "document_20_regular",
    "DocumentText": "document_text_20_regular",
    "Drag": "drag_20_regular",
    "Edit": "edit_20_regular",
    "Emoji": "emoji_20_regular",
    "ErrorCircle": "error_circle_20_regular",
    "Eye": "eye_20_regular",
    "EyeOff": "eye_off_20_regular",
    "Filter": "filter_20_regular",
    "Flash": "flash_20_regular",
    "Folder": "folder_20_regular",
    "FolderOpen": "folder_open_20_regular",
    "Globe": "globe_20_regular",
    "Gpu": "developer_board_lightning_20_regular",
    "Grid": "grid_20_regular",
    "HardDrive": "hard_drive_20_regular",
    "HeadphonesSoundWave": "headphones_sound_wave_20_regular",
    "History": "history_20_regular",
    "Image": "image_20_regular",
    "Info": "info_20_regular",
    "Keyboard": "keyboard_20_regular",
    "KeyboardShift": "keyboard_shift_20_regular",
    "LayoutQuarters": "layout_cell_four_20_regular",
    "LayoutSplitLeft": "layout_column_two_split_left_20_regular",
    "LayoutSplitRight": "layout_column_two_split_right_20_regular",
    "Link": "link_20_regular",
    "List": "list_20_regular",
    "LockClosed": "lock_closed_20_regular",
    "Maximize": "maximize_20_regular",
    "Mic": "mic_20_regular",
    "MicOff": "mic_off_20_regular",
    "MoreHorizontal": "more_horizontal_20_regular",
    "MusicNote": "music_note_2_20_regular",
    "Navigation": "navigation_20_regular",
    "Next": "next_20_regular",
    "Open": "open_20_regular",
    "Options": "options_20_regular",
    "Pause": "pause_20_regular",
    "Person": "person_20_regular",
    "Pin": "pin_20_regular",
    "PinFilled": "pin_20_filled",
    "PinOff": "pin_off_20_regular",
    "Play": "play_20_regular",
    "Power": "power_20_regular",
    "Previous": "previous_20_regular",
    "QuestionCircle": "question_circle_20_regular",
    "Ram": "ram_20_regular",
    "Record": "record_20_regular",
    "RecordFilled": "record_20_filled",
    "ReOrder": "re_order_dots_vertical_20_regular",
    "Ruler": "ruler_20_regular",
    "Screenshot": "screenshot_20_regular",
    "Search": "search_20_regular",
    "Settings": "settings_20_regular",
    "Shield": "shield_20_regular",
    "Sleep": "sleep_20_regular",
    "Sparkle": "sparkle_20_regular",
    "Speaker": "speaker_2_20_regular",
    "SpeakerLow": "speaker_1_20_regular",
    "SpeakerMute": "speaker_mute_20_regular",
    "Star": "star_20_regular",
    "StarFilled": "star_20_filled",
    "Subtract": "subtract_20_regular",
    "Tabs": "tabs_20_regular",
    "Temperature": "temperature_20_regular",
    "TextClearFormatting": "text_clear_formatting_20_regular",
    "TextT": "text_t_20_regular",
    "Timer": "timer_20_regular",
    "ToggleRight": "toggle_right_20_regular",
    "TrayItemAdd": "tray_item_add_20_regular",
    "Video": "video_20_regular",
    "Warning": "warning_20_regular",
    "WeatherMoon": "weather_moon_20_regular",
    "WeatherMoonFilled": "weather_moon_20_filled",
    "Window": "window_20_regular",
    "WindowConsole": "window_console_20_regular",
    "WindowMultiple": "window_multiple_20_regular",
    "WindowRestore": "arrow_minimize_20_regular",
    "Wrench": "wrench_screwdriver_20_regular",
}

ARG_COUNTS = {"m": 2, "l": 2, "h": 1, "v": 1, "c": 6, "s": 4, "q": 4, "t": 2, "a": 7, "z": 0}
NUMBER = re.compile(r"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")


def tokenize(d):
    i, n = 0, len(d)
    while i < n:
        ch = d[i]
        if ch.isspace() or ch == ",":
            i += 1
        elif ch.lower() in ARG_COUNTS:
            yield ("cmd", ch)
            i += 1
        else:
            m = NUMBER.match(d, i)
            if not m:
                raise ValueError(f"bad path data at {i}: {d[i:i + 20]!r}")
            yield ("num", m.group(0))
            i = m.end()


def read_flag(d, i):
    """Arc flags are single 0/1 characters that may touch the next number."""
    n = len(d)
    while i < n and (d[i].isspace() or d[i] == ","):
        i += 1
    if i < n and d[i] in "01":
        return d[i], i + 1
    raise ValueError(f"bad arc flag at {i}")


def normalize(d):
    """Re-emit SVG path data with explicit command letters and separators."""
    out = []
    i, n = 0, len(d)
    cmd = None
    first_after_move = False

    def skip(j):
        while j < n and (d[j].isspace() or d[j] == ","):
            j += 1
        return j

    def number(j):
        j = skip(j)
        m = NUMBER.match(d, j)
        if not m:
            raise ValueError(f"expected number at {j}: {d[j:j + 20]!r}")
        return fmt(m.group(0)), m.end()

    while True:
        i = skip(i)
        if i >= n:
            break
        ch = d[i]
        if ch.lower() in ARG_COUNTS:
            cmd = ch
            i += 1
            first_after_move = cmd in "Mm"
            if cmd in "Zz":
                out.append("Z")
                continue
        elif cmd is None:
            raise ValueError("path data must start with a command")
        # Implicit repeat: after M the extra pairs are L (m -> l).
        emit = cmd
        if cmd in "Mm" and not first_after_move:
            emit = "L" if cmd == "M" else "l"
        first_after_move = False
        if cmd.lower() == "a":
            rx, i = number(i)
            ry, i = number(i)
            rot, i = number(i)
            large, i = read_flag(d, i)
            sweep, i = read_flag(d, i)
            x, i = number(i)
            y, i = number(i)
            out.append(f"{emit}{rx},{ry} {rot} {large} {sweep} {x},{y}")
        else:
            count = ARG_COUNTS[cmd.lower()]
            args = []
            for _ in range(count):
                v, i = number(i)
                args.append(v)
            pairs = []
            if cmd.lower() in "hv":
                pairs = args
            else:
                pairs = [f"{args[k]},{args[k + 1]}" for k in range(0, len(args), 2)]
            out.append(emit + " ".join(pairs))
    return " ".join(out)


def fmt(num):
    value = float(num)
    text = f"{value:.4f}".rstrip("0").rstrip(".")
    return "0" if text in ("-0", "") else text


def fetch_icons():
    tmp = tempfile.mkdtemp(prefix="fluent-")
    data = urllib.request.urlopen(NPM).read()
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as tar:
        tar.extractall(tmp, filter="data")
    return os.path.join(tmp, "package", "icons")


def main():
    source = sys.argv[1] if len(sys.argv) > 1 else fetch_icons()
    ns = {"svg": "http://www.w3.org/2000/svg"}
    lines = [
        '<!-- Generated by tools/icons/build_icons.py from Fluent UI System Icons -->',
        '<!-- © Microsoft Corporation, MIT License. Do not edit by hand. -->',
        '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
        '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
    ]
    for key, name in sorted(ICONS.items()):
        path = os.path.join(source, name + ".svg")
        root = ET.parse(path).getroot()
        view_box = root.get("viewBox", "0 0 20 20").split()
        if view_box != ["0", "0", "20", "20"]:
            raise SystemExit(f"{name}: unexpected viewBox {view_box}")
        parts = [normalize(p.get("d")) for p in root.iter("{http://www.w3.org/2000/svg}path")]
        if not parts:
            raise SystemExit(f"{name}: no paths")
        rule = "F0" if any(p.get("fill-rule") == "evenodd" for p in root.iter("{http://www.w3.org/2000/svg}path")) else "F1"
        lines.append(f'    <StreamGeometry x:Key="Icon.{key}">{rule} {" ".join(parts)}</StreamGeometry>')
    lines.append("</ResourceDictionary>")
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"wrote {len(ICONS)} icons to {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    main()
