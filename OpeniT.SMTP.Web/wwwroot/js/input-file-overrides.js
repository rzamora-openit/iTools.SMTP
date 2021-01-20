"use strict";

(function () {
    window.BlazorFileFunctions = {
        init: init,
        toImageFile: toImageFile,
        ensureArrayBufferReadyForSharedMemoryInterop: ensureArrayBufferReadyForSharedMemoryInterop,
        readFileData: readFileData,
        fileToBlazorFile: fileToBlazorFile,
        getFileById: getFileById
    };

    // Reduce to purely serializable data, plus an index by ID.
    var _blazorFilesById = {};
    var _blazorInputFileNextFileId = 0;
    var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator.throw(value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments)).next());
        });
    };

    function init(callbackWrapper, elem) {
        elem.addEventListener('click', function () {
            // Permits replacing an existing file with a new one of the same file name.
            elem.value = '';
        });
        elem.addEventListener('change', function () {
            var fileList = Array.prototype.map.call(elem.files, function (file) {
                return fileToBlazorFile(file);
            });
            callbackWrapper.invokeMethodAsync('NotifyChange', fileList);
        });
    }

    function fileToBlazorFile(file) {
        var result = {
            id: ++_blazorInputFileNextFileId,
            lastModified: new Date(file.lastModified).toISOString(),
            name: file.name,
            size: file.size,
            contentType: file.type,
            readPromise: undefined,
            arrayBuffer: undefined
        };
        _blazorFilesById[result.id] = result;
        // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
        Object.defineProperty(result, 'blob', { value: file });
        return result;
	}

    function toImageFile(elem, fileId, format, maxWidth, maxHeight) {
        return __awaiter(this, void 0, Promise, function* () {
            var originalFile = getFileById(elem, fileId);
            var loadedImage = yield new Promise(function (resolve) {
                var originalFileImage = new Image();
                originalFileImage.onload = function () {
                    resolve(originalFileImage);
                };
                originalFileImage.src = URL.createObjectURL(originalFile['blob']);
            });
            var resizedImageBlob = yield new Promise(function (resolve) {
                var desiredWidthRatio = Math.min(1, maxWidth / loadedImage.width);
                var desiredHeightRatio = Math.min(1, maxHeight / loadedImage.height);
                var chosenSizeRatio = Math.min(desiredWidthRatio, desiredHeightRatio);
                var canvas = document.createElement('canvas');
                canvas.width = Math.round(loadedImage.width * chosenSizeRatio);
                canvas.height = Math.round(loadedImage.height * chosenSizeRatio);
                canvas.getContext('2d').drawImage(loadedImage, 0, 0, canvas.width, canvas.height);
                canvas.toBlob(resolve, format);
            });
            var result = {
                id: ++_blazorInputFileNextFileId,
                lastModified: originalFile.lastModified,
                name: originalFile.name,
                size: resizedImageBlob ? resizedImageBlob.size : 0,
                contentType: format,
                readPromise: undefined,
                arrayBuffer: undefined
            };
            _blazorFilesById[result.id] = result;
            // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
            Object.defineProperty(result, 'blob', { value: resizedImageBlob });
            return result;
        });
    }

    function ensureArrayBufferReadyForSharedMemoryInterop(elem, fileId) {
        return __awaiter(this, void 0, Promise, function* () {
            var arrayBuffer = yield getArrayBufferFromFileAsync(elem, fileId);
            getFileById(elem, fileId).arrayBuffer = arrayBuffer;
        });
    }

    function readFileData(elem, fileId, startOffset, count) {
        return __awaiter(this, void 0, Promise, function* () {
            var arrayBuffer = yield getArrayBufferFromFileAsync(elem, fileId);
            return btoa(String.fromCharCode.apply(null, new Uint8Array(arrayBuffer, startOffset, count)));
        });
    }

    function getFileById(elem, fileId) {
        var file = _blazorFilesById[fileId];
        if (!file) {
            throw new Error("There is no file with ID " + fileId + ". The file list may have changed.");
        }
        return file;
    }

    function getArrayBufferFromFileAsync(elem, fileId) {
        var file = getFileById(elem, fileId);
        // On the first read, convert the FileReader into a Promise<ArrayBuffer>.
        if (!file.readPromise) {
            file.readPromise = new Promise(function (resolve, reject) {
                var reader = new FileReader();
                reader.onload = function () {
                    resolve(reader.result);
                };
                reader.onerror = function (err) {
                    reject(err);
                };
                reader.readAsArrayBuffer(file['blob']);
            });
        }
        return file.readPromise;
    }
})();