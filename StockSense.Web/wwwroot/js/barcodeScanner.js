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
        qrbox: function (viewfinderWidth, viewfinderHeight) {
            const width = Math.floor(viewfinderWidth * 0.8);
            const height = Math.floor(viewfinderHeight * 0.55);

            return {
                width: Math.max(150, width),
                height: Math.max(100, height)
            };
        }
    };

    async function start(elementId, dotNetHelper) {
        try {
            await cleanupScanner();

            currentElementId = elementId;
            dotNetRef = dotNetHelper;

            const container = document.getElementById(elementId);

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${elementId}" was not found.`
                );
            }

            container.innerHTML = "";
            html5QrCode = new Html5Qrcode(elementId);

            await html5QrCode.start(
                { facingMode: { ideal: "environment" } },
                config,
                decodedText => onDecoded(decodedText),
                () => { }
            );

            await prepareVideoElement(container);
            await refreshCameraList();
            matchCurrentCamera();

            return true;
        }
        catch (err) {
            console.error("Camera start failed:", err);

            const callbackRef = dotNetRef;
            await cleanupScanner();

            if (callbackRef) {
                await callbackRef.invokeMethodAsync(
                    "OnScannerError",
                    err?.message || String(err)
                );
            }

            return false;
        }
    }

    async function prepareVideoElement(container) {
        await new Promise(resolve => setTimeout(resolve, 100));

        const video = container.querySelector("video");

        if (!video) {
            console.warn(
                "Html5Qrcode started, but no video element was found inside:",
                currentElementId
            );
            return;
        }

        video.setAttribute("playsinline", "");
        video.setAttribute("webkit-playsinline", "");
        video.setAttribute("autoplay", "");
        video.muted = true;

        video.style.width = "100%";
        video.style.height = "100%";
        video.style.objectFit = "cover";
        video.style.display = "block";

        try {
            await video.play();
        }
        catch (err) {
            console.warn("Manual video.play() failed:", err);
        }
    }

    async function refreshCameraList() {
        try {
            const devices = await navigator.mediaDevices.enumerateDevices();

            allCameras = devices.filter(device =>
                device.kind === "videoinput"
            );
        }
        catch (err) {
            console.warn("Unable to enumerate camera devices:", err);
            allCameras = [];
        }
    }

    function matchCurrentCamera() {
        if (!html5QrCode || !allCameras.length) {
            currentIndex = 0;
            return;
        }

        try {
            const video = document
                .getElementById(currentElementId)
                ?.querySelector("video");

            const track = video?.srcObject?.getVideoTracks?.()[0];
            const activeDeviceId = track?.getSettings?.()?.deviceId;

            if (!activeDeviceId) {
                currentIndex = 0;
                return;
            }

            const index = allCameras.findIndex(
                camera => camera.deviceId === activeDeviceId
            );

            currentIndex = index >= 0 ? index : 0;
        }
        catch {
            currentIndex = 0;
        }
    }

    async function cycleCamera(elementId) {
        try {
            if (allCameras.length === 0) {
                await refreshCameraList();
            }

            if (allCameras.length <= 1) {
                return;
            }

            currentIndex =
                (currentIndex + 1) % allCameras.length;

            await switchToCamera(
                allCameras[currentIndex].deviceId,
                elementId || currentElementId
            );
        }
        catch (err) {
            console.error("Camera cycle failed:", err);

            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync(
                    "OnScannerError",
                    err?.message || String(err)
                );
            }
        }
    }

    async function switchToCamera(deviceId, elementId) {
        if (!dotNetRef) return;

        const savedDotNetRef = dotNetRef;
        const targetElementId = elementId || currentElementId;

        try {
            await cleanupScanner();

            // iOS/iPadOS sometimes needs a little longer to release a camera.
            await new Promise(resolve => setTimeout(resolve, 350));

            const container = document.getElementById(targetElementId);

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${targetElementId}" was not found.`
                );
            }

            container.innerHTML = "";
            currentElementId = targetElementId;
            dotNetRef = savedDotNetRef;

            html5QrCode = new Html5Qrcode(targetElementId);

            await html5QrCode.start(
                { deviceId: { exact: deviceId } },
                config,
                decodedText => onDecoded(decodedText),
                () => { }
            );

            await prepareVideoElement(container);
        }
        catch (err) {
            console.error("Camera switch failed:", err);
            dotNetRef = savedDotNetRef;

            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync(
                    "OnScannerError",
                    err?.message || String(err)
                );
            }
        }
    }

    function onDecoded(decodedText) {
        if (!dotNetRef) return;

        dotNetRef
            .invokeMethodAsync("OnBarcodeScanned", decodedText)
            .catch(err =>
                console.error("Barcode callback failed:", err)
            );
    }

    async function cleanupScanner() {
        if (html5QrCode) {
            try {
                if (html5QrCode.isScanning) {
                    await html5QrCode.stop();
                }
            }
            catch (err) {
                console.warn("Scanner stop warning:", err);
            }

            try {
                html5QrCode.clear();
            }
            catch (err) {
                console.warn("Scanner clear warning:", err);
            }

            html5QrCode = null;
        }

        if (currentElementId) {
            const container = document.getElementById(currentElementId);
            const videos = container?.querySelectorAll("video") || [];

            videos.forEach(video => {
                const stream = video.srcObject;

                if (stream && typeof stream.getTracks === "function") {
                    stream.getTracks().forEach(track => track.stop());
                }

                video.srcObject = null;
            });

            if (container) {
                container.innerHTML = "";
            }
        }
    }

    async function stop() {
        await cleanupScanner();

        dotNetRef = null;
        currentElementId = null;
        allCameras = [];
        currentIndex = 0;
    }

    async function restartAfterViewportChange(elementId, dotNetHelper) {
        try {
            // Fully release whatever survived the orientation transition.
            await cleanupScanner();

            dotNetRef = dotNetHelper;
            currentElementId = elementId;

            // iPadOS / mobile Edge can keep the old camera surface alive briefly
            // after orientationchange. A longer delay is much more reliable.
            await new Promise(resolve => setTimeout(resolve, 750));

            // Wait for layout/media-query changes to settle.
            await new Promise(resolve =>
                requestAnimationFrame(() =>
                    requestAnimationFrame(resolve)
                )
            );

            const container = document.getElementById(elementId);

            if (!container) {
                throw new Error(
                    `Barcode scanner container "${elementId}" was not found after rotation.`
                );
            }

            // Make sure the target is actually laid out before Html5Qrcode starts.
            const rect = container.getBoundingClientRect();

            if (rect.width <= 0 || rect.height <= 0) {
                await new Promise(resolve => setTimeout(resolve, 250));
            }

            container.innerHTML = "";

            html5QrCode = new Html5Qrcode(elementId);

            await html5QrCode.start(
                { facingMode: { ideal: "environment" } },
                config,
                decodedText => onDecoded(decodedText),
                () => { }
            );

            await prepareVideoElement(container);
            await refreshCameraList();
            matchCurrentCamera();

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

            const callbackRef = dotNetRef;

            try {
                await cleanupScanner();
            }
            catch (_) { }

            if (callbackRef) {
                try {
                    await callbackRef.invokeMethodAsync(
                        "OnScannerError",
                        err?.message || String(err)
                    );
                }
                catch (_) { }
            }

            return false;
        }
    }

    function watchViewport(dotNetHelper) {
        unwatchViewport();

        viewportDotNetRef = dotNetHelper;

        viewportHandler = function () {
            if (viewportTimer) {
                clearTimeout(viewportTimer);
            }

            viewportTimer = setTimeout(async function () {
                if (!viewportDotNetRef) return;

                try {
                    const useDesktopScanner = window.matchMedia(
                        "(min-width: 1280px) and (hover: hover) and (pointer: fine)"
                    ).matches;

                    await viewportDotNetRef.invokeMethodAsync(
                        "OnPosViewportChanged",
                        window.innerWidth,
                        window.innerHeight,
                        useDesktopScanner
                    );
                }
                catch (err) {
                    console.warn("POS viewport callback failed:", err);
                }
            }, 300);
        };

        window.addEventListener("resize", viewportHandler, { passive: true });
        window.addEventListener("orientationchange", viewportHandler, { passive: true });
    }

    function unwatchViewport() {
        if (viewportTimer) {
            clearTimeout(viewportTimer);
            viewportTimer = null;
        }

        if (viewportHandler) {
            window.removeEventListener("resize", viewportHandler);
            window.removeEventListener("orientationchange", viewportHandler);
            viewportHandler = null;
        }

        viewportDotNetRef = null;
    }

    return {
        start,
        cycleCamera,
        stop,
        restartAfterViewportChange,
        watchViewport,
        unwatchViewport
    };
})();