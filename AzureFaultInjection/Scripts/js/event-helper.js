if (app.$nextButton) {
  app.$nextButton.on("click", function (e) {
    var $this = $(this),
      currentParent = $(this).closest("fieldset"),
      prevParent = currentParent.prev();
    if (app.isValid(currentParent)) {
      if (currentParent.attr("id") === "step-1") {
        getSubscriptions(currentParent);
      }
    }

    $('#azure-fault-injection-actions').SumoSelect({ selectAll: true });
  });
}

app.showNextStep((current, next) => {
  var $this = $(this),
    currentParent = $(this).closest("fieldset"),
    prevParent = currentParent.prev();
});
function getSubsId(id) {
  if (id && id.length > 2) {
    return id.split('/')[2];
  }
}
$("#submit").on("click", function (e) {
  e.preventDefault();
  var values = {};
  var formElement = $('#multiwizard');
  $.each(formElement.serializeArray(), function (i, field) {
    if (field.value === 'on') {
      field.value = true;
    }
    if (field.value === 'off') {
      field.value = false;
    }
    if (field.name === 'isFaultOrUpdateDomainEnabled') {
      var faultDomain = $("#isFaultDomainEnabled")[0].checked;
      var updateDomain = $("#isUpdateDomainEnabled")[0].checked;
      if (faultDomain) {
        values["isFaultDomainEnabled"] = faultDomain;
      }
      if (updateDomain) {
        values["isUpdateDomainEnabled"] = updateDomain;
      }
    }
    if (field.name === 'subscription') {
      values[field.name] = getSubsId(field.value);
    }
    else if (field.name === 'azureFiActions') {
      values[field.name] = $("#azure-fault-injection-actions").val();
    }
    else if (field.name === 'excludedResourceGroups') {
      values[field.name] = $("#excluded-resource-groups").val();
    } else {
      values[field.name] = field.value;
    }
  });
  var request = $.ajax({
    url: "api/FaultInjection/createblob",
    type: "POST",
    data: values,
    beforeSend: function () {
      $(".modal").show();
    },
    complete: function () {
      $(".modal").hide();
    },
    error: function () {
      $(".modal").hide();
    }
  });
  request.done(function (response) {
    if (response || response.Success || response.SuccessMessage) {
      alert(response.SuccessMessage);
      return;
    }
    else if (response && response.ErrorMessage) {
      alert(response.ErrorMessage);
    }
    else {
      alert("Something went wrong, please try again later!");
    }
  });

  request.fail(function (jqXHR, textStatus) {
    alert("Request failed: " + textStatus);
  });
});

$("#selectSubscription").change(function () {
  var subscription = this.value;
  getResourceGroups(subscription);
});

//function hideResourceGroups(selector, selectedValues) {
//  $(selector + " option").each(function (index) {
//    var $option = $(this)[0]
//    if (selectedValues && $.inArray($option.value)) {
//      $.each(selectedValues, function (key, value) {
//        var $element = $(selector + " option[value='" + value + "']");
//        $(selector).next()
//          .next(".optWrapper.selall.multiple")
//          .find(".options li:contains('" + $element.text() + "')").css("display", "none");
//        $element.css("display", "none");
//      });
//    } else {
//      $option.removeAttribute("style");
//      $(selector).next()
//        .next(".optWrapper.selall.multiple")
//        .find(".options li:contains('" + $option.text + "')").removeAttr("style");
//    }
//  })
//}

