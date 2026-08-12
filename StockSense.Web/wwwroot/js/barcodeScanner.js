window.barcodeScanner = (function () {
    let html5QrCode = null;
    let dotNetRef = null;
    let allCameras = [];
    let currentIndex = 0;
    let currentElementId = null;

    let viewportDotNetRef = null;
    let viewportTimer = null;
    let viewportHandler = null;

    const config = {
        fps: 10,
        aspectRatio: 1.777,

        }
    };


    // ============================================================
    // REQUEST CAMERA PERMISSION
    // ============================================================

    async function requestCameraPermission() {
        if (!window.isSecureContext) {
            throw new Error(
                "Camera access requires HTTPS."
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

    async function notifyScannerError(err) {
        if (!dotNetRef) {
            return;
        }

        try {
            await dotNetRef.invokeMethodAsync(
                "OnScannerError",
                err?.message || String(err)
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
    // START SCANNER
    // ============================================================

    async function start(elementId, dotNetHelper) {
        try {
            await cleanupScanner();

            currentElementId = elementId;
            dotNetRef = dotNetHelper;

            const container =
                document.getElementById(elementId);

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${elementId}" was not found.`
                );
            }

            /*
             * Request / verify browser camera permission first.
             */
            await requestCameraPermission();

            container.innerHTML = "";

            html5QrCode =
                new Html5Qrcode(elementId);

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

            console.log(
                "Barcode scanner started successfully."
            );

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

            console.log(
                "Available cameras:",
                allCameras.length
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

    async function cycleCamera(elementId) {
        try {
            if (allCameras.length === 0) {
                await refreshCameraList();
            }

            if (allCameras.length <= 1) {
                console.log(
                    "Only one camera is available."
                );

                return;
            }

            currentIndex =
                (currentIndex + 1) %
                allCameras.length;

            const camera =
                allCameras[currentIndex];

            await switchToCamera(
                camera.deviceId,
                elementId || currentElementId
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
            return;
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
                    setTimeout(resolve, 350)
            );

            const container =
                document.getElementById(
                    targetElementId
                );

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${targetElementId}" was not found.`
                );
            }

            container.innerHTML = "";

            currentElementId =
                targetElementId;

            dotNetRef =
                savedDotNetRef;

            html5QrCode =
                new Html5Qrcode(
                    targetElementId
                );

            await html5QrCode.start(
                {
                    deviceId: {
                        exact: deviceId
                    }
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

            /*
             * Camera changed successfully.
             *
             * Remove any previous scanner error from POS.
             */
            await notifyScannerStarted();

            console.log(
                "Camera switched successfully."
            );
        }
        catch (err) {
            console.error(
                "Camera switch failed:",
                err
            );

            dotNetRef =
                savedDotNetRef;

            await notifyScannerError(
                err
            );
        }
    }


    // ============================================================
    // BARCODE DETECTED
    // ============================================================

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

        dotNetRef
            .invokeMethodAsync(
                "OnBarcodeScanned",
                decodedText
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
        await cleanupScanner();

        dotNetRef = null;

        currentElementId = null;

        allCameras = [];

        currentIndex = 0;

        console.log(
            "Barcode scanner stopped."
        );
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
                    elementId
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
             * Restart succeeded — remove any previous error
             * from the POS interface.
             */
            await notifyScannerStarted();

            console.log(
                "Barcode scanner restarted after viewport/orientation change:",
                elementId
            );

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
    // PUBLIC API USED BY BLAZOR
    // ============================================================

    return {
        start,
        cycleCamera,
        stop,
        restartAfterViewportChange,
        watchViewport,
        unwatchViewport
    };
})();