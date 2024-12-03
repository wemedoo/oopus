﻿
// ----- Table Actions -----

function createTrial() {
    window.location.href = "/ProjectManagement/Create";
}

function editEntity(event, id) {
    window.location.href = `/ProjectManagement/Edit?projectId=${id}`;
    event.preventDefault();
}

function viewEntity(event, id) {
    window.location.href = `/ProjectManagement/View?projectId=${id}`;
    event.preventDefault();
}

function removeProject(event, id) {
    event.preventDefault();
    event.stopPropagation();
    let data = {
        projectId: id
    };
    $.ajax({
        type: "DELETE",
        url: `/ProjectManagement/Delete`,
        data: data,
        success: function (data) {
            toastr.success(`Success`);
            reloadTable();
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function archiveTrial(event, id) {
    event.preventDefault();
    event.stopPropagation();
    let data = {
        clinicalTrialId: id
    };
    $.ajax({
        type: "POST",
        url: `/ProjectManagement/Archive`,
        data: data,
        success: function (data) {
            toastr.success(`Success`);
            reloadTable();
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

// ----- Reload and Filtering -----

function reloadTable() {

    setFilterFromUrl();
    let requestObject = getFilterParametersObject();
    setFilterTagsFromObj(requestObject);
    setTableProperties(requestObject);
    requestObject.ProjectType = $('#projectType').find(':selected').attr('id');

    $.ajax({
        type: 'GET',
        url: '/ProjectManagement/ReloadTable',
        data: requestObject,
        success: function (data) {
            setTableContent(data, "#trialManagementTableContainer");
        },
        error: function (xhr, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function getFilterParametersObject() {
    let result = {};
    if (defaultFilter) {
        result = getDefaultFilter();
        defaultFilter = null;
    }
    else {
        addPropertyToObject(result, 'ProjectName', $('#projectName').val());
        addPropertyToObject(result, 'ProjectType', $('#projectType').val());
    }

    return result;
}

function getFilterParametersObjectForDisplay(filterObject) {
    return filterObject;
}

function advanceFilter() {
    filterData();
}

function mainFilter() {
    advanceFilter();
}