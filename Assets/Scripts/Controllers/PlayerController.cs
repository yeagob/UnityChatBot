using System.Threading.Tasks;
using UnityEngine;

public class PlayerController : TurnCharacter
{
    [SerializeField]
    private int _actionPoints = 3;

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


    private void ShowActions(bool show)
    {
        _actionMenu.SetActive(show);
    }

    public void ActionPointUsed()
    {
        _currentActionPoints--;
    }
}
