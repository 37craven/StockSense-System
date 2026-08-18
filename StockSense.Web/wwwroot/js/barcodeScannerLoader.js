(function () {
    const scannerScriptUrl =
        "js/barcodeScanner.js?v=20260817-lazy-scanner-1";
    let scannerLoadPromise = null;

    function ensureScannerLoaded() {
        if (scannerLoadPromise) {
            return scannerLoadPromise;
        }

        scannerLoadPromise = new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.src = scannerScriptUrl;
            script.async = true;
            script.onload = () => {
                if (window.barcodeScanner && window.barcodeScanner !== facade) {
                    resolve(window.barcodeScanner);
                    return;
                }

                scannerLoadPromise = null;
                script.remove();
                reject(new Error("The barcode scanner could not be initialized."));
            };
            script.onerror = () => {
                scannerLoadPromise = null;
                script.remove();
                reject(new Error("The barcode scanner could not be downloaded."));
            };
            document.head.appendChild(script);
        });

        return scannerLoadPromise;
    }

    function invoke(method, args) {
        return ensureScannerLoaded().then(scanner => scanner[method](...args));
    }

    const facade = {
        start: (...args) => invoke("start", args),
        cycleCamera: (...args) => invoke("cycleCamera", args),
        stop: (...args) => invoke("stop", args),
        restartAfterViewportChange: (...args) =>
            invoke("restartAfterViewportChange", args),
        watchViewport: (...args) => invoke("watchViewport", args),
        unwatchViewport: (...args) => invoke("unwatchViewport", args)
    };

    window.barcodeScanner = facade;
})();
