import { mountToaster, toast } from '@yusufceng1z/ding';

// Web Audio API Synthesizer (PWA offline-ready, zero latency, no audio file downloads)
let audioCtx = null;

function getAudioContext() {
    if (!audioCtx && (window.AudioContext || window.webkitAudioContext)) {
        const AudioContextClass = window.AudioContext || window.webkitAudioContext;
        audioCtx = new AudioContextClass();
    }
    if (audioCtx && audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
    return audioCtx;
}

function playSynthesizedChime(type) {
    try {
        const soundEnabled = localStorage.getItem('chronon_sound_enabled') !== 'false';
        if (!soundEnabled) return;

        const ctx = getAudioContext();
        if (!ctx) return;

        const now = ctx.currentTime;
        const gainNode = ctx.createGain();
        gainNode.connect(ctx.destination);

        if (type === 'success') {
            // Apple-inspired harmonious two-note chime (D5 -> A5)
            const osc1 = ctx.createOscillator();
            const osc2 = ctx.createOscillator();
            osc1.type = 'sine';
            osc2.type = 'sine';
            osc1.frequency.setValueAtTime(587.33, now); // D5
            osc2.frequency.setValueAtTime(880.00, now + 0.08); // A5

            gainNode.gain.setValueAtTime(0.18, now);
            gainNode.gain.exponentialRampToValueAtTime(0.001, now + 0.35);

            osc1.connect(gainNode);
            osc2.connect(gainNode);

            osc1.start(now);
            osc1.stop(now + 0.08);
            osc2.start(now + 0.08);
            osc2.stop(now + 0.35);
        } else if (type === 'error') {
            // Crisp, gentle alert buzz (320Hz -> 210Hz)
            const osc = ctx.createOscillator();
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(320, now);
            osc.frequency.exponentialRampToValueAtTime(210, now + 0.22);

            gainNode.gain.setValueAtTime(0.22, now);
            gainNode.gain.exponentialRampToValueAtTime(0.001, now + 0.25);

            osc.connect(gainNode);
            osc.start(now);
            osc.stop(now + 0.25);
        } else if (type === 'warning') {
            // Single clear bell ping (659.25Hz [E5])
            const osc = ctx.createOscillator();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(659.25, now);

            gainNode.gain.setValueAtTime(0.15, now);
            gainNode.gain.exponentialRampToValueAtTime(0.001, now + 0.28);

            osc.connect(gainNode);
            osc.start(now);
            osc.stop(now + 0.28);
        } else {
            // Subtle click/blip for info/loading
            const osc = ctx.createOscillator();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(523.25, now); // C5

            gainNode.gain.setValueAtTime(0.12, now);
            gainNode.gain.exponentialRampToValueAtTime(0.001, now + 0.18);

            osc.connect(gainNode);
            osc.start(now);
            osc.stop(now + 0.18);
        }
    } catch (e) {
        // Audio playback error or autoplay restrictions - safely ignore
    }
}

// Mount Toaster once DOM is ready
let isMounted = false;
function initToaster() {
    if (isMounted) return;
    try {
        mountToaster({
            position: 'top-center',
            theme: 'system',
            held: 'replace'
        });

        toast.configure({
            sound: (type) => playSynthesizedChime(type),
            held: 'replace'
        });

        isMounted = true;
    } catch (e) {
        console.error('Failed to mount Ding toaster:', e);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initToaster);
} else {
    initToaster();
}

// Global Interop API for Blazor
window.chrononDing = {
    show: function (type, title, description, duration) {
        initToaster();
        const opts = {};
        if (description) opts.description = description;
        if (typeof duration === 'number' && duration > 0) opts.duration = duration;

        switch (type) {
            case 'success':
                return toast.success(title, opts);
            case 'error':
                return toast.error(title, opts);
            case 'warning':
                return toast.warning(title, opts);
            case 'info':
                return toast.info(title, opts);
            case 'loading':
                return toast.loading(title, opts);
            default:
                return toast.show(type || 'info', title, opts);
        }
    },

    update: function (id, type, title, description) {
        initToaster();
        const patch = {};
        if (type) patch.type = type;
        if (title) patch.title = title;
        if (description !== undefined) patch.description = description;
        toast.update(id, patch);
    },

    dismiss: function (id) {
        initToaster();
        if (id) {
            toast.dismiss(id);
        } else {
            toast.dismiss();
        }
    },

    isSoundEnabled: function () {
        return localStorage.getItem('chronon_sound_enabled') !== 'false';
    },

    setSoundEnabled: function (enabled) {
        localStorage.setItem('chronon_sound_enabled', enabled ? 'true' : 'false');
        if (enabled) {
            // Give subtle confirmation chime
            playSynthesizedChime('info');
        }
        return enabled;
    },

    toggleSound: function () {
        const current = this.isSoundEnabled();
        const next = !current;
        return this.setSoundEnabled(next);
    }
};
