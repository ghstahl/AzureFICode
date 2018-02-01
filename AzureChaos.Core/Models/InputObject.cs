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

        /// <summary>Get or sets the scale out percentage of the resource </summary>
        public string ScaleOutPercentage { get; set; }
    }
}