(function () {
    window.GrapesjsFunctions = {
        init: function (editorElement) {
            // Set up GrapesJS editor with the Newsletter plugin
            var editor = grapesjs.init({
                storageManager: false,
                assetManager: {
                    upload: 0
                },
                container: editorElement,
                fromElement: true,
                colorPicker: { appendTo: 'parent', offset: { top: 26, left: -166, }, },
                plugins: ['gjs-preset-newsletter', 'grapesjs-custom-code'],
                pluginsOpts: {
                    'gjs-preset-newsletter': {
                        modalLabelImport: 'Paste all your code here below and click import',
                        modalLabelExport: 'Copy the code and use it wherever you want',
                        importPlaceholder: '<table class="table"><tr><td class="cell">Hello world!</td></tr></table>',
                        cellStyle: {
                            'font-size': '12px',
                            'font-weight': 300,
                            'vertical-align': 'top',
                            color: 'rgb(111, 119, 125)',
                            margin: 0,
                            padding: 0,
                        }
                    }
                }
            });

            // Let's add in this demo the possibility to test our newsletters
            var pnm = editor.Panels;

            pnm.addButton('options', [{
                id: 'undo',
                className: 'fa fa-undo',
                attributes: { title: 'Undo' },
                command: function () { editor.runCommand('core:undo') }
            }, {
                id: 'redo',
                className: 'fa fa-repeat',
                attributes: { title: 'Redo' },
                command: function () { editor.runCommand('core:redo') }
            }, {
                id: 'clear-all',
                className: 'fa fa-trash icon-blank',
                attributes: { title: 'Clear canvas' },
                command: {
                    run: function (editor, sender) {
                        sender && sender.set('active', false);
                        if (confirm('Are you sure to clean the canvas?')) {
                            editor.DomComponents.clear();
                        }
                    }
                }
            }, {
                id: 'preview',
                className: 'fa fa-eye',
                attributes: { title: 'Preview' },
                    command: function () { editor.runCommand('core:preview') }
            }]);

            // Hide component settings
            $(".gjs-pn-btn.fa.fa-cog").hide();

            // Beautify tooltips
            var titles = editorElement.querySelectorAll('*[title]');
            for (var i = 0; i < titles.length; i++) {
                var el = titles[i];
                el.setAttribute('data-toggle', "tooltip");
            }

            editorElement.grapesjsEditorInstance = editor;
        },
        setValue: function (editorElement, value) {
            editorElement.grapesjsEditorInstance.setComponents(value);
        },
        readValueLength: function (editorElement) {
            var value = editorElement.grapesjsEditorInstance.runCommand('gjs-get-inlined-html');
            return (new TextEncoder().encode(value)).length;
        },
        readValueData: function (startOffset, count, editorElement) {
            var str = editorElement.grapesjsEditorInstance.runCommand('gjs-get-inlined-html');

            var buf = new ArrayBuffer(count);
            var bufView = new Uint8Array(buf);
            for (var i = 0; i < count; i++) {
                bufView[i] = str.charCodeAt(startOffset + i);
            }

            return btoa(String.fromCharCode.apply(null, bufView));

        },
        enable: function (editorElement, mode) {
            //console.log(editorElement.grapesjsEditorInstance);
        },
        destroy: function (editorElement) {
            editorElement.grapesjsEditorInstance.destroy();
		}
    };
})();