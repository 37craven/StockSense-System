window.barcodeScanner = (function () {
    const html5QrcodeScriptUrl =
        "js/vendor/html5-qrcode.min.js?v=20260819-clean-qr-5";
    let html5QrcodeLoadPromise = null;
    let html5QrCode = null;
    let dotNetRef = null;
    let allCameras = [];
    let currentIndex = 0;
    let currentElementId = null;
    let cameraPermission = null;

    let viewportDotNetRef = null;
    let viewportTimer = null;
    let viewportHandler = null;

    const config = {
        fps: 15,
        /*
         * Cap the camera stream at 720p.
         *
         * Phones default to 1080p+; decoding full-resolution frames is
         * slow and unreliable on mobile. 720p is more than enough for
         * barcodes and decodes fast.
         *
         * NOTE: this is the only valid place for width/height — the
         * cameraIdOrConfig passed to start() must have exactly 1 key.
         */
        videoConstraints: {
            facingMode: "environment",
            width: { ideal: 1280, max: 1280 },
            height: { ideal: 720, max: 720 }
        }
    };

    /*
     * The viewfinder is a wide horizontal strip: 1D barcodes are wide
     * and short; a square crop slices the ends off and they never
     * decode. The library decodes ONLY this region, so the strip must
     * match how the user naturally holds the code.
     */
    function createQrbox() {
        return (videoWidth, videoHeight) => {
            const width = Math.max(160, videoWidth - 48);
            const height = Math.max(
                90,
                Math.min(
                    140,
                    Math.round(videoHeight * 0.5)
                )
            );
            return { width, height };
        };
    }

    function scannerFormatsConfig() {
        /*
         * Restrict decoding to the 1D barcode formats supported by the
         * scanner.
         *
         * useBarCodeDetectorIfSupported: false forces the built-in
         * ZXing engine instead of the browser's native BarcodeDetector.
         * The native detector on many Android devices only decodes QR
         * codes (getSupportedFormats() returns just ["qr_code"]), which
         * silently kills all 1D barcode scanning. ZXing handles every
         * format in the list consistently, and with the restricted
         * format set + qrbox crop it is fast enough.
         *
         * NOTE: formatsToSupport is only honored when passed to the
         * Html5Qrcode CONSTRUCTOR, not to start(). The enum global only
         * exists after the library script loads, so this is built
         * lazily (the first constructor call happens after loading).
         */
        const barcodeFormats = [
            Html5QrcodeSupportedFormats.EAN_13,
            Html5QrcodeSupportedFormats.EAN_8,
            Html5QrcodeSupportedFormats.UPC_A,
            Html5QrcodeSupportedFormats.UPC_E,
            Html5QrcodeSupportedFormats.CODE_128,
            Html5QrcodeSupportedFormats.CODE_39,
            Html5QrcodeSupportedFormats.CODE_93,
            Html5QrcodeSupportedFormats.ITF,
            Html5QrcodeSupportedFormats.CODABAR
        ];

        return {
            /*
             * Force the built-in ZXing engine. The browser's native
             * BarcodeDetector behaves inconsistently across devices: on
             * many Android phones it only supports QR (silently killing
             * 1D barcode scanning), and its detection quality varies.
             * ZXing handles every format in the list consistently.
             */
            useBarCodeDetectorIfSupported: false,
            formatsToSupport: barcodeFormats
        };
    }

    function ensureHtml5QrcodeLoaded() {
        if (typeof window.Html5Qrcode === "function") {
            return Promise.resolve();
        }

        if (html5QrcodeLoadPromise) {
            return html5QrcodeLoadPromise;
        }

        html5QrcodeLoadPromise = new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.src = html5QrcodeScriptUrl;
            script.async = true;
            script.onload = () => {
                if (typeof window.Html5Qrcode === "function") {
                    resolve();
                    return;
                }

                html5QrcodeLoadPromise = null;
                script.remove();
                reject(new Error("The barcode scanner library did not initialize."));
            };
            script.onerror = () => {
                html5QrcodeLoadPromise = null;
                script.remove();
                reject(new Error("The barcode scanner library could not be downloaded."));
            };
            document.head.appendChild(script);
        });

        return html5QrcodeLoadPromise;
    }


    // ============================================================
    // REQUEST CAMERA PERMISSION
    // ============================================================

    async function requestCameraPermission() {
        if (!window.isSecureContext) {
            throw new Error(
                "Camera access requires HTTPS. Open this site with https:// (not http://), especially from phones and tablets."
            );
        }

        if (
            !navigator.mediaDevices ||
            !navigator.mediaDevices.getUserMedia
        ) {
            throw new Error(
                "Camera access is not supported by this browser."
            );
        }

        if (cameraPermission === true) {
            return true;
        }

        if (cameraPermission === false) {
            throw new Error(
                "Camera permission was denied or blocked."
            );
        }

        try {
            /*
             * getUserMedia supports the normal MediaTrackConstraints
             * syntax, including "ideal".
             *
             * This explicitly triggers the browser permission prompt
             * if permission has not yet been decided.
             */
            const stream =
                await navigator.mediaDevices.getUserMedia({
                    video: {
                        facingMode: {
                            ideal: "environment"
                        }
                    },
                    audio: false
                });

            cameraPermission = true;

            /*
             * We only requested this temporary stream to obtain/check
             * camera permission.
             *
             * Html5Qrcode will open its own stream afterward.
             */
            stream
                .getTracks()
                .forEach(track => track.stop());

            return true;
        }
        catch (err) {
            console.error(
                "Camera permission request failed:",
                err
            );

            if (
                err?.name === "NotAllowedError" ||
                err?.name === "PermissionDeniedError"
            ) {
                cameraPermission = false;
            }

            switch (err?.name) {
                case "NotAllowedError":
                case "PermissionDeniedError":
                    throw new Error(
                        "Camera permission was denied or blocked."
                    );

                case "NotFoundError":
                case "DevicesNotFoundError":
                    throw new Error(
                        "No camera was found on this device."
                    );

                case "NotReadableError":
                case "TrackStartError":
                    throw new Error(
                        "The camera is being used by another application."
                    );

                case "OverconstrainedError":
                case "ConstraintNotSatisfiedError":
                    throw new Error(
                        "The requested camera is not available."
                    );

                case "SecurityError":
                    throw new Error(
                        "The browser blocked camera access for security reasons."
                    );

                case "AbortError":
                    throw new Error(
                        "Camera access was interrupted."
                    );

                default:
                    throw new Error(
                        err?.message ||
                        "The camera could not be accessed."
                    );
            }
        }
    }


    // ============================================================
    // NOTIFY BLAZOR THAT SCANNER IS WORKING
    // ============================================================

    async function notifyScannerStarted() {
        if (!dotNetRef) {
            return;
        }

        try {
            await dotNetRef.invokeMethodAsync(
                "OnScannerStarted"
            );
        }
        catch (err) {
            console.warn(
                "Unable to notify Blazor that scanner started:",
                err
            );
        }
    }


    // ============================================================
    // NOTIFY BLAZOR ABOUT ERROR
    // ============================================================

    function isTransientCameraError(err) {
        const name = err?.name || "";
        const message = err?.message || "";
        return (
            /NotReadable|TrackStart/i.test(name) ||
            /device in use|being used|in use/i.test(message)
        );
    }

    function friendlyErrorText(err) {
        const name = err?.name || "";
        const message = err?.message || String(err);

        if (
            /NotAllowed|PermissionDenied/i.test(name) ||
            /permission|denied|blocked/i.test(message)
        ) {
            return "Camera permission is blocked. Allow camera access in your browser settings and try again.";
        }
        if (
            /NotFound|DevicesNotFound/i.test(name) ||
            /no camera/i.test(message)
        ) {
            return "No camera was found on this device.";
        }
        if (isTransientCameraError(err)) {
            return "The camera is busy or being used by another application. Wait a moment and try switching cameras again.";
        }
        if (/Overconstrained|ConstraintNotSatisfied/i.test(name)) {
            return "The requested camera is not available.";
        }
        if (/SecurityError/i.test(name)) {
            return "The browser blocked camera access for security reasons.";
        }
        if (/AbortError/i.test(name)) {
            return "Camera access was interrupted.";
        }
        return "The camera could not be started. Turn the scanner off and on, then try again.";
    }

    async function notifyScannerError(err) {
        if (!dotNetRef) {
            return;
        }

        try {
            await dotNetRef.invokeMethodAsync(
                "OnScannerError",
                friendlyErrorText(err)
            );
        }
        catch (callbackError) {
            console.warn(
                "Unable to send scanner error to Blazor:",
                callbackError
            );
        }
    }


    // ============================================================
    // NOTIFY BLAZOR ABOUT A NON-ERROR SCANNER MESSAGE
    // ============================================================

    async function notifyScannerInfo(message) {
        if (!dotNetRef) {
            return;
        }

        try {
            await dotNetRef.invokeMethodAsync(
                "OnScannerInfo",
                message
            );
        }
        catch (callbackError) {
            console.warn(
                "Unable to send scanner info to Blazor:",
                callbackError
            );
        }
    }


    // ============================================================
    // START SCANNER
    // ============================================================

    async function start(elementId, dotNetHelper) {
        try {
            await cleanupScanner();

            currentElementId = elementId;
            dotNetRef = dotNetHelper;

            config.qrbox = createQrbox();

            const container =
                document.getElementById(elementId);

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${elementId}" was not found.`
                );
            }

            await ensureHtml5QrcodeLoaded();

            /*
             * Request / verify browser camera permission first.
             */
            await requestCameraPermission();

            container.innerHTML = "";

            html5QrCode = new Html5Qrcode(
                elementId,
                scannerFormatsConfig()
            );

            /*
             * IMPORTANT:
             *
             * Html5Qrcode expects facingMode as a STRING here.
             *
             * Do NOT use:
             *
             * facingMode: {
             *     ideal: "environment"
             * }
             *
             * because Html5Qrcode rejects that format.
             *
             * IMPORTANT: the cameraIdOrConfig object must have EXACTLY
             * one key ("facingMode" or "deviceId"). width/height are NOT
             * allowed here â€” they go in config.videoConstraints instead.
             */
            await html5QrCode.start(
                {
                    facingMode: "environment"
                },
                config,

                decodedText => {
                    onDecoded(decodedText);
                },

                () => {
                    // No barcode found in this frame.
                }
            );

            await prepareVideoElement(
                container
            );

            await refreshCameraList();

            matchCurrentCamera();

            /*
             * The scanner successfully started.
             *
             * This clears any previous error message in POS.razor.
             */
            await notifyScannerStarted();



            return true;
        }
        catch (err) {
            console.error(
                "Camera start failed:",
                err
            );

            /*
             * Save the Blazor callback reference because cleanup
             * may modify scanner state.
             */
            const callbackRef =
                dotNetRef;

            await cleanupScanner();

            /*
             * Restore temporarily so notifyScannerError can use it.
             */
            dotNetRef =
                callbackRef;

            await notifyScannerError(
                err
            );

            return false;
        }
    }


    // ============================================================
    // PREPARE VIDEO ELEMENT
    // ============================================================

    async function prepareVideoElement(container) {
        /*
         * Give Html5Qrcode a moment to insert the video element.
         */
        await new Promise(
            resolve =>
                setTimeout(resolve, 100)
        );

        const video =
            container.querySelector("video");

        if (!video) {
            console.warn(
                "Html5Qrcode started, but no video element was found inside:",
                currentElementId
            );

            return;
        }

        /*
         * Important for Safari / iOS / iPadOS.
         */
        video.setAttribute(
            "playsinline",
            ""
        );

        video.setAttribute(
            "webkit-playsinline",
            ""
        );

        video.setAttribute(
            "autoplay",
            ""
        );

        video.muted = true;

        video.style.width = "100%";
        video.style.height = "100%";
        video.style.objectFit = "cover";
        video.style.display = "block";

        try {
            await video.play();
        }
        catch (err) {
            console.warn(
                "Manual video.play() failed:",
                err
            );
        }
    }


    // ============================================================
    // GET AVAILABLE CAMERAS
    // ============================================================

    async function refreshCameraList() {
        try {
            if (
                !navigator.mediaDevices ||
                !navigator.mediaDevices.enumerateDevices
            ) {
                allCameras = [];
                return;
            }

            const devices =
                await navigator.mediaDevices.enumerateDevices();

            allCameras =
                devices.filter(
                    device =>
                        device.kind === "videoinput"
                );


        }
        catch (err) {
            console.warn(
                "Unable to enumerate camera devices:",
                err
            );

            allCameras = [];
        }
    }


    // ============================================================
    // FIND CURRENT ACTIVE CAMERA
    // ============================================================

    function matchCurrentCamera() {
        if (
            !html5QrCode ||
            !allCameras.length
        ) {
            currentIndex = 0;
            return;
        }

        try {
            const video =
                document
                    .getElementById(currentElementId)
                    ?.querySelector("video");

            const track =
                video
                    ?.srcObject
                    ?.getVideoTracks?.()[0];

            const activeDeviceId =
                track
                    ?.getSettings?.()
                    ?.deviceId;

            if (!activeDeviceId) {
                currentIndex = 0;
                return;
            }

            const index =
                allCameras.findIndex(
                    camera =>
                        camera.deviceId ===
                        activeDeviceId
                );

            currentIndex =
                index >= 0
                    ? index
                    : 0;
        }
        catch (err) {
            console.warn(
                "Unable to determine active camera:",
                err
            );

            currentIndex = 0;
        }
    }


    // ============================================================
    // FLIP / CYCLE CAMERA
    // ============================================================

    function currentCameraDeviceId() {
        if (!currentElementId) {
            return null;
        }

        try {
            const video =
                document
                    .getElementById(currentElementId)
                    ?.querySelector("video");

            return (
                video
                    ?.srcObject
                    ?.getVideoTracks?.()
                    ?.[0]
                    ?.getSettings?.()
                    ?.deviceId || null
            );
        }
        catch {
            return null;
        }
    }

    async function cycleCamera(elementId) {
        try {
            const previousDeviceId =
                currentCameraDeviceId();

            if (allCameras.length === 0) {
                await refreshCameraList();
            }

            if (allCameras.length === 0) {
                await notifyScannerInfo(
                    "No camera was found on this device. Turn the scanner off and on, then try again."
                );

                return;
            }

            if (allCameras.length === 1) {
                await notifyScannerInfo(
                    "No other camera is available. Turn the scanner off and on, then try again."
                );

                return;
            }

            const targetElementId =
                elementId || currentElementId;

            /*
             * Try every other camera before giving up. A listed
             * camera can fail to start (e.g. an IR/webcam that the
             * browser cannot grab), so skip it and try the next one.
             */
            const candidates =
                allCameras.filter(
                    camera =>
                        camera.deviceId !==
                        previousDeviceId
                );

            for (const camera of candidates) {
                const switched =
                    await switchToCamera(
                        camera.deviceId,
                        targetElementId
                    );

                if (switched) {
                    return;
                }
            }

            /*
             * Nothing else worked — restart the camera that was
             * working before, so the scanner is not left dead.
             */
            if (previousDeviceId) {
                const restored =
                    await switchToCamera(
                        previousDeviceId,
                        targetElementId
                    );

                if (restored) {
                    await notifyScannerInfo(
                        "The camera could not be switched, so the previous camera was restarted."
                    );

                    return;
                }
            }

            await notifyScannerInfo(
                "No other camera is available. Turn the scanner off and on, then try again."
            );
        }
        catch (err) {
            console.error(
                "Camera cycle failed:",
                err
            );

            await notifyScannerError(
                err
            );
        }
    }


    // ============================================================
    // SWITCH TO SPECIFIC CAMERA
    // ============================================================

    async function switchToCamera(
        deviceId,
        elementId
    ) {
        if (!dotNetRef) {
            return false;
        }

        const savedDotNetRef =
            dotNetRef;

        const targetElementId =
            elementId ||
            currentElementId;

        try {
            await cleanupScanner();

            /*
             * Mobile browsers can take a moment to release
             * the previous camera.
             */
            await new Promise(
                resolve =>
                    setTimeout(resolve, 500)
            );

            const container =
                document.getElementById(
                    targetElementId
                );

            if (!container) {
                return false;
            }

            container.replaceChildren();

            currentElementId =
                targetElementId;

            dotNetRef =
                savedDotNetRef;

            /*
             * IMPORTANT: the library IGNORES cameraIdOrConfig when
             * config.videoConstraints is set (it prefers the config's
             * constraints for getUserMedia). So the target deviceId
             * must be carried inside videoConstraints, otherwise the
             * switch silently restarts the SAME camera. Keep the 720p
             * cap here too, or the switch would revert to full
             * resolution and slow decoding back down.
             */
            const switchConfig = {
                ...config,
                videoConstraints: {
                    deviceId: { exact: deviceId },
                    width: { ideal: 1280, max: 1280 },
                    height: { ideal: 720, max: 720 }
                }
            };

            /*
             * The previous camera stream can still hold the device
             * for a moment (NotReadableError: "Device in use"),
             * especially on mobile. Retry a few times before giving
             * up on this camera.
             */
            let started = false;
            let lastError = null;

            for (let attempt = 0; attempt < 5 && !started; attempt++) {
                html5QrCode =
                    new Html5Qrcode(
                        targetElementId,
                        scannerFormatsConfig()
                    );

                try {
                    await html5QrCode.start(
                        {
                            deviceId: {
                                exact: deviceId
                            }
                        },
                        switchConfig,

                        decodedText => {
                            onDecoded(decodedText);
                        },

                        () => {
                            // No barcode detected in this frame.
                        }
                    );
                    started = true;
                }
                catch (err) {
                    lastError = err;
                    await cleanupScanner();

                    if (attempt < 4 && isTransientCameraError(err)) {
                        await new Promise(
                            resolve =>
                                setTimeout(resolve, 1000)
                        );
                        container.replaceChildren();
                        continue;
                    }
                }
            }

            if (!started) {
                console.warn(
                    "Camera switch failed for device:",
                    deviceId,
                    lastError
                );

                return false;
            }

            await prepareVideoElement(
                container
            );

            /*
             * Camera changed successfully.
             *
             * Remove any previous scanner error from POS.
             */
            await notifyScannerStarted();

            return true;
        }
        catch (err) {
            console.warn(
                "Camera switch failed:",
                err
            );

            dotNetRef =
                savedDotNetRef;

            return false;
        }
    }


    // ============================================================
    // BARCODE DETECTED
    // ============================================================

    let lastDecodedText = "";
    let lastDecodedAt = 0;

    function onDecoded(decodedText) {
        if (!dotNetRef) {
            return;
        }

        if (
            !decodedText ||
            typeof decodedText !== "string"
        ) {
            return;
        }

        const value = decodedText.trim();

        if (!value) {
            return;
        }

        const now = Date.now();

        if (
            value === lastDecodedText &&
            now - lastDecodedAt < 2500
        ) {
            return;
        }

        lastDecodedText = value;
        lastDecodedAt = now;

        dotNetRef
            .invokeMethodAsync(
                "OnBarcodeScanned",
                value
            )
            .catch(err => {
                console.error(
                    "Barcode callback failed:",
                    err
                );
            });
    }


    // ============================================================
    // CLEANUP SCANNER
    // ============================================================

    async function cleanupScanner() {
        if (html5QrCode) {
            try {
                if (html5QrCode.isScanning) {
                    await html5QrCode.stop();
                }
            }
            catch (err) {
                console.warn(
                    "Scanner stop warning:",
                    err
                );
            }

            try {
                html5QrCode.clear();
            }
            catch (err) {
                console.warn(
                    "Scanner clear warning:",
                    err
                );
            }

            html5QrCode = null;
        }


        /*
         * Manually stop any remaining MediaStream tracks.
         */
        if (currentElementId) {
            const container =
                document.getElementById(
                    currentElementId
                );

            const videos =
                container?.querySelectorAll("video") ||
                [];

            videos.forEach(video => {
                const stream =
                    video.srcObject;

                if (
                    stream &&
                    typeof stream.getTracks ===
                    "function"
                ) {
                    stream
                        .getTracks()
                        .forEach(
                            track =>
                                track.stop()
                        );
                }

                video.srcObject = null;
            });

            if (container) {
                container.innerHTML = "";
            }
        }
    }


    // ============================================================
    // STOP SCANNER
    // ============================================================

    async function stop() {
        try {
            await cleanupScanner();
        }
        catch (err) {
            console.warn(
                "Barcode scanner cleanup failed:",
                err
            );
        }

        dotNetRef = null;

        currentElementId = null;

        allCameras = [];

        currentIndex = 0;



        return true;
    }


    // ============================================================
    // RESTART AFTER VIEWPORT / ORIENTATION CHANGE
    // ============================================================

    async function restartAfterViewportChange(
        elementId,
        dotNetHelper
    ) {
        try {
            await cleanupScanner();

            dotNetRef =
                dotNetHelper;

            currentElementId =
                elementId;

            /*
             * iOS/iPadOS/mobile browsers can retain the old
             * camera surface for a short period.
             */
            await new Promise(
                resolve =>
                    setTimeout(resolve, 750)
            );

            /*
             * Wait for Blazor/CSS/media queries to finish
             * changing the layout.
             */
            await new Promise(
                resolve =>
                    requestAnimationFrame(
                        () =>
                            requestAnimationFrame(
                                resolve
                            )
                    )
            );

            const container =
                document.getElementById(
                    elementId
                );

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${elementId}" was not found after the layout changed.`
                );
            }

            /*
             * Ensure that the scanner container is visible and
             * has actual dimensions.
             */
            const rect =
                container.getBoundingClientRect();

            if (
                rect.width <= 0 ||
                rect.height <= 0
            ) {
                await new Promise(
                    resolve =>
                        setTimeout(
                            resolve,
                            250
                        )
                );
            }

            container.innerHTML = "";

            html5QrCode =
                new Html5Qrcode(
                    elementId,
                    scannerFormatsConfig()
                );

            /*
             * IMPORTANT:
             * Html5Qrcode requires the string version.
             */
            await html5QrCode.start(
                {
                    facingMode: "environment"
                },
                config,

                decodedText => {
                    onDecoded(decodedText);
                },

                () => {
                    // No barcode detected in this frame.
                }
            );

            await prepareVideoElement(
                container
            );

            await refreshCameraList();

            matchCurrentCamera();

            /*
             * Restart succeeded â€” remove any previous error
             * from the POS interface.
             */
            await notifyScannerStarted();



            return true;
        }
        catch (err) {
            console.error(
                "Scanner restart after viewport/orientation change failed:",
                err
            );

            const callbackRef =
                dotNetRef;

            try {
                await cleanupScanner();
            }
            catch (_) {
                // Ignore cleanup errors.
            }

            dotNetRef =
                callbackRef;

            await notifyScannerError(
                err
            );

            return false;
        }
    }


    // ============================================================
    // WATCH VIEWPORT / ORIENTATION
    // ============================================================

    function watchViewport(
        dotNetHelper
    ) {
        unwatchViewport();

        viewportDotNetRef =
            dotNetHelper;

        viewportHandler =
            function () {
                if (viewportTimer) {
                    clearTimeout(
                        viewportTimer
                    );
                }

                /*
                 * Debounce resize/orientation events.
                 *
                 * Mobile browsers can fire many resize events while
                 * rotating or opening browser controls.
                 */
                viewportTimer =
                    setTimeout(
                        async function () {
                            if (!viewportDotNetRef) {
                                return;
                            }

                            try {
                                /*
                                 * Keep this exactly aligned with the
                                 * desktop breakpoint in POS.razor.
                                 */
                                const useDesktopScanner =
                                    window.matchMedia(
                                        "(min-width: 1280px) and (hover: hover) and (pointer: fine)"
                                    ).matches;

                                await viewportDotNetRef
                                    .invokeMethodAsync(
                                        "OnPosViewportChanged",
                                        window.innerWidth,
                                        window.innerHeight,
                                        useDesktopScanner
                                    );
                            }
                            catch (err) {
                                console.warn(
                                    "POS viewport callback failed:",
                                    err
                                );
                            }

                        },
                        300
                    );
            };

        window.addEventListener(
            "resize",
            viewportHandler,
            {
                passive: true
            }
        );

        window.addEventListener(
            "orientationchange",
            viewportHandler,
            {
                passive: true
            }
        );
    }


    // ============================================================
    // STOP WATCHING VIEWPORT
    // ============================================================

    function unwatchViewport() {
        if (viewportTimer) {
            clearTimeout(
                viewportTimer
            );

            viewportTimer = null;
        }

        if (viewportHandler) {
            window.removeEventListener(
                "resize",
                viewportHandler
            );

            window.removeEventListener(
                "orientationchange",
                viewportHandler
            );

            viewportHandler = null;
        }

        viewportDotNetRef = null;
    }


    // ============================================================
    // VIEWPORT QUERIES (CSP-safe replacements for eval)
    // ============================================================

    function getViewportSize() {
        return {
            width: window.innerWidth,
            height: window.innerHeight
        };
    }

    function isDesktopScanner() {
        return window.matchMedia(
            "(min-width: 1280px) and (hover: hover) and (pointer: fine)"
        ).matches;
    }


    // ============================================================
    // CAMERA PERMISSION WITHIN USER GESTURE
    // ============================================================

    /*
     * Mobile browsers (especially iOS Safari) only show the camera
     * permission prompt when getUserMedia is called synchronously
     * inside a real user gesture (tap/click).
     *
     * Blazor's @bind checkbox goes through a server round-trip
     * before OnAfterRenderAsync starts the scanner, so the gesture
     * is already consumed by then and the prompt never appears.
     *
     * This attaches a native click listener to the toggle so the
     * permission is requested at the exact moment of the tap. The
     * result is cached and reused by requestCameraPermission() when
     * the scanner actually starts.
     */
    function requestPermissionOnToggle(checkboxId) {
        const toggle = document.getElementById(checkboxId);

        if (!toggle || toggle.__bbCameraPermissionAttached) {
            return;
        }

        toggle.__bbCameraPermissionAttached = true;

        toggle.addEventListener("click", () => {
            if (!toggle.checked || cameraPermission !== null) {
                return;
            }

            if (
                !window.isSecureContext ||
                !navigator.mediaDevices ||
                !navigator.mediaDevices.getUserMedia
            ) {
                return;
            }

            navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: {
                        ideal: "environment"
                    }
                },
                audio: false
            })
                .then(stream => {
                    cameraPermission = true;
                    stream
                        .getTracks()
                        .forEach(track => track.stop());
                })
                .catch(err => {
                    cameraPermission = false;
                    console.warn(
                        "Camera permission was not granted on toggle:",
                        err?.name || err
                    );
                });
        });
    }


    // ============================================================
    // PUBLIC API USED BY BLAZOR
    // ============================================================

    return {
        start,
        cycleCamera,
        stop,
        restartAfterViewportChange,
        watchViewport,
        unwatchViewport,
        getViewportSize,
        isDesktopScanner,
        requestPermissionOnToggle
    };
})();