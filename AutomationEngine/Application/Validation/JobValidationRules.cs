namespace AutomationEngine.Application.Validation;

public static class JobValidationRules
{
    public const int JobIdMinLength = 1;
    public const int JobIdMaxLength = 20;
    public const int DisplayNameMinLength = 1;
    public const int DisplayNameMaxLength = 200;
    public const int CommandMaxLength = 500;
    public const int ArgumentsMaxLength = 500;
    public const int WorkingDirectoryMaxLength = 200;
    public const int ScheduleMaxLength = 50;
    public const int TimeoutMinSeconds = 0;
    public const int TimeoutMaxSeconds = 86400;
    public const int RetryMin = 0;
    public const int RetryMax = 100;
    public const int SuccessExitCodesMaxLength = 100;
    public const int TargetFolderMaxLength = 500;
    public const int FileFilterMaxLength = 100;
    public const int FileAgeMinDays = 0;
    public const int FileAgeMaxDays = 36500;
}
