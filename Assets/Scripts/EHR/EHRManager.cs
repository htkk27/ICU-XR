using System;
using System.Collections.Generic;
using UnityEngine;

// Κεντρικός manager για τους ιατρικούς φακέλους. Κρατάει δεδομένα, φόρμες και τσεκάρει τα gates του σεναρίου
public class EHRManager : MonoBehaviour
{
    public static EHRManager Instance { get; private set; }

    [Header("EHR Data")]
    public Dictionary<string, FormData> completedForms = new Dictionary<string, FormData>();
    public Dictionary<string, Dictionary<string, string>> formFieldValues = new Dictionary<string, Dictionary<string, string>>();

    [Header("Current State")]
    public bool ehrPanelOpen = false;
    public string currentFormId = null;

    // Events 
    public event Action<string> OnFormOpened;
    public event Action<string, FormData> OnFormSubmitted;
    public event Action OnEHRPanelOpened;
    public event Action OnEHRPanelClosed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Μένει ενεργό σε όλο το game
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Εμφανίζει το EHR panel στο UI
    public void ShowEHRPanel()
    {
        ehrPanelOpen = true;
        OnEHRPanelOpened?.Invoke();
        UIManager.Instance?.ShowEHRUI();
    }

    // Κρύβει το EHR panel
    public void HideEHRPanel()
    {
        ehrPanelOpen = false;
        OnEHRPanelClosed?.Invoke();
        UIManager.Instance?.HideEHRUI();
    }

    // Ανοίγει μια φόρμα βάσει ID
    public void OpenForm(string formId)
    {
        currentFormId = formId;
        FormDefinition formDef = ScenarioManager.Instance?.GetFormDefinition(formId);
        
        if (formDef == null)
        {
            Debug.LogError($"Form definition not found: {formId}");
            return;
        }

        // Φτιάχνει κενά fields αν δεν υπάρχουν ήδη 
        if (!formFieldValues.ContainsKey(formId))
        {
            formFieldValues[formId] = new Dictionary<string, string>();
            foreach (var field in formDef.fields)
            {
                formFieldValues[formId][field] = "";
            }
        }

        OnFormOpened?.Invoke(formId);
        Debug.Log($"Opened form: {formId}");
    }

    // Ενημερώνει μια τιμή σε ένα πεδίο
    public void UpdateFieldValue(string formId, string fieldName, string value)
    {
        if (!formFieldValues.ContainsKey(formId))
        {
            formFieldValues[formId] = new Dictionary<string, string>();
        }

        formFieldValues[formId][fieldName] = value;
        Debug.Log($"Field updated: {formId}.{fieldName} = {value}");
    }

    // Υποβολή φόρμας - τσεκάρει και validation 
    public bool SubmitForm(string formId)
    {
        FormDefinition formDef = ScenarioManager.Instance?.GetFormDefinition(formId);
        if (formDef == null)
        {
            Debug.LogError($"Form definition not found: {formId}");
            return false;
        }

        List<string> fieldsToValidate = GetRequiredFieldsForCurrentGate(formId);
        if (fieldsToValidate == null || fieldsToValidate.Count == 0)
            fieldsToValidate = formDef.fields;

        if (!ValidateForm(formId, fieldsToValidate))
        {
            UIManager.Instance?.ShowToast("⚠️ Παρακαλώ συμπληρώστε τα απαιτούμενα πεδία", "warning");
            ScenarioDebugOverlay.Instance?.AddToast("⚠️ Παρακαλώ συμπληρώστε τα απαιτούμενα πεδία", "warning");
            return false;
        }

        // Φτιάχνει το τελικό object με τα δεδομένα
        FormData formData = new FormData
        {
            formId = formId,
            formTitle = formDef.title,
            submissionTime = DateTime.Now,
            fieldValues = new Dictionary<string, string>(formFieldValues[formId])
        };

        // Αποθηκεύει τη φόρμα 
        completedForms[formId] = formData;

        // Logging
        LoggingManager.Instance?.LogEvent("EHR_SUBMIT", new Dictionary<string, object>
        {
            { "form_id", formId },
            { "form_title", formDef.title },
            { "fields", formData.fieldValues }
        });

        OnFormSubmitted?.Invoke(formId, formData);
        UIManager.Instance?.ShowToast("✅ Η φόρμα αποθηκεύτηκε επιτυχώς", "success");
        ScenarioDebugOverlay.Instance?.AddToast("✅ Η φόρμα αποθηκεύτηκε", "success");
        
        Debug.Log($"Form submitted: {formId}");

        // Τσεκάρει μήπως αυτή η φόρμα ξεκλειδώνει το τρέχον gate
        CheckGateRequirements();

        return true;
    }

