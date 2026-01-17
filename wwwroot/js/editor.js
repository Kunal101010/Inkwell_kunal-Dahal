window.editorUtils = {
    insertMarkdown: function (elementId, prefix, suffix) {
        const textarea = document.getElementById(elementId);
        if (!textarea) return;

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);
        const beforeText = textarea.value.substring(0, start);
        const afterText = textarea.value.substring(end);

        let insertText;
        if (selectedText) {
            insertText = prefix + selectedText + suffix;
        } else {
            insertText = prefix + suffix;
        }

        textarea.value = beforeText + insertText + afterText;

        // Set cursor position
        const newCursorPos = start + prefix.length + (selectedText ? selectedText.length + suffix.length : 0);
        textarea.setSelectionRange(newCursorPos, newCursorPos);
        textarea.focus();

        // Trigger input event to update Blazor binding
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    },

    insertHeading: function (elementId, level) {
        const textarea = document.getElementById(elementId);
        if (!textarea) return;

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);
        const beforeText = textarea.value.substring(0, start);
        const afterText = textarea.value.substring(end);

        const prefix = '#'.repeat(level) + ' ';
        let insertText;
        if (selectedText) {
            insertText = prefix + selectedText;
        } else {
            insertText = prefix;
        }

        textarea.value = beforeText + insertText + afterText;

        const newCursorPos = start + insertText.length;
        textarea.setSelectionRange(newCursorPos, newCursorPos);
        textarea.focus();
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    },

    insertList: function (elementId, type) {
        const textarea = document.getElementById(elementId);
        if (!textarea) return;

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);
        const beforeText = textarea.value.substring(0, start);
        const afterText = textarea.value.substring(end);

        let prefix;
        if (type === 'bullet') {
            prefix = '- ';
        } else if (type === 'numbered') {
            prefix = '1. ';
        }

        let insertText;
        if (selectedText) {
            // Replace newlines with prefixed lines
            insertText = selectedText.split('\n').map((line, index) => {
                if (type === 'numbered') {
                    return (index + 1) + '. ' + line;
                } else {
                    return '- ' + line;
                }
            }).join('\n');
        } else {
            insertText = prefix;
        }

        textarea.value = beforeText + insertText + afterText;

        const newCursorPos = start + insertText.length;
        textarea.setSelectionRange(newCursorPos, newCursorPos);
        textarea.focus();
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    },

    insertLink: function (elementId) {
        const textarea = document.getElementById(elementId);
        if (!textarea) return;

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);
        const beforeText = textarea.value.substring(0, start);
        const afterText = textarea.value.substring(end);

        const linkText = selectedText || 'link text';
        const insertText = '[' + linkText + '](url)';

        textarea.value = beforeText + insertText + afterText;

        // Place cursor at 'url'
        const urlStart = start + '['.length + linkText.length + ']('.length;
        const urlEnd = urlStart + 3; // 'url'.length
        textarea.setSelectionRange(urlStart, urlEnd);
        textarea.focus();
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }
};