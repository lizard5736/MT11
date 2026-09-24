namespace Grip.Core.Search;

public sealed record EmojiEntry(string Emoji, string[] KeywordsEn, string[] KeywordsRu)
{
    public string Name(Localization.UiLanguage language) =>
        language == Localization.UiLanguage.Ru ? KeywordsRu[0] : KeywordsEn[0];

    public IEnumerable<string> AllKeywords => KeywordsEn.Concat(KeywordsRu);
}

/// <summary>The emoji people actually reach for, searchable in English and Russian.</summary>
public static class EmojiCatalog
{
    public static IReadOnlyList<EmojiEntry> All { get; } = Build();

    private static List<EmojiEntry> Build()
    {
        var list = new List<EmojiEntry>(300);
        void E(string emoji, string en, string ru) =>
            list.Add(new EmojiEntry(emoji, en.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                ru.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

        // Faces
        E("😀", "grinning smile happy", "улыбка радость счастье");
        E("😃", "smiley happy joy", "улыбка радость");
        E("😄", "smile laugh happy", "смех улыбка");
        E("😁", "grin beaming teeth", "ухмылка зубы");
        E("😆", "laughing lol", "смеюсь хохот");
        E("😅", "sweat smile relief", "пот фух облегчение");
        E("🤣", "rofl rolling laughing", "ржу катаюсь");
        E("😂", "joy tears laughing lol", "слёзы смех ржака");
        E("🙂", "slight smile ok", "улыбка ладно");
        E("🙃", "upside down irony", "вверх ногами ирония");
        E("😉", "wink", "подмигивание");
        E("😊", "blush smile warm", "румянец улыбка");
        E("😇", "innocent angel halo", "ангел невинный");
        E("🥰", "love hearts adore", "любовь обожаю сердечки");
        E("😍", "heart eyes love", "влюблён глаза сердечки");
        E("🤩", "star struck wow", "звёзды восторг вау");
        E("😘", "kiss blowing", "поцелуй чмок");
        E("😋", "yum tasty delicious", "вкусно ням");
        E("😛", "tongue playful", "язык дразнить");
        E("😜", "wink tongue crazy", "подмигивание язык");
        E("🤪", "zany crazy goofy", "безумный чудик");
        E("🤔", "thinking hmm think", "думаю хмм размышление");
        E("🤨", "raised eyebrow skeptical", "бровь сомнение скептик");
        E("😐", "neutral meh", "нейтрально пофиг");
        E("😑", "expressionless blank", "без эмоций");
        E("😶", "no mouth silent speechless", "молчание без слов");
        E("🙄", "eye roll whatever", "закатить глаза");
        E("😏", "smirk sly", "ухмылка хитрый");
        E("😬", "grimace awkward yikes", "неловко гримаса");
        E("😌", "relieved calm", "облегчение спокойствие");
        E("😔", "pensive sad", "грусть задумчивость");
        E("😪", "sleepy tired", "сонный устал");
        E("😴", "sleeping zzz sleep", "сплю сон");
        E("😷", "mask sick ill", "маска болею");
        E("🤒", "thermometer sick fever", "температура болею");
        E("🤯", "mind blown exploding", "взрыв мозга шок");
        E("🥳", "party celebrate birthday", "праздник вечеринка день рождения");
        E("😎", "cool sunglasses", "круто очки");
        E("🤓", "nerd geek glasses", "ботаник очки");
        E("🧐", "monocle inspect curious", "монокль изучаю");
        E("😕", "confused unsure", "растерян непонятно");
        E("😟", "worried concerned", "беспокойство тревога");
        E("🙁", "frown sad", "грусть");
        E("😮", "open mouth surprised wow", "удивление ого");
        E("😲", "astonished shocked", "изумление шок");
        E("😳", "flushed embarrassed", "смущение покраснел");
        E("🥺", "pleading puppy eyes please", "умоляю пожалуйста глазки");
        E("😢", "cry tear sad", "плачу слеза грусть");
        E("😭", "sob crying loud", "рыдаю плачу");
        E("😱", "scream fear horror", "крик ужас страх");
        E("😤", "triumph huff steam", "пар фыркаю");
        E("😡", "rage angry mad", "злость ярость гнев");
        E("🤬", "cursing swearing", "ругаюсь мат");
        E("😈", "devil smiling evil", "дьявол чертёнок");
        E("💀", "skull dead", "череп умер");
        E("💩", "poop pile", "какашка");
        E("🤡", "clown", "клоун");
        E("👻", "ghost boo", "привидение призрак");
        E("👽", "alien ufo", "пришелец инопланетянин");
        E("🤖", "robot bot", "робот бот");
        E("😺", "cat smile", "кот улыбка");
        E("🙈", "see no evil monkey", "обезьянка не вижу");
        E("🙉", "hear no evil monkey", "обезьянка не слышу");
        E("🙊", "speak no evil monkey", "обезьянка молчу");

        // Hands and people
        E("👍", "thumbs up like yes ok", "лайк класс да");
        E("👎", "thumbs down dislike no", "дизлайк нет");
        E("👌", "ok perfect", "окей отлично");
        E("✌️", "victory peace", "победа мир");
        E("🤞", "fingers crossed luck", "скрестил пальцы удача");
        E("🤟", "love you gesture", "люблю жест");
        E("🤘", "rock horns metal", "рок коза");
        E("🤙", "call me shaka", "позвони шака");
        E("👈", "point left", "налево указываю");
        E("👉", "point right", "направо указываю");
        E("👆", "point up", "вверх указываю");
        E("👇", "point down", "вниз указываю");
        E("✋", "raised hand stop high five", "ладонь стоп");
        E("👋", "wave hello hi bye", "привет пока машу");
        E("👏", "clap applause bravo", "аплодисменты браво хлопаю");
        E("🙌", "raising hands hooray", "ура руки вверх");
        E("🙏", "pray please thanks", "молюсь пожалуйста спасибо");
        E("🤝", "handshake deal", "рукопожатие сделка");
        E("💪", "muscle strong flex", "сила мышцы бицепс");
        E("✍️", "writing hand", "пишу");
        E("👀", "eyes look watching", "глаза смотрю");
        E("🧠", "brain smart", "мозг умный");
        E("🫡", "salute", "честь салют");
        E("🤷", "shrug dunno", "не знаю пожимаю плечами");
        E("🤦", "facepalm", "фейспалм рукалицо");
        E("🙋", "raising hand me", "я поднимаю руку");
        E("🙅", "no gesture", "нет жест");
        E("💁", "tipping hand info", "информация");
        E("🏃", "running run", "бегу бег");
        E("🚶", "walking walk", "иду пешком");
        E("💃", "dancing dance", "танцую танец");
        E("🧑‍💻", "technologist coder developer", "программист айтишник");
        E("🎬", "clapper film movie cinema", "хлопушка кино фильм съёмка");
        E("🎥", "movie camera film", "кинокамера съёмка");
        E("📽️", "film projector", "кинопроектор");
        E("🎞️", "film frames", "плёнка кадры");
        E("📹", "video camera", "видеокамера");
        E("🎤", "microphone mic sing", "микрофон пою");
        E("🎧", "headphones music", "наушники музыка");
        E("🎵", "music note", "нота музыка");
        E("🎶", "music notes", "ноты музыка");
        E("🎸", "guitar", "гитара");
        E("🎹", "piano keyboard", "пианино клавиши");
        E("🥁", "drum", "барабан");
        E("🎨", "art palette paint", "искусство палитра краски");
        E("📸", "camera flash photo", "фото вспышка");
        E("📷", "camera photo", "фотоаппарат фото");
        E("🎭", "theater masks drama", "театр маски драма");
        E("🎮", "game controller gaming", "игра геймпад");
        E("🏆", "trophy win award", "кубок победа награда");
        E("🥇", "gold medal first", "золото первое место медаль");
        E("🎯", "target bullseye goal", "цель мишень");
        E("🎉", "tada party celebrate", "праздник ура конфетти");
        E("🎊", "confetti ball", "конфетти");
        E("🎁", "gift present", "подарок");
        E("🎂", "birthday cake", "торт день рождения");
        E("🎄", "christmas tree", "ёлка новый год");

        // Hearts and symbols
        E("❤️", "red heart love", "сердце любовь красное");
        E("🧡", "orange heart", "оранжевое сердце");
        E("💛", "yellow heart", "жёлтое сердце");
        E("💚", "green heart", "зелёное сердце");
        E("💙", "blue heart", "синее сердце");
        E("💜", "purple heart", "фиолетовое сердце");
        E("🖤", "black heart", "чёрное сердце");
        E("🤍", "white heart", "белое сердце");
        E("💔", "broken heart", "разбитое сердце");
        E("💕", "two hearts", "два сердца");
        E("💯", "hundred perfect", "сто процентов идеально");
        E("✅", "check done yes", "галочка готово да");
        E("☑️", "checkbox checked", "отмечено");
        E("✔️", "check mark", "галочка");
        E("❌", "cross no wrong", "крестик нет ошибка");
        E("❗", "exclamation important", "восклицательный важно");
        E("❓", "question", "вопрос");
        E("⚠️", "warning caution", "внимание предупреждение");
        E("🚫", "prohibited forbidden", "запрещено нельзя");
        E("⛔", "no entry stop", "стоп въезд запрещён");
        E("🔥", "fire hot lit", "огонь горячо жара");
        E("✨", "sparkles magic shine", "блёстки магия");
        E("⭐", "star", "звезда");
        E("🌟", "glowing star", "сияющая звезда");
        E("💫", "dizzy", "головокружение");
        E("⚡", "lightning zap power", "молния энергия");
        E("💥", "boom collision", "бум взрыв");
        E("💤", "zzz sleep", "сон храп");
        E("💬", "speech bubble chat", "сообщение чат");
        E("💡", "idea bulb", "идея лампочка");
        E("📌", "pushpin pin", "булавка закрепить");
        E("📍", "location pin", "метка место");
        E("🔔", "bell notification", "колокольчик уведомление");
        E("🔕", "bell off mute", "без звука");
        E("🔒", "lock locked", "замок закрыто");
        E("🔓", "unlock open", "открыто замок");
        E("🔑", "key", "ключ");
        E("🔗", "link chain", "ссылка цепь");
        E("♻️", "recycle", "переработка");
        E("➕", "plus add", "плюс");
        E("➖", "minus", "минус");
        E("➡️", "right arrow", "стрелка вправо");
        E("⬅️", "left arrow", "стрелка влево");
        E("⬆️", "up arrow", "стрелка вверх");
        E("⬇️", "down arrow", "стрелка вниз");
        E("🔄", "repeat refresh", "повтор обновить");
        E("🆗", "ok button", "окей");
        E("🆕", "new", "новое");
        E("🔴", "red circle rec record", "красный круг запись");
        E("🟠", "orange circle", "оранжевый круг");
        E("🟡", "yellow circle", "жёлтый круг");
        E("🟢", "green circle", "зелёный круг");
        E("🔵", "blue circle", "синий круг");
        E("⚫", "black circle", "чёрный круг");
        E("⚪", "white circle", "белый круг");

        // Objects and work
        E("💻", "laptop computer", "ноутбук компьютер");
        E("🖥️", "desktop computer monitor", "компьютер монитор");
        E("⌨️", "keyboard", "клавиатура");
        E("🖱️", "mouse computer", "мышь компьютерная");
        E("📱", "phone mobile", "телефон смартфон");
        E("☎️", "telephone", "телефон");
        E("🔋", "battery", "батарея");
        E("🔌", "plug electric", "вилка розетка");
        E("💾", "floppy save", "дискета сохранить");
        E("💿", "disc cd", "диск");
        E("📀", "dvd", "двд");
        E("🖨️", "printer print", "принтер печать");
        E("📁", "folder", "папка");
        E("📂", "open folder", "открытая папка");
        E("📄", "page document", "документ страница");
        E("📝", "memo note write", "заметка записка");
        E("📋", "clipboard", "буфер планшет");
        E("📎", "paperclip attach", "скрепка вложение");
        E("✂️", "scissors cut", "ножницы вырезать");
        E("📊", "bar chart stats", "график диаграмма");
        E("📈", "chart up growth", "рост график");
        E("📉", "chart down decline", "падение график");
        E("📅", "calendar date", "календарь дата");
        E("🗓️", "spiral calendar schedule", "расписание календарь");
        E("⏰", "alarm clock", "будильник");
        E("⏱️", "stopwatch timer", "секундомер таймер");
        E("⌛", "hourglass time", "песочные часы время");
        E("🕐", "clock one", "часы время");
        E("📧", "email mail", "почта имейл");
        E("✉️", "envelope letter", "конверт письмо");
        E("📦", "package box", "посылка коробка");
        E("🛒", "shopping cart", "корзина покупки");
        E("💰", "money bag", "деньги мешок");
        E("💵", "dollar money", "доллар деньги");
        E("💳", "credit card", "карта банковская");
        E("🔍", "search magnifying", "поиск лупа");
        E("🔧", "wrench tool fix", "ключ гаечный инструмент");
        E("🔨", "hammer", "молоток");
        E("🛠️", "tools", "инструменты");
        E("⚙️", "gear settings", "шестерёнка настройки");
        E("🧰", "toolbox", "ящик инструментов");
        E("🧲", "magnet", "магнит");
        E("🧪", "test tube experiment", "пробирка эксперимент");
        E("💊", "pill medicine", "таблетка лекарство");
        E("🗑️", "wastebasket trash", "мусор корзина");
        E("🚀", "rocket launch ship", "ракета запуск");
        E("✈️", "airplane flight", "самолёт полёт");
        E("🚗", "car", "машина авто");
        E("🚕", "taxi", "такси");
        E("🚲", "bicycle bike", "велосипед");
        E("🏠", "house home", "дом");
        E("🏢", "office building", "офис здание");
        E("🌍", "earth globe world", "земля мир глобус");
        E("🗺️", "map", "карта");

        // Nature, food, weather
        E("☀️", "sun sunny", "солнце солнечно");
        E("🌤️", "sun cloud", "переменная облачность");
        E("☁️", "cloud", "облако");
        E("🌧️", "rain", "дождь");
        E("⛈️", "storm thunder", "гроза");
        E("❄️", "snowflake cold", "снежинка холод");
        E("☃️", "snowman", "снеговик");
        E("🌈", "rainbow", "радуга");
        E("🌙", "moon night", "луна ночь");
        E("🌊", "wave sea", "волна море");
        E("🌸", "cherry blossom flower", "сакура цветок");
        E("🌹", "rose flower", "роза цветок");
        E("🌻", "sunflower", "подсолнух");
        E("🌲", "tree forest", "ёлка лес дерево");
        E("🍀", "four leaf clover luck", "клевер удача");
        E("🐶", "dog puppy", "собака пёс");
        E("🐱", "cat kitty", "кошка кот");
        E("🐭", "mouse", "мышь");
        E("🦊", "fox", "лиса");
        E("🐻", "bear", "медведь");
        E("🐼", "panda", "панда");
        E("🐸", "frog", "лягушка");
        E("🦁", "lion", "лев");
        E("🐧", "penguin", "пингвин");
        E("🦄", "unicorn", "единорог");
        E("🐝", "bee", "пчела");
        E("🦋", "butterfly", "бабочка");
        E("🍎", "apple red", "яблоко");
        E("🍌", "banana", "банан");
        E("🍓", "strawberry", "клубника");
        E("🍋", "lemon", "лимон");
        E("🥑", "avocado", "авокадо");
        E("🍕", "pizza", "пицца");
        E("🍔", "burger hamburger", "бургер");
        E("🍟", "fries", "картошка фри");
        E("🌮", "taco", "тако");
        E("🍣", "sushi", "суши");
        E("🍜", "noodles ramen", "лапша рамен");
        E("🍰", "cake", "торт пирожное");
        E("🍩", "donut", "пончик");
        E("🍪", "cookie", "печенье");
        E("🍫", "chocolate", "шоколад");
        E("☕", "coffee hot", "кофе");
        E("🍵", "tea", "чай");
        E("🍺", "beer", "пиво");
        E("🍷", "wine", "вино");
        E("🥂", "cheers champagne", "бокалы шампанское");
        E("🍾", "champagne bottle", "шампанское бутылка");
        E("🧊", "ice cube", "лёд");

        // Flags
        E("🏁", "chequered flag finish", "финиш флаг");
        E("🚩", "red flag", "красный флаг");
        E("🏳️", "white flag", "белый флаг");
        E("🇷🇺", "russia flag", "россия флаг");
        E("🇺🇸", "usa america flag", "сша америка флаг");
        E("🇬🇧", "uk britain flag", "британия флаг");
        E("🇩🇪", "germany flag", "германия флаг");
        E("🇫🇷", "france flag", "франция флаг");
        E("🇯🇵", "japan flag", "япония флаг");
        return list;
    }

    public static IEnumerable<(EmojiEntry Entry, int Score)> Search(string query, int limit = 24)
    {
        var q = TextNormalizer.Normalize(query);
        if (q.Length == 0) return Enumerable.Empty<(EmojiEntry, int)>();
        return All
            .Select(e => (Entry: e, Score: e.AllKeywords.Max(k => FuzzyMatcher.Score(q, k))))
            .Where(x => x.Score >= 600)
            .OrderByDescending(x => x.Score)
            .Take(limit);
    }
}
