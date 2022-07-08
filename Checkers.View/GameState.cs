namespace Checkers.View;

public static class GameState
{
    public static GameStateType CurrentGameState { get; private set; }

    public static event Action<GameStateType, GameStateType>? StateChanged;

    public static void SwitchState(GameStateType newState)
    {
        if (newState == CurrentGameState)
        {
            return;
        }

        var oldState = CurrentGameState;
        CurrentGameState = newState;
        StateChanged?.Invoke(oldState, newState);
    }
}