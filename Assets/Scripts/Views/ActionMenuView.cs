using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenuView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private GameObject _inputTextGO;

    [SerializeField]
    private TextMeshProUGUI _actionPointsText;
    
    [SerializeField]
    private TMP_InputField _inputField;
    
    [SerializeField]
    private Button _sendButton;

    [SerializeField]
    private Button _moveButton;

    [SerializeField]
    private Button _talkButton;

    [SerializeField]
    private Button _giveButton;

    [SerializeField]
    private Button _hitButton;

    [Header("Controller")]
    [SerializeField]
    private PlayerController _playerController;

    private void Start()
    {
        SetupButtonListeners();
        HideInputField();
    }

    private void SetupButtonListeners()
    {
        if (_moveButton != null)
        {
            _moveButton.onClick.AddListener(OnMoveButtonClicked);
        }

        if (_talkButton != null)
        {
            _talkButton.onClick.AddListener(OnTalkButtonClicked);
        }

        if (_giveButton != null)
        {
            _giveButton.onClick.AddListener(OnGiveButtonClicked);
        }

        if (_hitButton != null)
        {
            _hitButton.onClick.AddListener(OnHitButtonClicked);
        }

        if (_sendButton != null)
        {
            _sendButton.onClick.AddListener(OnSendButtonClicked);
        }
    }

    private void OnMoveButtonClicked()
    {
        if (!ValidateController())
        {
            return;
        }

        HideInputField();
        _playerController.ExecuteMoveAction();
    }

    private void OnTalkButtonClicked()
    {
        if (!ValidateController())
        {
            return;
        }

        ShowInputField();
    }

    private void OnGiveButtonClicked()
    {
        if (!ValidateController())
        {
            return;
        }

        HideInputField();
        _playerController.ExecuteGiveAction();
        UpdateActionPointsDisplay();
    }

    private void OnHitButtonClicked()
    {
        if (!ValidateController())
        {
            return;
        }

        HideInputField();
        _playerController.ExecuteHitAction();
    }

    private void OnSendButtonClicked()
    {
        if (!ValidateController())
        {
            return;
        }

        string message = _inputField.text;

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("Cannot send empty message");
            return;
        }

        _playerController.ExecuteTalkAction(message);
        UpdateActionPointsDisplay();
        ClearAndHideInput();
    }

    public void UpdateActionPointsDisplay()
    {
        if (_actionPointsText != null && _playerController != null)
        {
            int remainingPoints = _playerController.GetCurrentActionPoints();
            _actionPointsText.text = $"Action Points: {remainingPoints}";
        }
    }

    private bool ValidateController()
    {
        if (_playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return false;
        }

        return true;
    }

    private void ShowInputField()
    {
        if (_inputTextGO != null)
        {
            _inputTextGO.SetActive(true);
        }
    }

    private void HideInputField()
    {
        if (_inputTextGO != null)
        {
            _inputTextGO.SetActive(false);
        }
    }

    private void ClearAndHideInput()
    {
        if (_inputField != null)
        {
            _inputField.text = string.Empty;
        }

        HideInputField();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    private void RemoveButtonListeners()
    {
        if (_moveButton != null)
        {
            _moveButton.onClick.RemoveListener(OnMoveButtonClicked);
        }

        if (_talkButton != null)
        {
            _talkButton.onClick.RemoveListener(OnTalkButtonClicked);
        }

        if (_giveButton != null)
        {
            _giveButton.onClick.RemoveListener(OnGiveButtonClicked);
        }

        if (_hitButton != null)
        {
            _hitButton.onClick.RemoveListener(OnHitButtonClicked);
        }

        if (_sendButton != null)
        {
            _sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }
    }
}
