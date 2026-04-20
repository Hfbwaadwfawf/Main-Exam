(function () {
    'use strict';

    const KEYS = {
        theme: 'glh-theme',
        contrast: 'glh-contrast',
        fontSize: 'glh-fontsize',
    };

    // ============================================================
    // 1. THEME
    // ============================================================

    function applyTheme(theme) {
        requestAnimationFrame(() => {
            document.documentElement.setAttribute('data-theme', theme);
        });
        localStorage.setItem(KEYS.theme, theme);

        const toggle = document.getElementById('themeToggle');
        if (toggle) toggle.classList.toggle('active', theme === 'dark');
    }

    function toggleTheme() {
        const current = localStorage.getItem(KEYS.theme) || 'light';
        applyTheme(current === 'dark' ? 'light' : 'dark');
    }

    // ============================================================
    // 2. HIGH CONTRAST
    // ============================================================

    function applyContrast(mode) {
        document.body.classList.toggle('high-contrast', mode === 'high');
        localStorage.setItem(KEYS.contrast, mode);
        const btn = document.getElementById('contrastBtn');
        if (btn) btn.textContent = mode === 'high' ? 'Disable High Contrast' : 'Enable High Contrast';
    }

    function toggleHighContrast() {
        const current = localStorage.getItem(KEYS.contrast) || 'normal';
        applyContrast(current === 'high' ? 'normal' : 'high');
    }

    // ============================================================
    // 3. FONT SIZE
    // ============================================================

    let currentSize = parseInt(localStorage.getItem(KEYS.fontSize) || '15');

    function changeTextSize(direction) {
        if (direction === 0) {
            currentSize = 15;
        } else {
            currentSize = Math.min(24, Math.max(12, currentSize + (direction * 2)));
        }
        document.documentElement.style.fontSize = currentSize + 'px';
        localStorage.setItem(KEYS.fontSize, currentSize);
    }

    // ============================================================
    // 4. REDUCE MOTION
    // ============================================================

    function toggleReduceMotion() {
        const active = document.body.classList.toggle('reduce-motion');
        const btn = document.getElementById('motionBtn');
        if (btn) btn.textContent = active ? 'Disable Reduce Motion' : 'Enable Reduce Motion';
        localStorage.setItem('glh-motion', active ? 'reduced' : 'normal');
    }

    // ============================================================
    // 5. DISABLE VISUAL EFFECTS
    // ============================================================

    function toggleEffects() {
        const active = document.body.classList.toggle('no-effects');
        const btn = document.getElementById('effectsBtn');
        if (btn) btn.textContent = active ? 'Enable Glassmorphism' : 'Disable Glassmorphism';
        localStorage.setItem('glh-effects', active ? 'off' : 'on');
    }

    // ============================================================
    // 6. TEXT TO SPEECH
    // ============================================================

    function getTTSText() {
        const selection = window.getSelection().toString().trim();
        if (selection.length > 0) return selection;
        const main = document.querySelector('main');
        if (main) return main.innerText.replace(/\s+/g, ' ').trim().substring(0, 5000);
        return document.title;
    }

    function startTTS() {
        if (!window.speechSynthesis) { alert('Text-to-speech not supported in this browser.'); return; }
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(getTTSText());
        utterance.rate = 0.95;
        utterance.pitch = 1;
        utterance.lang = 'en-GB';
        const voices = window.speechSynthesis.getVoices();
        const preferred = voices.find(v => v.lang.startsWith('en') && (v.name.includes('Google') || v.name.includes('UK'))) || voices.find(v => v.lang.startsWith('en'));
        if (preferred) utterance.voice = preferred;
        window.speechSynthesis.speak(utterance);
    }

    function stopTTS() {
        if (window.speechSynthesis) window.speechSynthesis.cancel();
    }

    // ============================================================
    // 7. ACCESSIBILITY PANEL TOGGLE
    // ============================================================

    function toggleAccessibilityPanel() {
        const panel = document.getElementById('accessibilityPanel');
        if (!panel) return;
        const isVisible = panel.style.display === 'block';
        panel.style.display = isVisible ? 'none' : 'block';
    }

    document.addEventListener('click', function (e) {
        const panel = document.getElementById('accessibilityPanel');
        const toggle = document.getElementById('accessibilityToggle');
        if (panel && toggle && !panel.contains(e.target) && !toggle.contains(e.target)) {
            panel.style.display = 'none';
        }
    });

    // ============================================================
    // 8. INIT
    // ============================================================

    function init() {
        const savedTheme = localStorage.getItem(KEYS.theme) || 'light';
        applyTheme(savedTheme);

        const savedContrast = localStorage.getItem(KEYS.contrast) || 'normal';
        if (savedContrast === 'high') applyContrast('high');

        const savedSize = localStorage.getItem(KEYS.fontSize);
        if (savedSize) {
            currentSize = parseInt(savedSize);
            document.documentElement.style.fontSize = currentSize + 'px';
        }

        if (localStorage.getItem('glh-motion') === 'reduced') {
            document.body.classList.add('reduce-motion');
            const btn = document.getElementById('motionBtn');
            if (btn) btn.textContent = 'Disable Reduce Motion';
        }

        if (localStorage.getItem('glh-effects') === 'off') {
            document.body.classList.add('no-effects');
            const btn = document.getElementById('effectsBtn');
            if (btn) btn.textContent = 'Enable Glassmorphism';
        }

        const themeToggle = document.getElementById('themeToggle');
        if (themeToggle) themeToggle.addEventListener('click', toggleTheme);

        const a11yToggle = document.getElementById('accessibilityToggle');
        if (a11yToggle) a11yToggle.addEventListener('click', toggleAccessibilityPanel);

        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') {
                const panel = document.getElementById('accessibilityPanel');
                if (panel) panel.style.display = 'none';
                stopTTS();
            }
        });

        if (window.speechSynthesis) window.speechSynthesis.onvoiceschanged = () => { };
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.toggleHighContrast = toggleHighContrast;
    window.changeTextSize = changeTextSize;
    window.toggleReduceMotion = toggleReduceMotion;
    window.toggleEffects = toggleEffects;
    window.startTTS = startTTS;
    window.stopTTS = stopTTS;

})();