using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

// Φορτώνει τα JSON αρχεία των σεναρίων και τα κάνει C# objects για να τα παίξουμε
public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [Header("Current Scenario")]
    public ScenarioData currentScenario;
    private Dictionary<string, Node> nodeDict = new Dictionary<string, Node>();

    [Header("Available Scenarios")]
    public List<string> availableScenarios = new List<string>();

    // Events
    public event Action<ScenarioData> OnScenarioLoaded;
    public event Action OnScenarioReset;

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

    private void Start()
    {
        LoadAvailableScenarios();
    }

    // Ψάχνει τον φάκελο Scenarios και βρίσκει τι σενάρια υπάρχουν
    public void LoadAvailableScenarios()
    {
        availableScenarios.Clear();
        
        TextAsset[] scenarioFiles = Resources.LoadAll<TextAsset>("Scenarios");
        foreach (var file in scenarioFiles)
        {
            availableScenarios.Add(file.name);
        }
        
        Debug.Log($"Loaded {availableScenarios.Count} available scenarios");
    }

    // Προσπαθεί να φορτώσει το σενάριο δίνοντας του το όνομα του αρχείου
    public bool LoadScenario(string scenarioName)
    {
        try
        {
            TextAsset scenarioFile = Resources.Load<TextAsset>($"Scenarios/{scenarioName}");
            if (scenarioFile == null)
            {
                Debug.LogError($"Scenario file not found: {scenarioName}");
                return false;
            }

            return LoadScenarioFromJson(scenarioFile.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading scenario: {e.Message}");
            return false;
        }
    }

    // Parsing του JSON string και φτιάχνει το αρχικό state
    public bool LoadScenarioFromJson(string jsonContent)
    {
        try
        {
            currentScenario = JsonConvert.DeserializeObject<ScenarioData>(jsonContent);
            
            if (currentScenario == null)
            {
                Debug.LogError("Failed to parse scenario JSON");
                return false;
            }

            BuildNodeDictionary();

            // Αρχικοποιούμε το game state βάσει του JSON
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.InitializeState(currentScenario.initial_state);
            }

            Debug.Log($"Scenario loaded: {currentScenario.scenario_meta.title}");
            OnScenarioLoaded?.Invoke(currentScenario);
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing scenario JSON: {e.Message}");
            return false;
        }
    }

    // Φτιάχνει ένα γρήγορο λεξικό για να βρίσκουμε τα nodes κατευθείαν με το ID τους
    private void BuildNodeDictionary()
    {
        nodeDict.Clear();
        foreach (var node in currentScenario.nodes)
        {
            nodeDict[node.id] = node;
        }
        Debug.Log($"Built node dictionary with {nodeDict.Count} nodes");
    }

    // Φέρνει το node αν ξέρουμε το ID του
    public Node GetNode(string nodeId)
    {
        if (nodeDict.ContainsKey(nodeId))
        {
            return nodeDict[nodeId];
        }
        Debug.LogError($"Node not found: {nodeId}");
        return null;
    }

    // Φέρνει το πρώτο node της λίστας
    public Node GetStartNode()
    {
        if (currentScenario?.nodes != null && currentScenario.nodes.Count > 0)
        {
            return currentScenario.nodes[0];
        }
        return null;
    }

    // Βρίσκει ένα hotspot από το ID του
    public Hotspot GetHotspot(string hotspotId)
    {
        return currentScenario?.hotspots?.Find(h => h.id == hotspotId);
    }

    // Βρίσκει μια φόρμα του EHR
    public FormDefinition GetFormDefinition(string formId)
    {
        if (currentScenario?.ehr_config?.forms == null) return null;

        switch (formId)
        {
            case "assessment_form":
                return currentScenario.ehr_config.forms.assessment_form;
            case "intervention_form":
                return currentScenario.ehr_config.forms.intervention_form;
            case "communication_log":
                return currentScenario.ehr_config.forms.communication_log;
            default:
                return null;
        }
    }

    // Επαναφέρει το σενάριο στην αρχική του κατάσταση
    public void ResetScenario()
    {
        if (currentScenario != null)
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.InitializeState(currentScenario.initial_state);
            }
            
            OnScenarioReset?.Invoke();
            Debug.Log("Scenario reset");
        }
    }

    // Export το τρέχον σενάριο σε JSON 
    public string ExportScenarioToJson()
    {
        if (currentScenario != null)
        {
            return JsonConvert.SerializeObject(currentScenario, Formatting.Indented);
        }
        return null;
    }
}