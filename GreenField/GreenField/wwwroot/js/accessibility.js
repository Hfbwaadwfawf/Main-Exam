(function () {
    const root = document.documentElement;
    let speaking = false;
    let fontSize = parseFloat(localStorage.getItem('a11y-font-size')) || 16;

    root.style.fontSize = fontSize + 'px';
    if (localStorage.getItem('a11y-contrast') === '1') root.classList.add('high-contrast');
    if (localStorage.getItem('a11y-dark') === '1') root.classList.add('dark-mode');

    window.a11y = {

        changeFontSize(delta) {
            fontSize = Math.min(Math.max(fontSize + delta, 12), 26);
            root.style.fontSize = fontSize + 'px';
            localStorage.setItem('a11y-font-size', fontSize);
        },

        toggleDark() {
            const on = root.classList.toggle('dark-mode');
            localStorage.setItem('a11y-dark', on ? '1' : '0');
            updateButtons();
        },

        toggleContrast() {
            const on = root.classList.toggle('high-contrast');
            localStorage.setItem('a11y-contrast', on ? '1' : '0');
            updateButtons();
        },

        toggleReadAloud() {
            if (!window.speechSynthesis) return;
            if (speaking) {
                speechSynthesis.cancel();
                speaking = false;
                updateButtons();
                return;
            }
            const text = document.querySelector('main')?.innerText?.trim();
            if (!text) return;
            const utt = new SpeechSynthesisUtterance(text);
            utt.lang = 'en-GB';
            utt.rate = 0.95;
            utt.onend = utt.onerror = () => { speaking = false; updateButtons(); };
            speechSynthesis.speak(utt);
            speaking = true;
            updateButtons();
        },

        togglePanel() {
            const panel = document.getElementById('a11y-panel');
            const trigger = document.getElementById('a11y-trigger');
            if (!panel) return;
            const open = panel.classList.toggle('a11y-panel-open');
            trigger?.setAttribute('aria-expanded', open);
        },

        resetAll() {
            speechSynthesis?.cancel();
            speaking = false;
            fontSize = 16;
            root.style.fontSize = '16px';
            root.classList.remove('high-contrast', 'dark-mode');
            localStorage.removeItem('a11y-font-size');
            localStorage.removeItem('a11y-contrast');
            localStorage.removeItem('a11y-dark');
            updateButtons();
        }
    };

    function updateButtons() {
        const contrast = root.classList.contains('high-contrast');
        const dark = root.classList.contains('dark-mode');

        document.getElementById('a11y-btn-contrast')?.classList.toggle('a11y-btn-on', contrast);
        document.getElementById('a11y-btn-dark')?.classList.toggle('a11y-btn-on', dark);

        const btnRead = document.getElementById('a11y-btn-read');
        if (btnRead) {
            btnRead.classList.toggle('a11y-btn-on', speaking);
            btnRead.innerHTML = speaking
                ? '<i class="bi bi-stop-circle"></i> Stop reading'
                : '<i class="bi bi-volume-up"></i> Read aloud';
        }
    }

    
    document.addEventListener('click', e => {
        const widget = document.getElementById('a11y-widget');
        if (widget && !widget.contains(e.target)) {
            document.getElementById('a11y-panel')?.classList.remove('a11y-panel-open');
            document.getElementById('a11y-trigger')?.setAttribute('aria-expanded', 'false');
        }
    });

    
    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', updateButtons);
    else
        updateButtons();

})();