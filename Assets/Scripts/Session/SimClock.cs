using System;

/// <summary>
/// Seconds of play time. GameSession advances it with scaled delta time, so it stops while the
/// game is paused (timeScale 0) and between sessions. Step 5 saves it with the universe.
/// </summary>
[Serializable]
public class SimClock
{
    public double seconds;

    public double Now => seconds;

    public void Advance(float deltaTime)
    {
        if (deltaTime > 0f)
            seconds += deltaTime;
    }
}
