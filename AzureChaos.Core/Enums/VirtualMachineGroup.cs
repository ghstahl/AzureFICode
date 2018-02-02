namespace AzureChaos.Enums
{
    /// <summary>Azure Resource types. </summary>
    public enum VirtualMachineGroup
    {
        /// <summary>Standlone virtual machines.</summary>
        VirtualMachines,

        /// <summary>Virtual machines in availability sets.</summary>
        AvailabilitySets,

        /// <summary>Virtual machines in scale sets.</summary>
        ScaleSets,

        /// <summary>Virtual machines in load balancers.</summary>
        LoadBalancers
    }
}