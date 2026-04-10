// background.js

chrome.runtime.onInstalled.addListener(function() {
    chrome.contextMenus.create({
        id: 'humanizeText',
        title: 'Humanize Text',
        contexts: ['selection']
    });
});

chrome.contextMenus.onClicked.addListener(function(info, tab) {
    if (info.menuItemId === 'humanizeText') {
        // Your text humanization logic here
        const selectedText = info.selectionText;
        const humanizedText = humanize(selectedText);
        // You could send this to a content script or display it somehow
        console.log(humanizedText);
    }
});

function humanize(text) {
    // Implement your text humanization logic here
    return text.replace(/\b(\w+)/g, function(match) {
        return match.charAt(0).toUpperCase() + match.slice(1);
    });
}