using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services;

public class PsychologyTipsService : IPsychologyTipsService
{
    private static readonly Dictionary<string, List<PsychologicalTip>> TipsByState = new(StringComparer.OrdinalIgnoreCase)
    {
        ["focused"] =
        [
            new(
                Title: "Protect flow bandwidth",
                Objective: "Extend your current deep-work state without switching contexts.",
                WhyItWorks: "Attention residue drops performance after every task switch. Protecting one stream of work keeps cognitive load low.",
                ActionPrompt: "Run a focused sprint now.",
                IfThenPlan: "If I feel the urge to check messages, then I write it in a capture list and return to the task.",
                RecoveryStep: "If interrupted, spend 60 seconds restating the goal and next 2 actions before resuming.",
                ReflectionPrompt: "What single behavior protected my focus most today?",
                TimeWindow: "20-30 min",
                Steps:
                [
                    "Define one concrete output for this sprint.",
                    "Close non-essential tabs and silence notifications.",
                    "Set a visible timer and work until it ends.",
                    "Write the next action before taking a break."
                ]),
            new(
                Title: "Finish with a launchpad",
                Objective: "Make tomorrow's first action frictionless.",
                WhyItWorks: "Pre-deciding the next step lowers start resistance and protects consistency.",
                ActionPrompt: "Leave a clear start point before you stop.",
                IfThenPlan: "If I am about to close my laptop, then I write one line: 'Tomorrow I start by...'.",
                RecoveryStep: "If I forgot to plan, spend 2 minutes now creating a first action for tomorrow.",
                ReflectionPrompt: "Did my end-of-day note reduce startup friction today?",
                TimeWindow: "2-5 min",
                Steps:
                [
                    "Capture unfinished thoughts in one short note.",
                    "Define the exact first keystroke/task for tomorrow.",
                    "Keep required files or materials open/bookmarked."
                ])
        ],
        ["overwhelmed"] =
        [
            new(
                Title: "Reduce the decision surface",
                Objective: "Convert a chaotic day into a short, executable plan.",
                WhyItWorks: "Overwhelm is often unbounded scope. Narrowing choices reduces stress and improves follow-through.",
                ActionPrompt: "Choose one 15-minute win.",
                IfThenPlan: "If I feel scattered, then I pause and choose only one outcome for the next 15 minutes.",
                RecoveryStep: "If you stall, downsize the task until you can finish a first visible step in 3 minutes.",
                ReflectionPrompt: "Which task was noise, and which one actually moved the day forward?",
                TimeWindow: "10-15 min",
                Steps:
                [
                    "Brain-dump everything onto a scratch list.",
                    "Circle the top 3 outcomes for today.",
                    "Pick outcome #1 and define the next physical action.",
                    "Work one short sprint before re-evaluating."
                ]),
            new(
                Title: "Rapid nervous-system reset",
                Objective: "Lower stress activation so planning becomes possible again.",
                WhyItWorks: "Stress narrows attention and planning capacity. A short physiological reset restores control.",
                ActionPrompt: "Reset your breathing first.",
                IfThenPlan: "If my chest feels tight, then I do 90 seconds of slow exhale breathing before any task.",
                RecoveryStep: "If anxiety returns mid-task, repeat 3 breath cycles and continue with the smallest action.",
                ReflectionPrompt: "How quickly did my stress level shift after the reset?",
                TimeWindow: "2-3 min",
                Steps:
                [
                    "Inhale through nose for 4 seconds.",
                    "Exhale slowly for 6-8 seconds.",
                    "Repeat for 8-10 cycles.",
                    "Start one tiny execution step immediately."
                ])
        ],
        ["anxious"] =
        [
            new(
                Title: "Certainty anchor protocol",
                Objective: "Replace worry loops with one measurable completion target.",
                WhyItWorks: "Anxiety drops when uncertainty decreases. Clear done criteria create immediate cognitive relief.",
                ActionPrompt: "Define done in one sentence.",
                IfThenPlan: "If I start catastrophizing, then I ask: what is one thing I can finish in 10 minutes?",
                RecoveryStep: "If focus breaks, return to the sentence that defines 'done' and continue from there.",
                ReflectionPrompt: "Did defining done reduce my mental noise?",
                TimeWindow: "10 min",
                Steps:
                [
                    "Name one deliverable for the next block.",
                    "Write the success criteria in one sentence.",
                    "Do the first action that proves progress.",
                    "Log completion to create evidence of control."
                ]),
            new(
                Title: "Threat-to-plan reframe",
                Objective: "Convert abstract fear into specific planning actions.",
                WhyItWorks: "Your brain treats vague risk as larger than it is. Specific plans reduce perceived threat.",
                ActionPrompt: "Turn your fear into a checklist.",
                IfThenPlan: "If my mind says 'this will fail', then I write three prevention actions and start action #1.",
                RecoveryStep: "If stuck, ask: what would a calm coach tell me to do in the next 5 minutes?",
                ReflectionPrompt: "Which planned action made me feel safer and more capable?",
                TimeWindow: "5-10 min",
                Steps:
                [
                    "Write the worst-case concern in one line.",
                    "List three controllable mitigations.",
                    "Pick the easiest mitigation and execute now.",
                    "Re-check anxiety level after action."
                ])
        ],
        ["tired"] =
        [
            new(
                Title: "Low-energy execution mode",
                Objective: "Make meaningful progress without relying on high motivation.",
                WhyItWorks: "When energy is low, reducing cognitive complexity preserves consistency.",
                ActionPrompt: "Switch to checklist mode.",
                IfThenPlan: "If I feel sluggish, then I use a pre-written checklist instead of planning in my head.",
                RecoveryStep: "If pace drops, do a 90-second movement reset and restart with the smallest item.",
                ReflectionPrompt: "Which low-energy habit still moved my goals forward?",
                TimeWindow: "15-25 min",
                Steps:
                [
                    "Pick one high-leverage but simple habit.",
                    "Break it into tiny checklist steps.",
                    "Complete steps in order without re-planning.",
                    "Stop after one full checklist pass."
                ]),
            new(
                Title: "Body-first activation",
                Objective: "Raise alertness before cognitive work.",
                WhyItWorks: "Movement and hydration increase arousal and attention faster than willpower alone.",
                ActionPrompt: "Do a quick physical reset now.",
                IfThenPlan: "If I yawn repeatedly, then I stand, move, drink water, and restart.",
                RecoveryStep: "If fatigue persists, move one demanding task to a lighter maintenance task.",
                ReflectionPrompt: "How much better did I think after moving first?",
                TimeWindow: "3-5 min",
                Steps:
                [
                    "Stand and stretch or walk briskly.",
                    "Hydrate and improve posture.",
                    "Open only the task needed for the next 10 minutes."
                ])
        ],
        ["procrastinating"] =
        [
            new(
                Title: "Two-minute launch",
                Objective: "Beat avoidance by making starting easier than delaying.",
                WhyItWorks: "Behavior starts with low friction. Tiny starts often expand into longer execution.",
                ActionPrompt: "Start for two minutes only.",
                IfThenPlan: "If I say 'later', then I immediately do the first step for 2 minutes.",
                RecoveryStep: "If resistance spikes, cut scope in half and run a 5-minute timer.",
                ReflectionPrompt: "What made it easiest to start today?",
                TimeWindow: "2-10 min",
                Steps:
                [
                    "Open the exact file/tool needed.",
                    "Set a 2-minute timer.",
                    "Complete one visible micro-step.",
                    "Decide: continue for 8 more minutes or close intentionally."
                ]),
            new(
                Title: "Anti-perfection sprint",
                Objective: "Prioritize progress over polish.",
                WhyItWorks: "Perfectionism delays feedback. Fast imperfect drafts create momentum and clarity.",
                ActionPrompt: "Ship a rough first pass.",
                IfThenPlan: "If I start over-editing, then I switch to draft mode and finish the outline first.",
                RecoveryStep: "If overthinking returns, ask: what is the minimum acceptable version?",
                ReflectionPrompt: "What improved once I allowed a rough draft?",
                TimeWindow: "10-20 min",
                Steps:
                [
                    "Define minimum acceptable output.",
                    "Create a rough draft quickly.",
                    "Do one improvement pass only.",
                    "Mark it done and move to the next task."
                ])
        ],
        ["distracted"] =
        [
            new(
                Title: "Single-stream attention",
                Objective: "Lower distraction by reducing sensory and decision clutter.",
                WhyItWorks: "Every additional cue competes for working memory. Fewer choices preserve sustained attention.",
                ActionPrompt: "Run one-tab mode.",
                IfThenPlan: "If I open unrelated content, then I close it and restart a 15-minute focus block.",
                RecoveryStep: "If derailed for more than 2 minutes, stand, reset posture, and restart with one line objective.",
                ReflectionPrompt: "What trigger pulled me off-task most often?",
                TimeWindow: "15-20 min",
                Steps:
                [
                    "Keep one work tab/window visible.",
                    "Put phone out of reach.",
                    "Use a timer for one uninterrupted block.",
                    "Capture distractions in a list for later."
                ]),
            new(
                Title: "Interruption firewall",
                Objective: "Create friction before impulsive context switching.",
                WhyItWorks: "A short pause interrupts automatic behavior and restores deliberate control.",
                ActionPrompt: "Use the 60-second pause rule.",
                IfThenPlan: "If I want to check social/email, then I wait 60 seconds and decide again.",
                RecoveryStep: "If you do switch, set a 2-minute cap and return immediately.",
                ReflectionPrompt: "How many distractions were prevented by the pause rule?",
                TimeWindow: "All day",
                Steps:
                [
                    "Notice the urge to switch.",
                    "Breathe slowly for 60 seconds.",
                    "Choose deliberately: continue current task or schedule switch later."
                ])
        ],
        ["low_mood"] =
        [
            new(
                Title: "Tiny-win momentum loop",
                Objective: "Rebuild agency with small completions when motivation is low.",
                WhyItWorks: "Completion creates evidence of control, which can lift mood and self-trust.",
                ActionPrompt: "Finish one 5-minute habit now.",
                IfThenPlan: "If I feel flat, then I complete the smallest useful habit immediately.",
                RecoveryStep: "If mood drops again, repeat one tiny completion before judging the day.",
                ReflectionPrompt: "Which small action improved my state the most?",
                TimeWindow: "5 min",
                Steps:
                [
                    "Pick the easiest high-value habit.",
                    "Set a 5-minute timer.",
                    "Complete the habit fully.",
                    "Log it to reinforce self-trust."
                ]),
            new(
                Title: "Environment state shift",
                Objective: "Use external cues to influence internal energy.",
                WhyItWorks: "Mood is state-dependent. Light, posture, and motion can quickly change emotional baseline.",
                ActionPrompt: "Change one physical variable right now.",
                IfThenPlan: "If I feel stuck, then I change lighting/position and restart a short task block.",
                RecoveryStep: "If you still feel low, switch to maintenance tasks and protect minimum streaks.",
                ReflectionPrompt: "What environment change had the strongest effect?",
                TimeWindow: "2-4 min",
                Steps:
                [
                    "Adjust lighting or move near daylight.",
                    "Stand or walk for 2 minutes.",
                    "Start a short, low-friction task immediately."
                ])
        ]
    };

