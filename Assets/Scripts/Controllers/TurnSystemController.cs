using System.Collections.Generic;
using ChatSystem.Characters;
using ChatSystem.Models.LLM;
using UnityEngine;
using UnityEngine.UI;

public class TurnSystemController : MonoBehaviour
{
    [SerializeField]
    private List<ITurnCharacter> _agents = new List<ITurnCharacter>();

    [SerializeField]
    private Image _turnPanelImage;
    
    int _currentAgentIndex = 0;
    int _currentTurn = 0;
    
    private async void Start()
    {
        foreach (ITurnCharacter agent in _agents)
        {
            agent.Initialize();
        }

        //INSENSATOS!!! NO HAGAIS ESTO JAMAS!!!
        while (true)
        {
            if (_currentAgentIndex == 0)
            {
                _currentTurn++;
                UniversalLogUI.Instance.Log($"\nCurrent turn: {_currentTurn}");
            }
            
            _turnPanelImage.sprite = _agents[_currentAgentIndex].AvatarImage;
             _agents[_currentAgentIndex].ExecuteTurn();
            _currentAgentIndex = (_currentAgentIndex+1) % _agents.Count;  
        }
    }

}
