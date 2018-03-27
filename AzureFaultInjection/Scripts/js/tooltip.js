$(document).ready(function () {

  $('#tooltip-tenant-id').attr("title", "Azure Active Directory (AAD) Id used to create Service Principal. This is also called Directory Id.");
  $('#tooltip-client-id').attr("title", "Application Id for fault injection operations.");
  $('#tooltip-client-secret').attr("title", "Secret Key associated with Application Id for fault injection operations.");
  $('#tooltip-subscription').attr("title", "Azure Subscription Id in scope for fault injection operations.");
  $('#tooltip-exclude-resourcegroups').attr("title", "Resource Group(s) to be excluded from fault injection operations.");
  $('#tooltip-vm-percentage').attr("title", "% of single Virtual Machines subjected to fault injection operations simultaneously.");
  $('#tooltip-vmss-percentage').attr("title", "% of instances in a particular Scale Set subjected to fault injection operations simultaneously.");
  $('#tooltip-scheduler-frequency').attr("title", "Time between AzureFI initiating fault operations.");
  $('#tooltip-rollback-frequency').attr("title", "Time between powering off and powering on a Virtual Machine.");
  $('#tooltip-crawler-frequency').attr("title", "Time between AzureFI crawl operations to identify new resources in the selected Resource Groups.");
  $('#tooltip-meanTime').attr("title", "Time between a specific Virtual Machine picked for fault injection operations.");

  //$('[data-toggle="tooltip"]').tooltip();
})