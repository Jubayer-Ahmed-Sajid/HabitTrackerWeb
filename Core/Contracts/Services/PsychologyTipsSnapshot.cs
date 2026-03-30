namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record PsychologicalTip(
    string Title,
    string Objective,
    string WhyItWorks,
    string ActionPrompt,
    string IfThenPlan,
    string RecoveryStep,
    string ReflectionPrompt,
    string TimeWindow,
    IReadOnlyList<string> Steps);

public sealed record PsychologyTipsSnapshot(
    string SelectedState,
    string IntroLine,
    IReadOnlyList<string> AvailableStates,
    IReadOnlyList<PsychologicalTip> Tips);
