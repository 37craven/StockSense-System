(function () {
    const scannerScriptUrl =
        "js/barcodeScanner.js?v=20260820-barcode-only-8";

    let scannerLoadPromise = null;

    function ensureScannerLoaded() {
        if (window.barcodeScannerCore) {
            return Promise.resolve(window.barcodeScannerCore);
        }

        if (scannerLoadPromise) {
            return scannerLoadPromise;
        }

        scannerLoadPromise = new Promise((resolve, reject) => {
            const script = document.createElement("script");

            script.src = scannerScriptUrl;
            script.async = true;

            script.onload = () => {
                if (
                    window.barcodeScannerCore &&
                    typeof window.barcodeScannerCore.start === "function"
                ) {
                    resolve(window.barcodeScannerCore);
                    return;
                }

                scannerLoadPromise = null;
                script.remove();

                reject(
                    new Error(
                        "The barcode scanner could not be initialized."
                    )
                );
            };

            script.onerror = () => {
                scannerLoadPromise = null;
                script.remove();

                reject(
                    new Error(
                        "The barcode scanner could not be downloaded."
                    )
                );
            };

            document.head.appendChild(script);
        });

        return scannerLoadPromise;
    }

    function invoke(method, args) {
        return ensureScannerLoaded().then(scanner => {
            if (typeof scanner[method] !== "function") {
                throw new Error(
                    `Barcode scanner method "${method}" is unavailable.`
                );
            }

            return scanner[method](...args);
        });
    }

    const facade = {
        start: (...args) =>
            invoke("start", args),

        cycleCamera: (...args) =>
            invoke("cycleCamera", args),

        stop: (...args) =>
            invoke("stop", args),

        restartAfterViewportChange: (...args) =>
            invoke("restartAfterViewportChange", args),

        watchViewport: (...args) =>
            invoke("watchViewport", args),

        unwatchViewport: (...args) =>
            invoke("unwatchViewport", args),

        getViewportSize: (...args) =>
            invoke("getViewportSize", args),

        isDesktopScanner: (...args) =>
            invoke("isDesktopScanner", args),

        requestPermissionOnToggle: (...args) =>
            invoke("requestPermissionOnToggle", args)
    };

    window.barcodeScanner = facade;
})();
