﻿namespace theatrel.Common
{
    public class CommonTags
    {
        public const string NotDefinedTag = "Not defined";
        public const string WasMovedTag = "WasMoved";
        public const string NoTicketsTag = "NoTickets";

        public const string BuyTicket = "Купить билет";
        public const string NoTickets = "Билетов нет";
        public const string WasMoved = "Переносится";

        public const string CastWillBeAddedLater = "Cостав исполнителей будет объявлен позднее";
        public const string WillDeclaredLater = "будет объявлено позднее";

        public const string Conductor = "Дирижер";
        public const string Actor = "Исполнитель";
        public const string Phonogram = "Исполняется под фонограмму";

        public const string PerformanceDuration = "Продолжительность спектакля";
        public const string GapsPhraseStart = "Спектакль идет с";
        public const string EmTag = "<em>";

        public static readonly string[] TechnicalTagsInCastList = { PerformanceDuration, GapsPhraseStart, EmTag};
        public static readonly string[] TechnicalStateTags = {NotDefinedTag, WasMovedTag, NoTicketsTag};

        public const string JavascriptVoid = "javascript:void(0)";
    }
}