function getSubscriptions(currentStepObj) {
  var tenantId = currentStepObj.find("#tenant-id").val();
  var clientId = currentStepObj.find("#client-id").val();
  var clientSecret = currentStepObj.find("#client-secret").val();
  var request = $.ajax({
    url: "api/FaultInjection/getsubscriptions",
    type: "GET",
    data: { tenantId: tenantId, clientId: clientId, clientSecret: clientSecret },
    beforeSend: function () {
      $(".modal").show();
    },
    complete: function () {
      $(".modal").hide();
    },
    error: function () {
      $(".modal").hide();
    }
  });
  request.done(function (response) {
    if (!response) {
      alert("Something went wrong, please try again later!");
      return;
    }
    if (response.Success === false || !response.Result) {
      console.log("subscription list is empty");
      if (response.ErrorMessage) {
        alert(response.ErrorMessage);
      }

      return;
    }

    var result = response.Result;
    bindOptions($('#selectSubscription'), result.SubcriptionList);
    $('#selectSubscription').SumoSelect();
    $("#submit").val("Configure and Deploy");
    $("#submit").text("Configure and Deploy");
    if (result.Config) {
      $("#submit").val("Update Configuration");
      $("#submit").text("Update Configuration");
      $("#selectSubscription")[0].sumo.selectItem(result.Config.subscription);
      bindExistingConfig(result.Config, result.ResourceGroups)
    }
    else {
      getResourceGroups(result.SubcriptionList[0].id);
    }
  });

  request.fail(function (jqXHR, textStatus) {
    alert("Request failed: " + textStatus);
  });
}

function getResourceGroups(subscription) {
  var tenantId = $.find("#tenant-id")[0].value;
  var clientId = $.find("#client-id")[0].value;
  var clientSecret = $.find("#client-secret")[0].value;
  var request = $.ajax({
    url: "api/FaultInjection/getresourcegroups",
    type: "GET",
    data: { tenantId: tenantId, clientId: clientId, clientSecret: clientSecret, subscription: subscription }
  });
  request.done(function (result) {
    if (!result) {
      console.log("resource group list is empty");
      return;
    }
    console.log("InItlize Multi list");

    bindOptions($('#excluded-resource-groups'), result);
    bindOptions($('#included-resource-groups'), result);
    $('#excluded-resource-groups').SumoSelect({ selectAll: true });
    $('#included-resource-groups').SumoSelect({ selectAll: true });
  });

  request.fail(function (jqXHR, textStatus) {
    alert("Request failed: " + textStatus);
  });
}

function bindExistingConfig(model, resourceGroups) {
  if (resourceGroups) {
    bindOptions($('#excluded-resource-groups'), resourceGroups);
    $('#excluded-resource-groups').SumoSelect({ selectAll: true });
    $('#azure-fault-injection-actions').SumoSelect({ selectAll: true });
  }

  if (model) {
    selectItem(model.SubcriptionList, '#selectSubscription');
    selectItem(model.excludedResourceGroups, '#excluded-resource-groups');
    selectItem(model.azureFiActions, '#azure-fault-injection-actions');
    $("#vm-percentage")[0].value = model.vmPercentage;
    $("#vm-enabled")[0].checked = model.isVmEnabled;
    $("#avset-enabled")[0].checked = model.isAvSetEnabled;
    $("#isFaultDomainEnabled")[0].checked = model.isFaultDomainEnabled;
    $("#isUpdateDomainEnabled")[0].checked = model.isUpdateDomainEnabled;
    $("#avzone-enabled")[0].checked = model.isAvZoneEnabled;
    $("#vmss-percentage")[0].value = model.vmssPercentage;
    $("#vmss-enabled")[0].checked = model.isVmssEnabled;
    $("#scheduler-frequency")[0].value = model.schedulerFrequency;
    $("#rollback-frequency")[0].value = model.rollbackFrequency;
    $("#crawler-frequency")[0].value = model.crawlerFrequency;
    $("#mean-time")[0].value = model.meanTime;
    $("#chaos-enabled")[0].checked = model.isChaosEnabled;
  }
}

function selectItem(needsToBeSelected, selector) {
  if (selector) {
    $.each(needsToBeSelected, function (index, value) {
      $(selector)[0].sumo.selectItem(value);
    });
  }
}


function bindOptions($element, result) {
  $element.empty();
  $.each(result, function (index, item) {
    $element.append(
      $('<option/>', {
        value: item.id,
        text: item.displayName ? item.displayName : item.name
      })
    );
  });
}