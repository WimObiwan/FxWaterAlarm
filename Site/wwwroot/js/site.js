﻿function setCookie(cname, cvalue, exdays) {
    const d = new Date();
    d.setTime(d.getTime() + (exdays*24*60*60*1000));
    let cookie = cname + "=" + cvalue + ";expires="+ d.toUTCString() + ";path=/"
    console.log("Setting cookie: " + cookie)
    document.cookie = cookie;
}

function getCookie(cname) {
    return document.cookie
        .split("; ")
        .find((row) => row.startsWith(cname + "="))
        ?.split("=")[1];
}

function refreshAt(hours, minutes, seconds) {
    var now = new Date();
    var then = new Date();

    if(now.getHours() > hours ||
        (now.getHours() == hours && now.getMinutes() > minutes) ||
        now.getHours() == hours && now.getMinutes() == minutes && now.getSeconds() >= seconds) {
        then.setDate(now.getDate() + 1);
    }
    then.setHours(hours);
    then.setMinutes(minutes);
    then.setSeconds(seconds);

    var timeout = (then.getTime() - now.getTime());
    setTimeout(function() { window.location.reload(true); }, timeout);
}

var clipboardDemos=new ClipboardJS('[data-clipboard]');
clipboardDemos.on('success',function(e)
{
    e.clearSelection();
    var img = e.trigger.querySelector('.bi');
    img.classList.remove("bi-clipboard", "bi-check2");
    img.classList.add("bi-check2");
    setTimeout(() => {
        img.classList.remove("bi-clipboard", "bi-check2");
        img.classList.add("bi-clipboard");
    }, 2000);
});

if ('serviceWorker' in navigator) {
    window.addEventListener('load', function() {
        navigator.serviceWorker.register('/service-worker.js', { scope: '/' })
            .then(function(registration) {
                // Registration was successful
                console.log('ServiceWorker registration successful with scope: ', registration.scope);
            }, function(err) {
                // registration failed :(
                console.log('ServiceWorker registration failed: ', err);
            });

        butInstall.addEventListener('click', async () => {
            console.log('👍', 'butInstall-clicked');
            const promptEvent = window.deferredPrompt;
            if (!promptEvent) {
                // The deferred prompt isn't available.
                return;
            }
            // Show the install prompt.
            promptEvent.prompt();
            // Log the result
            const result = await promptEvent.userChoice;
            console.log('👍', 'userChoice', result);
            // Reset the deferred prompt variable, since
            // prompt() can only be called once.
            window.deferredPrompt = null;
            // Hide the install button.
            installContainer.classList.toggle('hidden', true);
        });
    });

    window.addEventListener('beforeinstallprompt', (event) => {
        console.log('👍', 'beforeinstallprompt', event);
        // Stash the event so it can be triggered later.
        window.deferredPrompt = event;
        // Remove the 'hidden' class from the install button container
        installContainer.classList.toggle('hidden', false);
    });
}

// if are standalone android OR safari
if (window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true) {
    installContainer.classList.toggle('hidden', true);
}