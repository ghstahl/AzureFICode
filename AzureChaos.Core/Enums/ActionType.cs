namespace AzureChaos.Enums
{
    /// <summary>Possible action on Virtual Machine</summary>
    public enum ActionType
    {
        /// <summary>Default Action type</summary>
        Unknown = 0,

        /// <summary>Start action for the Resource</summary>
        Start,

        /// <summary>PowerOff/Stop action for the Resource</summary>
        PowerOff,

        /// <summary>Deallocate action for the Resource (for now its particular to VM)</summary>
        Deallocate,

        /// <summary>Restart action for the Resource</summary>
        Restart
    }
}