    public PsychologyTipsSnapshot GetTips(string? state)
    {
        var normalized = NormalizeState(state);
        var tips = TipsByState.TryGetValue(normalized, out var values)
            ? values
            : TipsByState["focused"];

        return new PsychologyTipsSnapshot(
            SelectedState: normalized,
            IntroLine: IntroForState(normalized),
            AvailableStates: TipsByState.Keys.OrderBy(k => k).ToList(),
            Tips: tips);
    }

    private static string NormalizeState(string? state)
    {
        var value = string.IsNullOrWhiteSpace(state)
            ? "focused"
            : state.Trim().ToLowerInvariant();

        return TipsByState.ContainsKey(value) ? value : "focused";
    }

    private static string IntroForState(string state)
    {
        return state switch
        {
            "overwhelmed" => "Use this stabilization protocol: narrow scope, reduce stress activation, then execute one short win.",
            "anxious" => "Follow an anxiety-to-action loop: regulate body state, define certainty, then gather progress evidence.",
            "tired" => "Switch to low-energy mode: simplified checklists, short work blocks, and body-first activation.",
            "procrastinating" => "Use anti-avoidance mechanics: tiny starts, visible timers, and minimum viable progress.",
            "distracted" => "Run an attention defense: one stream, interruption friction, and fast reset scripts.",
            "low_mood" => "Rebuild agency with tiny wins and environmental state shifts before expecting motivation.",
            _ => "You are in a strong state. Preserve momentum with flow protection and clear next-action planning."
        };
    }
}
