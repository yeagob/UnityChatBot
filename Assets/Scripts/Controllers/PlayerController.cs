using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Characters;
using ChatSystem.Configuration.ScriptableObjects;
using Grid;
using Grid.Models.Grid;
using MapSystem.Elements;
using PlayerSystem.Configuration;
using PlayerSystem.Enums;
using TMPro;
using UnityEngine;

public class PlayerController : TurnCharacter
{
    [SerializeField]
    private string _myName = "Santiago";
    
    [Header("Turn Configuration")]
    [SerializeField]
    private int _actionPoints = 3;
    
    [Header("Game References")]
    [SerializeField]
    private CharacterElement _characterElement;
    
    [SerializeField]
    private MapSystem.MapSystem _mapSystem;

    [SerializeField]
    private GridSystem _gridSystem;

    [SerializeField]
    private Camera _mainCamera;

    [Header("UI References")]
    [SerializeField]
    private ActionMenuView _actionMenu;
    
    [SerializeField] 
    private TextMeshProUGUI _dialogText ;
        
    [SerializeField] 
    private GameObject _dialogObject;
    
    private bool _myTurn;
    private int _currentActionPoints;
    private PlayerActionState _currentActionState;

    public override void Initialize()
    {
        ShowActions(false);
        HideDialog();
        
        _myTurn = false;
        _currentActionState = PlayerActionState.None;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    public override async Task ExecuteTurn()
    {
        _currentActionPoints = _actionPoints;
        
        _myTurn = true;
        ShowActions(true);
        HideDialog();

        while (_currentActionPoints > 0 && _myTurn)
        {
            await Task.Yield();
        }
        
        _myTurn = false;
        ShowActions(false);
        _currentActionState = PlayerActionState.None;
    }

    private void Update()
    {
        if (!_myTurn)
        {
            return;
        }

        if (_currentActionState == PlayerActionState.None)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        ProcessMouseClick();
    }

    public void ExecuteMoveAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        _currentActionState = PlayerActionState.WaitingForMoveTarget;
        Debug.Log("Click on map to select movement destination");
    }

    public void ExecuteTalkAction(string message)
    {
        if (!CanExecuteAction())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("Cannot talk with empty message");
            return;
        }

