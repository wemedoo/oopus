﻿
function changePage(num, e, personnelTeamId) {
    
    if (personnelTeamId > 0) 
        reloadSinglePersonnelTeamTable(e, personnelTeamId, num);
    else 
        reloadPersonnelTeamTable(num);
}

function reloadPersonnelTeamTable(pageNum = 1, doShowHidePersonnelTeamTable=true) {

    requestObject = createRequestObject(pageNum);

    $.ajax({
        type: 'GET',
        url: '/PersonnelTeam/ReloadTable',
        data: requestObject,
        success: function (data) {
            $("#personnelTeamTableContainer").html(data);
            $('#pageSizeSelector').hide();  // hide paging size selection
            if (doShowHidePersonnelTeamTable) {
                showHidePersonnelTeamTable();
            }
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function createRequestObject(pageNumber=1) {
    requestObject = {}
    requestObject.OrganizationId = $('#organizationId').html();
    requestObject.TeamName = $('#team-name-select2 :selected').text();
    requestObject.TeamType = $('#team-type-select2 :selected').val();
    requestObject.Active = $('input[name="active-radio-btn"]:checked').val();

    requestObject.Page = pageNumber;
    requestObject.PageSize = 5; // fixed

    return requestObject;
}

// Initially setting table during first load
$(document).ready(function () {

    reloadPersonnelTeamTable();
});

// Showing Table and Filters only when there's at least 1 team
function showHidePersonnelTeamTable() {

    $.ajax({
        type: 'GET',
        url: '/PersonnelTeam/CountTeamsPerOrganization',
        data: requestObject,
        success: function (data) {

            let count = 0;
            if (data.count != null)
                count = data.count;

            if (count >= 1) {
                $('#personnel-team-filters-table-container').show();
                $('#no-personnel-team-container').hide();
            }
            else {
                $('#personnel-team-filters-table-container').hide();
                $('#no-personnel-team-container').show();
            }
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });

}

// ---------- Single Care Team Table ----------

function createSinglePersonnelTeamTableRequest(personnelTeamId, pageNum) {
    filter = {};
    filter.PersonnelTeamId = personnelTeamId;
    filter.RelationTypeCD = $('#member-role-select2 :selected').val();
    filter.UserId = $('#member-name-select2 :selected').val();

    filter.Page = pageNum;
    filter.PageSize = 5; //fixed

    return filter;
}

function showSinglePersonnelTeamTable(event, personnelTeamId, pageNum=1, doTogglePersonnelTeamTabs=true) {
    event.preventDefault();

    request = createSinglePersonnelTeamTableRequest(personnelTeamId, pageNum);

    $.ajax({
        type: 'GET',
        data: request,
        url: `/PersonnelTeamRelation/GetSinglePersonnelTeam`,
        success: function (data) {
            $("#single-personnel-team-container").html(data);
            $("#single-personnel-team-container").find('#pageSizeSelector').hide();  // hide paging size selection
            if (doTogglePersonnelTeamTabs) {
                togglePersonnelTeamTabs();
            }
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function reloadSinglePersonnelTeamTable(event, personnelTeamId, pageNum = 1) {
    event.preventDefault();

    request = createSinglePersonnelTeamTableRequest(personnelTeamId, pageNum);

    $.ajax({
        type: 'GET',
        data: request,
        url: `/PersonnelTeamRelation/ReloadSinglePersonnelTeamTable`,
        success: function (data) {
            $("#single-personnel-team-table-container").html(data);
            $("#single-personnel-team-container").find('#pageSizeSelector').hide();  // hide paging size selection
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

// Switching Views between PersonnelTeamTable and SinglePersonnelTeamTable
function togglePersonnelTeamTabs() {
    $('#personnel-team-filters-table-container').toggle();
    $('.organization-tabs').toggle();
    $('#top-identifier-line').toggle();

    $("#single-personnel-team-container").toggle();
}


// triggered by ViewList / CloseList buttons
function toggleTeamMembersList() {
    $('#personnel-team-members-container').toggle();
    $('#view-list-button').toggle();
    $('#close-list-button').toggle();
}