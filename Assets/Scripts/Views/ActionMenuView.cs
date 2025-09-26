using UnityEngine;

public class ActionMenuView : MonoBehaviour
{
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
        playerController.ActionPointUsed();
    }

    public void OnTalkButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteTalkAction();
        playerController.ActionPointUsed();
    }

    public void OnGiveButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteGiveAction();
        playerController.ActionPointUsed();
    }

    public void OnHitButtonClicked()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is missing");
            return;
        }

        ExecuteHitAction();
        playerController.ActionPointUsed();
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
