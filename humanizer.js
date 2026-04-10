// humanizer.js

/**
 * Function to humanize text by inserting appropriate spaces, punctuation, and converting to a more natural language format.
 * @param {string} text - The input text to be humanized.
 * @return {string} - The humanized string.
 */
function humanizeText(text) {
    // Replace underscores with spaces
    text = text.replace(/_/g, ' ');
    // Capitalize the first letter of each sentence
    text = text.replace(/(?:^|[.?!]\s*)([a-z])/g, function (match) {
        return match.toUpperCase();
    });
    // Add more humanizing logic as needed
    return text;
}

// Example usage:
const input = "this_is_a_test_string.";
const output = humanizeText(input);
console.log(output); // This is a test string.
