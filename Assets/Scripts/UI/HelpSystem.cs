using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Το σύστημα βοήθειας (Help) που βγάζει οδηγίες και tips μέσα στο παιχνίδι
public class HelpSystem : MonoBehaviour
{
    public static HelpSystem Instance { get; private set; }

    [Header("Help UI")]
    public GameObject helpPanel;
    public TextMeshProUGUI helpTitleText;
    public TextMeshProUGUI helpContentText;
    public Button closeButton;
    public Transform helpTopicsContainer;
    public Button helpTopicButtonPrefab;

    [Header("Help Topics")]
    private Dictionary<string, HelpTopic> helpTopics = new Dictionary<string, HelpTopic>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeHelpTopics();
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideHelp);
        }

        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }

        CreateHelpTopicButtons();
    }

    private void Update()
    {
        // Ανοιγοκλείνουμε τη βοήθεια με το F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleHelp();
        }
    }

    // Σετάρει όλα τα κείμενα για τα help topics
    private void InitializeHelpTopics()
    {
        helpTopics.Clear();

        // Γενικές Οδηγίες
        helpTopics["general"] = new HelpTopic
        {
            id = "general",
            title = "Γενικές Οδηγίες",
            content = @"<b>Καλώς ήρθατε στην Προσομοίωση ΜΕΘ</b>

Σε αυτή την εφαρμογή θα διαχειριστείτε κλινικά περιστατικά σε περιβάλλον Μονάδας Εντατικής Θεραπείας.

<b>Βασικός Στόχος:</b>
- Αξιολογήστε τον ασθενή
- Λάβετε αποφάσεις με βάση τα ζωτικά σημεία
- Τεκμηριώστε όλες τις ενέργειές σας στο EHR
- Κλιμακώστε όταν χρειάζεται

<b>Πλήκτρα Συντομεύσεων:</b>
- F1: Εμφάνιση/Απόκρυψη Βοήθειας
- ESC: Παύση
- M: Εμφάνιση Μόνιτορ Ζωτικών

<b>Σκορ:</b>
Κερδίζετε πόντους για σωστές αποφάσεις και χάνετε για λάθη ή καθυστερήσεις."
        };

        // Hotspots
        helpTopics["hotspots"] = new HelpTopic
        {
            id = "hotspots",
            title = "Hotspots - Σημεία Αλληλεπίδρασης",
            content = @"<b>Διαθέσιμα Hotspots:</b>

<b>1. Μόνιτορ Ζωτικών Λειτουργιών</b>
Εμφανίζει τα ζωτικά σημεία του ασθενούς:
- HR (Heart Rate): Καρδιακοί Παλμοί
- SpO2: Κορεσμός Οξυγόνου
- RR (Respiratory Rate): Αναπνοές/λεπτό
- BP: Αρτηριακή Πίεση
- Temp: Θερμοκρασία

<b>2. Ασθενής/Κλίνη</b>
Αξιολογήστε την κατάσταση του ασθενούς.
Παρατηρήστε συμπτώματα και σημεία.

<b>3. Αναπνευστήρας</b>
Ρυθμίστε τις παραμέτρους οξυγόνωσης:
- FiO2: Ποσοστό οξυγόνου (21-100%)
- Flow Rate: Ρυθμός ροής (L/min)

<b>4. Τερματικό EHR</b>
Τεκμηριώστε τις ενέργειές σας.
Συμπληρώστε φόρμες αξιολόγησης και παρεμβάσεων.

<b>5. Κουμπί Κλήσης</b>
Καλέστε για βοήθεια ή κλιμακώστε την περίπτωση."
        };

        // EHR System
        helpTopics["ehr"] = new HelpTopic
        {
            id = "ehr",
            title = "Σύστημα EHR (Ηλεκτρονικός Φάκελος)",
            content = @"<b>Σύστημα Ηλεκτρονικού Φακέλου Υγείας</b>

Το EHR είναι κρίσιμο για την τεκμηρίωση των ενεργειών σας.

<b>Διαθέσιμες Φόρμες:</b>

<b>1. Κλινική Αξιολόγηση</b>
Καταγράψτε τις παρατηρήσεις σας:
- Observation: Κύρια παρατήρηση
- Skin Color: Χρώμα δέρματος
- Consciousness: Επίπεδο συνείδησης

<b>2. Παρεμβάσεις/Ρυθμίσεις</b>
Καταγράψτε τις ενέργειές σας:
- Device: Συσκευή που χρησιμοποιήθηκε
- FiO2 Setting: Ρύθμιση οξυγόνου
- Flow Rate: Ρυθμός ροής

<b>3. Ημερολόγιο Επικοινωνίας</b>
Καταγράψτε επικοινωνίες:
- Recipient: Αποδέκτης κλήσης
- Reason: Λόγος επικοινωνίας
- Outcome: Αποτέλεσμα

<b>⚠️ ΠΡΟΣΟΧΗ:</b>
Ορισμένα σημεία του σεναρίου (Gates) απαιτούν υποχρεωτική τεκμηρίωση για να προχωρήσετε!"
        };

        // Decision Making
        helpTopics["decisions"] = new HelpTopic
        {
            id = "decisions",
            title = "Λήψη Αποφάσεων",
            content = @"<b>Οδηγός Λήψης Αποφάσεων</b>

<b>Βήματα Διαχείρισης Περιστατικού:</b>

<b>1. Αξιολόγηση (Assessment)</b>
- Ελέγξτε το μόνιτορ ζωτικών
- Παρατηρήστε τον ασθενή
- Εντοπίστε το πρόβλημα

<b>2. Ανάλυση</b>
- Αξιολογήστε τη σοβαρότητα
- Προσδιορίστε προτεραιότητες
- Σχεδιάστε ενέργειες

<b>3. Παρέμβαση (Intervention)</b>
- Εφαρμόστε άμεσες ενέργειες
- Ρυθμίστε εξοπλισμό αν χρειάζεται
- Παρακολουθήστε την απόκριση

<b>4. Τεκμηρίωση (Documentation)</b>
- Καταγράψτε στο EHR
- Συμπληρώστε όλα τα απαιτούμενα πεδία
- Τεκμηριώστε χρόνους και αποτελέσματα

<b>5. Κλιμάκωση (Escalation)</b>
- Καλέστε για βοήθεια αν χρειάζεται
- Ενημερώστε τον υπεύθυνο ιατρό
- Καταγράψτε την επικοινωνία

<b>Κρίσιμοι Συναγερμοί:</b>
- SpO2 < 90%: Υποξαιμία
- HR < 50 ή > 150: Αρρυθμία
- RR < 8 ή > 30: Αναπνευστική δυσχέρεια"
        };

        // Scoring
        helpTopics["scoring"] = new HelpTopic
        {
            id = "scoring",
            title = "Σύστημα Βαθμολόγησης",
            content = @"<b>Πώς Βαθμολογείστε</b>

<b>Κερδίζετε Πόντους:</b>
+ Σωστή και έγκαιρη αξιολόγηση
+ Κατάλληλες παρεμβάσεις
+ Πλήρης τεκμηρίωση
+ Έγκαιρη κλιμάκωση

<b>Χάνετε Πόντους:</b>
- Καθυστερήσεις στην απόκριση
- Λάθος επιλογές
- Ελλιπής τεκμηρίωση
- Παράλειψη κρίσιμων ενεργειών

<b>Κλίμακα Αξιολόγησης:</b>
- 90-100: Άριστα
- 75-89: Πολύ Καλά
- 60-74: Καλά
- 50-59: Μέτρια
- 0-49: Χρειάζεται Βελτίωση

<b>Απολογισμός (Debriefing):</b>
Στο τέλος του σεναρίου θα δείτε:
- Το τελικό σας σκορ
- Τη διαδρομή αποφάσεων που ακολουθήσατε
- Τι τεκμηριώσατε και τι παραλείψατε
- Αναλυτικά στατιστικά"
        };

        // Troubleshooting
        helpTopics["troubleshooting"] = new HelpTopic
        {
            id = "troubleshooting",
            title = "Αντιμετώπιση Προβλημάτων",
            content = @"<b>Συχνές Ερωτήσεις & Λύσεις</b>

<b>Δεν μπορώ να προχωρήσω:</b>
Ελέγξτε αν υπάρχει ενεργό Gate (🛑).
Πρέπει να συμπληρώσετε το EHR για να προχωρήσετε.

<b>Το μόνιτορ χτυπάει συνέχεια:</b>
Αντιμετωπίστε το κλινικό πρόβλημα:
- Χαμηλό SpO2 → Ρυθμίστε οξυγόνο
- Υψηλός/χαμηλός παλμός → Αξιολογήστε

<b>Δεν ξέρω τι να επιλέξω:</b>
1. Ελέγξτε τα ζωτικά σημεία
2. Διαβάστε προσεκτικά το μήνυμα
3. Σκεφτείτε κλινικά
4. Τεκμηριώστε πάντα!

<b>Πώς επανεκκινώ το σενάριο:</b>
Πατήστε ESC για παύση και επιλέξτε Restart.

<b>Τεχνικά Προβλήματα:</b>
Αν η εφαρμογή δεν ανταποκρίνεται:
- Επανεκκινήστε το σενάριο
- Ελέγξτε τα logs
- Επικοινωνήστε με τον διαχειριστή"
        };
    }

    // Φτιάχνει δυναμικά τα κουμπιά για το κάθε topic στο μενού βοήθειας
    private void CreateHelpTopicButtons()
    {
        if (helpTopicsContainer == null || helpTopicButtonPrefab == null) return;

        // Καταστρέφουμε τα παλιά κουμπιά
        foreach (Transform child in helpTopicsContainer)
        {
            Destroy(child.gameObject);
        }

        // Φτιάχνουμε τα νέα
        foreach (var kvp in helpTopics)
        {
            Button btn = Instantiate(helpTopicButtonPrefab, helpTopicsContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = kvp.Value.title;
            
            string topicId = kvp.Key;
            btn.onClick.AddListener(() => ShowTopic(topicId));
        }
    }

    // Ανοιγοκλείνει το πάνελ της βοήθειας
    public void ToggleHelp()
    {
        if (helpPanel != null)
        {
            if (helpPanel.activeSelf)
            {
                HideHelp();
            }
            else
            {
                ShowHelp("general");
            }
        }
    }

    // Ανοίγει τη βοήθεια κατευθείαν σε ένα συγκεκριμένο θέμα (topic)
    public void ShowHelp(string topicId = "general")
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
            ShowTopic(topicId);
        }
    }

    // Κρύβει το πάνελ βοήθειας
    public void HideHelp()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
    }

    // Αλλάζει το κείμενο για να δείξει το επιλεγμένο topic
    public void ShowTopic(string topicId)
    {
        if (!helpTopics.ContainsKey(topicId))
        {
            Debug.LogWarning($"Help topic not found: {topicId}");
            return;
        }

        HelpTopic topic = helpTopics[topicId];

        if (helpTitleText != null)
        {
            helpTitleText.text = topic.title;
        }

        if (helpContentText != null)
        {
            helpContentText.text = topic.content;
        }
    }

    // Πετάει βοήθεια ανάλογα με το που βρισκόμαστε στο σενάριο
    public void ShowContextualHelp()
    {
        if (DecisionTreeEngine.Instance?.currentNode != null)
        {
            Node currentNode = DecisionTreeEngine.Instance.currentNode;
            
            if (currentNode.type == "gate")
            {
                ShowHelp("ehr");
            }
            else if (currentNode.type == "decision")
            {
                ShowHelp("decisions");
            }
            else
            {
                ShowHelp("general");
            }
        }
        else
        {
            ShowHelp("general");
        }
    }
}

// Απλό class που κρατάει τα δεδομένα για το κάθε θέμα βοήθειας
[System.Serializable]
public class HelpTopic
{
    public string id;
    public string title;
    public string content;
}