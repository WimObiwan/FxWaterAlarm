function setCookie(cname, cvalue, exdays) {
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

// The "install as app" banner can be dismissed; remember that for 30 days.
// localStorage rather than a cookie: only the browser needs it, the server never does.
const INSTALL_DISMISSED_KEY = 'installBannerDismissedUntil';
const INSTALL_DISMISS_DAYS = 30;

function isInstallBannerDismissed() {
    try {
        const until = Number(localStorage.getItem(INSTALL_DISMISSED_KEY));
        return until > Date.now();
    } catch {
        return false;
    }
}

function dismissInstallBanner() {
    try {
        localStorage.setItem(INSTALL_DISMISSED_KEY, String(Date.now() + INSTALL_DISMISS_DAYS * 24 * 60 * 60 * 1000));
    } catch {
        // storage blocked: the banner just comes back on the next visit
    }
    installContainer.classList.toggle('hidden', true);
}

if (typeof butInstallDismiss !== 'undefined') {
    butInstallDismiss.addEventListener('click', dismissInstallBanner);
}

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
        if (!isInstallBannerDismissed()) {
            installContainer.classList.toggle('hidden', false);
        }
    });
}

// if are standalone android OR safari
if (window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true) {
    installContainer.classList.toggle('hidden', true);
}

// Dark mode functionality
function getStoredTheme() {
    return localStorage.getItem('theme');
}

function setStoredTheme(theme) {
    localStorage.setItem('theme', theme);
}

function getPreferredTheme() {
    const storedTheme = getStoredTheme();
    if (storedTheme) {
        return storedTheme;
    }
    return 'light'; // Default to light mode as required
}

function setTheme(theme) {
    if (theme === 'auto') {
        document.documentElement.setAttribute('data-bs-theme', window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
}

function showActiveTheme(theme) {
    const activeThemeIcon = document.querySelector('.theme-icon-active');
    const btnToActive = document.querySelector(`[data-bs-theme-value="${theme}"]`);
    const iconOfActiveBtn = btnToActive ? btnToActive.querySelector('i') : null;

    document.querySelectorAll('[data-bs-theme-value]').forEach(element => {
        element.classList.remove('active');
    });

    if (btnToActive) {
        btnToActive.classList.add('active');
    }

    if (activeThemeIcon && iconOfActiveBtn) {
        activeThemeIcon.className = `theme-icon-active ${iconOfActiveBtn.className.replace(/\s*me-2$/, '')}`;
    }
}

// Initialize theme on page load
window.addEventListener('DOMContentLoaded', () => {
    const theme = getPreferredTheme();
    setTheme(theme);
    showActiveTheme(theme);

    document.querySelectorAll('[data-bs-theme-value]').forEach(toggle => {
        toggle.addEventListener('click', () => {
            const theme = toggle.getAttribute('data-bs-theme-value');
            setStoredTheme(theme);
            setTheme(theme);
            showActiveTheme(theme);
        });
    });
});

// Listen for system theme changes when in auto mode
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    const storedTheme = getStoredTheme();
    if (storedTheme !== 'light' && storedTheme !== 'dark') {
        setTheme(getPreferredTheme());
    }
});

