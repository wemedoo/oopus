﻿var isInvalidCodeThesaurus = false;
var isValidFromOrToChanged = false;

$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
    triggerTimeOnChange("#idCodes");
});

addUnsavedChangesEventHandler("#codeSetsForm");

function reloadCodeSetGroup(reloadCode) {
    var isReadOnly = $('#isReadOnly').val() === 'true' ? true : false; 
    var canChangeCodeSet = document.getElementById("thesaurusIdCode") == null && !isReadOnly ? true : false;
    $.ajax({
        type: 'GET',
        url: `/Code/ReloadCodeSetGroup?CodeSetId=${$('#codeSetId').val()}&CanChangeCodeSet=${canChangeCodeSet}`,
        success: function (data) {
            $("#codeSetsGroupId").html(data);
            if (reloadCode)
                reloadTable();
            saveInitialCodeFormData("#idCodes");
        },
        error: function (xhr, textStatus, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function createCodeEntry() {
    activeThesaurus = 0;
    var codeSetDisplay = encodeURIComponent($('#codeSetDisplay').val());
    window.location.href = `/Code/Create?CodeSetId=${$('#codeSetId').val()}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`;
}

function cancelCode() {
    var codeSetDisplay = encodeURIComponent($('#thesaurusSearchInputCode').val());
    if (!compareForms("#idCodes")) {
        saveInitialFormData("#idCodes");
        window.location.href = `/Code/GetAll?CodeSetId=${$('#newCodeSetNumberForCode').val()}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`;
    } else {
        window.location.href = `/Code/GetAll?CodeSetId=${$('#newCodeSetNumberForCode').val()}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`;
    }
}

function populateCodeValueName(thesaurusId, preferredTerm) {
    $('.designer-form-modal').removeClass('show');
    $('body').removeClass('no-scrollable');
    document.getElementById("codeValueDisplay").value = preferredTerm;
    $("#codeValueDisplay").attr('data-value', thesaurusId);
    if ($('#idCodes').valid()) { }
}

$(document).on('keyup', '#codeValueDisplay', function (e) {
    if (e.which !== enter) {
        document.getElementById("codeValueDisplay").setAttribute('value', null);
        isInvalidCodeThesaurus = true;
    }
});

function submitCodeForm(isSaveAndClose, isEdit) {
    $('#idCodes').validate();
    if ($('#idCodes').valid()) {
        if (isEdit && $("#codeValueDisplay").data("value") == null)
            $("#codeValueDisplay").attr('data-value', $("#thesaurusIdCode").val());

        var request = {};
        let codeId = $("#codeValue").val();
        let action = codeId != "" ? 'Edit' : 'Create';
        thesaurusId = $("#codeValueDisplay").data("value");
        if (codeSetId == null)
            codeSetId = $("#codeSetNumberForCode").val();
        request['ThesaurusEntryId'] = thesaurusId;
        request['CodeSetId'] = codeSetId;
        request['Id'] = codeId;
        request['ActiveFrom'] = calculateDateTimeWithOffset("#activeFromDate", "#activeFromTime");
        request['ActiveTo'] = calculateDateTimeWithOffset("#activeToDate", "#activeToTime");
        var codeSetDisplay = encodeURIComponent($('#thesaurusSearchInputCode').val());
        var codeDisplay = encodeURIComponent($('#codeValueDisplay').val());

        $.ajax({
            type: "POST",
            url: `/Code/${action}`,
            data: request,
            success: function (data, textStatus, jqXHR) {
                toastr.options = {
                    timeOut: 100
                }
                saveInitialCodeFormData("#idCodes");
                if (isSaveAndClose)
                    toastr.options.onHidden = function () { window.location.href = `/Code/GetAll?CodeSetId=${codeSetId}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`; }
                else {
                    toastr.options.onHidden = function () { window.location.href = `/Code/Edit?CodeId=${data.codeId}&CodeDisplay=${encodeURIComponent(codeDisplay)}&CodeSetId=${codeSetId}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`; }
                }
                toastr.success("Success");
            },
            error: function (xhr, ajaxOptions, thrownError) {
                handleResponseError(xhr);
            }
        });
    }
}

function isValidCodeThesaurus() {
    resetValidation($("#idCodes"));
    if (document.getElementById("codeValueDisplay").getAttribute("value") != null && !isInvalidCodeThesaurus)
        return false;
    return true;
}

$(document).ready(function () {
    jQuery.validator.addMethod("duplicate", function (value, element) {
        return !isValidCodeThesaurus();
    }, "Please select thesaurus entry!");

    jQuery.validator.addMethod("validateCodeActiveFromTo", function (value, element) {
        return compareActiveDateTime("activeFromDate", "activeToDate", "activeFromTime", "activeToTime", "activeToTimeWrapper");
    }, "Active From shouldn't be greater than Active To!");

    jQuery.validator.addMethod("dateInputValidation", function (value, element) {
        return validateDateInput($(element));
    }, `Please put your date in [${getDateFormatDisplay()}] format.`);

    $("#idCodes").validate({
        onkeyup: false,
        rules: {
            CodeValueDisplay: {
                required: true,
                duplicate: true
            },
            activeToDate: {
                validateCodeActiveFromTo: true,
                dateInputValidation: true
            },
            activeFromDate: {
                dateInputValidation: true
            },
        },

    });
});

$("#activeToDate, #activeFromDate").on('change', function () {
    $("#idCodes").validate().element("#activeToDate");
    $("#idCodes").validate().element("#activeFromDate");
});

$(document).on('click', '.search-code-button', function (e) {
    editCodeEntry(e, $('#codeValueDisplay').val(), $('#codeSetNumberForCode').val(), $('#codeThesaurusId').val())
});

function editCodeEntry(e, preferredTerm, id, thesaurusId) {
    $('#thesaurusFilterModal').addClass('show');
    document.getElementById("thesaurusSearchInput").setAttribute("value", preferredTerm);
    codeSetId = id;
    $('#applySearchButton').click();
}

function editCodeValues(e, id, codeDisplay) {
    codeDisplay = encodeURIComponent(codeDisplay);
    var codeSetDisplay = encodeURIComponent($('#thesaurusSearchInputCode').val());
    var isReadOnly = $('#isReadOnly').val() === 'true' ? true : false;
    if (!isReadOnly && !$(e.target).hasClass('dropdown-button') && !$(e.target).hasClass('fa-bars') && !$(e.target).hasClass('dropdown-item') && !$(e.target).hasClass('dots') && !$(e.target).hasClass('table-more'))
        window.location.href = `/Code/Edit?CodeId=${id}&CodeDisplay=${encodeURIComponent(codeDisplay)}&CodeSetId=${$('#codeSetNumberForCode').val()}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`;
    else if (!isReadOnly && $(e.target).hasClass('editCodeSet'))
        window.location.href = `/Code/Edit?CodeId=${id}&CodeDisplay=${encodeURIComponent(codeDisplay)}&CodeSetId=${$('#codeSetNumberForCode').val()}&CodeSetDisplay=${encodeURIComponent(codeSetDisplay)}`;
}

function showCodeValues(event, element) {
    event.stopPropagation();
    document.getElementById("codeValueTab").classList.add("code-active-tab");
    document.getElementById("aliasesTab").classList.remove("code-active-tab");
    $("#aliasTableContainer").hide();
    $("#showCodeValues").show();
}

function codeSetExists() {
    let result = false;
    if (isChangedCodeSetId) {
        if ($("#newCodeSetNumberForCode").val() == $("#codeSetNumberForCode").val())
            return false;
        $.ajax({
            type: 'GET',
            url: `/CodeSet/ExistCodeSetId?codeSetId=${$('#newCodeSetNumberForCode').val()}`,
            async: false,
            success: function (data) {
                result = data;
            },
            error: function (xhr, textStatus, thrownError) {
                handleResponseError(xhr);
            }
        });
    }
    return result;
}

function changeValidFromOrTo() {
    isValidFromOrToChanged = true;
    $("#codeSetsForm").validate().element("#newCodeSetActiveFromForCode");
    $("#codeSetsForm").validate().element("#newCodeSetActiveToForCode");
    if ($("#codeValueDisplay").data("value") != undefined)
        thesaurusId = $("#codeValueDisplay").data("value");
    else
        thesaurusId = $('#codeThesaurusId').val();
}

function goToCodeSetThesaurus() {
    var codeSetThesaurusId;
    if ($('#thesaurusSearchInputCode').attr('data-value') != null)
        codeSetThesaurusId = $('#thesaurusSearchInputCode').attr('data-value');
    else
        codeSetThesaurusId = $('#codeThesaurusId').val();

    window.open(`/ThesaurusEntry/EditByO4MtId?id=${codeSetThesaurusId}`, '_blank');
}

function goToCodeThesaurus(isEdit) {
    if ($('#codeValueDisplay').attr('data-value') != null || isEdit) {
        var codeThesaurusId;
        if ($('#codeValueDisplay').attr('data-value') != null)
            codeThesaurusId = $('#codeValueDisplay').attr('data-value');
        else
            codeThesaurusId = $('#thesaurusIdCode').val();

        window.open(`/ThesaurusEntry/EditByO4MtId?id=${codeThesaurusId}`, '_blank');
    }
}

$(document).on('keypress', '.code-value-search', function (e) {
    if (e.which === enter) {
        e.preventDefault();
        e.stopPropagation();
        thesaurusFilterModal();
    }
});

var initialCodeFormData;

function saveInitialCodeFormData(form) {
    initialCodeFormData = serializeForm(form);
}

function compareCodeForms(form) {
    var currentFormData = serializeForm(form);
    return initialCodeFormData === currentFormData;
}