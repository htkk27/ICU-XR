using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Η κύρια μηχανή του σεναρίου. Τρέχει τα nodes, τις αποφάσεις, τα timeouts και τα gates (μπλόκα)
public class DecisionTreeEngine : MonoBehaviour
{
    public static DecisionTreeEngine Instance { get; private set; }

    [Header("Current Execution State")]
    public Node currentNode;
    public bool isWaitingForInput = false;
    public bool isGateBlocked = false;

    // Events
    public event Action<Node> OnNodeEntered;
    public event Action<Option> OnOptionSelected;
    public event Action<string> OnToastMessage;
    public event Action OnScenarioCompleted;

    private Coroutine timeoutCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Ξεκινάει το σενάριο
    public void StartScenario()
    {
        if (ScenarioManager.Instance?.currentScenario == null)
        {
            Debug.LogError("No scenario loaded!");
            return;
        }

        Node startNode = ScenarioManager.Instance.GetStartNode();
        if (startNode != null)
        {
            EnterNode(startNode);
        }
    }

    // Μπαίνει σε ένα νέο βήμα (node) του σεναρίου
    public void EnterNode(Node node)
    {
        if (node == null) return;

        currentNode = node;
        isWaitingForInput = false;
        isGateBlocked = false;

        // Κρατάμε log
        LoggingManager.Instance?.LogEvent("NODE_ENTER", new Dictionary<string, object>
        {
            { "node_id", node.id },
            { "node_type", node.type },
            { "node_text", node.text }
        });

        Debug.Log($"Entered node: {node.id} ({node.type})");
        OnNodeEntered?.Invoke(node);

        // Τρέχουμε τους κανόνες του παιχνιδιού
        CheckGlobalRules();

        // Κοιτάμε τι είδους node είναι
        switch (node.type)
        {
            case "message":
                HandleMessageNode(node);
                break;
            case "decision":
                HandleDecisionNode(node);
                break;
            case "gate":
                HandleGateNode(node);
                break;
            case "end":
                HandleEndNode(node);
                break;
            default:
                Debug.LogError($"Unknown node type: {node.type}");
                break;
        }
    }

    // Διαχειρίζεται τα απλά μηνύματα
    private void HandleMessageNode(Node node)
    {
        // Πάει στο επόμενο αυτόματα μετά από 2 δευτερόλεπτα
        if (!string.IsNullOrEmpty(node.next_node_id))
        {
            StartCoroutine(AutoProgressToNextNode(node.next_node_id, 2f));
        }
    }

    // Διαχειρίζεται τα βήματα που θέλουν επιλογή (decision)
    private void HandleDecisionNode(Node node)
    {
        isWaitingForInput = true;

        // Αν υπάρχει χρονόμετρο, το ξεκινάμε
        if (node.timeout != null && node.timeout.seconds > 0)
        {
            if (timeoutCoroutine != null)
            {
                StopCoroutine(timeoutCoroutine);
            }
            timeoutCoroutine = StartCoroutine(HandleTimeout(node.timeout));
        }
    }

    // Διαχειρίζεται τα gates (εκεί που μπλοκάρει το σενάριο μέχρι να κάνουμε κάτι)
    private void HandleGateNode(Node node)
    {
        isGateBlocked = true;
        isWaitingForInput = true;

        // Εδώ περιμένουμε από το EHR system να μας πει ότι έγιναν οι φόρμες και μετά θα καλέσει το PassGate()
    }

    // Κλείνει το σενάριο
    private void HandleEndNode(Node node)
    {
        Debug.Log("Scenario completed!");
        OnScenarioCompleted?.Invoke();
        
        // Πετάει την τελική οθόνη
        if (node.debrief_config != null)
        {
            DebriefingManager.Instance?.ShowDebriefing(node.debrief_config);
        }
    }

    // Το trigger όταν ο ασθενής πεθαίνει - τελειώνει το σενάριο απευθείας
    public void TriggerPatientDeathEnd()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        isWaitingForInput = false;
        isGateBlocked = false;

