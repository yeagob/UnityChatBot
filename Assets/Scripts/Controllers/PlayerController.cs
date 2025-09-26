using System.Collections.Generic;
using System.Threading.Tasks;
using MapSystem.Elements;
using UnityEngine;

public class PlayerController : TurnCharacter
{
    [Header("Game References")]
    [SerializeField]
    private CharacterElement _characterElement;
    
    [SerializeField]
    private MapSystem.MapSystem _mapSystem;

    [Header("Turn Configuration")]
    [SerializeField]
    private int _actionPoints = 3;

    [Header("UI References")]
    [SerializeField]
    private GameObject _actionMenu;
    
    private bool _myTurn;
    private int _currentActionPoints;

    public override void Initialize()
    {
        ShowActions(false);
        _myTurn = false;
    }

    public override async Task ExecuteTurn()
    {
        _currentActionPoints = _actionPoints;
        
        _myTurn = true;
        ShowActions(true);

        while (_currentActionPoints > 0 && _myTurn)
        {
            await Task.Yield();
        }
        
        _myTurn = false;
        ShowActions(false);
    }

    public void ExecuteMoveAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        GridSystem.Models.GridCell currentCell = _characterElement.CurrentGridCell;
        GridSystem.Models.GridCell targetCell = CalculateTargetMoveCell(currentCell);

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
        Debug.Log($"Player said: {message}");
    }

    public void ExecuteGiveAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        ConsumeActionPoint();
        Debug.Log("Give action executed - implementation pending");
    }

    public void ExecuteHitAction()
    {
        if (!CanExecuteAction())
        {
            return;
        }

        ConsumeActionPoint();
        Debug.Log("Hit action executed - implementation pending");
    }

    public int GetCurrentActionPoints()
    {
        return _currentActionPoints;
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
    }

    private void ShowActions(bool show)
    {
        _actionMenu.SetActive(show);
    }

    private GridSystem.Models.GridCell CalculateTargetMoveCell(GridSystem.Models.GridCell currentCell)
    {
        MapSystem.Enums.ViewDirection facingDirection = _characterElement.FacingDirection;
        
        int targetRow = currentCell.row;
        int targetColumn = currentCell.column;

        if (facingDirection == MapSystem.Enums.ViewDirection.Right)
        {
            targetColumn++;
        }
        else
        {
            targetColumn--;
        }

        return new GridSystem.Models.GridCell
        {
            row = targetRow,
            column = targetColumn
        };
    }

    private void BroadcastMessageToNearbyCharacters(string message)
    {
        GridSystem.Models.GridCell currentCell = _characterElement.CurrentGridCell;
        List<CharacterElement> nearbyCharacters = _mapSystem.GetAllCharactesAtDistance(2, currentCell);

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
        ChatSystem.Characters.CharacterAgent agent = character.GetComponent<ChatSystem.Characters.CharacterAgent>();

        if (agent != null)
        {
            Debug.Log($"Player message received by {character.name}");
        }
    }
}