    // Βασικό validation - τσεκάρει ότι δεν είναι άδεια τα required fields
    private bool ValidateForm(string formId, List<string> requiredFields)
    {
        if (!formFieldValues.ContainsKey(formId)) return false;

        foreach (var field in requiredFields)
        {
            if (!formFieldValues[formId].ContainsKey(field) || 
                string.IsNullOrWhiteSpace(formFieldValues[formId][field]))
            {
                return false;
            }
        }
        return true;
    }

    // Παίρνει τα fields που ζητάει το τρέχον gate
    private List<string> GetRequiredFieldsForCurrentGate(string formId)
    {
        if (DecisionTreeEngine.Instance?.currentNode?.type != "gate")
            return null;

        var reqs = DecisionTreeEngine.Instance.currentNode.gate_requirements;
        if (reqs?.required_forms == null)
            return null;

        foreach (var rf in reqs.required_forms)
        {
            if (rf.form_id == formId) return rf.fields;
        }
        return null;
    }

    // Τσεκάρει αν έχουμε περάσει το gate στο decision tree
    private void CheckGateRequirements()
    {
        if (DecisionTreeEngine.Instance?.currentNode?.type == "gate" &&
            DecisionTreeEngine.Instance.isGateBlocked)
        {
            Node gateNode = DecisionTreeEngine.Instance.currentNode;
            
            if (CheckGateRequirementsMet(gateNode.gate_requirements))
            {
                DecisionTreeEngine.Instance.PassGate();
            }
        }
    }

    // Helper για να δούμε αν ικανοποιούνται τα requirements του gate γενικά
    public bool CheckGateRequirementsMet(GateRequirements requirements)
    {
        if (requirements == null || requirements.required_forms == null)
            return true;

        foreach (var requiredForm in requirements.required_forms)
        {
            // Έλεγχος αν η φόρμα έχει συμπληρωθεί καθόλου
            if (!completedForms.ContainsKey(requiredForm.form_id))
                return false;

            // Έλεγχος για όλα τα πεδία
            FormData formData = completedForms[requiredForm.form_id];
            foreach (var field in requiredForm.fields)
            {
                if (!formData.fieldValues.ContainsKey(field) || 
                    string.IsNullOrWhiteSpace(formData.fieldValues[field]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Επιστρέφει αν μια φόρμα έχει συμπληρωθεί
    public bool IsFormCompleted(string formId)
    {
        return completedForms.ContainsKey(formId);
    }

    // Επιστρέφει τα δεδομένα 
    public FormData GetFormData(string formId)
    {
        return completedForms.ContainsKey(formId) ? completedForms[formId] : null;
    }

    // Κάνει reset (πχ σε restart σεναρίου)
    public void ClearAllForms()
    {
        completedForms.Clear();
        formFieldValues.Clear();
        currentFormId = null;
    }

    // Εξάγει τα data σε JSON
    public string ExportFormsToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(completedForms, Newtonsoft.Json.Formatting.Indented);
    }
}

// Struct που κρατάει τα δεδομένα μιας φόρμας
[Serializable]
public class FormData
{
    public string formId;
    public string formTitle;
    public DateTime submissionTime;
    public Dictionary<string, string> fieldValues;
}