        Node deathNode = new Node
        {
            id = "patient_death",
            type = "end",
            text = "💀 Ο ασθενής δεν επιβίωσε. Τα ζωτικά σημεία έφτασαν σε μη αναστρέψιμο επίπεδο.",
            options = new List<Option>(),
            debrief_config = new DebriefConfig
            {
                show_score = true,
                show_decision_path = true,
                highlight_missed_docs = true,
                export_log = true
            }
        };

        currentNode = deathNode;

        LoggingManager.Instance?.LogEvent("NODE_ENTER", new Dictionary<string, object>
        {
            { "node_id", deathNode.id },
            { "node_type", deathNode.type },
            { "node_text", deathNode.text }
        });

        ShowToast("💀 Ο ασθενής κατέρρευσε. Το σενάριο τερματίστηκε.", "danger");

        OnNodeEntered?.Invoke(deathNode);
        HandleEndNode(deathNode);
    }

    // Τρέχει όταν ο παίκτης κάνει κλικ σε μια επιλογή
    public void SelectOption(Option option)
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.patientDied)
        {
            return;
        }

        if (!isWaitingForInput || isGateBlocked)
        {
            Debug.LogWarning("Not waiting for input or gate is blocked!");
            return;
        }

        // Σταματάμε το χρονόμετρο
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        Debug.Log($"Option selected: {option.id}");
        OnOptionSelected?.Invoke(option);

        // Κρατάμε log για το κλικ
        LoggingManager.Instance?.LogEvent("OPTION_SELECTED", new Dictionary<string, object>
        {
            { "option_id", option.id },
            { "option_label", option.label },
            { "node_id", currentNode.id }
        });

        // Κρατάμε log αν η επιλογή ήταν συνδεδεμένη με κάποιο hotspot
        if (!string.IsNullOrEmpty(option.target_hotspot))
        {
            LoggingManager.Instance?.LogEvent("HOTSPOT_INTERACTION", new Dictionary<string, object>
            {
                { "hotspot_id", option.target_hotspot },
                { "option_id", option.id }
            });
        }

        // Ρίχνουμε τα effects (πχ αλλάζουμε σκορ ή vitals)
        if (option.effects != null)
        {
            ApplyEffects(option.effects);
        }

        // Πάμε στο επόμενο βήμα
        if (!string.IsNullOrEmpty(option.next_node_id))
        {
            Node nextNode = ScenarioManager.Instance.GetNode(option.next_node_id);
            if (nextNode != null)
            {
                EnterNode(nextNode);
            }
        }
    }

    // Ξεκλειδώνει το gate (πχ όταν συμπληρωθούν σωστά τα έγγραφα)
    public void PassGate()
    {
        if (!isGateBlocked)
        {
            Debug.LogWarning("No gate is currently active!");
            return;
        }

        isGateBlocked = false;

        // Ρίχνουμε τυχόν effects που έχει το gate όταν περνάει
        if (currentNode.effects_on_pass != null)
        {
            ApplyEffects(currentNode.effects_on_pass);
        }

        // Δείχνουμε το μήνυμα επιτυχίας
        if (!string.IsNullOrEmpty(currentNode.feedback_success))
        {
            ShowToast(currentNode.feedback_success, "success");
        }

        // Και πάμε στο επόμενο
        if (!string.IsNullOrEmpty(currentNode.next_node_id))
        {
            Node nextNode = ScenarioManager.Instance.GetNode(currentNode.next_node_id);
            if (nextNode != null)
            {
                EnterNode(nextNode);
            }
        }
    }

    // Εφαρμόζει τις συνέπειες (effects) στο game state
    private void ApplyEffects(EffectsConfig effects)
    {
        if (effects == null) return;

        // Σκορ
        if (effects.score_delta != 0)
        {
            GameStateManager.Instance?.UpdateScore(effects.score_delta);
        }

        // Αλλαγή σε flags
        if (effects.state_update != null && effects.state_update.Count > 0)
        {
            GameStateManager.Instance?.UpdateFlags(effects.state_update);
        }

        // Αλλαγή στα ζωτικά
        if (effects.vitals_update != null && effects.vitals_update.Count > 0)
        {
            GameStateManager.Instance?.UpdateVitals(effects.vitals_update);
            
            // Logάρουμε την αλλαγή
            LoggingManager.Instance?.LogEvent("VITALS_CHANGE", effects.vitals_update);
        }

        // Ειδοποίηση
        if (!string.IsNullOrEmpty(effects.toast))
        {
            ShowToast(effects.toast, "info");
        }
    }

    private void ShowToast(string message, string style)
    {
        OnToastMessage?.Invoke(message);
        UIManager.Instance?.ShowToast(message, style);
        ScenarioDebugOverlay.Instance?.AddToast(message, style);
    }

    // Τσεκάρει τους γενικούς κανόνες (πχ αν έπεσε το οξυγόνο)
    private void CheckGlobalRules()
    {
        if (ScenarioManager.Instance?.currentScenario?.rules?.global_rules == null)
            return;

        foreach (var rule in ScenarioManager.Instance.currentScenario.rules.global_rules)
        {
            if (EvaluateRuleCondition(rule.condition))
            {
                Debug.Log($"Global rule triggered: {rule.id}");
                ApplyRuleEffects(rule.effects);
            }
        }
    }

    // Βλέπει αν ισχύει μια συνθήκη (πχ SpO2 < 90)
    private bool EvaluateRuleCondition(RuleCondition condition)
    {
        if (condition == null || condition.conditions == null)
            return false;

        // Απλή υλοποίηση που τσεκάρει τα vitals
        foreach (var kvp in condition.conditions)
        {
            string key = kvp.Key;
            
            // Parse το string
            if (key.StartsWith("vitals."))
            {
                string vitalName = key.Substring(7); // Διώχνουμε το "vitals."
                object vitalValue = GameStateManager.Instance?.GetVitalValue(vitalName);
                
                if (vitalValue == null) continue;

                // Τσεκάρουμε τους operators (πχ { "lt": 90 })
                if (kvp.Value is Dictionary<string, object> condDict)
                {
                    foreach (var condKvp in condDict)
                    {
                        string op = condKvp.Key;
                        int threshold = Convert.ToInt32(condKvp.Value);
                        int currentValue = Convert.ToInt32(vitalValue);

                        switch (op)
                        {
                            case "lt":
                                return currentValue < threshold;
                            case "gt":
                                return currentValue > threshold;
                            case "eq":
                                return currentValue == threshold;
                            case "lte":
                                return currentValue <= threshold;
                            case "gte":
                                return currentValue >= threshold;
                        }
                    }
                }
            }
        }

        return false;
    }

    // Τι γίνεται όταν πιάσει ο κανόνας
    private void ApplyRuleEffects(List<Effect> effects)
    {
        if (effects == null) return;

        foreach (var effect in effects)
        {
            switch (effect.type)
            {
                case "ui_visual":
                    UIManager.Instance?.ApplyVisualEffect(effect.target, effect.state);
                    break;
                case "ui_toast":
                    ShowToast(effect.message, effect.style);
                    break;
            }
        }
    }

    // Τι γίνεται αν ο παίκτης αργήσει να απαντήσει 
    private IEnumerator HandleTimeout(TimeoutConfig timeout)
    {
        yield return new WaitForSeconds(timeout.seconds);

        // Αν πέθανε ο παίκτης στο ενδιάμεσο, σταματάμε
        if (GameStateManager.Instance != null && GameStateManager.Instance.patientDied)
        {
            timeoutCoroutine = null;
            yield break;
        }

        Debug.Log("Timeout triggered!");
        
        // Ρίχνουμε τα effects της καθυστέρησης
        if (timeout.on_timeout_effects != null)
        {
            ApplyEffects(timeout.on_timeout_effects);
        }

        // Πάμε στο επόμενο node
        if (!string.IsNullOrEmpty(timeout.next_node_id))
        {
            Node nextNode = ScenarioManager.Instance.GetNode(timeout.next_node_id);
            if (nextNode != null)
            {
                EnterNode(nextNode);
            }
        }

        timeoutCoroutine = null;
    }

    // Πάει στο επόμενο node μόνο του μετά από λίγο
    private IEnumerator AutoProgressToNextNode(string nextNodeId, float delay)
    {
        yield return new WaitForSeconds(delay);

        Node nextNode = ScenarioManager.Instance.GetNode(nextNodeId);
        if (nextNode != null)
        {
            EnterNode(nextNode);
        }
    }
}