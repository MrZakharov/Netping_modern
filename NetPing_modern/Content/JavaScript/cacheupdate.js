﻿var taskNames = [
    "PushAll",
    "GenerateYml"
    
];

$(document).ready(function () {

    updateInvoke(0);
})

function updateInvoke(itemId) {
    operationName = taskNames[itemId];
    $('#update-messages').append('<p id="' + operationName + '">Task ' + operationName
                       + ' is processing <img style="margin: 0 10px;vertical-align: middle;" height=16 src="/Content/Images/preloader.gif" /></p>');
    $.ajax({
        type: "POST",
        url: "/innerpages/ucacheasyncwork",
        data: JSON.stringify({ dataName: operationName }),
        dataType: 'json',
        processData: true,
        traditional: true,
        contentType: 'application/json; charset=utf-8',
        async: true,
        success: function (response) {
            $('#' + operationName).text('Task ' + operationName
            + " completed with message: " + response);
            if (response == "OK" && ++itemId < taskNames.length)
                updateInvoke(itemId);
            else
            {
                document.title += "(completed)";
                $('#update-messages').append('<h3>Cache updated</h3>');
            }

        },
        error: function (response) {
            $('#' + operationName).text('Task ' + operationName
                          + " completed with message: " + response);
            console.log('Task ' + operationName + " completed with message: ");
            console.log(response);
        },
    });
}