using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenuView : MonoBehaviour
{
    [SerializeField]
    private GameObject _inputTextGO;

    [SerializeField]
    private TextMeshProUGUI _actionPonintsText;
    
    [SerializeField]
    private TMP_InputField _inputField;
    
    [SerializeField]
    private Button _sendButton;
    
    [SerializeField]
    private PlayerController playerController;

    public void OnMoveButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteMoveAction();
        _actionPonintsText.text = "Action Points: " + playerController.ActionPointUsed();
    }

    public void OnTalkButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteTalkAction();
        _actionPonintsText.text = "Action Points: " + playerController.ActionPointUsed();
    }

    public void OnGiveButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteGiveAction();
        _actionPonintsText.text = "Action Points: " + playerController.ActionPointUsed();

    }

    public void OnHitButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteHitAction();
        _actionPonintsText.text = "Action Points: " + playerController.ActionPointUsed();

    }

    private void ExecuteMoveAction()
    {
        Debug.Log("Move action executed");
    }

    private void ExecuteTalkAction()
    {
        Debug.Log("Talk action executed");
    }

    private void ExecuteGiveAction()
    {
        Debug.Log("Give action executed");
    }

    private void ExecuteHitAction()
    {
        Debug.Log("Hit action executed");
    }
}
