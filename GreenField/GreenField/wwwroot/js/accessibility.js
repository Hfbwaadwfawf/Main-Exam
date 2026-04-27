(function () {
    // Get the root HTML element (<html>)
    const root = document.documentElement;

    // Tracks if text-to-speech is currently active
    let speaking = false;

    // Get saved font size from localStorage, default to 16 if none exists
    let fontSize = parseFloat(localStorage.getItem('a11y-font-size')) || 16;

    // Apply the font size to the whole page
    root.style.fontSize = fontSize + 'px';

    // Apply saved high contrast mode if enabled
    if (localStorage.getItem('a11y-contrast') === '1') 
        root.classList.add('high-contrast');

    // Apply saved dark mode if enabled
    if (localStorage.getItem('a11y-dark') === '1') 
        root.classList.add('dark-mode');

    // Create a global accessibility object
    window.a11y = {

        // Increase or decrease font size
        changeFontSize(delta) {
            // Clamp font size between 12px and 26px
            fontSize = Math.min(Math.max(fontSize + delta, 12), 26);

            // Apply new size
            root.style.fontSize = fontSize + 'px';

            // Save preference
            localStorage.setItem('a11y-font-size', fontSize);
        },

        // Toggle dark mode
        toggleDark() {
            // Toggle class and store result
            const on = root.classList.toggle('dark-mode');
            localStorage.setItem('a11y-dark', on ? '1' : '0');

            // Update button states
            updateButtons();
        },

        // Toggle high contrast mode
        toggleContrast() {
            const on = root.classList.toggle('high-contrast');
            localStorage.setItem('a11y-contrast', on ? '1' : '0');

            updateButtons();
        },

        // Toggle text-to-speech (read aloud)
        toggleReadAloud() {
            // Check if browser supports speech synthesis
            if (!window.speechSynthesis) return;

            // If already speaking, stop it
            if (speaking) {
                speechSynthesis.cancel();
                speaking = false;
                updateButtons();
                return;
            }

            // Get all visible text inside <main>
            const text = document.querySelector('main')?.innerText?.trim();

            // If no text found, do nothing
            if (!text) return;

            // Create speech object
            const utt = new SpeechSynthesisUtterance(text);

            // Set voice settings
            utt.lang = 'en-GB';
            utt.rate = 0.95;

            // When speech ends or errors, reset state
            utt.onend = utt.onerror = () => { 
                speaking = false; 
                updateButtons(); 
            };

            // Start speaking
            speechSynthesis.speak(utt);
            speaking = true;

            updateButtons();
        },

        // Open/close accessibility panel
        togglePanel() {
            const panel = document.getElementById('a11y-panel');
            const trigger = document.getElementById('a11y-trigger');

            if (!panel) return;

            // Toggle panel visibility
            const open = panel.classList.toggle('a11y-panel-open');

            // Update accessibility attribute
            trigger?.setAttribute('aria-expanded', open);
        },

        // Reset all accessibility settings
        resetAll() {
            // Stop any speech
            speechSynthesis?.cancel();
            speaking = false;

            // Reset font size
            fontSize = 16;
            root.style.fontSize = '16px';

            // Remove modes
            root.classList.remove('high-contrast', 'dark-mode');

            // Clear saved preferences
            localStorage.removeItem('a11y-font-size');
            localStorage.removeItem('a11y-contrast');
            localStorage.removeItem('a11y-dark');

            updateButtons();
        }
    };

    // Updates button states (UI feedback)
    function updateButtons() {
        const contrast = root.classList.contains('high-contrast');
        const dark = root.classList.contains('dark-mode');

        // Toggle active state for contrast button
        document.getElementById('a11y-btn-contrast')
            ?.classList.toggle('a11y-btn-on', contrast);

        // Toggle active state for dark mode button
        document.getElementById('a11y-btn-dark')
            ?.classList.toggle('a11y-btn-on', dark);

        // Handle read aloud button
        const btnRead = document.getElementById('a11y-btn-read');

        if (btnRead) {
            btnRead.classList.toggle('a11y-btn-on', speaking);

            // Change icon + text depending on state
            btnRead.innerHTML = speaking
                ? '<i class="bi bi-stop-circle"></i> Stop reading'
                : '<i class="bi bi-volume-up"></i> Read aloud';
        }
    }

    // Close panel when clicking outside of it
    document.addEventListener('click', e => {
        const widget = document.getElementById('a11y-widget');

        if (widget && !widget.contains(e.target)) {
            document.getElementById('a11y-panel')
                ?.classList.remove('a11y-panel-open');

            document.getElementById('a11y-trigger')
                ?.setAttribute('aria-expanded', 'false');
        }
    });

    // Ensure buttons are correct when page loads
    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', updateButtons);
    else
        updateButtons();

})();