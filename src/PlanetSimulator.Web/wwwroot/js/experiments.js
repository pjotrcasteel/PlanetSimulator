const storageKey = 'planet-simulator-scenario-v1';

export function save(json) { localStorage.setItem(storageKey, json); }
export function load() { return localStorage.getItem(storageKey); }
export function download(filename, type, text) {
    const blob = new Blob([text], { type: `${type};charset=utf-8` });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}