        BroadcastMessageToNearbyCharacters(message);
        ConsumeActionPoint();
        _dialogText.text = message;
        ShowDialog();
    }

    private void ShowDialog()
    {
        _dialogObject.SetActive(true);
    }

    private void HideDialog()
    {
        _dialogObject.SetActive(false);
    }
    
    public void ExecuteGiveAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        Debug.Log("Give action - not implemented yet");
    }

    public void ExecuteHitAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        _currentActionState = PlayerActionState.WaitingForAttackTarget;
        Debug.Log("Click on a character to attack");
    }

    public int GetCurrentActionPoints()
    {
        return _currentActionPoints;
    }

    private void ProcessMouseClick()
    {
        Vector3 mousePosition = Input.mousePosition;

        if (_currentActionState == PlayerActionState.WaitingForMoveTarget)
        {
            ProcessMovementClick(mousePosition);
        }
        else if (_currentActionState == PlayerActionState.WaitingForAttackTarget)
        {
            ProcessAttackClick(mousePosition);
        }
    }

    private void ProcessMovementClick(Vector3 mousePosition)
    {
        GridResult<GridCell> result = _gridSystem.ScreenPointToGridCell(mousePosition);

        GridCell targetCell = result.value;
        bool moved = _characterElement.TryMoveTo(targetCell.row, targetCell.column);

        if (moved)
        {
            ConsumeActionPoint();
            Debug.Log($"Player moved to ({targetCell.row}, {targetCell.column})");
        }
        else
        {
            Debug.LogWarning("Movement failed: invalid target cell");
        }

        _currentActionState = PlayerActionState.None;
    }

    private void ProcessAttackClick(Vector3 mousePosition)
    {
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == null)
        {
            Debug.LogWarning("No target found");
            _currentActionState = PlayerActionState.None;
            return;
        }

        CharacterElement targetCharacter = hit.collider.GetComponent<CharacterElement>();

        if (targetCharacter == null)
        {
            Debug.LogWarning("Clicked object is not a character");
            _currentActionState = PlayerActionState.None;
            return;
        }

        if (targetCharacter == _characterElement)
        {
            Debug.LogWarning("Cannot attack yourself");
            _currentActionState = PlayerActionState.None;
            return;
        }

        if (!IsTargetInRange(targetCharacter))
        {
            Debug.LogWarning("Target is out of attack range");
            _currentActionState = PlayerActionState.None;
            return;
        }

        ExecuteAttack(targetCharacter);
        _currentActionState = PlayerActionState.None;
    }

    private void ExecuteAttack(CharacterElement target)
    {
        int damage = PlayerActionConfiguration.AttackDamage;
        
        target.ModifyHealth(-damage);
        
        NotifyCharacterOfAttack(target, damage);
        ConsumeActionPoint();
        
        Debug.Log($"Player attacked {target.name} for {damage} damage");
    }

    private bool IsTargetInRange(CharacterElement target)
    {
        GridCell currentCell = _characterElement.CurrentGridCell;
        GridCell targetCell = target.CurrentGridCell;

        int distance = _mapSystem.GetGridDistanceBetweenElements(_characterElement, target);
        return distance <= PlayerActionConfiguration.AttackRange;
    }

    private void NotifyCharacterOfAttack(CharacterElement target, int damage)
    {
        ChatSystem.Characters.CharacterAgent agent = target.GetComponent<ChatSystem.Characters.CharacterAgent>();

        if (agent == null)
        {
            return;
        }

        int currentHealth = target.HealthPoints;
        string attackerName = _characterElement.name;

        PromptConfig attackPrompt = ScriptableObject.CreateInstance<PromptConfig>();
        attackPrompt.promptId = "combat-damage-received";
        attackPrompt.promptName = "Combat";
        attackPrompt.category = "";
        attackPrompt.description = "Damage received in combat";
        attackPrompt.enabled = true;
        attackPrompt.priority = 10;
        attackPrompt.version = "1.0";

        attackPrompt.content = $"{attackerName} has attacked you for {damage} damage. Your current health: {currentHealth}";

        agent.AddContextPrompt(attackPrompt);
    }

    private bool CanExecuteAction()
    {
        if (!_myTurn)
        {
            Debug.LogWarning("Cannot execute action: not player's turn");
            return false;
        }

        if (_currentActionPoints <= 0)
        {
            Debug.LogWarning("Cannot execute action: no action points remaining");
            return false;
        }

        return true;
    }

    private void ConsumeActionPoint()
    {
         _currentActionPoints--;
         _actionMenu.UpdateActionPoints();
    }

    private void ShowActions(bool show)
    {
        _actionMenu.gameObject.SetActive(show);
    }

    private void BroadcastMessageToNearbyCharacters(string message)
    {
        GridCell currentCell = _characterElement.CurrentGridCell;
        List<CharacterElement> nearbyCharacters = _mapSystem.GetAllCharactesAtDistance(
            PlayerActionConfiguration.TalkRange, 
            currentCell
        );

        foreach (CharacterElement character in nearbyCharacters)
        {
            if (character == _characterElement)
            {
                continue;
            }

            NotifyCharacterOfMessage(character, message);
        }
    }

    private void NotifyCharacterOfMessage(CharacterElement character, string message)
    {
        CharacterAgent agent = character.GetComponent<CharacterAgent>();

        if (agent == null)
        {
            return;
        }

        string senderName = _myName;

        PromptConfig conversationPrompt = ScriptableObject.CreateInstance<PromptConfig>();
        conversationPrompt.promptId = "conversation-message";
        conversationPrompt.promptName = "Conversaciones";
        conversationPrompt.category = "";
        conversationPrompt.description = "Conversaciones acumuladas en el turno actual";
        conversationPrompt.enabled = true;
        conversationPrompt.priority = 10;
        conversationPrompt.version = "1.0";

        conversationPrompt.content = $"{senderName} ha dicho: {message}";

        agent.AddContextPrompt(conversationPrompt);
    }
}
