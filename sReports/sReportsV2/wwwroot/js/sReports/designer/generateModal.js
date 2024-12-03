
function showGenerateModal(e, formId) {
    executeEventFunctions(e, true);
    $.ajax({
        type: "GET",
        url: "/Form/GetGenerateNewLanguage",
        data: { formId },
        success: function (data) {
            $('#generateModal')
                .html(data)
                .modal('show');
        },
        error: function (xhr, ajaxOptions, thrownError) {
            handleResponseError(xhr);
        }
    });
}

function generateNewLanguage() {
    var request = {
        formId: $('#generatedFormId').val(),
        language: $('#language').val()
    };

    $.ajax({
        type: "POST",
        url: "/Form/GenerateNewLanguage",
        data: request,
        success: function (data) {
            toastr.success(data.message);
            $('#generateModal').modal('hide');
        },
        error: function (xhr, ajaxOptions, thrownError) {
            handleResponseError(xhr);
        }
    });

    return false;
}