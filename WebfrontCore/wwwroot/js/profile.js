﻿// keeps track of how many events have been displayed
let count = 1;

$(document).ready(function () {

    if (typeof clientInfo === 'undefined') {
        return false;
    }

	/*
	Expand alias tab if they have any
	*/
    $('#profile_aliases_btn').click(function (e) {
        const aliases = $('#profile_aliases').text().trim();
        if (aliases && aliases.length !== 0) {
            $('#profile_aliases').slideToggle(150);
            $(this).toggleClass('oi-caret-top');
        }
    });

    /* 
    load the initial 40 events
    */
    $.each(clientInfo.Meta, function (index, meta) {
        if (meta.key.includes("Event")) {
            loadMeta(meta);
            if (count % 40 === 0) {
                count++;
                return false;
            }
            count++;
        }
    });

    /*
    load additional events on scroll
    */
    $(window).scroll(function () {
        if ($(window).scrollTop() === $(document).height() - $(window).height() || $(document).height() === $(window).height()) {
            while (count % 40 !== 0 && count < clientInfo.Meta.length) {
                loadMeta(clientInfo.Meta[count - 1]);
                count++;
            }
            count++;
        }
    });

    /*
    load meta thats not an event
    */
    $.each(clientInfo.Meta, function (index, meta) {
        if (!meta.key.includes("Event")) {
            let metaString = `<div class="profile-meta-entry"><span class="profile-meta-value text-primary">${meta.value}</span><span class="profile-meta-title text-muted"> ${meta.key}</span></div>`;
            $("#profile_meta").append(metaString);
        }
    });

    /*
     * load context of chat 
     */
    $(document).on('click', '.client-message', function (e) {
        showLoader();
        const location = $(this);
        $.get('/Stats/GetMessageAsync', {
            'serverId': $(this).data('serverid'),
            'when': $(this).data('when')
        })
        .done(function (response) {
            $('.client-message-context').remove();
            location.after(response);
            hideLoader();
        })
        .fail(function (jqxhr, textStatus, error) {
            errorLoader();
        });
    });

    /*
 * load info on ban/flag
 */
    $(document).on('click', '.automated-penalty-info-detailed', function (e) {
        showLoader();
        const location = $(this).parent();
        $.get('/Stats/GetAutomatedPenaltyInfoAsync', {
            'clientId': $(this).data('clientid'),
        })
            .done(function (response) {
                $('.penalty-info-context').remove();
                location.after(response);
                hideLoader();
            })
            .fail(function (jqxhr, textStatus, error) {
                errorLoader();
            });
    });

    /*
     get ip geolocation info into modal
     */
    $('.ip-locate-link').click(function (e) {
        e.preventDefault();
        const ip = $(this).data("ip");
        $.getJSON('https://extreme-ip-lookup.com/json/' + ip)
            .done(function (response) {
                $('#mainModal .modal-title').text(ip);
                $('#mainModal .modal-body').text("");
                if (response.ipName.length > 0) {
                    $('#mainModal .modal-body').append("Hostname &mdash; " + response.ipName + '<br/>');
                }
                if (response.isp.length > 0) {
                    $('#mainModal .modal-body').append("ISP &mdash; " + response.isp + '<br/>');
                }
                if (response.org.length > 0) {
                    $('#mainModal .modal-body').append("Organization &mdash; " + response.org + '<br/>');
                }
                if (response['businessName'].length > 0) {
                    $('#mainModal .modal-body').append("Business &mdash; " + response.businessName + '<br/>');
                }
                if (response['businessWebsite'].length > 0) {
                    $('#mainModal .modal-body').append("Website &mdash; " + response.businessWebsite + '<br/>');
                }
                if (response.city.length > 0 || response.region.length > 0 || response.country.length > 0) {
                    $('#mainModal .modal-body').append("Location &mdash; ");
                }
                if (response.city.length > 0) {
                    $('#mainModal .modal-body').append(response.city);
                }
                if (response.region.length > 0) {
                    $('#mainModal .modal-body').append(', ' + response.region);
                }
                if (response.country.length > 0) {
                    $('#mainModal .modal-body').append(', ' + response.country);
                }

                $('#mainModal').modal();
            })
            .fail(function (jqxhr, textStatus, error) {
                $('#mainModal .modal-title').text("Error");
                $('#mainModal .modal-body').html('<span class="text-danger">&mdash;' + error + '</span>');
                $('#mainModal').modal();
            });
    });
});

