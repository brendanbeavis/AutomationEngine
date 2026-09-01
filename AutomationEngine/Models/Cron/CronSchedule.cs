using Cronos;

namespace AutomationEngine.Models.Cron
{
    public sealed class CronSchedule
    {
        public CronScheduleType ScheduleType { get; set; } = CronScheduleType.EveryMinute;

        public int MinuteInterval { get; set; } = 5;

        public int HourInterval { get; set; } = 1;

        public int Hour { get; set; } = 0;

        public int Minute { get; set; } = 0;

        public HashSet<DayOfWeek> DaysOfWeek { get; set; } = [];

        public int DayOfMonth { get; set; } = 1;

        public string CustomExpression { get; set; } = "* * * * *";


        public static CronSchedule FromExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException(
                    "Cron expression cannot be empty.",
                    nameof(expression));

            expression = expression.Trim();

            // Let Cronos perform the actual validation.
            CronExpression.Parse(expression);

            var fields = expression
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 5)
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.Custom,
                    CustomExpression = expression
                };
            }

            var minute = fields[0];
            var hour = fields[1];
            var dayOfMonth = fields[2];
            var month = fields[3];
            var dayOfWeek = fields[4];


            // ---------------------------------------------------------
            // Every minute
            // * * * * *
            // ---------------------------------------------------------

            if (minute == "*" &&
                hour == "*" &&
                dayOfMonth == "*" &&
                month == "*" &&
                dayOfWeek == "*")
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.EveryMinute,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Every N minutes
            // */5 * * * *
            // ---------------------------------------------------------

            if (TryParseInterval(minute, out var minuteInterval) &&
                hour == "*" &&
                dayOfMonth == "*" &&
                month == "*" &&
                dayOfWeek == "*")
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.MinuteInterval,
                    MinuteInterval = minuteInterval,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Every hour
            // 0 * * * *
            // ---------------------------------------------------------

            if (IsInteger(minute, out var hourlyMinute) &&
                hour == "*" &&
                dayOfMonth == "*" &&
                month == "*" &&
                dayOfWeek == "*")
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.Hourly,
                    Minute = hourlyMinute,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Every N hours
            // 0 */2 * * *
            // ---------------------------------------------------------

            if (IsInteger(minute, out var intervalMinute) &&
                TryParseInterval(hour, out var hourInterval) &&
                dayOfMonth == "*" &&
                month == "*" &&
                dayOfWeek == "*")
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.HourlyInterval,
                    HourInterval = hourInterval,
                    Minute = intervalMinute,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Daily
            // 30 14 * * *
            // ---------------------------------------------------------

            if (IsInteger(minute, out var dailyMinute) &&
                IsInteger(hour, out var dailyHour) &&
                dayOfMonth == "*" &&
                month == "*" &&
                dayOfWeek == "*")
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.Daily,
                    Hour = dailyHour,
                    Minute = dailyMinute,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Weekly
            // 30 14 * * 1,3
            // ---------------------------------------------------------

            if (IsInteger(minute, out var weeklyMinute) &&
                IsInteger(hour, out var weeklyHour) &&
                dayOfMonth == "*" &&
                month == "*" &&
                TryParseDaysOfWeek(dayOfWeek, out var days))
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.Weekly,
                    Hour = weeklyHour,
                    Minute = weeklyMinute,
                    DaysOfWeek = days,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Monthly
            // 30 14 15 * *
            // ---------------------------------------------------------

            if (IsInteger(minute, out var monthlyMinute) &&
                IsInteger(hour, out var monthlyHour) &&
                IsInteger(dayOfMonth, out var monthlyDay) &&
                month == "*" &&
                dayOfWeek == "*")
            {
                return new CronSchedule
                {
                    ScheduleType = CronScheduleType.Monthly,
                    Hour = monthlyHour,
                    Minute = monthlyMinute,
                    DayOfMonth = monthlyDay,
                    CustomExpression = expression
                };
            }


            // ---------------------------------------------------------
            // Anything else
            // ---------------------------------------------------------

            return new CronSchedule
            {
                ScheduleType = CronScheduleType.Custom,
                CustomExpression = expression
            };
        }


        private static bool TryParseInterval(
            string value,
            out int interval)
        {
            interval = 0;

            if (!value.StartsWith("*/"))
                return false;

            return int.TryParse(
                value[2..],
                out interval);
        }


        private static bool IsInteger( string value, out int number)
        {
            return int.TryParse(value, out number);
        }


        private static bool TryParseDaysOfWeek(
            string value,
            out HashSet<DayOfWeek> days)
        {
            days = [];

            if (value == "*")
                return false;

            foreach (var part in value.Split(','))
            {
                if (!int.TryParse(part, out var number))
                    return false;

                if (number < 0 || number > 6)
                    return false;

                days.Add((DayOfWeek)number);
            }

            return days.Count > 0;
        }
    }
}
