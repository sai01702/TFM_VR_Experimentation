using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginUIController : MonoBehaviour
{
    [SerializeField] TMP_InputField participantInput;
    [SerializeField] TMP_Text errorText;
    [SerializeField] string nextSceneName = "RoomScene";

    public void OnSaveClicked()
    {
        string id = participantInput.text.Trim();

        if (string.IsNullOrEmpty(id))
        {
            if (errorText != null)
                errorText.text = "Please enter a participant ID.";
            return;
        }

        if (ParticipantSession.Instance != null)
        {
            ParticipantSession.Instance.SetParticipant(id);
            ParticipantSession.Instance.AppendLog($"Login OK. ID = {id}");
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
