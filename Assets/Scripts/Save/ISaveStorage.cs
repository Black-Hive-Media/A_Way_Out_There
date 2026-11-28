using System.Collections.Generic;

/// <summary>
/// Where save slots live. Game code only talks to this interface; each platform has its own
/// implementation (PC files now; Switch and Xbox storage in step 14).
/// </summary>
public interface ISaveStorage
{
    bool Exists(int slot);

    /// <summary>The slot's text, or null if there is none.</summary>
    string Read(int slot);

    /// <summary>Writes the whole slot; must never leave a half-written file behind.</summary>
    void Write(int slot, string text);

    void Delete(int slot);

    /// <summary>Slots that have a save, in ascending order.</summary>
    IReadOnlyList<int> ListSlots();

    /// <summary>Human-readable location of a slot, for logs and the Editor (may be empty).</summary>
    string Describe(int slot);
}
