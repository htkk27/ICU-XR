using System;
using System.Collections.Generic;
using UnityEngine;

// Εδώ είναι όλα τα class/μοντέλα που κάνουν map το JSON αρχείο του σεναρίου σε C# objects

[Serializable]
public class ScenarioData
{
    public string schema_version;
    public ScenarioMeta scenario_meta;
    public InitialState initial_state;
    public List<Hotspot> hotspots;
    public EHRConfig ehr_config;
    public Rules rules;
    public List<Node> nodes;
    public LoggingConfig logging;
}

[Serializable]
public class ScenarioMeta
{
    public string id;
    public string title;
    public string description;
    public int estimated_duration_minutes;
    public string difficulty;
    public List<string> learning_goals;
}

[Serializable]
public class InitialState
{
    public int time_elapsed;
    public int current_score;
    public Flags flags;
    public Vitals vitals;
    public UIState ui;
}

[Serializable]
public class Flags
{
    public bool assessment_complete;
    public bool oxygen_adjusted;
    public bool documentation_1_complete;
    public bool escalation_complete;
    public bool documentation_2_complete;
}

[Serializable]
public class Vitals
{
    public int hr;      // Καρδιακός Παλμός
    public int spo2;    // Oxygen Saturation
    public int rr;      // Respiratory Rate
    public string bp;   // Πίεση Αίματος
    public float temp;  // Θερμοκρασία
}

[Serializable]
public class UIState
{
    public List<string> active_hotspots;
    public bool monitor_alert;
}

[Serializable]
public class Hotspot
{
    public string id;
    public string label;
}

[Serializable]
public class EHRConfig
{
    public Forms forms;
}

[Serializable]
public class Forms
{
    public FormDefinition assessment_form;
    public FormDefinition intervention_form;
    public FormDefinition communication_log;
}

[Serializable]
public class FormDefinition
{
    public string title;
    public List<string> fields;
}

[Serializable]
public class Rules
{
    public List<GlobalRule> global_rules;
}

[Serializable]
public class GlobalRule
{
    public string id;
    public RuleCondition condition;
    public List<Effect> effects;
}

[Serializable]
public class RuleCondition
{
    // Δυναμική αξιολόγηση συνθήκης 
    public Dictionary<string, object> conditions;
}

[Serializable]
public class Node
{
    public string id;
    public string type; // "message", "decision", "gate", "end"
    public string text;
    public string description;
    public List<Option> options;
    public string next_node_id;
    public TimeoutConfig timeout;
    public GateRequirements gate_requirements;
    public string feedback_blocked;
    public string feedback_success;
    public EffectsConfig effects_on_pass;
    public DebriefConfig debrief_config;
}

[Serializable]
public class Option
{
    public string id;
    public string label;
    public string target_hotspot;
    public EffectsConfig effects;
    public string next_node_id;
}

[Serializable]
public class TimeoutConfig
{
    public int seconds;
    public EffectsConfig on_timeout_effects;
    public string next_node_id;
}

[Serializable]
public class GateRequirements
{
    public string target_hotspot;
    public List<RequiredForm> required_forms;
}

[Serializable]
public class RequiredForm
{
    public string form_id;
    public List<string> fields;
}

[Serializable]
public class EffectsConfig
{
    public int score_delta;
    public Dictionary<string, object> state_update;
    public Dictionary<string, object> vitals_update;
    public string toast;
}

[Serializable]
public class Effect
{
    public string type; // "ui_visual", "ui_toast", "vitals_update", "state_update"
    public string target;
    public string state;
    public string style;
    public string message;
    public Dictionary<string, object> data;
}

[Serializable]
public class DebriefConfig
{
    public bool show_score;
    public bool show_decision_path;
    public bool highlight_missed_docs;
    public bool export_log;
}

[Serializable]
public class LoggingConfig
{
    public bool enabled;
    public List<string> log_events;
}