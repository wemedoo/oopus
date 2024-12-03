﻿function newEntity() {
    window.location.href = `/SmartOncology/ProgressNote`;
}

function editEntity(event, id) {
    event.preventDefault();
    window.location.href = `/SmartOncology/ProgressNote?schemaInstanceId=${id}`;
}

function viewEntity(event, id) {
    event.preventDefault();
    window.location.href = `/SmartOncology/ProgressNote?schemaInstanceId=${id}`;
}

function deleteEntity(event) {
    event.preventDefault();
    event.stopPropagation();

    var id = document.getElementById("buttonSubmitDelete").getAttribute('data-id')

    $.ajax({
        type: "DELETE",
        url: `/SmartOncology/DeleteSchemaInstance/${id}`,
        success: function (data) {
            toastr.success(`Success`);
            $(`#row-${id}`).remove();
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function mainFilter() {
    $('#indication').val($('#indicationTemp').val());
    $('#stateCD').val($('#stateCDTemp').val());
    $('#name').val($('#nameTemp').val());
    filterData();
}

function advanceFilter() {
    $('#indicationTemp').val($('#indication').val());
    $('#stateCDTemp').val($('#stateCD').val());
    $('#nameTemp').val($('#name').val());
    filterData();
}

function reloadTable() {
    hideAdvancedFilterModal();
    setFilterFromUrl();
    let requestObject = getFilterParametersObject();
    setFilterTagsFromObj(requestObject);
    setAdvancedFilterBtnStyle(requestObject, ['Indication', 'StateCD', 'Name', 'patientId', 'page', 'pageSize']);
    checkUrlPageParams();
    setTableProperties(requestObject, {doOrdering: false})

    if (!requestObject.Page) {
        requestObject.Page = 1;
    }

    $.ajax({
        type: 'GET',
        url: '/SmartOncology/ReloadSchemaInstances',
        data: requestObject,
        success: function (data) {
            $("#tableContainer").html(data);
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function getFilterParametersObject() {
    let result = {};
    var state = $("#stateCD").val();
    var clinicalConstelation = $("#clinicalConstelation").val();
    var name = $("#name").val();
    var patient = $("#patient").val();
    var createdBy = $("#createdBy").val();

    if (defaultFilter) {
        result = getDefaultFilter();
        defaultFilter = null;
    }
    else {
        addPropertyToObject(result, 'StateCD', state);
        addPropertyToObject(result, 'ClinicalConstelation', clinicalConstelation);
        addPropertyToObject(result, 'Name', name);
        addPropertyToObject(result, 'patientId', patient);
        addPropertyToObject(result, 'CreatedBy', createdBy);
    }

    return result;
}

function getFilterParametersObjectForDisplay(filterObject) {
    delete filterObject.patientId;

    return filterObject;
}