$(document).ready(function () {
  $('#scheduleFromDate').datetimepicker();
  $('#scheduleToDate').datetimepicker({
    useCurrent: false //Important! See issue #1075
  });
  $("#scheduleFromDate").on("dp.change", function (e) {
    $('#scheduleToDate').data("DateTimePicker").minDate(e.date);
  });
  $("#scheduleToDate").on("dp.change", function (e) {
    $('#scheduleFromDate').data("DateTimePicker").maxDate(e.date);
  });


  $('#activityFromDate').datetimepicker();
  $('#activityToDate').datetimepicker({
    useCurrent: false //Important! See issue #1075
  });
  $("#activityFromDate").on("dp.change", function (e) {
    $('#activityToDate').data("DateTimePicker").minDate(e.date);
  });
  $("#activityToDate").on("dp.change", function (e) {
    $('#activityFromDate').data("DateTimePicker").maxDate(e.date);
  });
});

$("#activitySearch").on("click", function () {
  var fromdate = $('#activityFromDate input')[0].value;
  var todate = $('#activityToDate input')[0].value;
  getActivities(fromdate, todate);
});

$("#scheduleSearch").on("click", function () {
  var fromdate = $('#scheduleFromDate input')[0].value;
  var todate = $('#scheduleToDate input')[0].value;
  getSchedules(fromdate, todate);
});

function getActivities(fromdate, todate) {
  var request = $.ajax({
    url: "api/FaultInjection/getactivities",
    type: "GET",
    data: { fromDate: fromdate, toDate: todate }
  });
  request.done(function (response) {
    var $tbody = $("table tbody");
    appendActivityBody($tbody, response);
  });

  request.fail(function (jqXHR, textStatus) {
    alert("Request failed: " + textStatus);
  });
}

function getSchedules(fromdate, todate) {
  var request = $.ajax({
    url: "api/FaultInjection/getschedules",
    type: "GET",
    data: { fromDate: fromdate, toDate: todate }
  });
  request.done(function (response) {
    var $tbody = $("table tbody");
    appendScheduleBody($tbody, response);
  });

  request.fail(function (jqXHR, textStatus) {
    alert("Request failed: " + textStatus);
  });
}

function appendActivityBody($tbody, existingData) {
  $.each(existingData, function (index) {
    this.CanEdit = true;
    var $tr = $("<tr></tr>");
    var $rowNumber = $("<td></td>");
    $rowNumber.text(index + 1);

    var $resourceId = $("<td></td>");
    $resourceId.text(this.ResourceId);

    var $chaosOperation = $("<td></td>");
    $chaosOperation.text(this.ChaosOperation);

    var $chaosStartedTime = $("<td></td>");
    $chaosStartedTime.text(this.ChaosStartedTime);

    var $chaosCompletedTime = $("<td></td>");
    $chaosCompletedTime.text(this.ChaosSCompletedTime);

    var $initialState = $("<td></td>");
    $initialState.text(this.InitialState);

    var $finalState = $("<td></td>");
    $finalState.text(this.FinalState);

    var $status = $("<td></td>");
    $status.text(this.Status);

    var $error = $("<td></td>");
    $error.text(this.Error);

    var $warning = $("<td></td>");
    $warning.text(this.Warning);

    $tr.append($rowNumber);
    $tr.append($resourceId);
    $tr.append($chaosOperation);
    $tr.append($chaosStartedTime);
    $tr.append($chaosCompletedTime);
    $tr.append($initialState);
    $tr.append($finalState);
    $tr.append($status);
    $tr.append($error);
    $tr.append($warning);
    $tbody.append($tr);
  });
}

function appendScheduleBody($tbody, existingData) {
  if (!existingData || existingData.length === 0) {
    var $tr = $("<tr></tr>");
    var $emptyRecord = $("<td colspan=7></td>");
    $emptyRecord.text("No records found");
    $tr.append($emptyRecord);
    $tbody.append($tr);
    return;
  }
  $.each(existingData, function (index) {
    var $tr = $("<tr></tr>");
    var $rowNumber = $("<td></td>");
    $rowNumber.text(index + 1);

    var $resourceIName = $("<td></td>");
    $resourceIName.text(this.ResourceName);

    var $resourceId = $("<td></td>");
    $resourceId.text(this.ResourceId);

    var $scheduledTime = $("<td></td>");
    $scheduledTime.text(this.ScheduledTime);

    var $chaosOperation = $("<td></td>");
    $chaosOperation.text(this.ChaosOperation);

    var $isRollBacked = $("<td></td>");
    $isRollBacked.text(this.IsRollbacked);

    var $status = $("<td></td>");
    $status.text(this.Status);

    $tr.append($rowNumber);
    $tr.append($resourceId);
    $tr.append($scheduledTime);
    $tr.append($chaosOperation);
    $tr.append($isRollBacked);
    $tr.append($status);
    $tbody.append($tr);
  });
}