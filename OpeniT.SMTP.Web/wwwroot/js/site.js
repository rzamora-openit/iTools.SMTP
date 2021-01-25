(function () {
    window.SiteFunctions = {
        init: function () {
            // Prevent closing dropdown-menu on click inside
            $(document).on("click", ".dropdown-menu.prevent-close-on-click-inside", function (e) {
                e.stopPropagation();
            });

            // Activate the Tooltips
            $(document).tooltip({
                selector: "[data-toggle='tooltip'], [rel='tooltip']",
                trigger: 'hover',
                animation: true
            });

            $(document).on("mouseenter mousedown", "[data-toggle='tooltip'], [rel='tooltip']", function () {
                var $this = $(this);

                if ($this.data("bs.tooltip")) {
                    var title = $this.attr("title");
                    if (title) {
                        $this.data("bs.tooltip")._fixTitle();
                    }

                    var tooltipTitle = $this.data("bs.tooltip").getTitle();
                    if (tooltipTitle === null || tooltipTitle.match(/^ *$/) !== null) {
                        $this.data("bs.tooltip").disable();
                        $this.data("bs.tooltip").hide();
                    } else {
                        $this.data("bs.tooltip").enable();
                        $this.data("bs.tooltip").show();
                    }
                } else {
                    $this.tooltip();
                    $this.data("bs.tooltip").show();
                }

                if ($this.hasClass("text-truncate")) {
                    if ($this[0].offsetWidth < $this[0].scrollWidth) {
                        $this.data("bs.tooltip").enable();
                        $this.data("bs.tooltip").show();
                    }
                    else {
                        $this.data("bs.tooltip").disable();
                        $this.data("bs.tooltip").hide();
                    }
                }
            });

            // Activate Popovers
            $(document).popover({
                html: true,
                selector: "[data-toggle='popover'], [rel='popover']",
                sanitize: false,
                content: function () {
                    var content = $(this).attr("data-popover-content");
                    return $(content + " .popover-body").html();
                },
                title: function () {
                    var title = $(this).attr("data-popover-content");
                    return $(title + " .popover-heading").html();
                }
            });

            $(document).on("mouseup", function (e) {
                var element = $(e.target);
                if (element.parents(".popover").length === 0) {
                    $(".popover").each(function () {
                        $(this).popover("hide");
                    });
                }
            });

            $(document).on("DOMNodeRemoved", function (e) {
                var $this = $(e.target);

                if ($this.attr("data-toggle") === "tooltip" || $this.attr("rel") === "tooltip") {
                    $this.tooltip("dispose");
                }

                if ($this.attr("data-toggle") === "popover" || $this.attr("rel") === "popover") {
                    $this.popover("dispose");
                }

                var tooltipAnchors = $this.find("[data-toggle='tooltip'], [rel='tooltip']");
                if (tooltipAnchors.length > 0) {
                    tooltipAnchors.tooltip("dispose");
                }

                var popoverAnchors = $this.find("[data-toggle='popover'], [rel='popover']");
                if (popoverAnchors.length > 0) {
                    popoverAnchors.popover("dispose");
                }
            });
        },
        updateMatBlazorTheme: function (matBlazorStyle) {
            $("head > #matBlazorStyleTag").remove();
            $("head").append("<style type='text/css' id='matBlazorStyleTag'> :root { " + matBlazorStyle + " } </style>");
        },
        createOverlayScrollbars: function (element, className) {
            if (className == null) {
                className = "os-theme-light";
            }

            OverlayScrollbars(element, { className: className, overflowBehavior: { x: "h" }, scrollbars: { visibility: "v" } });
        },
        destroyOverlayScrollbars: function (element) {
            var scrollBarInstance = OverlayScrollbars(element, {});
            scrollBarInstance.destroy();
        },
        getElementWidth: function (element) {
            return element.offsetWidth;
        },
        getElementHeight: function (element) {
            return element.offsetHeight;
        },
        requestFullscreen: function (element) {
            if (element.requestFullscreen) {
                element.requestFullscreen();
            } else if (element.webkitRequestFullscreen) {
                element.webkitRequestFullscreen();
            } else if (element.mozRequestFullScreen) {
                element.mozRequestFullScreen();
            } else if (element.msRequestFullscreen) {
                element.msRequestFullscreen();
            } else {
                console.warn("Fullscreen API is not supported.");
            }
        },
        exitFullscreen: function () {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (document.webkitExitFullscreen) {
                document.webkitExitFullscreen();
            } else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            } else if (document.msExitFullscreen) {
                document.msExitFullscreen();
            } else {
                console.warn("Fullscreen API is not supported.");
            }
        },
        getInnerHTML: function (element) {
            return element.innerHTML;
        },
        loadPDFViewer: function (base64, fileName, hideControls) {
            const blob = window.SiteFunctions.base64toBlob(base64, "application/pdf");
            var url = URL.createObjectURL(blob);

            if (fileName) {
                url = {
                    url,
                    originalUrl: fileName
                };
            }

            if (window.PDFViewerApplication) {
                window.PDFViewerApplication.run();
                window.PDFViewerApplication.cleanup();
                window.PDFViewerApplication.open(url).then(onAfterOpen);

                function onAfterOpen() {
                    window.PDFViewerApplication.eventBus.on("resize", function (e) {
                        if (hideControls) {
                            document.getElementById("outerContainer").style.height = (document.getElementById("viewer").scrollHeight + 10) + "px";
                        }
                    });
                    window.PDFViewerApplication.eventBus.on("pagesloaded", function (e) {
                        window.PDFViewerApplication.zoomReset();

                        if (hideControls) {
                            document.getElementById("outerContainer").style.height = (document.getElementById("viewer").scrollHeight + 10) + "px";

                            if (window.PDFViewerApplication.pdfSidebar) {
                                window.PDFViewerApplication.pdfSidebar.close();
                            }
                        }
                        else {
                            if (window.PDFViewerApplication.pdfSidebar) {
                                window.PDFViewerApplication.pdfSidebar.open();
                            }
                        }

                        window.PDFViewerApplication.contentDispositionFilename = fileName;
                        var appConfig = window.PDFViewerApplication.appConfig;
                        appConfig.toolbar.download.removeAttribute("hidden");
                        appConfig.secondaryToolbar.downloadButton.removeAttribute("hidden");
                    });

                    var appConfig = window.PDFViewerApplication.appConfig;
                    appConfig.toolbar.viewBookmark.setAttribute("hidden", "true");
                    appConfig.secondaryToolbar.viewBookmarkButton.setAttribute("hidden", "true");
                    appConfig.toolbar.print.setAttribute("hidden", "true");
                    appConfig.secondaryToolbar.printButton.setAttribute("hidden", "true");
                    appConfig.toolbar.openFile.setAttribute("hidden", "true");
                    appConfig.secondaryToolbar.openFileButton.setAttribute("hidden", "true");
                    appConfig.toolbar.download.setAttribute("hidden", "true");
                    appConfig.secondaryToolbar.downloadButton.setAttribute("hidden", "true");
                }
            }
        },
        loadPDFViewerCanvas: function (canvas, base64) {
            if (window.SiteFunctions.isElement(canvas) && base64 != null) {
                var pdfData = atob(base64);
                pdfjsLib.GlobalWorkerOptions.workerSrc = "/lib/pdf.js/pdf.worker.js";
                var loadingTask = pdfjsLib.getDocument({ data: pdfData });
                loadingTask.promise.then(function (pdf) {
                    var pageNumber = 1;
                    pdf.getPage(pageNumber).then(function (page) {
                        var unscaledViewport = page.getViewport({ scale: 1 });
                        var scale = canvas.width / unscaledViewport.width;
                        var viewport = page.getViewport({ scale: scale });

                        var context = canvas.getContext("2d");

                        var renderContext = {
                            canvasContext: context,
                            viewport: viewport
                        };
                        page.render(renderContext);
                    });
                }, function (reason) {
                    console.error(reason);
                });
			}
        },
        base64toBlob: function (b64Data, contentType = "", sliceSize = 512) {
            const byteCharacters = atob(b64Data);
            const byteArrays = [];

            for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
                const slice = byteCharacters.slice(offset, offset + sliceSize);

                const byteNumbers = new Array(slice.length);
                for (let i = 0; i < slice.length; i++) {
                    byteNumbers[i] = slice.charCodeAt(i);
                }

                const byteArray = new Uint8Array(byteNumbers);
                byteArrays.push(byteArray);
            }

            const blob = new Blob(byteArrays, { type: contentType });
            return blob;
        },
        initDropdown: function (jsHelper, menuElement, anchorElement, anchorCorner, flipCornerHorizontally) {
            var isAnchorCornerInitialized = false;
            var previousClassList = [].slice.call(menuElement);

            window.setInterval(function () {
                var classList = [].slice.call(menuElement.classList);
                if ((previousClassList.indexOf("mdc-menu-surface--open") < 0 && classList.indexOf("mdc-menu-surface--open") > -1) ||
                    (previousClassList.indexOf("mdc-menu-surface--open") > -1 && classList.indexOf("mdc-menu-surface--open") < 0)) {
                    var isOpen = menuElement.classList.contains("mdc-menu-surface--open");
                    previousClassList = classList;

                    jsHelper.invokeMethodAsync("NotifyIsOpenChanged", isOpen);
                }

                if (!isAnchorCornerInitialized && menuElement.matBlazorRef) {
                    isAnchorCornerInitialized = true;

                    menuElement.matBlazorRef.menuSurface_.setAnchorCorner(anchorCorner);
                    if (flipCornerHorizontally) {
                        menuElement.matBlazorRef.menuSurface_.foundation.flipCornerHorizontally();
					}
				}
            }, 10);
        },
        initMenu: function (jsHelper, menuElement, anchorPosition, flipCornerHorizontally) {
            var isAnchorCornerInitialized = false;
            var previousClassList = [].slice.call(menuElement);

            window.setInterval(function () {
                var classList = [].slice.call(menuElement.classList);
                if ((previousClassList.indexOf("mdc-menu-surface--open") < 0 && classList.indexOf("mdc-menu-surface--open") > -1) ||
                    (previousClassList.indexOf("mdc-menu-surface--open") > -1 && classList.indexOf("mdc-menu-surface--open") < 0)) {
                    var isOpen = menuElement.classList.contains("mdc-menu-surface--open");
                    previousClassList = classList;

                    jsHelper.invokeMethodAsync("NotifyIsOpenChanged", isOpen);
                }

                if (!isAnchorCornerInitialized && menuElement.matBlazorRef) {
                    isAnchorCornerInitialized = true;

                    menuElement.matBlazorRef.menuSurface_.setAnchorCorner(anchorPosition);
                    if (flipCornerHorizontally) {
                        menuElement.matBlazorRef.menuSurface_.foundation.flipCornerHorizontally();
                    }
                }
            }, 10);
        },
        getParentWithClass: function (el, cls) {
            while ((el = el.parentElement) && !el.classList.contains(cls));
            return el;
        },
        scrollIntoView: function (element) {
            element.scrollIntoView();
        },
        scrollToHighest: function (elements) {
            var highestElement = $(elements[0]);
            for (var i = 1; i < elements.length; i++) {
                var element = $(elements[i]);

                if (element.offset() && element.offset().top < highestElement.offset().top) {
                    highestElement = element;
                }
            }

            if (highestElement && highestElement.is(":visible")) {
                var scrollParent = $(window.SiteFunctions.getScrollParent(highestElement[0], false));
                var scrollOffset = scrollParent.offset().top + 150;
                var scrollTop = highestElement.offset().top - scrollOffset;

                scrollParent.animate({
                    scrollTop: scrollTop
                });
			}
        },
        getScrollParent: function (element, includeHidden) {
            var style = getComputedStyle(element);
            var excludeStaticParent = style.position === "absolute";
            var overflowRegex = includeHidden ? /(auto|scroll|hidden)/ : /(auto|scroll)/;

            if (style.position === "fixed") return document.documentElement;
            for (var parent = element; (parent = parent.parentElement);) {
                style = getComputedStyle(parent);
                if (excludeStaticParent && style.position === "static") {
                    continue;
                }

                if (overflowRegex.test(style.overflow + style.overflowY + style.overflowX)) return parent;
            }

            return document.documentElement;
        },
        openFileSelector: function (inputContainerElement) {
            $(inputContainerElement).find("input[type='file']").trigger('click');
        },
        initDropzone: function (dropzoneElement, targetFileInputElement, accepts, multipleFile) {
            //dropzoneElement.addEventListener("click", function (e) {
            //    window.SiteFunctions.openFileSelector(targetFileInputElement);
            //});

            //var dropzone = new Dropzone(dropzoneElement, {
            //    url: "/",
            //    autoProcessQueue: false,
            //    previewElement: null,
            //    maxFiles: multipleFile ? null : 1,
            //    acceptedFiles: accepts
            //});
            //dropzone.on("addedfile", function (file) {
            //    file.previewElement.innerHTML = "";
            //});
            //dropzone.on("dragenter", function (e) {
            //    dropzoneElement.classList.add("bg-mdc-gray");
            //});
            //dropzone.on("dragleave", function (e) {
            //    dropzoneElement.classList.remove("bg-mdc-gray");
            //});
            //dropzone.on("drop", function (e) {
            //    dropzoneElement.classList.remove("bg-mdc-gray");

            //    targetFileInputElement.files = e.dataTransfer.files;

            //    var event;
            //    if (typeof (Event) === "function") {
            //        event = new UIEvent("change", { bubbles: true });
            //    } else {
            //        event = document.createEvent("UIEvents");
            //        event.initUIEvent("change", true, true);    
            //    }

            //    targetFileInputElement.dispatchEvent(event);
            //});
		},
        initFileDragAndDrop: function (parentElement, inputElement) {
            if (parentElement && inputElement) {
                inputElement.addEventListener("dragenter", dragEnter, false);
                inputElement.addEventListener("dragleave", dragLeave, false);
                inputElement.addEventListener("drop", drop, false);

                inputElement.onclick = function () {
                    this.value = null;
                };

                function dragEnter(e) {
                    parentElement.classList.add("mdc-ripple-upgraded--background-focused");
                }

                function dragLeave(e) {
                    parentElement.classList.remove("mdc-ripple-upgraded--background-focused");
                }

                function drop(e) {
                    parentElement.classList.remove("mdc-ripple-upgraded--background-focused");

                    var isValid = true;
                    var files = e.dataTransfer.files;
                    var promises = [];
                    for (var i = 0; i < files.length; i++) {
                        var accept = inputElement.getAttribute("accept");
                        var file = files[i];

                        var isFile = window.SiteFunctions.isFile(file);
                        promises.push(isFile);

                        if (file == null || (accept && !file.name.includes(accept) && !file.type.includes(accept))) {
                            isValid = false;
                            break;
                        }
                    }

                    Promise.all(promises).then(function (values){
                        if (values.includes(false)) {
                            isValid = false;
                        }
                    });

                    if (!isValid) {
                        e.dataTransfer.clearData();
                        e.preventDefault();
                        e.stopPropagation();
                    }
                }
			}
        },
        isFile: function (maybeFile) {
            return new Promise(function (resolve, reject) {
                if (maybeFile.type !== "") {
                    return resolve(true);
                }
                const reader = new FileReader();
                reader.onloadend = function () {
                    if (reader.error && reader.error.name === "NotFoundError") {
                        return resolve(false);
                    }
                    resolve(maybeFile);
                }
                reader.readAsBinaryString(maybeFile);
            });
        },
        debounce: function(func, wait, immediate) {
            var timeout;
            return function () {
                var context = this, args = arguments;
                var later = function () {
                    timeout = null;
                    if (!immediate) func.apply(context, args);
                };
                var callNow = immediate && !timeout;
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
                if (callNow) func.apply(context, args);
            };
        },
        isBase64: function(str) {
            if (str === "" || str.trim() === "") { return false; }
            try {
                return btoa(atob(str)) == str;
            } catch (err) {
                return false;
            }
        },
        hoistToBody: function (element) {
            document.body.appendChild(element);
        },
        getUrlParam: function (paramName) {
            var urlParams = new URLSearchParams(window.location.search);
            return urlParams.get(paramName);
        },
        isElement: function (o){
            return (
                typeof HTMLElement === "object" ? o instanceof HTMLElement : //DOM2
                    o && typeof o === "object" && o !== null && o.nodeType === 1 && typeof o.nodeName === "string"
            );
        },
        initResizableTable: function (table) {
            $(table).colResizable({ liveDrag: true, minWidth: 100 });
        },
        openUrl: function (url, windowName, windowFeatures) {
            window.open(url, windowName, windowFeatures);
        },
        replaceState: function (stateObj, title, url) {
            window.history.replaceState(stateObj, title, url)
        }
    };
})();