// ---------------------------------------------------------------------------
// Docs images: a drawing scaled down to fit a phone is too small to read, so
// tapping one opens it in an overlay with zoom buttons.  Pinch, drag and
// double-tap work there too; the drawings are SVG, so they stay sharp at any
// zoom level.  Every image also stays wrapped in a plain link to itself, so
// "open in new tab" and a JS-less browser keep working.
// ---------------------------------------------------------------------------
(function () {
    const MIN_SCALE = 1;
    const MAX_SCALE = 10;

    let viewer, stage, image, level, openLink, lastFocus;
    let scale = 1, tx = 0, ty = 0;
    const pointers = new Map();
    let pinchDistance = 0, pinchX = 0, pinchY = 0;

    function build() {
        viewer = document.createElement('div');
        viewer.className = 'media-viewer';
        viewer.setAttribute('role', 'dialog');
        viewer.setAttribute('aria-modal', 'true');
        viewer.setAttribute('aria-label', 'Afbeelding bekijken');
        viewer.tabIndex = -1;
        viewer.innerHTML =
            '<div class="media-viewer-bar">' +
            '<button type="button" data-act="out" title="Uitzoomen" aria-label="Uitzoomen"><i class="bi bi-zoom-out"></i></button>' +
            '<span class="media-viewer-level" aria-live="polite">100%</span>' +
            '<button type="button" data-act="in" title="Inzoomen" aria-label="Inzoomen"><i class="bi bi-zoom-in"></i></button>' +
            '<button type="button" data-act="fit" title="Passend maken" aria-label="Passend maken"><i class="bi bi-aspect-ratio"></i></button>' +
            '<a class="media-viewer-open" target="_blank" rel="noopener" title="In een nieuw tabblad openen" aria-label="In een nieuw tabblad openen"><i class="bi bi-box-arrow-up-right"></i></a>' +
            '<button type="button" data-act="close" title="Sluiten (Esc)" aria-label="Sluiten"><i class="bi bi-x-lg"></i></button>' +
            '</div>' +
            '<div class="media-viewer-stage"><img alt=""></div>';
        document.body.appendChild(viewer);

        stage = viewer.querySelector('.media-viewer-stage');
        image = viewer.querySelector('.media-viewer-stage img');
        level = viewer.querySelector('.media-viewer-level');
        openLink = viewer.querySelector('.media-viewer-open');

        viewer.querySelectorAll('[data-act]').forEach(button => {
            button.addEventListener('click', () => {
                const act = button.getAttribute('data-act');
                if (act === 'close') close();
                else if (act === 'fit') fit();
                else zoomAt(act === 'in' ? 1.4 : 1 / 1.4, centreX(), centreY());
            });
        });

        // clicking the backdrop, but not the image itself, closes
        stage.addEventListener('click', event => {
            if (event.target === stage) close();
        });

        stage.addEventListener('wheel', event => {
            event.preventDefault();
            zoomAt(event.deltaY < 0 ? 1.15 : 1 / 1.15, event.clientX, event.clientY);
        }, { passive: false });

        stage.addEventListener('dblclick', event => {
            if (scale > 1.05) fit();
            else zoomAt(2.5, event.clientX, event.clientY);
        });

        stage.addEventListener('pointerdown', event => {
            stage.setPointerCapture(event.pointerId);
            pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
            if (pointers.size === 2) startPinch();
        });

        stage.addEventListener('pointermove', event => {
            const previous = pointers.get(event.pointerId);
            if (!previous) return;
            const dx = event.clientX - previous.x;
            const dy = event.clientY - previous.y;
            pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });

            if (pointers.size === 1) {
                if (scale > 1.001) { tx += dx; ty += dy; apply(); }
            } else if (pointers.size === 2) {
                const [a, b] = [...pointers.values()];
                const distance = Math.hypot(a.x - b.x, a.y - b.y);
                const midX = (a.x + b.x) / 2, midY = (a.y + b.y) / 2;
                if (pinchDistance > 0) {
                    tx += midX - pinchX;
                    ty += midY - pinchY;
                    zoomAt(distance / pinchDistance, midX, midY);
                }
                pinchDistance = distance;
                pinchX = midX;
                pinchY = midY;
            }
        });

        ['pointerup', 'pointercancel', 'pointerleave'].forEach(type => {
            stage.addEventListener(type, event => {
                pointers.delete(event.pointerId);
                if (pointers.size < 2) pinchDistance = 0;
            });
        });

        document.addEventListener('keydown', event => {
            if (!viewer.classList.contains('is-open')) return;
            if (event.key === 'Escape') close();
            else if (event.key === '+' || event.key === '=') zoomAt(1.4, centreX(), centreY());
            else if (event.key === '-') zoomAt(1 / 1.4, centreX(), centreY());
            else if (event.key === '0') fit();
        });

        window.addEventListener('popstate', () => {
            if (viewer.classList.contains('is-open')) close(true);
        });
    }

    function startPinch() {
        const [a, b] = [...pointers.values()];
        pinchDistance = Math.hypot(a.x - b.x, a.y - b.y);
        pinchX = (a.x + b.x) / 2;
        pinchY = (a.y + b.y) / 2;
    }

    function centreX() { const r = stage.getBoundingClientRect(); return r.left + r.width / 2; }
    function centreY() { const r = stage.getBoundingClientRect(); return r.top + r.height / 2; }

    function apply() {
        image.style.transform = 'translate(' + tx + 'px, ' + ty + 'px) scale(' + scale + ')';
        level.textContent = Math.round(scale * 100) + '%';
        stage.classList.toggle('is-zoomed', scale > 1.001);
    }

    // zoom by `factor`, keeping whatever sits under (px, py) in place
    function zoomAt(factor, px, py) {
        const next = Math.min(Math.max(scale * factor, MIN_SCALE), MAX_SCALE);
        const applied = next / scale;
        if (applied === 1) return;
        const rect = image.getBoundingClientRect();
        tx += (px - (rect.left + rect.width / 2)) * (1 - applied);
        ty += (py - (rect.top + rect.height / 2)) * (1 - applied);
        scale = next;
        apply();
    }

    function fit() { scale = 1; tx = 0; ty = 0; apply(); }

    function open(src, alt) {
        if (!viewer) build();
        image.src = src;
        image.alt = alt || '';
        openLink.href = src;
        fit();
        viewer.classList.add('is-open');
        document.body.classList.add('media-viewer-open');
        lastFocus = document.activeElement;
        viewer.focus();   // move focus into the dialog without ringing a button
        // so the back button/gesture closes the overlay instead of leaving the page
        history.pushState({ mediaViewer: true }, '');
    }

    function close(fromPopState) {
        if (!viewer || !viewer.classList.contains('is-open')) return;
        viewer.classList.remove('is-open');
        document.body.classList.remove('media-viewer-open');
        image.removeAttribute('src');
        pointers.clear();
        if (lastFocus && lastFocus.focus) lastFocus.focus();
        if (!fromPopState && history.state && history.state.mediaViewer) history.back();
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('.markdown-body img').forEach(img => {
            let link = img.closest('a');
            if (!link) {
                link = document.createElement('a');
                link.href = img.getAttribute('src');
                link.target = '_blank';
                link.rel = 'noopener';
                link.className = 'media-zoom';
                link.title = 'Klik om te vergroten';
                img.parentNode.insertBefore(link, img);
                link.appendChild(img);
            }
            link.addEventListener('click', event => {
                if (event.metaKey || event.ctrlKey || event.shiftKey || event.button !== 0) return;
                event.preventDefault();
                open(link.href, img.alt);
            });
        });
    });
})();