function penaltyToName(penaltyName) {
    switch (penaltyName) {
        case "Flag":
            return "Flagged";
        case "Warning":
            return "Warned";
        case "Report":
            return "Reported";
        case "Ban":
            return "Banned";
        case "Kick":
            return "Kicked";
        case "TempBan":
            return "Temp Banned";
        case "Unban":
            return "Unbanned";
    }
}

function shouldIncludePlural(num) {
    return num > 1 ? 's' : '';
}

let mostRecentDate = 0;
let currentStepAmount = 0;
let lastStep = '';
function timeStep(stepDifference) {
    let hours = stepDifference / (1000 * 60 * 60);
    let days = stepDifference / (1000 * 60 * 60 * 24);
    let weeks = stepDifference / (1000 * 60 * 60 * 24 * 7);

    if (Math.round(weeks) > Math.round(currentStepAmount / 24 * 7)) {
        currentStepAmount = Math.round(weeks);
        return `${currentStepAmount} week${shouldIncludePlural(currentStepAmount)} ago`;
    }

    if (Math.round(days) > Math.round(currentStepAmount / 24)) {
        currentStepAmount = Math.round(days);
        return `${currentStepAmount} day${shouldIncludePlural(currentStepAmount)} ago`;
    }

    if (Math.round(hours) > currentStepAmount) {
        currentStepAmount = Math.round(hours);
        return `${currentStepAmount} hour${shouldIncludePlural(currentStepAmount)} ago`;
    }
}

function loadMeta(meta) {
    let eventString = '';
    const metaDate = moment.utc(meta.when).valueOf();

    if (mostRecentDate === 0) {
        mostRecentDate = metaDate;
    }

    const step = timeStep(moment.utc().valueOf() - metaDate);

    if (step !== lastStep && step !== undefined && metaDate > 0) {
        $('#profile_events').append('<span class="p2 text-white profile-event-timestep"><span class="text-primary">&mdash;</span> ' + step + '</span>');
        lastStep = step;
    }

    // it's a penalty
    if (meta.class.includes("Penalty")) {
        if (meta.value.punisherId !== clientInfo.clientId) {
            const timeRemaining = meta.value.type === 'TempBan' && meta.value.timeRemaining.length > 0 ?
                `(${meta.value.timeRemaining} remaining)` :
                '';
            eventString = `<div><span class="penalties-color-${meta.value.type.toLowerCase()}">${penaltyToName(meta.value.type)}</span> by <span class="text-highlight"> <a class="link-inverse"  href="${meta.value.punisherId}">${meta.value.punisherName}</a></span > for <span style="color: white; " class="automated-penalty-info-detailed" data-clientid="${meta.value.offenderId}">${meta.value.offense}</span><span class="text-muted"> ${timeRemaining}</span></div>`;
        }
        else {
            eventString = `<div><span class="penalties-color-${meta.value.type.toLowerCase()}">${penaltyToName(meta.value.type)} </span> <span class="text-highlight"><a class="link-inverse" href="${meta.value.offenderId}"> ${meta.value.offenderName}</a></span > for <span style="color: white;" class="automated-penalty-info-detailed" data-clientid="${meta.value.offenderId}">${meta.value.offense}</span></div>`;
        }
    }
    else if (meta.key.includes("Alias")) {
        eventString = `<div><span class="text-success">${meta.value}</span></div>`;
    }
    // it's a message
    else if (meta.key.includes("Event")) {
        eventString = `<div><span style="color:white;">></span><span class="client-message text-muted" data-serverid="${meta.extra}" data-when="${meta.when}"> ${meta.value}</span></div>`;
    }
    $('#profile_events').append(eventString);
}
