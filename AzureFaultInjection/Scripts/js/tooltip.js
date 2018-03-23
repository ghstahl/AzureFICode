$(document).ready(function () {

  $('#tooltip-tenant-id').attr("title", "Tenant Id that represents the id of the service principal under AAD. This is also called as Directory id");
  $('#tooltip-client-id').attr("title", "Application Id represents the id of an application created to perform fault injection operations. This is also called as application id.");
  $('#tooltip-client-secret').attr("title", "Application key represents the keys that are connected to a client id to perform fault injection operations.");
  $('#tooltip-subscription').attr("title", "Subscription id of the azure account on which fault injection operations are to be executed");
  $('#tooltip-exclude-resourcegroups').attr("title", "Comma separated list of Resource groups excluded from fault injection operations. By default, the target resource group mentioned should be  added into the excluded list.");
  $('#tooltip-vm-percentage').attr("title", "value lies between 0 to 100. Percentage of VMs on which fault injection operations are performed simultaneously.");
  $('#tooltip-vmss-percentage').attr("title", "value lies between 0 to 100. Percentage of VMs in a particular VMSS on which fault injection operations are performed simultaneously.");
  $('#tooltip-scheduler-frequency').attr("title", "The time frequency in minutes for which the scheduler function will run to create fault injection rules.");
  $('#tooltip-rollback-frequency').attr("title", "The time frequency in minutes for which the executor function will run to check the successfully executed rules to perform rollback operation.");
  $('#tooltip-crawler-frequency').attr("title", "Time frequency in minutes for which the crawler function will run to crawl the resources (VM, RGs, VMSS, AvSets).");
  $('#tooltip-meanTime').attr("title", "This property will ensure that the chaos happened on the particular resource only once within this mean time.");

  //$('[data-toggle="tooltip"]').tooltip();
})