using Cronos;
using System.Globalization;

namespace AutomationEngine.Models.Cron
{
   
    public static class CronExpressionGenerator
    {
        public static string Generate(CronSchedule schedule)
        {
            ArgumentNullException.ThrowIfNull(schedule);

            string result = schedule.ScheduleType switch
            {
                CronScheduleType.EveryMinute =>
                    "* * * * *",

                CronScheduleType.MinuteInterval =>
                    $"*/{ValidateRange(schedule.MinuteInterval, 1, 59, nameof(schedule.MinuteInterval))} * * * *",

                CronScheduleType.Hourly =>
                    $"{ValidateMinute(schedule.Minute)} * * * *",

                CronScheduleType.HourlyInterval =>
                    $"{ValidateMinute(schedule.Minute)} */{ValidateRange(schedule.HourInterval, 1, 23, nameof(schedule.HourInterval))} * * *",

                CronScheduleType.Daily =>
                    $"{ValidateMinute(schedule.Minute)} {ValidateHour(schedule.Hour)} * * *",

                CronScheduleType.Weekly =>
                    GenerateWeekly(schedule),

                CronScheduleType.Monthly =>
                    $"{ValidateMinute(schedule.Minute)} {ValidateHour(schedule.Hour)} {ValidateRange(schedule.DayOfMonth, 1, 31, nameof(schedule.DayOfMonth))} * *",

                CronScheduleType.Custom =>
                    schedule.CustomExpression?.Trim() ?? string.Empty,

                _ => throw new ArgumentOutOfRangeException()
            };

            if (IsValidCron(result))
            {
                return result;
            }

            Console.WriteLine("Cron expression is invalid, generated expression: " + result);
            return string.Empty;
        }


        private static bool IsValidCron(string expression)
        {
            try
            {
                CronExpression.Parse(expression);
                return true;
            }
            catch (CronFormatException)
            {
                return false;
            }
        }

        private static string GenerateWeekly(CronSchedule schedule)
        {
            if (schedule.DaysOfWeek.Count == 0)
                throw new ArgumentException(
                    "At least one day of the week must be selected.");

            var days = schedule.DaysOfWeek
                .OrderBy(GetCronDayNumber)
                .Select(d => GetCronDayNumber(d).ToString(CultureInfo.InvariantCulture));

            return $"{ValidateMinute(schedule.Minute)} " +
                   $"{ValidateHour(schedule.Hour)} * * {string.Join(",", days)}";
        }

        private static int GetCronDayNumber(DayOfWeek day)
        {
            // Cron convention:
            // Sunday = 0
            // Monday = 1
            // ...
            // Saturday = 6

            return (int)day;
        }

        private static int ValidateMinute(int value)
        {
            return ValidateRange(value, 0, 59, nameof(value));
        }

        private static int ValidateHour(int value)
        {
            return ValidateRange(value, 0, 23, nameof(value));
        }

        private static int ValidateRange(
            int value,
            int min,
            int max,
            string name)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    $"Value must be between {min} and {max}.");
            }

            return value;
        }
    }
}
