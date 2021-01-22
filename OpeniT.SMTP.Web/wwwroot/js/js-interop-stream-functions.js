(function () {
    window.JsIntropStreamFunctions = {
        init: init,
        destroy: destroy,
        readData: readData
    };

    var _jsRuntimeStreamInfoById = {};
    var _jsRuntimeStreamInfoNextId = 0;

    var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator.throw(value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments)).next());
        });
    };

    function init(funcName, args) {
        return __awaiter(this, void 0, Promise, function* () {
            args = [funcName, window].concat(args);
            var obj = executeFunctionByName.apply(this, args);
            var blob = new Blob(Object(obj));
            var result = {
                id: ++_jsRuntimeStreamInfoNextId,
                size: blob.size
            };
            _jsRuntimeStreamInfoById[result.id] = result;

            // Attach the blob data itself as a non-enumerable property so it doesn't appear in the JSON.
            Object.defineProperty(result, 'blob', { value: blob });

            return result;
        });
    };

    function destroy(jsRuntimeStreamInfoId) {
        delete _jsRuntimeStreamInfoById[jsRuntimeStreamInfoId];
    };

    function readData(jsRuntimeStreamInfoId, startOffset, count) {
        return __awaiter(this, void 0, Promise, function* () {
            var jsRuntimeStreamInfo = _jsRuntimeStreamInfoById[jsRuntimeStreamInfoId];

            var arrayBuffer = yield getArrayBuffer(jsRuntimeStreamInfo);
            return btoa(String.fromCharCode.apply(null, new Uint8Array(arrayBuffer, startOffset, count)));
        });
    };

    function getArrayBuffer(jsRuntimeStreamInfo) {
        if (!jsRuntimeStreamInfo) {
            throw new TypeError("jsRuntimeStreamInfo cannot be null");
        }

        if (!jsRuntimeStreamInfo.arrayBuffer) {
            jsRuntimeStreamInfo.arrayBuffer = jsRuntimeStreamInfo['blob'].arrayBuffer();
		}

        return jsRuntimeStreamInfo.arrayBuffer;
    };

    function executeFunctionByName(functionName, context) {
        var args = Array.prototype.slice.call(arguments, 2);
        var namespaces = functionName.split(".");
        var func = namespaces.pop();
        for (var i = 0; i < namespaces.length; i++) {
            context = context[namespaces[i]];
        }
        return context[func].apply(context, args);
    };
})();