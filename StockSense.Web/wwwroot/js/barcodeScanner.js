// Thin wrapper around html5-qrcode so Blazor can start/stop the camera
// and receive decoded barcodes via [JSInvokable] callbacks.
// Docs: https://github.com/mebjas/html5-qrcode

window.barcodeScanner = (function () {
    let html5QrCode = null;
    let dotNetRef = null;

    // elementId: id of the <div> the camera preview renders into.
    // dotNetHelper: DotNetObjectReference to the Blazor component.
    async function start(elementId, dotNetHelper) {
        if (html5QrCode) {
            await stop();
        }

        dotNetRef = dotNetHelper;
        html5QrCode = new Html5Qrcode(elementId);

        const config = {
            fps: 10,
            // Scan box size only — the surrounding preview area is sized/positioned
            // by CSS on the container div (e.g. half-screen, upper-middle, etc.)
            qrbox: { width: 250, height: 150 },
            aspectRatio: 1.777 // 16:9, matches a typical webcam feed
        };

        try {
            await html5QrCode.start(
                { facingMode: "environment" }, // rear camera on phones/tablets; falls back on laptops
                config,
                (decodedText) => onDecoded(decodedText),
                () => { /* per-frame "no barcode found" - expected constantly, ignore */ }
            );
            return true;
        } catch (err) {
            console.error("Camera start failed:", err);
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync("OnScannerError", err?.message || String(err));
            }
            return false;
        }
    }

    function onDecoded(decodedText) {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync("OnBarcodeScanned", decodedText);
        }
    }

    async function stop() {
        if (html5QrCode) {
            try {
                await html5QrCode.stop();
                html5QrCode.clear();
            } catch (err) {
                // Camera may already be stopped/torn down - safe to ignore.
                console.warn("Camera stop warning:", err);
            }
            html5QrCode = null;
        }
        dotNetRef = null;
    }

    return { start, stop };
})();