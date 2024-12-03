﻿var organizationCommunicationColumnName;
var organizationCommunicationSwitchCount = 0;
var organizationCommunicationIsAscending = null;
let isSubmitting = false;

addUnsavedChangesEventHandler("#idOrganization");

function clickedSubmit() {
    $('#idOrganization').validate();

    if ($('#idOrganization').valid()) {
        submitOrganizationForm();
    }
    return false;
}

function submitOrganizationForm() {
    if (isSubmitting) {
        return;
    }

    let organizationData = getOrganizationData();
    let action = $("#id").val() != 0 ? 'Edit' : 'Create';
    isSubmitting = true;

    $.ajax({
        type: "POST",
        url: `/Organization/${action}`,
        data: organizationData,
        success: function (data) {
            toastr.options = {
                timeOut: 100
            }

            if ($("#id").val()) {
                setRowVersion(data);
                reloadPartialTelecomOrIdentifier(data.id);
            }
            else {
                toastr.options.onHidden = function () {
                    window.location.href = `/Organization/Edit?organizationId=${data.id}`;
                }
            }
            saveInitialFormData("#idOrganization");
            toastr.success("Success");

        },
        error: function (xhr, ajaxOptions, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function setRowVersion(data) {
    $("#rowVersion").val(data.rowVersion);
    isSubmitting = false;
}

function reloadPartialTelecomOrIdentifier(organizationId) {
    var activeTab = document.querySelector('.organization-tabs .organization-tab.active');
    var dataId = activeTab.getAttribute('data-id');
    if (dataId == "Telecoms")
        reloadTelecomOrIdentifierTable(organizationId, "Telecoms");
    else if (dataId == "Identifiers")
        reloadTelecomOrIdentifierTable(organizationId, "Identifiers");
}

function reloadTelecomOrIdentifierTable(organizationId, partialViewId) {
    $.ajax({
        type: "POST",
        url: `/Organization/GetOrganization${partialViewId}?organizationId=${organizationId}`,
        success: function (data) {
            var divElement = document.getElementById(partialViewId);
            while (divElement.firstChild) {
                divElement.removeChild(divElement.firstChild);
            }
            divElement.innerHTML = data;
        },
        error: function (xhr, ajaxOptions, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function getOrganizationData() {
    var request = {};
    var address = {
        Id: $('#addressId').val(),
        City: $("#city").val(),
        State: $("#state").val(),
        PostalCode: $("#postalCode").val(),
        CountryCD: $("#countryCD").val(),
        Street: $('#street').val()
    };

    request['Type'] = getSelectedTypes();
    request['Id'] = $("#id").val();
    request['Name'] = $("#name").val();

    request['Alias'] = $("#alias").val();
    request['Telecom'] = getTelecoms('OrganizationTelecom');
    request['Identifiers'] = getIdentifiers();
    request['AddressId'] = $('#addressId').val(),
    request['Address'] = address;
    request['ParentId'] = $("#parentId").val();
    request['PrimaryColor'] = $("#primaryColor").val();
    request['SecondaryColor'] = $("#secondaryColor").val();
    request['LogoUrl'] = $("#logoUrl").val();
    request['Email'] = $("#email").val();
    request['RowVersion'] = $("#rowVersion").val();
    request['ClinicalDomains'] = setClinicalDomain(getOrganizationClinicalDomainObject());
    request['Impressum'] = $("#impressum").val();
    var timezoneElement = document.getElementById("timezone");
    var selectedTimezone = timezoneElement.options[timezoneElement.selectedIndex];
    request['TimeZone'] = selectedTimezone.getAttribute("data-timezone");
    request['TimeZoneOffset'] = selectedTimezone.getAttribute("data-offset");

    return request;
}

function getOrganizationClinicalDomainObject() {
    return function (element) {
        return {
            clinicalDomainCD: $(element).attr("data-value"),
            organizationClinicalDomainId: $(element).attr("data-id")
        };

    }
}

function getSelectedTypes() {
    var chkArray = [];

    $('.organization-type:checked').each(function () {
        chkArray.push($(this).val());
    });

    return chkArray;
}

function reloadHierarchy() {
    let parentId = $('#parentId').val() ? $('#parentId').val() : '';
    if ($("#name").val()) {
        $.ajax({
            type: 'GET',

            url: `/Organization/ReloadHierarchy?parentId=${parentId}`,
            success: function (data) {
                let content = $(data);
                let name = $(content).find('#organizationHierarchyActiveName')[0];
                $(name).html(getNameAndCity());
                $("#organizationHierarchyContainer").html($(content));
            },
            error: function (xhr, textStatus, thrownError) {
                handleResponseError(xhr);
            }
        });
    } else {
        $("#organizationHierarchyContainer").html($(appendPlaceholder()));
    }
}

function appendPlaceholder() {
    let element = document.createElement('div');
    $(element).addClass("no-result-content");
    let noResultElement = document.createElement('img');
    var noResultIcon = document.getElementById("notFound").src;
    $(noResultElement).attr("src", noResultIcon);
    let brElement = document.createElement('br');
    let noResult = document.createElement('div');
    $(noResult).addClass("no-result-text");
    var noResultFoundText = noResultFound;
    $(noResult).append(noResultFoundText);
    $(element).append(noResultElement).append(brElement).append(noResult);

    return element;
}

function getNameAndCity() {
    let name = $('#name').val();
    let city = $('#city').val();
    return `${name} ${city ? ', '+ city : ''}`;

}

$('#name').on('blur', function (e) {
    reloadHierarchy();
});

$('#parentId').on('change', function (e) {
    reloadHierarchy();
});

$('#city').on('blur', function (e) {
    reloadHierarchy();
});

function cancelOrganization() {
    if (!compareForms("#idOrganization")) {
        if (confirm("You have unsaved changes. Are you sure you want to cancel?")) {
            saveInitialFormData("#idUserInfo");
            window.location.href = '/Organization/GetAll';
        }
    } else {
        window.location.href = '/Organization/GetAll';
    }
}

$(document).on('click', '#primaryColorInput', function (e) {
    $('#primaryColor').click();
});

$(document).on('change', '#primaryColor', function (e) {
    var color = $('#primaryColor').val();
    $('#colorPrimary').css('background-color', color);
});

$(document).ready(function () {
    var color = $('#primaryColor').val();
    $('#colorPrimary').css('background-color', color);
    var color = $('#secondaryColor').val();
    $('#colorSecondary').css('background-color', color);
    $('#showOrgEntityInactive').prop('checked', false);
    saveInitialFormData("#idOrganization");
    $('#showOrgEntityActive, #showOrgEntityInactive').change(function () {
        reloadOrganizationCommunicationsTable();
    });
});

$(document).on('click', '#secondaryColorInput', function (e) {
    $('#secondaryColor').click();
});

$(document).on('change', '#secondaryColor', function (e) {
    var color = $('#secondaryColor').val();
    $('#colorSecondary').css('background-color', color);
});

function setCountryAutocomplete() {
    $.ajax({
        method: 'get',
        url: `/FormConsensus/GetMapObject`,
        success: function (data) {
            setAutocompleteByData(data);
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function setAutocompleteByData(data) {
    world = JSON.parse(data);
    let countries = topojson.feature(world, world.objects.countries);
    let countriesNames = countries.features.map(function (v) {
        return v.properties.name;
    });

    $("#country").autocomplete({
        source: countriesNames
    });
}

$(document).on("change", "#uploadLogo", function () {
    var fileInput = this;
    if (fileInput.files[0]) {
        var file = fileInput.files[0];
        var type = file.type;
        if (validUploadFormat(type)) {            
            let filesData = [{
                id: $(fileInput).attr('data-id'),
                content: file
            }];
            sendFileData(filesData,
                setResourceName,
                function (resourceName) {
                    let displayedName = getDisplayFileName(resourceName, true);
                    $('.file-name-display')
                        .text(displayedName)
                        .attr('title', displayedName);
                    $('#logo-action-btns').removeClass("d-none");
                },
                getBinaryDomain('organizationLogo'),
                '/Blob/UploadLogo'
            );
            $(fileInput).val('');
        } else {
            removeLogo();
            $(fileInput).closest('.file-field').addClass('error');
            $("#logoUrlError").text("Logo url is invalid, allowed extensions are: jpg, jpeg, bmp, png, svg");
            setTimeout(function () {
                $(fileInput).closest('.file-field').removeClass('error');
                $("#logoUrlError").text("");
            }, 2000);
        }
    }
});

function validUploadFormat(type) {
    var allowedImageFormats = [
        'image/svg+xml',
        'image/png',
        'image/jpeg',
        'image/bmp'
    ];

    return allowedImageFormats.includes(type);
}

function removeLogo() {
    let imageName = $('#logoUrl').val();
    $('.file-name-display')
        .text('')
        .attr('title', '');
    $('#logoUrl').val('');
    $('#uploadLogo').val('');
    $('#logo-action-btns').addClass("d-none");
    deleteExistingBinaryFromServer(imageName, 'organizationLogo');
}

$(document).on("click", ".upload-logo-btn", function () {
    $("#uploadLogo").click();
});

function getOrganizationLogo(event) {
    event.preventDefault();
    let dataGuidName = $('#logoUrl').val();
    downloadResource(event, dataGuidName, getDisplayFileName(dataGuidName, true), 'organizationLogo');
}

//function impressumWordCounter()
$('#impressum').on('keyup', function (e) {
    let charCount = $('#impressum').val().length;

    $('.char-limit-text').html(`${charCount}/600`);
});


$(document).on('click', '.organization-tab', function (e) {

    $('.organization-tab').removeClass('active');

    $(this).addClass('active');
    $('.organization-cont').hide();
    let containerId = $(this).attr("data-id");

    activeContainerId = containerId;
    $(`#${containerId}`).show();

});

function sortOrgCommTable(column) {
    if (organizationCommunicationSwitchCount == 0) {
        if (organizationCommunicationColumnName == column)
            organizationCommunicationIsAscending = checkIfAsc(organizationCommunicationIsAscending);
        else
            organizationCommunicationIsAscending = true;
        organizationCommunicationSwitchCount++;
    }
    else {
        if (organizationCommunicationColumnName != column)
            organizationCommunicationIsAscending = true;
        else
            organizationCommunicationIsAscending = checkIfAsc(organizationCommunicationIsAscending);
        organizationCommunicationSwitchCount--;
    }
    organizationCommunicationColumnName = column;

    reloadOrganizationCommunicationsTable(organizationCommunicationColumnName, organizationCommunicationIsAscending);
}

function addOrganizationCommunicationSortArrows() {
    var element = document.getElementById(organizationCommunicationColumnName);
    if (element != null) {
        element.classList.remove("sort-arrow");
        if (organizationCommunicationIsAscending) {
            element.classList.remove("sort-arrow-desc");
            element.classList.add("sort-arrow-asc");
        }
        else {
            element.classList.remove("sort-arrow-asc");
            element.classList.add("sort-arrow-desc");
        }
    }
}

function reloadOrganizationCommunicationsTable(taskColumnName, taskIsAscending) {
    var request = {};
    request['OrganizationId'] = $("#orgId").val();
    request['IsAscending'] = taskIsAscending;
    request['ColumnName'] = taskColumnName;
    request['ShowActive'] = $("#showOrgEntityActive").is(":checked") ? true : false;
    request['ShowInactive'] = $("#showOrgEntityInactive").is(":checked") ? true : false;

    $.ajax({
        type: 'GET',
        url: '/Organization/ReloadOrgCommunicationTable',
        data: request,
        success: function (data) {
            $("#orgCommunicationTable").html(data);
            addOrganizationCommunicationSortArrows();
        },
        error: function (xhr, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function checkIfAsc(isAscending) {
    if (!isAscending)
        return true;
    else
        return false;
}

function showOrganizationCommunicationModal(event, orgCommunicationEntityId, readOnly) {
    event.stopPropagation();
    $.ajax({
        type: 'GET',
        url: `/Organization/ShowOrganizationCommunicationModal?orgCommunicationEntityId=${orgCommunicationEntityId}&readOnly=${readOnly}`,
        success: function (data) {
            $('#addOrgCommunicationModal').html(data);
            $('#addOrgCommunicationModal').modal('show');
        },
        error: function (xhr, ajaxOptions, thrownError) {
            handleResponseError(xhr, true);
        }
    });
}

function editOrganizationCommunicationModal(e, orgCommunicationEntityId, readOnly) {
    if (!$(e.target).hasClass('dropdown-button') && !$(e.target).hasClass('fa-bars') && !$(e.target).hasClass('dropdown-item') && !$(e.target).hasClass('dots') && !$(e.target).hasClass('table-more')) {
        showOrganizationCommunicationModal(e, orgCommunicationEntityId, readOnly);
    }
}

function addNewOrgCommunication(e) {
    updateDisabledOptions(false);
    $('#newOrgCommunicationForm').validate();
    if ($('#newOrgCommunicationForm').valid()) {

        var request = {};
        request['OrgCommunicationEntityId'] = $("#orgCommunicationEntityId").val();
        request['DisplayName'] = $("#displayName").val();
        request['OrgCommunicationEntityCD'] = $("#orgCommunicationEntity").val();
        request['PrimaryCommunicationSystemCD'] = $("#primaryCommunicationSystem").val();
        request['OrganizationId'] = $("#orgId").val();
        request['ActiveFrom'] = calculateDateWithOffset($("#activeFromDefault").val());
        request['ActiveTo'] = calculateDateWithOffset($("#activeToDefault").val());

        $.ajax({
            type: 'POST',
            url: `/Organization/CreateOrganizationCommunication`,
            data: request,
            success: function (data, jqXHR) {
                $('#addOrgCommunicationModal').modal('hide');
                reloadOrganizationCommunicationsTable();
                toastr.success("Success");
            },
            error: function (xhr, thrownError) {
                handleResponseError(xhr);
            }
        });
    }
}

function setParentIdAndReturn(identifierEntity) {
    identifierEntity["organizationId"] = getParentId();
    return identifierEntity;
}

function getParentId() {
    return $("#orgId").val();
}

function submitParentForm() {
    return submitOrganizationForm();
}