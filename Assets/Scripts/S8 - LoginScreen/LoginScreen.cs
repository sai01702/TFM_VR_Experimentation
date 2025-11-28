using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;   // use this if you’re using TextMeshPro
using UnityEngine.UI;

public class LoginScreen : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField idInput;   // or InputField if you don’t use TMP
    public Button okButton;
    public TextMeshProUGUI errorText;   // optional

    [Header("Flow")]
    public string nextSceneName = "RoomScene";   // set in Inspector

    void Start()
    {
        if (errorText != null) errorText.text = "";

        // Pre-fill last ID if we have one
        if (ParticipantSession.Instance != null &&
            !string.IsNullOrEmpty(ParticipantSession.Instance.ParticipantId))
        {
            idInput.text = ParticipantSession.Instance.ParticipantId;
        }

        okButton.onClick.AddListener(OnOkClicked);
    }

    void OnOkClicked()
    {
        string raw = idInput.text.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            if (errorText != null)
                errorText.text = "Please enter a Participant ID.";
            return;
        }

        // OPTIONAL: enforce numeric IDs
        // if (!int.TryParse(raw, out _)) { ... return; }

        ParticipantSession.Instance.SetParticipant(raw);

        // Optional immediate log that session started
        ParticipantSession.Instance.AppendLog("SessionStart");

        SceneManager.LoadScene(nextSceneName);
    }
}
