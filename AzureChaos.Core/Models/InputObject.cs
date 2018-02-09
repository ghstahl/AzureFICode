using AzureChaos.Enums;

namespace AzureChaos.Models
{
    /// <summary>The input object for the chaos executer.</summary>
    public class InputObject
    {
        /// <summary>Get or sets the action name i.e. what action should be performed on the resource.</summary>
        public ActionType Action { get; set; }

        /// <summary>Get or sets  the resource name.</summary>
        public string ResourceName { get; set; }

        /// <summary>Get or sets  the resource group.</summary>
        public string ResourceGroup { get; set; }

        /// <summary>Get or sets  the resource group.</summary>
        public string ScaleSetName { get; set; }

        /// <summary>AvailibilitySet Name to be passed</summary>
        public string AvailibilitySet { get; set; }

        /// <summary>Fault Domain Number in AvailibilitySet  to be passed</summary>
        public string FaultDomain { get; set;  }

        /// <summary>Update Domain Number in AvailibilitySet  to be passed</summary>
        public string UpdateDomain { get; set; }

        /// <summary>Percntage of VMs to be under Action</summary>
        public int VMPercentage { get; set; }
    }
}