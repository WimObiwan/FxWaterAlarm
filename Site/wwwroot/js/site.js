// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
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
