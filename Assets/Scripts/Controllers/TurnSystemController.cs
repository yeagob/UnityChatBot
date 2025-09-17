using System.Collections.Generic;
using ChatSystem.Characters;
using ChatSystem.Models.LLM;
using UnityEngine;

public class TurnSystemController : MonoBehaviour
{
    [SerializeField]
    private List<CharacterAgent> _agents = new List<CharacterAgent>();
    
    int _currentAgentIndex = 0;
    int _currentTurn = 0;
    
    private async void Start()
    {
        foreach (CharacterAgent agent in _agents)
        {
            agent.InitializeAgent();
        }

        //INSENSATOS!!! NO HAGAIS ESTO JAMAS!!!
        while (true)
        {
            if (_currentAgentIndex == 0)
            {
                _currentTurn++;
                UniversalLogUI.Instance.Log($"\nCurrent turn: {_currentTurn}");
            }
            
            LLMResponse response = await _agents[_currentAgentIndex].ExecuteInitialAgentCall();
            _currentAgentIndex = (_currentAgentIndex+1) % _agents.Count;  
        }
    }

}
