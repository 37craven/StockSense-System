window.barcodeScanner = (function () {
    let html5QrCode = null;
    let dotNetRef = null;
    let allCameras = [];
    let currentIndex = 0;

    async function start(elementId, dotNetHelper) {
        if (html5QrCode) await stop();

        dotNetRef = dotNetHelper;
        html5QrCode = new Html5Qrcode(elementId);

        const config = { fps: 10, aspectRatio: 1.777 };

        // Start with environment (back) camera first — this triggers getUserMedia
        // which grants camera permission, so subsequent enumerateDevices() returns labels.
        try {
            await html5QrCode.start(
                { facingMode: "environment" },
                config,
                decodedText => onDecoded(decodedText),
                () => {}
            );

            // Now enumerate all cameras (permission granted, labels available)
            await refreshCameraList();

            return true;
        } catch (err) {
            console.error("Camera start failed:", err);
            if (dotNetRef) await dotNetRef.invokeMethodAsync("OnScannerError", err?.message || String(err));
            return false;
        }
    }

    async function refreshCameraList() {
        const devices = await navigator.mediaDevices.enumerateDevices();
        allCameras = devices.filter(d => d.kind === "videoinput");
        // Try to match currentIndex to the camera that's currently active
        currentIndex = 0;
    }

    async function cycleCamera() {
        if (allCameras.length === 0) {
            await refreshCameraList();
        }
        if (allCameras.length <= 1) return;

        // Cycle to next camera
        currentIndex = (currentIndex + 1) % allCameras.length;
        await switchToCamera(allCameras[currentIndex].deviceId);
    }

    async function switchToCamera(deviceId) {
        if (!html5QrCode || !dotNetRef) return;

        // Save ref and stop current
        const ref = dotNetRef;
        try { await html5QrCode.stop(); html5QrCode.clear(); } catch (_) {}
        html5QrCode = null;
        dotNetRef = null;

        // Find the container element and restart
        const container = document.querySelector("#barcode-camera-container");
        if (!container) return;

        html5QrCode = new Html5Qrcode(container.id);
        dotNetRef = ref;

        const config = { fps: 10, aspectRatio: 1.777 };

        try {
            await html5QrCode.start(
                { deviceId: { exact: deviceId } },
                config,
                decodedText => onDecoded(decodedText),
                () => {}
            );
        } catch (err) {
            console.error("Camera switch failed:", err);
            if (dotNetRef) await dotNetRef.invokeMethodAsync("OnScannerError", err?.message || String(err));
        }
    }

    function onDecoded(decodedText) {
        if (dotNetRef) dotNetRef.invokeMethodAsync("OnBarcodeScanned", decodedText);
    }

    async function stop() {
        if (html5QrCode) {
            try { await html5QrCode.stop(); html5QrCode.clear(); }
            catch (_) {}
            html5QrCode = null;
        }
        dotNetRef = null;
    }

    return { start, cycleCamera, stop };
})();
